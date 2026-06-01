using System;
using System.Collections.Generic;

public class Parser
{
    private const int NON_TERM_START = 100;

    // ── Нетерминалы ─────────────────────────────────────────────────────────
    private const int NT_P  = 100; // Программа
    private const int NT_R  = 101; // Список объявлений обычных переменных (int)
    private const int NT_L  = 102; // Список объявлений массивов (int1)
    private const int NT_A  = 103; // Один оператор внутри блока
    private const int NT_X  = 104; // Правая часть оператора присваивания
    private const int NT_Y  = 105; // Переменная внутри оператора read
    private const int NT_Y1 = 106; // Необязательный индекс переменной в операторе read
    private const int NT_B  = 107; // Альтернативная ветка else
    private const int NT_S  = 108; // Арифметическое выражение (+, -)
    private const int NT_T  = 109; // Терм (*, /)
    private const int NT_U  = 110; // Хвост выражения
    private const int NT_W  = 111; // Хвост терма
    private const int NT_F  = 112; // Фактор
    private const int NT_F1 = 113; // Необязательный индекс в выражении
    private const int NT_O  = 114; // Операция сравнения
    private const int NT_V  = 115; // Условие (для if/while)
    private const int NT_LIST = 116; // Список операторов

    // ── Семантические маркеры (отрицательные, чтобы не путать с терминалами) ─
    private const int ACT_ASSIGN      = -14;
    private const int ACT_WRITE       = -12;
    private const int ACT_READ        = -11;
    private const int ACT_ADD         = -20;
    private const int ACT_SUB         = -21;
    private const int ACT_MUL         = -22;
    private const int ACT_DIV         = -23;
    private const int ACT_LT          = -24;
    private const int ACT_GT          = -25;
    private const int ACT_EQ          = -26;
    private const int ACT_NE          = -27;
    private const int ACT_LE          = -28;
    private const int ACT_GE          = -29;
    private const int ACT_INDEX       = -30;
    private const int ACT_NEG         = -31; // унарный минус → "neg"
    private const int ACT_WHILE_START = -32; // фиксирует адрес начала while в ОПС
    private const int ACT_WHILE_JF    = -33; // генерирует jf и дыру для патча конца while
    private const int ACT_WHILE_END   = -34; // генерирует УП назад + патчит дыру конца while

    private const int ERROR_RULE   = -1;
    private const int EPSILON_RULE =  0;

    private readonly int[,] parsingTable = new int[17, 35];

    // Стек адресов начала while и индексов заглушки jf
    private Stack<int> whileStartStack = new Stack<int>();
    private Stack<int> whileJfStack    = new Stack<int>();

    // Стек заглушек для if/else
    private Stack<int> ifLabelStack = new Stack<int>();

    public Parser()
    {
        InitializeParsingTable();
    }

    // ────────────────────────────────────────────────────────────────────────
    private void InitializeParsingTable()
    {
        for (int i = 0; i < parsingTable.GetLength(0); i++)
            for (int j = 0; j < parsingTable.GetLength(1); j++)
                parsingTable[i, j] = ERROR_RULE;

        // NT_P
        parsingTable[NT_P - NON_TERM_START, 1]  = 1;  // P -> int R P
        parsingTable[NT_P - NON_TERM_START, 2]  = 2;  // P -> int1 L P
        parsingTable[NT_P - NON_TERM_START, 30] = 3;  // P -> begin LIST end

        // NT_R
        parsingTable[NT_R - NON_TERM_START, 1] = EPSILON_RULE;
        parsingTable[NT_R - NON_TERM_START, 2] = EPSILON_RULE;
        parsingTable[NT_R - NON_TERM_START, 10] = 4;
        parsingTable[NT_R - NON_TERM_START, 30] = EPSILON_RULE;

        // NT_L
        parsingTable[NT_L - NON_TERM_START, 1] = EPSILON_RULE;
        parsingTable[NT_L - NON_TERM_START, 2] = EPSILON_RULE;
        parsingTable[NT_L - NON_TERM_START, 10] = 6;
        parsingTable[NT_L - NON_TERM_START, 30] = EPSILON_RULE;

        // NT_LIST
        parsingTable[NT_LIST - NON_TERM_START, 10] = 43;
        parsingTable[NT_LIST - NON_TERM_START, 3]  = 43;
        parsingTable[NT_LIST - NON_TERM_START, 6]  = 43;
        parsingTable[NT_LIST - NON_TERM_START, 8]  = 43;
        parsingTable[NT_LIST - NON_TERM_START, 9]  = 43;
        parsingTable[NT_LIST - NON_TERM_START, 30] = 43;
        parsingTable[NT_LIST - NON_TERM_START, 31] = EPSILON_RULE;
        parsingTable[NT_LIST - NON_TERM_START, 5]  = EPSILON_RULE;

        // NT_A
        parsingTable[NT_A - NON_TERM_START, 10] = 8;
        parsingTable[NT_A - NON_TERM_START, 3]  = 9;
        parsingTable[NT_A - NON_TERM_START, 6]  = 10;
        parsingTable[NT_A - NON_TERM_START, 8]  = 11;
        parsingTable[NT_A - NON_TERM_START, 9]  = 12;
        parsingTable[NT_A - NON_TERM_START, 30] = 42;

        // NT_X
        parsingTable[NT_X - NON_TERM_START, 22] = 14;
        parsingTable[NT_X - NON_TERM_START, 17] = 15;

        // NT_Y
        parsingTable[NT_Y - NON_TERM_START, 10] = 16;

        // NT_Y1
        parsingTable[NT_Y1 - NON_TERM_START, 17] = 17;
        parsingTable[NT_Y1 - NON_TERM_START, 16] = EPSILON_RULE;

        // NT_B
        parsingTable[NT_B - NON_TERM_START, 5]  = 19;
        parsingTable[NT_B - NON_TERM_START, 19] = EPSILON_RULE;
        parsingTable[NT_B - NON_TERM_START, 10] = EPSILON_RULE;
        parsingTable[NT_B - NON_TERM_START, 3]  = EPSILON_RULE;
        parsingTable[NT_B - NON_TERM_START, 6]  = EPSILON_RULE;
        parsingTable[NT_B - NON_TERM_START, 8]  = EPSILON_RULE;
        parsingTable[NT_B - NON_TERM_START, 9]  = EPSILON_RULE;
        parsingTable[NT_B - NON_TERM_START, 31] = EPSILON_RULE;

        // NT_S
        parsingTable[NT_S - NON_TERM_START, 10] = 21;
        parsingTable[NT_S - NON_TERM_START, 28] = 21;
        parsingTable[NT_S - NON_TERM_START, 29] = 21;
        parsingTable[NT_S - NON_TERM_START, 15] = 21;
        parsingTable[NT_S - NON_TERM_START, 12] = 21;

        // NT_T
        parsingTable[NT_T - NON_TERM_START, 10] = 22;
        parsingTable[NT_T - NON_TERM_START, 28] = 22;
        parsingTable[NT_T - NON_TERM_START, 29] = 22;
        parsingTable[NT_T - NON_TERM_START, 15] = 22;
        parsingTable[NT_T - NON_TERM_START, 12] = 22;

        // NT_U
        parsingTable[NT_U - NON_TERM_START, 11] = 23;
        parsingTable[NT_U - NON_TERM_START, 12] = 24;
        parsingTable[NT_U - NON_TERM_START, 16] = EPSILON_RULE;
        parsingTable[NT_U - NON_TERM_START, 18] = EPSILON_RULE;
        parsingTable[NT_U - NON_TERM_START, 19] = EPSILON_RULE;
        for (int op = 22; op <= 27; op++) parsingTable[NT_U - NON_TERM_START, op] = EPSILON_RULE;

        // NT_W
        parsingTable[NT_W - NON_TERM_START, 13] = 26;
        parsingTable[NT_W - NON_TERM_START, 14] = 27;
        parsingTable[NT_W - NON_TERM_START, 11] = EPSILON_RULE;
        parsingTable[NT_W - NON_TERM_START, 12] = EPSILON_RULE;
        parsingTable[NT_W - NON_TERM_START, 16] = EPSILON_RULE;
        parsingTable[NT_W - NON_TERM_START, 18] = EPSILON_RULE;
        parsingTable[NT_W - NON_TERM_START, 19] = EPSILON_RULE;
        for (int op = 22; op <= 27; op++) parsingTable[NT_W - NON_TERM_START, op] = EPSILON_RULE;

        // NT_F
        parsingTable[NT_F - NON_TERM_START, 15] = 29;
        parsingTable[NT_F - NON_TERM_START, 10] = 30;
        parsingTable[NT_F - NON_TERM_START, 28] = 31;
        parsingTable[NT_F - NON_TERM_START, 29] = 31;
        parsingTable[NT_F - NON_TERM_START, 12] = 32;

        // NT_F1
        parsingTable[NT_F1 - NON_TERM_START, 17] = 33;
        parsingTable[NT_F1 - NON_TERM_START, 11] = EPSILON_RULE;
        parsingTable[NT_F1 - NON_TERM_START, 12] = EPSILON_RULE;
        parsingTable[NT_F1 - NON_TERM_START, 13] = EPSILON_RULE;
        parsingTable[NT_F1 - NON_TERM_START, 14] = EPSILON_RULE;
        parsingTable[NT_F1 - NON_TERM_START, 16] = EPSILON_RULE;
        parsingTable[NT_F1 - NON_TERM_START, 18] = EPSILON_RULE;
        parsingTable[NT_F1 - NON_TERM_START, 19] = EPSILON_RULE;
        for (int op = 22; op <= 27; op++) parsingTable[NT_F1 - NON_TERM_START, op] = EPSILON_RULE;

        // NT_O
        parsingTable[NT_O - NON_TERM_START, 23] = 35; // <
        parsingTable[NT_O - NON_TERM_START, 24] = 36; // >
        parsingTable[NT_O - NON_TERM_START, 25] = 37; // ==
        parsingTable[NT_O - NON_TERM_START, 28] = 38; // !=
        parsingTable[NT_O - NON_TERM_START, 26] = 39; // <=
        parsingTable[NT_O - NON_TERM_START, 27] = 40; // >=

        // NT_V
        parsingTable[NT_V - NON_TERM_START, 10] = 41;
        parsingTable[NT_V - NON_TERM_START, 28] = 41;
        parsingTable[NT_V - NON_TERM_START, 29] = 41;
        parsingTable[NT_V - NON_TERM_START, 15] = 41;
        parsingTable[NT_V - NON_TERM_START, 12] = 41;
    }

    // ────────────────────────────────────────────────────────────────────────
    public bool Parse(List<Token> tokens, out List<string> rpn)
    {
        rpn = new List<string>();
        Stack<int> grammarStack = new Stack<int>();
        whileStartStack.Clear();
        whileJfStack.Clear();
        ifLabelStack.Clear();

        grammarStack.Push(NT_P);
        int tokenIdx = 0;

        while (grammarStack.Count > 0 && tokenIdx < tokens.Count)
        {
            int top = grammarStack.Peek();
            Token currentToken = tokens[tokenIdx];

            // ── Семантический маркер ─────────────────────────────────────────
            if (top < 0)
            {
                grammarStack.Pop();
                switch (top)
                {
                    case ACT_ASSIGN: rpn.Add("=");     break;
                    case ACT_WRITE:  rpn.Add("w");     break;
                    case ACT_READ:   rpn.Add("r");     break;
                    case ACT_ADD:    rpn.Add("+");     break;
                    case ACT_SUB:    rpn.Add("-");     break;
                    case ACT_MUL:    rpn.Add("*");     break;
                    case ACT_DIV:    rpn.Add("/");     break;
                    case ACT_LT:     rpn.Add("<");     break;
                    case ACT_GT:     rpn.Add(">");     break;
                    case ACT_EQ:     rpn.Add("==");    break;
                    case ACT_NE:     rpn.Add("!=");    break;
                    case ACT_LE:     rpn.Add("<=");    break;
                    case ACT_GE:     rpn.Add(">=");    break;
                    case ACT_INDEX:  rpn.Add("index"); break;

                    case ACT_NEG:
                        rpn.Add("neg");
                        break;

                    case ACT_WHILE_START:
                        whileStartStack.Push(rpn.Count);
                        break;

                    case ACT_WHILE_JF:
                        whileJfStack.Push(rpn.Count);
                        rpn.Add("0"); // Заглушка
                        rpn.Add("jf");
                        break;

                    case ACT_WHILE_END:
                        int startAddr = whileStartStack.Pop();
                        int jfIdx     = whileJfStack.Pop();
                        rpn.Add(startAddr.ToString());
                        rpn.Add("УП");
                        rpn[jfIdx] = rpn.Count.ToString(); // Прямой патчинг адреса
                        break;
                }
                continue;
            }

            // ── Терминал ─────────────────────────────────────────────────────
            if (top < NON_TERM_START)
            {
                if (top == currentToken.Id)
                {
                    grammarStack.Pop();

                    // Маркер начала блока
                    if (currentToken.Id == 30 && rpn.Count == 0) rpn.Add("bp");

                    // Операнды → в ОПС
                    if (currentToken.Id == 10 || currentToken.Id == 28 || currentToken.Id == 29)
                        rpn.Add(currentToken.Value);

                    // После ')' перед 'then' → генерируем jf для if
                    if (currentToken.Id == 16 && grammarStack.Count > 0 && grammarStack.Peek() == 4)
                    {
                        ifLabelStack.Push(rpn.Count);
                        rpn.Add("0"); // Заглушка
                        rpn.Add("jf");
                    }

                    // При встрече 'else' → генерируем УП + патчим jf ветки then
                    if (currentToken.Id == 5)
                    {
                        int ifFalseIdx = ifLabelStack.Pop();
                        ifLabelStack.Push(rpn.Count);
                        rpn.Add("0"); // Заглушка
                        rpn.Add("УП");
                        rpn[ifFalseIdx] = rpn.Count.ToString();
                    }

                    tokenIdx++; // Продвигаемся к следующему токену
                    continue;   // ВАЖНО: Переходим к следующей итерации while
                }
                else
                {
                    Console.WriteLine($"[Синтаксическая ошибка] Строка {currentToken.Line}, " +
                                      $"позиция {currentToken.Column}: ожидался токен с ID={top}, " +
                                      $"встречен '{currentToken.Value}' (ID={currentToken.Id}).");
                    return false;
                }
            }
            // ── Нетерминал ───────────────────────────────────────────────────
            else
            {
                int row = top - NON_TERM_START;
                int col = currentToken.Id;

                if (col < 0 || col >= parsingTable.GetLength(1))
                {
                    Console.WriteLine($"[Синтаксическая ошибка] Строка {currentToken.Line}: " +
                                      $"неожиданный токен '{currentToken.Value}'.");
                    return false;
                }

                int ruleId = parsingTable[row, col];
                if (ruleId == ERROR_RULE)
                {
                    Console.WriteLine($"[Синтаксическая ошибка] Строка {currentToken.Line}, " +
                                      $"позиция {currentToken.Column}: недопустимый токен " +
                                      $"'{currentToken.Value}' (ID={col}) для нетерминала {top}.");
                    return false;
                }

                grammarStack.Pop();

                if (ruleId != EPSILON_RULE)
                    PushRuleToStack(ruleId, grammarStack);
            }
        }

        // Закрываем незапатченные метки одиночных if (без else)
        while (ifLabelStack.Count > 0)
            rpn[ifLabelStack.Pop()] = rpn.Count.ToString();

        if (grammarStack.Count == 0)
        {
            rpn.Add("ret");
            Console.WriteLine($"[Parser]: Итоговая ОПС: {string.Join(" ", rpn)}");
            return true;
        }

        Console.WriteLine("[Синтаксическая ошибка]: программа разобрана не полностью.");
        return false;
    }

    // ────────────────────────────────────────────────────────────────────────
    private void PushRuleToStack(int ruleId, Stack<int> grammarStack)
    {
        // Правила кладутся в обратном порядке — стек LIFO, вершина = первый символ правила.
        switch (ruleId)
        {
            // P -> int R P
            case 1:
                grammarStack.Push(NT_P);
                grammarStack.Push(NT_R);
                grammarStack.Push(1);    // 'int'
                break;

            // P -> int1 L P
            case 2:
                grammarStack.Push(NT_P);
                grammarStack.Push(NT_L);
                grammarStack.Push(2);    // 'int1'
                break;

            // P -> begin LIST end
            case 3:
                grammarStack.Push(31);   // 'end'
                grammarStack.Push(NT_LIST);
                grammarStack.Push(30);   // 'begin'
                break;

            // R -> id ; R
            case 4:
                grammarStack.Push(NT_R);
                grammarStack.Push(19);   // ';'
                grammarStack.Push(10);   // id
                break;

            // L -> id [ num ] ; L
            case 6:
                grammarStack.Push(NT_L);
                grammarStack.Push(19);   // ';'
                grammarStack.Push(18);   // ']'
                grammarStack.Push(28);   // num
                grammarStack.Push(17);   // '['
                grammarStack.Push(10);   // id
                break;

            // A -> id X ;
            case 8:
                grammarStack.Push(19);   // ';'
                grammarStack.Push(NT_X);
                grammarStack.Push(10);   // id  ← матч добавит id в ОПС
                break;

            // A -> if ( V ) then A B
            case 9:
                grammarStack.Push(NT_B);
                grammarStack.Push(NT_A);
                grammarStack.Push(4);    // 'then'
                grammarStack.Push(16);   // ')'
                grammarStack.Push(NT_V);
                grammarStack.Push(15);   // '('
                grammarStack.Push(3);    // 'if'
                break;

            // A -> while ( V ) do A
            case 10:
                grammarStack.Push(ACT_WHILE_END);  // генерирует УП + патчит jf
                grammarStack.Push(NT_A);           // тело цикла
                grammarStack.Push(7);              // 'do'
                grammarStack.Push(ACT_WHILE_JF);   // генерирует jf + заглушку
                grammarStack.Push(16);             // ')'
                grammarStack.Push(NT_V);           // условие
                grammarStack.Push(15);             // '('
                grammarStack.Push(ACT_WHILE_START);// фиксирует адрес начала
                grammarStack.Push(6);              // 'while'
                break;

            // A -> read ( Y ) ;
            case 11:
                grammarStack.Push(19);      // ';'
                grammarStack.Push(ACT_READ);
                grammarStack.Push(16);      // ')'
                grammarStack.Push(NT_Y);
                grammarStack.Push(15);      // '('
                grammarStack.Push(8);       // 'read'
                break;

            // A -> write ( S ) ;
            case 12:
                grammarStack.Push(19);       // ';'
                grammarStack.Push(ACT_WRITE);
                grammarStack.Push(16);       // ')'
                grammarStack.Push(NT_S);
                grammarStack.Push(15);       // '('
                grammarStack.Push(9);        // 'write'
                break;

            // X -> = S
            case 14:
                grammarStack.Push(ACT_ASSIGN);
                grammarStack.Push(NT_S);
                grammarStack.Push(22);         // '='
                break;

            // X -> [ S ] = S  (присваивание элементу массива)
            case 15:
                grammarStack.Push(ACT_ASSIGN);
                grammarStack.Push(NT_S);
                grammarStack.Push(22);    // '='
                grammarStack.Push(18);    // ']'
                grammarStack.Push(ACT_INDEX);
                grammarStack.Push(NT_S);
                grammarStack.Push(17);    // '['
                break;

            // Y -> id Y1
            case 16:
                grammarStack.Push(NT_Y1);
                grammarStack.Push(10);   // id
                break;

            // Y1 -> [ S ]
            case 17:
                grammarStack.Push(18);       // ']'
                grammarStack.Push(ACT_INDEX);
                grammarStack.Push(NT_S);
                grammarStack.Push(17);       // '['
                break;

            // B -> else A
            case 19:
                grammarStack.Push(NT_A);
                grammarStack.Push(5);     // 'else'
                break;

            // S -> T U
            case 21:
                grammarStack.Push(NT_U);
                grammarStack.Push(NT_T);
                break;

            // T -> F W
            case 22:
                grammarStack.Push(NT_W);
                grammarStack.Push(NT_F);
                break;

            // U -> + T U
            case 23:
                grammarStack.Push(NT_U);
                grammarStack.Push(ACT_ADD);
                grammarStack.Push(NT_T);
                grammarStack.Push(11);    // '+'
                break;

            // U -> - T U
            case 24:
                grammarStack.Push(NT_U);
                grammarStack.Push(ACT_SUB);
                grammarStack.Push(NT_T);
                grammarStack.Push(12);    // '-'
                break;

            // W -> * F W
            case 26:
                grammarStack.Push(NT_W);
                grammarStack.Push(ACT_MUL);
                grammarStack.Push(NT_F);
                grammarStack.Push(13);    // '*'
                break;

            // W -> / F W
            case 27:
                grammarStack.Push(NT_W);
                grammarStack.Push(ACT_DIV);
                grammarStack.Push(NT_F);
                grammarStack.Push(14);    // '/'
                break;

            // F -> ( S )
            case 29:
                grammarStack.Push(16);    // ')'
                grammarStack.Push(NT_S);
                grammarStack.Push(15);    // '('
                break;

            // F -> id F1
            case 30:
                grammarStack.Push(NT_F1);
                grammarStack.Push(10);
                break;

            // F -> num (ничего в стек не кладем, обрабатывается как терминал)
            case 31:
                grammarStack.Push(28);
                break;

            // F -> - F (унарный минус)
            case 32:
                grammarStack.Push(ACT_NEG);
                grammarStack.Push(NT_F);
                grammarStack.Push(12);    // '-'
                break;

            // F1 -> [ S ]
            case 33:
                grammarStack.Push(18);       // ']'
                grammarStack.Push(ACT_INDEX);
                grammarStack.Push(NT_S);
                grammarStack.Push(17);       // '['
                break;

            // O -> < S
            case 35:
                grammarStack.Push(ACT_LT);
                grammarStack.Push(NT_S);
                grammarStack.Push(23);    // '<'
                break;

            // O -> > S
            case 36:
                grammarStack.Push(ACT_GT);
                grammarStack.Push(NT_S);
                grammarStack.Push(24);    // '>'
                break;

            // O -> == S
            case 37:
                grammarStack.Push(ACT_EQ);
                grammarStack.Push(NT_S);
                grammarStack.Push(25);    // '=='
                break;

            // O -> != S
            case 38:
                grammarStack.Push(ACT_NE);
                grammarStack.Push(NT_S);
                grammarStack.Push(28);    // '!='
                break;

            // O -> <= S
            case 39:
                grammarStack.Push(ACT_LE);
                grammarStack.Push(NT_S);
                grammarStack.Push(26);    // '<='
                break;

            // O -> >= S
            case 40:
                grammarStack.Push(ACT_GE);
                grammarStack.Push(NT_S);
                grammarStack.Push(27);    // '>='
                break;

            // V -> S O
            case 41:
                grammarStack.Push(NT_O);
                grammarStack.Push(NT_S);
                break;

            case 42:
                grammarStack.Push(31);   // 'end'
                grammarStack.Push(NT_LIST); // Операторы ВНУТРИ блока
                grammarStack.Push(30);   // 'begin'
                break;
                
            case 43:
                grammarStack.Push(NT_LIST);
                grammarStack.Push(NT_A);
                break;
        }
    }
}

// ── Вспомогательные методы расширения для ОПС ───────────────────────────────
public static class RpnExtensions
{
    // Патчит заглушку по индексу текущим размером ОПС (адрес следующей команды).
    public static void DynamicAddressPatch(this List<string> rpn, int index)
    {
        if (index >= 0 && index < rpn.Count)
        {
            string placeholder = rpn[index];
            if (placeholder == "[IF_FALSE_PTR]"  ||
                placeholder == "[ELSE_END_PTR]"  ||
                placeholder == "[WHILE_END_PTR]")
            {
                rpn[index] = rpn.Count.ToString();
            }
        }
    }
}