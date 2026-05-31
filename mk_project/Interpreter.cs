using System;
using System.Collections.Generic;
using System.Globalization;

public class Interpreter
{
    // Оперативная память для простых переменных: имя → значение
    private Dictionary<string, double> variables = new Dictionary<string, double>();

    // Память для массивов: имя массива → (индекс → значение)
    private Dictionary<string, Dictionary<int, double>> arrays =
        new Dictionary<string, Dictionary<int, double>>();

    // Строковый стек операндов виртуальной машины
    private Stack<string> stack = new Stack<string>();

    // ── Разыменование токена в числовое значение ──────────────────────────
    // Поддерживает: числовые константы, ячейки массива "arr[i]", переменные.
    private double GetValue(string token)
    {
        // Числовая константа (включая результаты вычислений с точкой)
        if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out double num))
            return num;

        // Ячейка массива вида "arr[3]"
        if (token.Contains("[") && token.Contains("]"))
        {
            string arrName = token.Substring(0, token.IndexOf('['));
            int idx = int.Parse(token.Substring(
                token.IndexOf('[') + 1,
                token.IndexOf(']') - token.IndexOf('[') - 1));

            if (arrays.ContainsKey(arrName) && arrays[arrName].ContainsKey(idx))
                return arrays[arrName][idx];
            return 0.0; // неинициализированная ячейка = 0
        }

        // Обычная переменная
        if (variables.ContainsKey(token))
            return variables[token];

        return 0.0;
    }

    // ── Форматирование числа для вывода ───────────────────────────────────
    // Использует инвариантную культуру, чтобы '.' всегда было десятичным разделителем.
    private string FormatNumber(double val)
    {
        // Если значение целое — выводим без дробной части
        if (val == Math.Floor(val) && !double.IsInfinity(val))
            return ((long)val).ToString();
        return val.ToString("G", CultureInfo.InvariantCulture);
    }

    // ── Основной цикл исполнения ОПС ──────────────────────────────────────
    public void Execute(List<string> rpn)
    {
        Console.WriteLine("[Interpreter]: Исполнение ОПС начато...");
        int pc = 0;

        while (pc < rpn.Count)
        {
            string command = rpn[pc];

            // ── bp: начало блока (no-op) ─────────────────────────────────
            if (command == "bp") { pc++; continue; }

            // ── ret: завершение программы ────────────────────────────────
            if (command == "ret")
            {
                Console.WriteLine("[Interpreter]: Программа завершила работу (команда ret).");
                break;
            }

            // ── r: ввод (read) ───────────────────────────────────────────
            if (command == "r")
            {
                if (stack.Count > 0)
                {
                    string varName = stack.Pop();
                    Console.Write($"Введите значение для {varName}: ");
                    string? input = Console.ReadLine();

                    if (double.TryParse(input, NumberStyles.Any,
                                        CultureInfo.InvariantCulture, out double val))
                    {
                        if (varName.Contains("["))
                            AssignToArray(varName, val);
                        else
                            variables[varName] = val;
                    }
                    else
                    {
                        Console.WriteLine($"[Предупреждение]: Не удалось прочитать число для '{varName}', присвоено 0.");
                        if (varName.Contains("["))
                            AssignToArray(varName, 0.0);
                        else
                            variables[varName] = 0.0;
                    }
                }
                pc++;
                continue;
            }

            // ── w: вывод (write) ─────────────────────────────────────────
            if (command == "w")
            {
                if (stack.Count > 0)
                {
                    double val = GetValue(stack.Pop());
                    Console.WriteLine($"Вывод: {FormatNumber(val)}");
                }
                pc++;
                continue;
            }

            // ── jf: условный переход по лжи ─────────────────────────────
            if (command == "jf")
            {
                int targetAddress = (int)GetValue(stack.Pop());
                double condition  = GetValue(stack.Pop());

                pc = (condition == 0.0) ? targetAddress : pc + 1;
                continue;
            }

            // ── УП: безусловный переход ───────────────────────────────────
            if (command == "УП")
            {
                int targetAddress = (int)GetValue(stack.Pop());
                pc = targetAddress;
                continue;
            }

            // ── index: формирование строкового адреса ячейки массива ─────
            if (command == "index")
            {
                int idx        = (int)GetValue(stack.Pop()); // вычисленный индекс
                string arrName = stack.Pop();                 // имя массива
                stack.Push($"{arrName}[{idx}]");
                pc++;
                continue;
            }

            // ── =: присваивание ──────────────────────────────────────────
            if (command == "=")
            {
                if (stack.Count >= 2)
                {
                    double val     = GetValue(stack.Pop()); // значение
                    string varName = stack.Pop();            // целевая переменная / ячейка

                    if (varName.Contains("["))
                        AssignToArray(varName, val);
                    else
                        variables[varName] = val;
                }
                pc++;
                continue;
            }

            // ИСПРАВЛЕНИЕ #7: neg — унарный минус ─────────────────────────
            if (command == "neg")
            {
                double operand = GetValue(stack.Pop());
                stack.Push((-operand).ToString("G", CultureInfo.InvariantCulture));
                pc++;
                continue;
            }

            // ── Бинарные операции: арифметика и сравнение ────────────────
            if (command == "+" || command == "-" || command == "*" || command == "/" ||
                command == ">"  || command == "<"  || command == "==" ||
                command == "!=" || command == "<=" || command == ">=")
            {
                if (stack.Count < 2)
                {
                    Console.WriteLine($"[Ошибка интерпретатора]: Недостаточно операндов для '{command}'.");
                    break;
                }

                double b      = GetValue(stack.Pop());
                double a      = GetValue(stack.Pop());
                double result = 0.0;

                switch (command)
                {
                    case "+":  result = a + b; break;
                    case "-":  result = a - b; break;
                    case "*":  result = a * b; break;
                    // ИСПРАВЛЕНИЕ #8: защита от деления на ноль
                    case "/":
                        if (b == 0.0)
                        {
                            Console.WriteLine("[Ошибка времени выполнения]: Деление на ноль.");
                            return;
                        }
                        result = a / b;
                        break;
                    case ">":  result = a >  b ? 1.0 : 0.0; break;
                    case "<":  result = a <  b ? 1.0 : 0.0; break;
                    case "==": result = a == b ? 1.0 : 0.0; break;
                    case "!=": result = a != b ? 1.0 : 0.0; break;
                    case "<=": result = a <= b ? 1.0 : 0.0; break;
                    case ">=": result = a >= b ? 1.0 : 0.0; break;
                }

                stack.Push(result.ToString("G", CultureInfo.InvariantCulture));
                pc++;
                continue;
            }

            // ── Всё остальное — операнд, кладём на стек ──────────────────
            stack.Push(command);
            pc++;
        }
    }

    // ── Запись значения в ячейку массива ─────────────────────────────────
    private void AssignToArray(string token, double value)
    {
        string arrName = token.Substring(0, token.IndexOf('['));
        int idx = int.Parse(token.Substring(
            token.IndexOf('[') + 1,
            token.IndexOf(']') - token.IndexOf('[') - 1));

        if (!arrays.ContainsKey(arrName))
            arrays[arrName] = new Dictionary<int, double>();

        arrays[arrName][idx] = value;
    }
}