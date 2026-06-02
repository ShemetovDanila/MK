using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Интерпретатор стековой машины.
/// Выполняет программу, представленную в виде Обратной Польской Записи (ОПС).
/// </summary>
public class Interpreter
{
    // Хранилище простых переменных: имя -> числовое значение
    private Dictionary<string, double> variables = new Dictionary<string, double>();
    
    // Хранилище массивов: имя_массива -> (индекс -> числовое значение)
    private Dictionary<string, Dictionary<int, double>> arrays = new Dictionary<string, Dictionary<int, double>>();
    
    // Рабочий стек для хранения операндов (чисел, имен переменных или строковых литералов)
    private Stack<string> stack = new Stack<string>();

    /// <summary>
    /// Извлекает числовое значение из токена. 
    /// Если в стеке имя переменной — ищет в словаре, если число — парсит его.
    /// </summary>
    private double GetValue(string token)
    {
        if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out double num))
            return num;

        // Обработка доступа к элементу массива вида "имя[индекс]"
        if (token.Contains("[") && token.Contains("]"))
        {
            string arrName = token.Substring(0, token.IndexOf('['));
            string idxStr = token.Substring(token.IndexOf('[') + 1, token.IndexOf(']') - token.IndexOf('[') - 1);
            int idx = (int)double.Parse(idxStr, CultureInfo.InvariantCulture);
            return (arrays.ContainsKey(arrName) && arrays[arrName].ContainsKey(idx)) ? arrays[arrName][idx] : 0.0;
        }

        if (variables.ContainsKey(token)) return variables[token];
        return 0.0;
    }

    /// <summary>
    /// Удаляет лишнюю дробную часть при выводе (например, 5.0 выведет как 5).
    /// </summary>
    private string FormatNumber(double val)
    {
        if (val == Math.Floor(val) && !double.IsInfinity(val)) return ((long)val).ToString();
        return val.ToString("G", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Основной цикл исполнения команд ОПС.
    /// </summary>
    public void Execute(List<string> rpn)
    {
        Console.WriteLine("[Interpreter]:");
        int pc = 0; // Program Counter — указатель текущей команды
        stack.Clear();

        while (pc < rpn.Count)
        {
            string command = rpn[pc];

            if (command == "bp") { pc++; continue; }
            if (command == "ret") break;

            // jf: Переход по адресу из стека, если условие на вершине стека ложно (0)
            if (command == "jf")
            {
                int target = (int)GetValue(stack.Pop());
                double cond = GetValue(stack.Pop());
                pc = (cond == 0.0) ? target : pc + 1;
                continue;
            }

            // УП: Безусловный переход по адресу из стека
            if (command == "УП")
            {
                pc = (int)GetValue(stack.Pop());
                continue;
            }

            // index: Формирование ключа массива (например, из "a" и "0" делает "a[0]")
            if (command == "index")
            {
                double idxVal = GetValue(stack.Pop());
                string arrName = stack.Pop();
                stack.Push($"{arrName}[{(int)idxVal}]");
                pc++; continue;
            }

            // =: Присваивание значения (из стека) переменной (имя из стека)
            if (command == "=")
            {
                double val = GetValue(stack.Pop());
                string varName = stack.Pop();
                if (varName.Contains("[")) AssignToArray(varName, val);
                else variables[varName] = val;
                pc++; continue;
            }

            // r: Чтение числа из консоли
            if (command == "r")
            {
                if (stack.Count > 0)
                {
                    string varName = stack.Pop();
                    string input = Console.ReadLine() ?? "";
                    if (double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out double res))
                    {
                        if (varName.Contains("[")) AssignToArray(varName, res);
                        else variables[varName] = res;
                    }
                }
                pc++; continue;
            }

            // w: ВАЖНО — Вывод на экран. Поддерживает как вычисляемые выражения, так и текст в кавычках.
            if (command == "w")
            {
                string valStr = stack.Pop();
                // Если это строка (начинается и кончается кавычкой), выводим её содержимое без кавычек
                if (valStr.StartsWith("\"") && valStr.EndsWith("\""))
                {
                    Console.WriteLine(valStr.Substring(1, valStr.Length - 2));
                }
                else
                {
                    // Иначе считаем это математическим выражением
                    Console.WriteLine(FormatNumber(GetValue(valStr)));
                }
                pc++; continue;
            }

            // neg: Унарный минус
            if (command == "neg")
            {
                double val = GetValue(stack.Pop());
                stack.Push((-val).ToString(CultureInfo.InvariantCulture));
                pc++; continue;
            }

            // Математика и Логика
            if (command == "+" || command == "-" || command == "*" || command == "/" ||
                command == ">" || command == "<" || command == "==" || command == "!=" ||
                command == "<=" || command == ">=")
            {
                double b = GetValue(stack.Pop());
                double a = GetValue(stack.Pop());
                double res = 0;
                switch (command)
                {
                    case "+": res = a + b; break;
                    case "-": res = a - b; break;
                    case "*": res = a * b; break;
                    case "/": res = (b != 0) ? a / b : 0; break;
                    case ">": res = (a > b) ? 1.0 : 0.0; break;
                    case "<": res = (a < b) ? 1.0 : 0.0; break;
                    case "==": res = (a == b) ? 1.0 : 0.0; break;
                    case "!=": res = (a != b) ? 1.0 : 0.0; break;
                    case "<=": res = (a <= b) ? 1.0 : 0.0; break;
                    case ">=": res = (a >= b) ? 1.0 : 0.0; break;
                }
                stack.Push(res.ToString(CultureInfo.InvariantCulture));
                pc++; continue;
            }

            // Все операнды (числа, имена, строки) просто кладутся на стек
            stack.Push(command);
            pc++;
        }
    }

    private void AssignToArray(string token, double value)
    {
        string arrName = token.Substring(0, token.IndexOf('['));
        string idxStr = token.Substring(token.IndexOf('[') + 1, token.IndexOf(']') - token.IndexOf('[') - 1);
        int idx = (int)double.Parse(idxStr, CultureInfo.InvariantCulture);
        if (!arrays.ContainsKey(arrName)) arrays[arrName] = new Dictionary<int, double>();
        arrays[arrName][idx] = value;
    }
}