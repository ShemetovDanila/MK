using System;
using System.Collections.Generic;

public class Interpreter
{
    private Dictionary<string, double> variables = new Dictionary<string, double>();
    private Dictionary<string, string> variableTypes = new Dictionary<string, string>();
    private Stack<double> stack = new Stack<double>();

    public void Execute(List<string> rpn)
    {
        Console.WriteLine("[Interpreter]: Исполнение ОПС начато...");
        
        int pc = 0; // Счетчик команд (Program Counter)

        while (pc < rpn.Count)
        {
            string command = rpn[pc];

            // 1. Обработка команд объявления типов
            if (command == "INT" || command == "INT1")
            {
                // Предполагаем, что перед типом или после него идет имя переменной (в зависимости от вашей семантики)
                // Если ОПС содержит "1 bp a", мы можем обрабатывать объявления динамически при присваивании,
                // либо фиксировать типы здесь. Для простоты пропустим маркеры типов, если они служебные.
                pc++;
                continue;
            }

            // 2. Служебные маркеры начала/конца программы
            if (command == "bp")
            {
                pc++;
                continue;
            }
            if (command == "ret")
            {
                Console.WriteLine("[Interpreter]: Программа завершила работу (команда ret).");
                break;
            }

            // 3. Операторы ввода и вывода
            if (command == "r" || command == "READ")
            {
                // Оператор ввода: имя переменной берется из ОПС или стека. 
                // В вашей ОПС: "a r", значит имя переменной перед "r". Но в стековом исполнителе 
                // имена переменных часто хранятся как строки, либо "r" знает, что операнд был до этого.
                // Если переменная лежит в ОПС перед 'r', мы берем её из rpn[pc-1] или выталкиваем имя.
                string varName = rpn[pc - 1]; 
                Console.Write($"Введите значение для {varName}: ");
                if (double.TryParse(Console.ReadLine(), out double val))
                {
                    variables[varName] = val;
                }
                else
                {
                    Console.WriteLine("[Ошибка выполнения]: Неверный формат числа.");
                    return;
                }
                pc++;
                continue;
            }

            if (command == "w" || command == "WRITE")
            {
                // Вывод значения. Значение выражения уже посчитано и лежит на вершине стека
                if (stack.Count > 0)
                {
                    Console.WriteLine($"Вывод: {stack.Pop()}");
                }
                else
                {
                    // Альтернативно, если в ОПС "a w", берем значение переменной напрямую
                    string varName = rpn[pc - 1];
                    if (variables.ContainsKey(varName))
                    {
                        Console.WriteLine($"Вывод ({varName}): {variables[varName]}");
                    }
                    else
                    {
                        Console.WriteLine($"[Ошибка выполнения]: Переменная '{varName}' не инициализирована.");
                        return;
                    }
                }
                pc++;
                continue;
            }

            // 4. Условные и безусловные переходы (УП / ЧП / jf)
            if (command == "jf" || command == "ЧП")
            {
                int targetAddress = (int)stack.Pop(); // Адрес перехода
                double condition = stack.Pop();      // Результат условия (0 - ложь, иначе - истина)

                if (condition == 0)
                {
                    pc = targetAddress; // Совершаем прыжок
                    continue;
                }
                else
                {
                    pc++; // Условие истинно, идем дальше
                    continue;
                }
            }

            if (command == "УП")
            {
                int targetAddress = (int)stack.Pop();
                pc = targetAddress;
                continue;
            }

            // 5. Операция присваивания
            if (command == "=")
            {
                double val = stack.Pop();
                // Имя переменной в ОПС "a a 10 + =" находится в вычислениях. 
                // Нам нужно знать, куда присвоить. Обычно адрес/имя переменной тоже кладется в стек, 
                // либо мы берем имя переменной, которое шло перед операцией.
                // В классической ОПС для "a = 5" пишется "a 5 =", в стек кидают строку "a", потом 5, потом "=".
                // Если вы не кладете имена в стек, возьмем операнд из ОПС слева от цепочки вычислений.
                
                // Самый надежный стековый вариант: если мы встретили имя переменной как левый операнд присваивания.
                // Для простоты найдем имя переменной, пройдя назад по ОПС до начала выражения.
                string varName = FindTargetVariable(rpn, pc);
                
                if (!string.IsNullOrEmpty(varName))
                {
                    variables[varName] = val;
                }
                pc++;
                continue;
            }

            // 6. Математические и логические операции
            if (command == "+" || command == "-" || command == "*" || command == "/" || 
                command == ">" || command == "<" || command == "==" || command == "!=" || command == "<=" || command == ">=")
            {
                double b = stack.Pop();
                double a = stack.Pop();
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
                    case ">": result = a > b ? 1 : 0; break;
                    case "<": result = a < b ? 1 : 0; break;
                    case "==": result = a == b ? 1 : 0; break;
                    case "!=": result = a != b ? 1 : 0; break;
                    case "<=": result = a <= b ? 1 : 0; break;
                    case ">=": result = a >= b ? 1 : 0; break;
                }

                stack.Push(result);
                pc++;
                continue;
            }

            // 7. Операнды (Числа, адреса переходов или Переменные)
            // Если это число — просто кладем в стек
            if (double.TryParse(command, out double number))
            {
                stack.Push(number);
            }
            // Если это имя переменной — извлекаем её текущее значение и кладем в стек
            else if (char.IsLetter(command[0]))
            {
                if (variables.ContainsKey(command))
                {
                    stack.Push(variables[command]);
                }
                else
                {
                    // Если переменная еще не создана (например, при первом присваивании), 
                    // кладем 0 или саму строку-имя (зависит от того, является ли она левой частью '=' или правой)
                    // Для вычислений правых частей инициализируем нулем по умолчанию
                    stack.Push(0);
                }
            }

            pc++;
        }

        Console.WriteLine("[Interpreter]: Программа успешно выполнена.");
    }

    private string FindTargetVariable(List<string> rpn, int currentPc)
    {
        // Метод ищет имя переменной, которой присваивается значение.
        // Идет назад от знака '=' и ищет первый идентификатор, перед которым нет знака операции.
        for (int i = currentPc - 1; i >= 0; i--)
        {
            if (char.IsLetter(rpn[i][0]) && rpn[i] != "bp" && rpn[i] != "ret" && rpn[i] != "jf" && rpn[i] != "ЧП")
            {
                return rpn[i];
            }
        }
        return null;
    }
}