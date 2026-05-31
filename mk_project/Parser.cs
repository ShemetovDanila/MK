using System;
using System.Collections.Generic;

public class Parser
{
    private const int NON_TERM_START = 100;

    // Нетерминалы грамматики
    private const int NT_P  = 100; // Программа
    private const int NT_R  = 101; // Объявления int
    private const int NT_L  = 102; // Объявления int1
    private const int NT_A  = 103; // Операторы
    private const int NT_X  = 104; // Правая часть присваивания
    private const int NT_Y  = 105; // Аргумент read
    private const int NT_Y1 = 106; // Индекс в read
    private const int NT_B  = 107; // Ветка else
    private const int NT_S  = 108; // Выражение
    private const int NT_T  = 109; // Терм
    private const int NT_U  = 110; // Хвост выражения (+)
    private const int NT_W  = 111; // Хвост терма (*)
    private const int NT_F  = 112; // Фактор
    private const int NT_F1 = 113; // Индекс переменной
    private const int NT_O  = 114; // Операция сравнения
    private const int NT_V  = 115; // Условие (if/while)

    // Отрицательные маркеры отложенных семантических действий
    private const int ACT_ASSIGN = -14; // Выталкивает '='
    private const int ACT_WRITE  = -12; // Выталкивает 'w'
    private const int ACT_READ   = -11; // Выталкивает 'r'
    private const int ACT_ADD    = -20; // Выталкивает '+'
    private const int ACT_SUB    = -21; // Выталкивает '-'
    private const int ACT_MUL    = -22; // Выталкивает '*'
    private const int ACT_DIV    = -23; // Выталкивает '/'
    private const int ACT_LT     = -24; // Выталкивает '<'
    private const int ACT_GT     = -25; // Выталкивает '>'
    private const int ACT_EQ     = -26; // Выталкивает '=='
    private const int ACT_NE     = -27; // Выталкивает '!='
    private const int ACT_LE     = -28; // Выталкивает '<='
    private const int ACT_GE     = -29; // Выталкивает '>='

    private const int ERROR_RULE = -1;
    private const int EPSILON_RULE = 0;

    private readonly int[,] parsingTable = new int[16, 35];
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

        // P (Программа)
        parsingTable[NT_P - NON_TERM_START, 1] = 1;   // P -> int R P
        parsingTable[NT_P - NON_TERM_START, 2] = 2;   // P -> int1 L P
        parsingTable[NT_P - NON_TERM_START, 30] = 3;  // P -> begin A end

        // R (Список int)
        parsingTable[NT_R - NON_TERM_START, 10] = 4;  // R -> id ; R
        parsingTable[NT_R - NON_TERM_START, 30] = EPSILON_RULE;

        // L (Список int1)
        parsingTable[NT_L - NON_TERM_START, 10] = 6;  // L -> id [ num ] ; L
        parsingTable[NT_L - NON_TERM_START, 30] = EPSILON_RULE;

        // A (Операторы)
        parsingTable[NT_A - NON_TERM_START, 10] = 8;  // A -> id X ; A
        parsingTable[NT_A - NON_TERM_START, 3] = 9;   // A -> if ( V ) then A B A
        parsingTable[NT_A - NON_TERM_START, 6] = 10;  // A -> while ( V ) do A A
        parsingTable[NT_A - NON_TERM_START, 8] = 11;  // A -> read ( Y ) ; A
        parsingTable[NT_A - NON_TERM_START, 9] = 12;  // A -> write ( S ) ; A
        parsingTable[NT_A - NON_TERM_START, 31] = EPSILON_RULE;
        parsingTable[NT_A - NON_TERM_START, 5] = EPSILON_RULE;

        // X (Правая часть присваивания)
        parsingTable[NT_X - NON_TERM_START, 22] = 14; // X -> = S
        parsingTable[NT_X - NON_TERM_START, 17] = 15; // X -> [ S ] = S

        // Y (Аргумент read)
        parsingTable[NT_Y - NON_TERM_START, 10] = 16; // Y -> id Y1

        // Y1 (Индекс в read)
        parsingTable[NT_Y1 - NON_TERM_START, 17] = 17;
        parsingTable[NT_Y1 - NON_TERM_START, 16] = EPSILON_RULE;

        // B (Ветка else)
        parsingTable[NT_B - NON_TERM_START, 5] = 19;
        parsingTable[NT_B - NON_TERM_START, 19] = EPSILON_RULE;
        parsingTable[NT_B - NON_TERM_START, 10] = EPSILON_RULE;
        parsingTable[NT_B - NON_TERM_START, 3] = EPSILON_RULE;
        parsingTable[NT_B - NON_TERM_START, 6] = EPSILON_RULE;
        parsingTable[NT_B - NON_TERM_START, 8] = EPSILON_RULE;
        parsingTable[NT_B - NON_TERM_START, 9] = EPSILON_RULE;
        parsingTable[NT_B - NON_TERM_START, 31] = EPSILON_RULE;

        // S (Выражение)
        parsingTable[NT_S - NON_TERM_START, 10] = 21; // S -> T U
        parsingTable[NT_S - NON_TERM_START, 28] = 21;
        parsingTable[NT_S - NON_TERM_START, 29] = 21;
        parsingTable[NT_S - NON_TERM_START, 15] = 21;
        parsingTable[NT_S - NON_TERM_START, 12] = 21;

        // T (Терм)
        parsingTable[NT_T - NON_TERM_START, 10] = 22; // T -> F W
        parsingTable[NT_T - NON_TERM_START, 28] = 22;
        parsingTable[NT_T - NON_TERM_START, 29] = 22;
        parsingTable[NT_T - NON_TERM_START, 15] = 22;
        parsingTable[NT_T - NON_TERM_START, 12] = 22;

        // U (Хвост выражения)
        parsingTable[NT_U - NON_TERM_START, 11] = 23; // U -> + T U
        parsingTable[NT_U - NON_TERM_START, 12] = 24; // U -> - T U
        parsingTable[NT_U - NON_TERM_START, 16] = EPSILON_RULE;
        parsingTable[NT_U - NON_TERM_START, 18] = EPSILON_RULE;
        parsingTable[NT_U - NON_TERM_START, 19] = EPSILON_RULE;
        for (int op = 22; op <= 27; op++) parsingTable[NT_U - NON_TERM_START, op] = EPSILON_RULE;

        // W (Хвост терма)
        parsingTable[NT_W - NON_TERM_START, 13] = 26; // W -> * F W
        parsingTable[NT_W - NON_TERM_START, 14] = 27; // W -> / F W
        parsingTable[NT_W - NON_TERM_START, 11] = EPSILON_RULE;
        parsingTable[NT_W - NON_TERM_START, 12] = EPSILON_RULE;
        parsingTable[NT_W - NON_TERM_START, 16] = EPSILON_RULE;
        parsingTable[NT_W - NON_TERM_START, 18] = EPSILON_RULE;
        parsingTable[NT_W - NON_TERM_START, 19] = EPSILON_RULE;
        for (int op = 22; op <= 27; op++) parsingTable[NT_W - NON_TERM_START, op] = EPSILON_RULE;

        // F (Фактор)
        parsingTable[NT_F - NON_TERM_START, 15] = 29; // F -> ( S )
        parsingTable[NT_F - NON_TERM_START, 10] = 30; // F -> id F1
        parsingTable[NT_F - NON_TERM_START, 28] = 31; // F -> num
        parsingTable[NT_F - NON_TERM_START, 29] = 31;
        parsingTable[NT_F - NON_TERM_START, 12] = 32; // F -> - F

        // F1 (Индекс в выражении)
        parsingTable[NT_F1 - NON_TERM_START, 17] = 33;
        parsingTable[NT_F1 - NON_TERM_START, 11] = EPSILON_RULE;
        parsingTable[NT_F1 - NON_TERM_START, 12] = EPSILON_RULE;
        parsingTable[NT_F1 - NON_TERM_START, 13] = EPSILON_RULE;
        parsingTable[NT_F1 - NON_TERM_START, 14] = EPSILON_RULE;
        parsingTable[NT_F1 - NON_TERM_START, 16] = EPSILON_RULE;
        parsingTable[NT_F1 - NON_TERM_START, 18] = EPSILON_RULE;
        parsingTable[NT_F1 - NON_TERM_START, 19] = EPSILON_RULE;
        for (int op = 22; op <= 27; op++) parsingTable[NT_F1 - NON_TERM_START, op] = EPSILON_RULE;

        // O (Операции сравнения)
        parsingTable[NT_O - NON_TERM_START, 23] = 35; // O -> < S
        parsingTable[NT_O - NON_TERM_START, 24] = 36; // O -> > S
        parsingTable[NT_O - NON_TERM_START, 25] = 37; // O -> == S
        parsingTable[NT_O - NON_TERM_START, 28] = 38; // O -> != S
        parsingTable[NT_O - NON_TERM_START, 26] = 39; // O -> <= S
        parsingTable[NT_O - NON_TERM_START, 27] = 40; // O -> >= S

        // V (Условие)
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

            // Если на вершине маркер действия — транслируем оператор в ОПС СТРОГО ПОСЛЕ операндов
            if (top < 0)
            {
                grammarStack.Pop();
                switch (top)
                {
                    case ACT_ASSIGN: rpn.Add("="); break;
                    case ACT_WRITE:  rpn.Add("w"); break;
                    case ACT_READ:   rpn.Add("r"); break;
                    case ACT_ADD:    rpn.Add("+"); break;
                    case ACT_SUB:    rpn.Add("-"); break;
                    case ACT_MUL:    rpn.Add("*"); break;
                    case ACT_DIV:    rpn.Add("/"); break;
                    case ACT_LT:     rpn.Add("<"); break;
                    case ACT_GT:     rpn.Add(">"); break;
                    case ACT_EQ:     rpn.Add("=="); break;
                    case ACT_NE:     rpn.Add("!="); break;
                    case ACT_LE:     rpn.Add("<="); break;
                    case ACT_GE:     rpn.Add(">="); break;
                }
                continue;
            }

            // Терминалы
            if (top < NON_TERM_START)
            {
                if (top == currentToken.Id)
                {
                    grammarStack.Pop();

                    if (currentToken.Id == 30) rpn.Add("bp");

                    // Заносим в ОПС только чистые операнды (идентификаторы и числа)
                    // Знаки операций (=, +, >, ;) сюда не попадают, они управляются маркерами!
                    if (currentToken.Id == 10 || currentToken.Id == 28 || currentToken.Id == 29)
                    {
                        rpn.Add(currentToken.Value);
                    }

                    // Обработка логики ветвления IF
                    if (currentToken.Id == 16 && grammarStack.Count > 0 && grammarStack.Peek() == 4)
                    {
                        labelStack.Push(rpn.Count); 
                        rpn.Add("[IF_FALSE_PTR]");  
                        rpn.Add("jf");             
                    }

                    if (currentToken.Id == 5) // else
                    {
                        int ifFalseIdx = labelStack.Pop();
                        labelStack.Push(rpn.Count);
                        rpn.Add("[ELSE_END_PTR]");
                        rpn.Add("УП");
                        rpn.DynamicAddressPatch(ifFalseIdx); // Подставляем точный адрес начала else
                    }

                    tokenIdx++;
                }
                else
                {
                    Console.WriteLine($"[Синтаксическая ошибка] Строка {currentToken.Line}: Ожидался токен {top}, встречен '{currentToken.Value}'");
                    return false;
                }
            }
            // Нетерминалы
            else
            {
                int row = top - NON_TERM_START;
                int col = currentToken.Id;

                if (col >= 35) return false;

                int ruleId = parsingTable[row, col];
                if (ruleId == ERROR_RULE)
                {
                    Console.WriteLine($"[Синтаксическая ошибка] Строка {currentToken.Line}: Неверная структура грамматики.");
                    return false;
                }

                grammarStack.Pop();

                if (ruleId != EPSILON_RULE)
                {
                    PushRuleToStack(ruleId, grammarStack);
                }
            }
        }

        if (labelStack.Count > 0)
        {
            int lastLabelIdx = labelStack.Pop();
            rpn.DynamicAddressPatch(lastLabelIdx);
        }

        if (grammarStack.Count == 0)
        {
            rpn.Add("ret");
            Console.WriteLine($"[Parser]: Итоговая ОПС: {string.Join(" ", rpn)}");
            return true;
        }

        return false;
    }

    private void PushRuleToStack(int ruleId, Stack<int> grammarStack)
    {
        switch (ruleId)
        {
            case 1: grammarStack.Push(NT_P); grammarStack.Push(NT_R); grammarStack.Push(1); break;
            case 2: grammarStack.Push(NT_P); grammarStack.Push(NT_L); grammarStack.Push(2); break;
            case 3: grammarStack.Push(31); grammarStack.Push(NT_A); grammarStack.Push(30); break;
            case 4: grammarStack.Push(NT_R); grammarStack.Push(19); grammarStack.Push(10); break;
            case 6: grammarStack.Push(NT_L); grammarStack.Push(19); grammarStack.Push(18); grammarStack.Push(28); grammarStack.Push(17); grammarStack.Push(10); break;
            
            // A -> id X ; A
            case 8: 
                grammarStack.Push(NT_A); 
                grammarStack.Push(19); 
                grammarStack.Push(NT_X); 
                grammarStack.Push(10); // Сначала пишется имя переменной-приемника
                break;

            case 9:  grammarStack.Push(NT_A); grammarStack.Push(NT_B); grammarStack.Push(NT_A); grammarStack.Push(4); grammarStack.Push(16); grammarStack.Push(NT_V); grammarStack.Push(15); grammarStack.Push(3); break;
            case 10: grammarStack.Push(NT_A); grammarStack.Push(NT_A); grammarStack.Push(7); grammarStack.Push(16); grammarStack.Push(NT_V); grammarStack.Push(15); grammarStack.Push(6); break;
            
            // A -> read ( Y ) ; A
            case 11: 
                grammarStack.Push(NT_A); grammarStack.Push(19); 
                grammarStack.Push(ACT_READ); // Сработает строго после того, как имя переменной уйдет в ОПС
                grammarStack.Push(16); grammarStack.Push(NT_Y); grammarStack.Push(15); grammarStack.Push(8); 
                break;

            // A -> write ( S ) ; A
            case 12: 
                grammarStack.Push(NT_A); grammarStack.Push(19); 
                grammarStack.Push(ACT_WRITE); // Сработает строго после полного вычисления выражения S
                grammarStack.Push(16); grammarStack.Push(NT_S); grammarStack.Push(15); grammarStack.Push(9); 
                break;

            // X -> = S
            case 14: 
                grammarStack.Push(ACT_ASSIGN); // Ложится на дно: сработает ПОСЛЕ вычисления выражения S
                grammarStack.Push(NT_S); 
                grammarStack.Push(22); 
                break;

            case 15: grammarStack.Push(ACT_ASSIGN); grammarStack.Push(NT_S); grammarStack.Push(22); grammarStack.Push(18); grammarStack.Push(NT_S); grammarStack.Push(17); break;
            case 16: grammarStack.Push(NT_Y1); grammarStack.Push(10); break;
            case 17: grammarStack.Push(18); grammarStack.Push(NT_S); grammarStack.Push(17); break;
            case 19: grammarStack.Push(NT_A); grammarStack.Push(5); break;
            case 21: grammarStack.Push(NT_U); grammarStack.Push(NT_T); break;
            case 22: grammarStack.Push(NT_W); grammarStack.Push(NT_F); break;
            
            // U -> + T U
            case 23: 
                grammarStack.Push(NT_U); 
                grammarStack.Push(ACT_ADD); // Задерживаем плюс: выполнится ПОСЛЕ разбора терма T
                grammarStack.Push(NT_T); 
                grammarStack.Push(11); 
                break;

            // U -> - T U
            case 24: grammarStack.Push(NT_U); grammarStack.Push(ACT_SUB); grammarStack.Push(NT_T); grammarStack.Push(12); break;
            
            // W -> * F W
            case 26: grammarStack.Push(NT_W); grammarStack.Push(ACT_MUL); grammarStack.Push(NT_F); grammarStack.Push(13); break;
            
            // W -> / F W
            case 27: grammarStack.Push(NT_W); grammarStack.Push(ACT_DIV); grammarStack.Push(NT_F); grammarStack.Push(14); break;
            
            case 29: grammarStack.Push(16); grammarStack.Push(NT_S); grammarStack.Push(15); break;
            case 30: grammarStack.Push(NT_F1); grammarStack.Push(10); break;
            case 31: if (grammarStack.Count > 0 && grammarStack.Peek() == 29) grammarStack.Push(29); else grammarStack.Push(28); break;
            case 32: grammarStack.Push(NT_F); grammarStack.Push(12); break;
            case 33: grammarStack.Push(18); grammarStack.Push(NT_S); grammarStack.Push(17); break;
            
            // O -> < S
            case 35: grammarStack.Push(ACT_LT); grammarStack.Push(NT_S); grammarStack.Push(23); break;
            // O -> > S
            case 36: grammarStack.Push(ACT_GT); grammarStack.Push(NT_S); grammarStack.Push(24); break;
            case 37: grammarStack.Push(ACT_EQ); grammarStack.Push(NT_S); grammarStack.Push(25); break;
            case 38: grammarStack.Push(ACT_NE); grammarStack.Push(NT_S); grammarStack.Push(28); break;
            case 39: grammarStack.Push(ACT_LE); grammarStack.Push(NT_S); grammarStack.Push(26); break;
            case 40: grammarStack.Push(ACT_GE); grammarStack.Push(NT_S); grammarStack.Push(27); break;
            
            case 41: grammarStack.Push(NT_O); grammarStack.Push(NT_S); break;
        }
    }
}

// Вспомогательное расширение для красивой и безопасной подстановки адресов в ОПС
public static class RpnExtensions
{
    public static void DynamicAddressPatch(this List<string> rpn, int index)
    {
        if (index >= 0 && index < rpn.Count)
        {
            if (rpn[index] == "[IF_FALSE_PTR]" || rpn[index] == "[ELSE_END_PTR]")
            {
                rpn[index] = rpn.Count.ToString();
            }
        }
    }
}