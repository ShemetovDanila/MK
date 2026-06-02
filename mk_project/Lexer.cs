using System;
using System.Collections.Generic;

/// <summary>
/// Лексическая единица программы. 
/// Содержит тип (Id), текст (Value) и координаты для диагностики ошибок.
/// </summary>
public struct Token
{
    public int Id;
    public string Value;
    public int Line;
    public int Column;

    public Token(int id, string value, int line, int column)
    {
        Id = id; Value = value; Line = line; Column = column;
    }
}

/// <summary>
/// Лексический анализатор (ДКА). 
/// Преобразует текст в поток токенов.
/// </summary>
public class Lexer
{
    // Определение классов входных символов
    private enum CharClass { Letter = 0, Digit = 1, Dot = 2, Space = 3, Sign = 4, Equal = 5, Less = 6, Greater = 7, Exclamation = 8, EOF = 9, Quote = 10, Other = 11 }
    
    // Определение состояний автомата (9 - чтение строки)
    private enum State { Start = 0, Id = 1, IntPart = 2, AfterDot = 3, FracPart = 4, AfterEq = 5, AfterLt = 6, AfterGt = 7, AfterNe = 8, String = 9 }

    // Матрица переходов ДКА
    private readonly int[,] transitionTable = new int[10, 12]
    {
        // <б>  <ц>   .    ' '  <с>   =    <    >    !    ┴    "   Other
        {    1,   2, -100,   0,  -11,   5,   6,   7,   8,  -99,   9, -100 }, // 0: Start
        {    1,   1,   1,  -10,  -10, -10, -10, -10, -10,  -10, -10, -10 },  // 1: Id
        { -100,   2,   3,  -29,  -29,  -29,  -29,  -29,  -29,  -29, -29, -29 },  // 2: Int
        { -100,   4, -100, -100, -100, -100, -100, -100, -100, -100, -100, -100 }, // 3: Dot
        { -100,   4, -100,  -32,  -32,  -32,  -32,  -32,  -32,  -32, -32, -32 },  // 4: Frac
        {  -22,  -22, -100,  -22,  -22,  -25, -100, -100, -100,  -22, -22, -22 },  // 5: =
        {  -23,  -23, -100,  -23,  -23,  -26, -100, -100, -100,  -23, -23, -23 },  // 6: <
        {  -24,  -24, -100,  -24,  -24,  -27, -100, -100, -100,  -24, -24, -24 },  // 7: >
        { -100, -100, -100, -100, -100,  -28, -100, -100, -100, -100, -100, -100 }, // 8: !
        {    9,   9,    9,    9,    9,    9,   9,   9,   9, -100, -33,   9 }   // 9: Строка
    };

    private readonly Dictionary<string, int> keywords = new Dictionary<string, int>
    {
        { "int", 1 }, { "int1", 2 }, { "if", 3 }, { "then", 4 }, { "else", 5 },
        { "while", 6 }, { "do", 7 }, { "read", 8 }, { "write", 9 }, { "begin", 30 }, { "end", 31 }
    };

    /// <summary>
    /// Основной метод разбиения исходного текста на токены.
    /// </summary>
    public bool Tokenize(string source, out List<Token> tokens)
    {
        tokens = new List<Token>();
        int currentState = 0;
        string currentLexeme = "";
        int line = 1, column = 1, lexemeStartColumn = 1, i = 0;
        source += "┴"; 

        while (i < source.Length)
        {
            char ch = source[i];
            if (currentLexeme.Length == 0) lexemeStartColumn = column;
            
            CharClass charClass = GetCharClass(ch);
            int nextState = transitionTable[currentState, (int)charClass];

            // Если встретили неизвестный символ
            if (nextState == -100)
            {
                Console.WriteLine($"[Лексическая ошибка] Строка {line}, Позиция {column}: Недопустимый символ '{ch}'");
                return false;
            }

            if (nextState < 0)
            {
                if (nextState == -99) break;
                int finalCode = Math.Abs(nextState);
                bool isLookahead = IsLookaheadAction(currentState, charClass);

                if (!isLookahead && ch != ' ' && ch != '\r' && ch != '\n' && charClass != CharClass.EOF)
                    currentLexeme += ch;

                // Определение ID токена (ключевое слово или тип)
                int tokenId = (finalCode == 10) ? (keywords.ContainsKey(currentLexeme) ? keywords[currentLexeme] : 10) :
                              (finalCode == 11) ? GetSignId(currentLexeme.Length > 0 ? currentLexeme[0] : ch) : finalCode;

                tokens.Add(new Token(tokenId, currentLexeme, line, lexemeStartColumn));
                currentLexeme = ""; currentState = 0;
                if (isLookahead) continue;
            }
            else
            {
                // Накопление символов (пробелы поглощаются только внутри строк)
                if (!(currentState == 0 && (ch == ' ' || ch == '\r' || ch == '\n'))) currentLexeme += ch;
                currentState = nextState;
            }
            if (ch == '\n') { line++; column = 1; } else column++;
            i++;
        }
        Console.WriteLine($"[Lexer]: Успешно выделено лексем: {tokens.Count}");
        return true;
    }

    private CharClass GetCharClass(char ch)
    {
        if (ch == '┴') return CharClass.EOF;
        if (ch == '\"') return CharClass.Quote;
        if (char.IsLetter(ch)) return CharClass.Letter;
        if (char.IsDigit(ch)) return CharClass.Digit;
        if (ch == '.') return CharClass.Dot;
        if (char.IsWhiteSpace(ch)) return CharClass.Space;
        if (ch == '=') return CharClass.Equal;
        if (ch == '<') return CharClass.Less;
        if (ch == '>') return CharClass.Greater;
        if (ch == '!') return CharClass.Exclamation;
        if ("+-*/()[];".Contains(ch)) return CharClass.Sign;
        return CharClass.Other; 
    }

    private int GetSignId(char c) => c switch { '+' => 11, '-' => 12, '*' => 13, '/' => 14, '(' => 15, ')' => 16, '[' => 17, ']' => 18, ';' => 19, _ => 11 };

    private bool IsLookaheadAction(int state, CharClass @class)
    {
        if (state == 1 || state == 2 || state == 4) return true;
        if (state == 5 && @class != CharClass.Equal) return true;
        if (state == 6 && @class != CharClass.Equal) return true;
        if (state == 7 && @class != CharClass.Equal) return true;
        return false;
    }
}