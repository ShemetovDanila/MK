using System;
using System.Collections.Generic;

public class Parser
{
    private const int NON_TERM_START = 100;

    // Нетерминалы грамматики
    private const int NT_P  = 100;
    private const int NT_R  = 101;
    private const int NT_L  = 102;
    private const int NT_A  = 103;
    private const int NT_X  = 104;
    private const int NT_Y  = 105;
    private const int NT_Y1 = 106;
    private const int NT_B  = 107;
    private const int NT_S  = 108;
    private const int NT_T  = 109;
    private const int NT_U  = 110;
    private const int NT_W  = 111;
    private const int NT_F  = 112;
    private const int NT_F1 = 113;
    private const int NT_O  = 114;
    private const int NT_V  = 115;

    private const int ERROR_RULE = -1;
    private const int EPSILON_RULE = 0;

    // Таблица синтаксического анализа: 16 нетерминалов х 35 возможных токенов
    private readonly int[,] parsingTable = new int[16, 35];
    
    // Специальный стек для разметки ветвлений и циклов в ОПС
    private Stack<int> labelStack = new Stack<int>();

    public Parser()
    {
        InitializeParsingTable();
    }

    private void InitializeParsingTable()
    {
        for (int i = 0; i < parsingTable.GetLength(0); i++)
            for (int j = 0; j < parsingTable.GetLength(1); j++)
                parsingTable[i, j] = ERROR_RULE;

        // --- Правила для P (0) ---
        parsingTable[NT_P - NON_TERM_START, 1] = 1;   // P -> int R P
        parsingTable[NT_P - NON_TERM_START, 2] = 2;   // P -> int1 L P
        parsingTable[NT_P - NON_TERM_START, 30] = 3;  // P -> begin A end (зададим код для begin = 30)

        // --- Правила для R (1) ---
        parsingTable[NT_R - NON_TERM_START, 10] = 4;  // R -> id ; R
        parsingTable[NT_R - NON_TERM_START, 1] = EPSILON_RULE;  // R -> λ
        parsingTable[NT_R - NON_TERM_START, 2] = EPSILON_RULE;  // R -> λ
        parsingTable[NT_R - NON_TERM_START, 30] = EPSILON_RULE; // R -> λ

        // --- Правила для L (2) ---
        parsingTable[NT_L - NON_TERM_START, 10] = 6;  // L -> id [ num ] ; L
        parsingTable[NT_L - NON_TERM_START, 1] = EPSILON_RULE;  // L -> λ
        parsingTable[NT_L - NON_TERM_START, 2] = EPSILON_RULE;  // L -> λ
        parsingTable[NT_L - NON_TERM_START, 30] = EPSILON_RULE; // L -> λ

        // --- Правила для A (3) ---
        parsingTable[NT_A - NON_TERM_START, 10] = 8;  // A -> id X ; A
        parsingTable[NT_A - NON_TERM_START, 3] = 9;   // A -> if ( V ) then A B ; A
        parsingTable[NT_A - NON_TERM_START, 6] = 10;  // A -> while ( V ) do A ; A
        parsingTable[NT_A - NON_TERM_START, 8] = 11;  // A -> read ( Y ) ; A
        parsingTable[NT_A - NON_TERM_START, 9] = 12;  // A -> write ( S ) ; A
        parsingTable[NT_A - NON_TERM_START, 31] = EPSILON_RULE; // A -> λ (перед end = 31)
        parsingTable[NT_A - NON_TERM_START, 5] = EPSILON_RULE;  // A -> λ (перед else)

        // --- Правила для X (4) ---
        parsingTable[NT_X - NON_TERM_START, 21] = 14; // X -> = S
        parsingTable[NT_X - NON_TERM_START, 17] = 15; // X -> [ S ] = S

        // --- Правила для Y (5) ---
        parsingTable[NT_Y - NON_TERM_START, 10] = 16; // Y -> id Y1

        // --- Правила для Y1 (6) ---
        parsingTable[NT_Y1 - NON_TERM_START, 17] = 17; // Y1 -> [ S ]
        parsingTable[NT_Y1 - NON_TERM_START, 16] = EPSILON_RULE; // Y1 -> λ (перед ')')

        // --- Правила для B (7) ---
        parsingTable[NT_B - NON_TERM_START, 5] = 19;  // B -> else A
        parsingTable[NT_B - NON_TERM_START, 19] = EPSILON_RULE; // B -> λ (перед ';')

        // --- Правила для S (8) ---
        parsingTable[NT_S - NON_TERM_START, 10] = 21; // S -> T U
        parsingTable[NT_S - NON_TERM_START, 28] = 21; // S -> T U
        parsingTable[NT_S - NON_TERM_START, 29] = 21; // S -> T U
        parsingTable[NT_S - NON_TERM_START, 15] = 21; // S -> T U
        parsingTable[NT_S - NON_TERM_START, 12] = 21; // S -> T U (-F)

        // --- Правила для T (9) ---
        parsingTable[NT_T - NON_TERM_START, 10] = 22; // T -> F W
        parsingTable[NT_T - NON_TERM_START, 28] = 22; // T -> F W
        parsingTable[NT_T - NON_TERM_START, 29] = 22; // T -> F W
        parsingTable[NT_T - NON_TERM_START, 15] = 22; // T -> F W
        parsingTable[NT_T - NON_TERM_START, 12] = 22; // T -> F W

        // --- Правила для U (10) ---
        parsingTable[NT_U - NON_TERM_START, 11] = 23; // U -> + T U
        parsingTable[NT_U - NON_TERM_START, 12] = 24; // U -> - T U
        parsingTable[NT_U - NON_TERM_START, 16] = EPSILON_RULE; // U -> λ
        parsingTable[NT_U - NON_TERM_START, 18] = EPSILON_RULE; // U -> λ
        parsingTable[NT_U - NON_TERM_START, 19] = EPSILON_RULE; // U -> λ
        for (int op = 22; op <= 27; op++) parsingTable[NT_U - NON_TERM_START, op] = EPSILON_RULE; 

        // --- Правила для W (11) ---
        parsingTable[NT_W - NON_TERM_START, 13] = 26; // W -> * F W
        parsingTable[NT_W - NON_TERM_START, 14] = 27; // W -> / F W
        parsingTable[NT_W - NON_TERM_START, 11] = EPSILON_RULE; // W -> λ
        parsingTable[NT_W - NON_TERM_START, 12] = EPSILON_RULE; // W -> λ
        parsingTable[NT_W - NON_TERM_START, 16] = EPSILON_RULE; // W -> λ
        parsingTable[NT_W - NON_TERM_START, 18] = EPSILON_RULE; // W -> λ
        parsingTable[NT_W - NON_TERM_START, 19] = EPSILON_RULE; // W -> λ
        for (int op = 22; op <= 27; op++) parsingTable[NT_W - NON_TERM_START, op] = EPSILON_RULE;

        // --- Правила для F (12) ---
        parsingTable[NT_F - NON_TERM_START, 15] = 29; // F -> ( S )
        parsingTable[NT_F - NON_TERM_START, 10] = 30; // F -> id F1
        parsingTable[NT_F - NON_TERM_START, 28] = 31; // F -> num (целое)
        parsingTable[NT_F - NON_TERM_START, 29] = 31; // F -> num (вещественное)
        parsingTable[NT_F - NON_TERM_START, 12] = 32; // F -> - F

        // --- Правила для F1 (13) ---
        parsingTable[NT_F1 - NON_TERM_START, 17] = 33; // F1 -> [ S ]
        parsingTable[NT_F1 - NON_TERM_START, 11] = EPSILON_RULE;
        parsingTable[NT_F1 - NON_TERM_START, 12] = EPSILON_RULE;
        parsingTable[NT_F1 - NON_TERM_START, 13] = EPSILON_RULE;
        parsingTable[NT_F1 - NON_TERM_START, 14] = EPSILON_RULE;
        parsingTable[NT_F1 - NON_TERM_START, 16] = EPSILON_RULE;
        parsingTable[NT_F1 - NON_TERM_START, 18] = EPSILON_RULE;
        parsingTable[NT_F1 - NON_TERM_START, 19] = EPSILON_RULE;
        for (int op = 22; op <= 27; op++) parsingTable[NT_F1 - NON_TERM_START, op] = EPSILON_RULE;

        // --- Правила для O (14) ---
        parsingTable[NT_O - NON_TERM_START, 22] = 35; // O -> < S
        parsingTable[NT_O - NON_TERM_START, 23] = 36; // O -> > S
        parsingTable[NT_O - NON_TERM_START, 24] = 37; // O -> == S
        parsingTable[NT_O - NON_TERM_START, 27] = 38; // O -> != S
        parsingTable[NT_O - NON_TERM_START, 25] = 39; // O -> <= S
        parsingTable[NT_O - NON_TERM_START, 26] = 40; // O -> >= S

        // --- Правила для V (15) ---
        parsingTable[NT_V - NON_TERM_START, 10] = 41; // V -> S O
        parsingTable[NT_V - NON_TERM_START, 28] = 41;
        parsingTable[NT_V - NON_TERM_START, 29] = 41;
        parsingTable[NT_V - NON_TERM_START, 15] = 41;
        parsingTable[NT_V - NON_TERM_START, 12] = 41;
    }

    public bool Parse(List<Token> tokens, out List<string> rpn)
    {
        rpn = new List<string>();
        Stack<int> grammarStack = new Stack<int>();

        grammarStack.Push(NT_P); 
        int tokenIdx = 0;

        while (grammarStack.Count > 0 && tokenIdx < tokens.Count)
        {
            int top = grammarStack.Peek();
            Token currentToken = tokens[tokenIdx];

            if (top < NON_TERM_START)
            {
                if (top == currentToken.Id)
                {
                    grammarStack.Pop();
                    
                    if (currentToken.Id == 10 || currentToken.Id == 28 || currentToken.Id == 29)
                    {
                        rpn.Add(currentToken.Value);
                    }
                    tokenIdx++;
                }
                else
                {
                    Console.WriteLine($"[Синтаксическая ошибка] Строка {currentToken.Line}: Ожидался токен {top}, встречен '{currentToken.Value}'");
                    return false;
                }
            }
            else
            {
                int row = top - NON_TERM_START;
                int col = currentToken.Id;

                if (col >= 35) col = 19; // Коррекция выхода за границы

                int ruleId = parsingTable[row, col];

                if (ruleId == ERROR_RULE)
                {
                    Console.WriteLine($"[Синтаксическая ошибка] Строка {currentToken.Line}: Ошибочная структура возле '{currentToken.Value}'");
                    return false;
                }

                grammarStack.Pop();

                if (ruleId != EPSILON_RULE)
                {
                    PushRuleToStack(ruleId, grammarStack);
                    ExecuteSemanticAction(ruleId, currentToken, rpn);
                }
            }
        }

        return grammarStack.Count == 0;
    }

    private void PushRuleToStack(int ruleId, Stack<int> grammarStack)
    {
        switch (ruleId)
        {
            case 1: // P -> int R P
                grammarStack.Push(NT_P); grammarStack.Push(NT_R); grammarStack.Push(1); break;
            case 2: // P -> int1 L P
                grammarStack.Push(NT_P); grammarStack.Push(NT_L); grammarStack.Push(2); break;
            case 3: // P -> begin A end
                grammarStack.Push(31); grammarStack.Push(NT_A); grammarStack.Push(30); break;
            case 4: // R -> id ; R
                grammarStack.Push(NT_R); grammarStack.Push(19); grammarStack.Push(10); break;
            case 6: // L -> id [ num ] ; L
                grammarStack.Push(NT_L); grammarStack.Push(19); grammarStack.Push(18); grammarStack.Push(28); grammarStack.Push(17); grammarStack.Push(10); break;
            case 8: // A -> id X ; A
                grammarStack.Push(NT_A); grammarStack.Push(19); grammarStack.Push(NT_X); grammarStack.Push(10); break;
            case 9: // A -> if ( V ) then A B ; A
                grammarStack.Push(NT_A); grammarStack.Push(19); grammarStack.Push(NT_B); grammarStack.Push(NT_A); grammarStack.Push(4); grammarStack.Push(16); grammarStack.Push(NT_V); grammarStack.Push(15); grammarStack.Push(3); break;
            case 10: // A -> while ( V ) do A ; A
                grammarStack.Push(NT_A); grammarStack.Push(19); grammarStack.Push(NT_A); grammarStack.Push(7); grammarStack.Push(16); grammarStack.Push(NT_V); grammarStack.Push(15); grammarStack.Push(6); break;
            case 11: // A -> read ( Y ) ; A
                grammarStack.Push(NT_A); grammarStack.Push(19); grammarStack.Push(16); grammarStack.Push(NT_Y); grammarStack.Push(15); grammarStack.Push(8); break;
            case 12: // A -> write ( S ) ; A
                grammarStack.Push(NT_A); grammarStack.Push(19); grammarStack.Push(16); grammarStack.Push(NT_S); grammarStack.Push(15); grammarStack.Push(9); break;
            case 14: // X -> = S
                grammarStack.Push(NT_S); grammarStack.Push(21); break;
            case 15: // X -> [ S ] = S
                grammarStack.Push(NT_S); grammarStack.Push(21); grammarStack.Push(18); grammarStack.Push(NT_S); grammarStack.Push(17); break;
            case 16: // Y -> id Y1
                grammarStack.Push(NT_Y1); grammarStack.Push(10); break;
            case 17: // Y1 -> [ S ]
                grammarStack.Push(18); grammarStack.Push(NT_S); grammarStack.Push(17); break;
            case 19: // B -> else A
                grammarStack.Push(NT_A); grammarStack.Push(5); break;
            case 21: // S -> T U
                grammarStack.Push(NT_U); grammarStack.Push(NT_T); break;
            case 22: // T -> F W
                grammarStack.Push(NT_W); grammarStack.Push(NT_F); break;
            case 23: // U -> + T U
                grammarStack.Push(NT_U); grammarStack.Push(NT_T); grammarStack.Push(11); break;
            case 24: // U -> - T U
                grammarStack.Push(NT_U); grammarStack.Push(NT_T); grammarStack.Push(12); break;
            case 26: // W -> * F W
                grammarStack.Push(NT_W); grammarStack.Push(NT_F); grammarStack.Push(13); break;
            case 27: // W -> / F W
                grammarStack.Push(NT_W); grammarStack.Push(NT_F); grammarStack.Push(14); break;
            case 29: // F -> ( S )
                grammarStack.Push(16); grammarStack.Push(NT_S); grammarStack.Push(15); break;
            case 30: // F -> id F1
                grammarStack.Push(NT_F1); grammarStack.Push(10); break;
            case 31: // F -> num
                if (grammarStack.Count > 0 && grammarStack.Peek() == 29) grammarStack.Push(29); else grammarStack.Push(28); break;
            case 32: // F -> - F
                grammarStack.Push(NT_F); grammarStack.Push(12); break;
            case 33: // F1 -> [ S ]
                grammarStack.Push(18); grammarStack.Push(NT_S); grammarStack.Push(17); break;
            case 35: // O -> < S
                grammarStack.Push(NT_S); grammarStack.Push(22); break;
            case 36: // O -> > S
                grammarStack.Push(NT_S); grammarStack.Push(23); break;
            case 37: // O -> == S
                grammarStack.Push(NT_S); grammarStack.Push(24); break;
            case 38: // O -> != S
                grammarStack.Push(NT_S); grammarStack.Push(27); break;
            case 39: // O -> <= S
                grammarStack.Push(NT_S); grammarStack.Push(25); break;
            case 40: // O -> >= S
                grammarStack.Push(NT_S); grammarStack.Push(26); break;
            case 41: // V -> S O
                grammarStack.Push(NT_O); grammarStack.Push(NT_S); break;
        }
    }

    private void ExecuteSemanticAction(int ruleId, Token currentToken, List<string> rpn)
    {
        switch (ruleId)
        {
            case 1: rpn.Add("INT"); break;
            case 2: rpn.Add("INT1"); break;
            case 4: rpn.Add("DECL_VAR"); break;
            case 6: rpn.Add("DECL_ARR"); break;
            case 11: rpn.Add("READ"); break;
            case 12: rpn.Add("WRITE"); break;
            case 14: rpn.Add("="); break;
            case 15: rpn.Add("=[]"); break; 
            case 17: rpn.Add("INDEX"); break;
            case 23: rpn.Add("+"); break;
            case 24: rpn.Add("-"); break;
            case 26: rpn.Add("*"); break;
            case 27: rpn.Add("/"); break;
            case 32: rpn.Add("NEG"); break;
            case 33: rpn.Add("INDEX"); break;
            case 35: rpn.Add("<"); break;
            case 36: rpn.Add(">"); break;
            case 37: rpn.Add("=="); break;
            case 38: rpn.Add("!="); break;
            case 39: rpn.Add("<="); break;
            case 40: rpn.Add(">="); break;

            // Логика генерации переходов для управляющих конструкций (IF и WHILE)
            case 9: // Обработка начала IF (после вычисления V)
                rpn.Add("[IF_FALSE_PTR]");
                rpn.Add("ЧП");
                labelStack.Push(rpn.Count - 2);
                break;
                
            case 19: // Ветка ELSE
                int ifFalseIdx = labelStack.Pop();
                rpn.Add("[ELSE_END_PTR]");
                rpn.Add("УП");
                rpn[ifFalseIdx] = rpn.Count.ToString(); // Перенаправляем ЧП на блок ELSE
                labelStack.Push(rpn.Count - 2);
                break;

            case 10: // Начало цикла WHILE
                int whileCondStart = rpn.Count;
                labelStack.Push(whileCondStart);
                rpn.Add("[WHILE_END_PTR]");
                rpn.Add("ЧП");
                labelStack.Push(rpn.Count - 2);
                break;
        }
    }
}