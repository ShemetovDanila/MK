using System;
using System.Collections.Generic;
using System.Globalization;

public class Interpreter
{
    private Dictionary<string, double> variables = new Dictionary<string, double>();
    private Dictionary<string, Dictionary<int, double>> arrays = new Dictionary<string, Dictionary<int, double>>();
    private Stack<string> stack = new Stack<string>();

    private double GetValue(string token)
    {
        if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out double num))
            return num;
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

    private string FormatNumber(double val)
    {
        if (val == Math.Floor(val) && !double.IsInfinity(val)) return ((long)val).ToString();
        return val.ToString("G", CultureInfo.InvariantCulture);
    }

    public void Execute(List<string> rpn)
    {
        Console.WriteLine("[Interpreter]:");
        int pc = 0;
        stack.Clear();
        while (pc < rpn.Count)
        {
            string command = rpn[pc];

            if (command == "bp") { pc++; continue; }
            if (command == "ret") break;

            if (command == "jf")
            {
                int target = (int)GetValue(stack.Pop());
                double cond = GetValue(stack.Pop());
                pc = (cond == 0.0) ? target : pc + 1;
                continue;
            }

            if (command == "УП")
            {
                pc = (int)GetValue(stack.Pop());
                continue;
            }

            if (command == "index")
            {
                double idxVal = GetValue(stack.Pop());
                string arrName = stack.Pop();
                stack.Push($"{arrName}[{(int)idxVal}]");
                pc++; continue;
            }

            if (command == "=")
            {
                double val = GetValue(stack.Pop());
                string varName = stack.Pop();
                if (varName.Contains("[")) AssignToArray(varName, val);
                else variables[varName] = val;
                pc++; continue;
            }

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

            if (command == "w")
            {
                double val = GetValue(stack.Pop());
                Console.WriteLine(FormatNumber(val));
                pc++; continue;
            }

            if (command == "neg")
            {
                double val = GetValue(stack.Pop());
                stack.Push((-val).ToString(CultureInfo.InvariantCulture));
                pc++; continue;
            }

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