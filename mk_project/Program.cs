using System;
using System.Collections.Generic;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        // Проверяем, передан ли файл с кодом в качестве аргумента
        if (args.Length < 1)
        {
            Console.WriteLine("Ошибка: Укажите путь к файлу с исходным кодом.");
            Console.WriteLine("Пример: dotnet run test1.txt");
            return;
        }

        string filePath = args[0];

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Ошибка: Файл {filePath} не найден.");
            return;
        }

        try
        {
            // 1. Чтение файла
            string sourceCode = File.ReadAllText(filePath);

            // 2. Вызов Лексического анализатора
            Lexer lexer = new Lexer();
            if (!lexer.Tokenize(sourceCode, out List<Token> tokens))
            {
                Console.WriteLine("Выполнение прервано из-за лексической ошибки.");
                return;
            }

            // 3. Вызов Синтаксического анализатора + Генератора ОПС
            Parser parser = new Parser();
            if (!parser.Parse(tokens, out List<string> rpn))
            {
                Console.WriteLine("Выполнение прервано из-за синтаксической ошибки.");
                return;
            }

            // 4. Вызов Интерпретатора ОПС
            Interpreter interpreter = new Interpreter();
            interpreter.Execute(rpn);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Непредвиденная ошибка при трансляции: {ex.Message}");
        }
    }
}