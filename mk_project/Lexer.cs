using System;
using System.Collections.Generic;

public struct Token
{
    public int Id;
    public string Value;
    public int Line;
    public int Column;

    public Token(int id, string value, int line, int column)
    {
        Id = id;
        Value = value;
        Line = line;
        Column = column;
    }
}

public class Lexer
{
    private enum CharClass
    {
        Letter = 0,      // <б>
        Digit = 1,       // <ц>
        Dot = 2,         // .
        Space = 3,       // пробел
        Sign = 4,        // <с> (+, -, *, /, (, ), [, ], ;)
        Equal = 5,       // =
        Less = 6,        // <
        Greater = 7,     // >
        Exclamation = 8, // !
        EOF = 9          // ┴ (конец файла или строки)
    }

    private enum State
    {
        Start = 0,       // старт (S)
        Id = 1,          // имя (I)
        IntPart = 2,     // целая часть (C)
        AfterDot = 3,    // после точки (D)
        FracPart = 4,    // дробная часть (E)
        AfterEq = 5,     // после = (EQ)
        AfterLt = 6,     // после < (LT)
        AfterGt = 7,     // после > (GT)
        AfterNe = 8      // после ! (NE)
    }

    // Матрица переходов по вашей таблице.
    // Отрицательные числа — коды лексем (переход в финальное состояние).
    // -100 — пустая ячейка (лексическая ошибка).
    private readonly int[,] transitionTable = new int[9, 10]
    {
        //  <б>  <ц>   .    ' '  <с>   =    <    >    !    ┴
        {    1,   2, -100,   0,  -11,   5,   6,   7,   8,  -99 }, // 0: Start
        {    1,   1,   1,  -10,  -10, -10, -10, -10, -10,  -10 }, // 1: Id
        { -100,   2,   3,  -28,  -28, -28, -28, -28, -28,  -28 }, // 2: IntPart
        { -100,   4, -100, -100, -100, -100, -100, -100, -100, -100 }, // 3: AfterDot
        { -100,   4, -100,  -29,  -29, -29, -29, -29, -29,  -29 }, // 4: FracPart
        {  -21,  -21, -100,  -22,  -22, -26, -100, -100, -100,  -22 }, // 5: AfterEq
        {  -22,  -22, -100,  -23,  -23, -24, -100, -100, -100,  -23 }, // 6: AfterLt
        {  -23,  -23, -100,  -24,  -24, -25, -100, -100, -100,  -24 }, // 7: AfterGt
        { -100, -100, -100, -100, -100, -27, -100, -100, -100, -100 }  // 8: AfterNe
    };

    private readonly Dictionary<string, int> keywords = new Dictionary<string, int>
    {
        { "int", 1 }, { "int1", 2 }, { "if", 3 }, { "then", 4 },
        { "else", 5 }, { "while", 6 }, { "do", 7 }, { "read", 8 }, { "write", 9 }
    };

    private readonly Dictionary<char, int> singleSigns = new Dictionary<char, int>
    {
        { '+', 11 }, { '-', 12 }, { '*', 13 }, { '/', 14 },
        { '(', 15 }, { ')', 16 }, { '[', 17 }, { ']', 18 }, { ';', 19 }
    };

    public bool Tokenize(string source, out List<Token> tokens)
    {
        tokens = new List<Token>();
        int currentState = 0;
        string currentLexeme = "";
        
        int line = 1;
        int column = 1;
        int lexemeStartColumn = 1;

        int i = 0;
        source += "┴"; 

        while (i < source.Length)
        {
            char ch = source[i];
            
            if (currentLexeme.Length == 0)
            {
                lexemeStartColumn = column;
            }

            CharClass charClass = GetCharClass(ch);
            int nextState = transitionTable[currentState, (int)charClass];

            if (nextState == -100)
            {
                Console.WriteLine($"[Лексическая ошибка] Строка {line}, позиция {column}: Неверный символ '{ch}' для состояния {currentState}.");
                return false;
            }

            if (nextState < 0)
            {
                if (nextState == -99) 
                {
                    break;
                }

                int finalCode = Math.Abs(nextState);
                int tokenId = 0;
                bool isLookahead = IsLookaheadAction(currentState, charClass);

                if (!isLookahead && ch != ' ' && ch != '\r' && ch != '\n' && charClass != CharClass.EOF)
                {
                    currentLexeme += ch;
                }

                if (finalCode == 10) 
                {
                    tokenId = keywords.ContainsKey(currentLexeme) ? keywords[currentLexeme] : 10;
                }
                else if (finalCode == 11) 
                {
                    char signChar = currentLexeme.Length > 0 ? currentLexeme[0] : ch;
                    tokenId = singleSigns.ContainsKey(signChar) ? singleSigns[signChar] : 11;
                }
                else
                {
                    tokenId = finalCode; 
                }

                tokens.Add(new Token(tokenId, currentLexeme, line, lexemeStartColumn));

                currentLexeme = "";
                currentState = 0;

                if (isLookahead)
                {
                    continue; 
                }
            }
            else
            {
                if (currentState == 0 && (ch == ' ' || ch == '\r' || ch == '\n'))
                {
                }
                else
                {
                    currentLexeme += ch;
                }
                currentState = nextState;
            }

            if (ch == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
            i++;
        }

        Console.WriteLine($"[Lexer]: Успешно выделено лексем: {tokens.Count}");
        return true;
    }

    private CharClass GetCharClass(char ch)
    {
        if (ch == '┴') return CharClass.EOF;
        if (char.IsLetter(ch)) return CharClass.Letter;
        if (char.IsDigit(ch)) return CharClass.Digit;
        if (ch == '.') return CharClass.Dot;
        if (ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n') return CharClass.Space;
        if (ch == '=') return CharClass.Equal;
        if (ch == '<') return CharClass.Less;
        if (ch == '>') return CharClass.Greater;
        if (ch == '!') return CharClass.Exclamation;
        if ("+-*/()[];".Contains(ch)) return CharClass.Sign;

        return CharClass.EOF; 
    }

    private bool IsLookaheadAction(int state, CharClass @class)
    {
        if (state == 1 || state == 2 || state == 4) return true;
        if (state == 5 && @class != CharClass.Equal && @class != CharClass.Space) return true;
        if (state == 6 && @class != CharClass.Equal) return true;
        if (state == 7 && @class != CharClass.Equal) return true;

        return false;
    }
}