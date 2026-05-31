using System;
using System.Collections.Generic;

public class Interpreter
{
    // Оперативная память виртуальной машины
    private Dictionary<string, double> variables = new Dictionary<string, double>();
    
    // Единый вычислительный стек для данных и адресов
    private Stack<string> stack = new Stack<string>();

    // Разыменование операнда: преобразует имя переменной или строку в double
    private double GetValue(string token)
    {
        if (double.TryParse(token, out double num))
        {
            return num;
        }
        if (variables.ContainsKey(token))
        {
            return variables[token];
        }
        return 0; // Неинициализированные переменные по умолчанию равны 0
    }

    public void Execute(List<string> rpn)
    {
        Console.WriteLine("[Interpreter]: Исполнение ОПС начато...");
        int pc = 0; // Указатель командной строки

        while (pc < rpn.Count)
        {
            string command = rpn[pc];

            if (command == "bp") { pc++; continue; }

            if (command == "ret")
            {
                Console.WriteLine("[Interpreter]: Программа завершила работу (команда ret).");
                break;
            }

            // Операция ввода ввода 'r' (read)
            if (command == "r")
            {
                if (stack.Count > 0)
                {
                    string varName = stack.Pop(); // Сверху стека лежит чистое имя переменной
                    Console.Write($"Введите значение для {varName}: ");
                    string? input = Console.ReadLine();
                    
                    if (double.TryParse(input, out double val))
                    {
                        variables[varName] = val;
                    }
                    else
                    {
                        Console.WriteLine("[Ошибка выполнения]: Неверный формат числового ввода.");
                        return;
                    }
                }
                pc++;
                continue;
            }

            // Операция вывода 'w' (write)
            if (command == "w")
            {
                if (stack.Count > 0)
                {
                    double val = GetValue(stack.Pop()); // Разыменовываем значение перед выводом
                    Console.WriteLine($"Вывод: {val}");
                }
                else
                {
                    Console.WriteLine("[Ошибка выполнения]: Стек пуст. Нечего выводить.");
                    return;
                }
                pc++;
                continue;
            }

            // Условный переход Jump if False (jf)
            if (command == "jf")
            {
                // Постфиксный вид: [условие] [адрес] jf
                int targetAddress = (int)GetValue(stack.Pop()); // Вершина стека — адрес перехода
                double condition = GetValue(stack.Pop());      // Под ней — результат логического условия

                if (condition == 0)
                {
                    pc = targetAddress; // Переходим, если ложь
                    continue;
                }
                else
                {
                    pc++; // Идем дальше, если истина
                    continue;
                }
            }

            // Безусловный переход (УП)
            if (command == "УП")
            {
                int targetAddress = (int)GetValue(stack.Pop());
                pc = targetAddress;
                continue;
            }

            // Операция присваивания '='
            if (command == "=")
            {
                if (stack.Count >= 2)
                {
                    // Постфиксный вид: a [выражение] =
                    double val = GetValue(stack.Pop()); // Результат выражения сверху стека
                    string varName = stack.Pop();       // Имя переменной под ним

                    if (double.TryParse(varName, out _))
                    {
                        Console.WriteLine("[Ошибка выполнения]: Левая часть присваивания должна быть переменной.");
                        return;
                    }

                    variables[varName] = val;
                }
                pc++;
                continue;
            }

            // Математические и логические операции
            if (command == "+" || command == "-" || command == "*" || command == "/" || 
                command == ">" || command == "<" || command == "==" || command == "!=" || command == "<=" || command == ">=")
            {
                if (stack.Count < 2)
                {
                    Console.WriteLine($"[Ошибка выполнения]: Недостаточно операндов для операции '{command}'");
                    return;
                }

                double b = GetValue(stack.Pop()); // Второй операнд (сверху)
                double a = GetValue(stack.Pop()); // Первый операнд (снизу)
                double result = 0;

                switch (command)
                {
                    case "+": result = a + b; break;
                    case "-": result = a - b; break;
                    case "*": result = a * b; break;
                    case "/": 
                        if (b == 0) { Console.WriteLine("[Ошибка выполнения]: Деление на ноль."); return; }
                        result = a / b; 
                        break;
                    case ">":  result = a > b ? 1 : 0; break;
                    case "<":  result = a < b ? 1 : 0; break;
                    case "==": result = a == b ? 1 : 0; break;
                    case "!=": result = a != b ? 1 : 0; break;
                    case "<=": result = a <= b ? 1 : 0; break;
                    case ">=": result = a >= b ? 1 : 0; break;
                }

                stack.Push(result.ToString()); // Результат возвращается на стек как строковое число
                pc++;
                continue;
            }

            // Если элемент — операнд (число или имя переменной), просто пушим его на стек
            if (command != "jf" && command != "ret" && command != "bp" && command != "УП" && command != "w" && command != "r" && command != "=")
            {
                stack.Push(command);
            }

            pc++;
        }

        Console.WriteLine("[Interpreter]: Программа успешно выполнена.");
    }
}