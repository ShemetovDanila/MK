using System;
using System.Collections.Generic;
using System.Linq;

public class Parser
{
    private const int NON_TERM_START = 100;
    private const int NT_P = 100, NT_R = 101, NT_L = 102, NT_A = 103, NT_X = 104, NT_Y = 105, NT_Y1 = 106, 
                      NT_B = 107, NT_S = 108, NT_T = 109, NT_U = 110, NT_W = 111, NT_F = 112, NT_F1 = 113, 
                      NT_O = 114, NT_V = 115, NT_LIST = 116;

    private const int ACT_ASSIGN = -14, ACT_WRITE = -12, ACT_READ = -11, ACT_ADD = -20, ACT_SUB = -21, 
                      ACT_MUL = -22, ACT_DIV = -23, ACT_LT = -24, ACT_GT = -25, ACT_EQ = -26, ACT_NE = -27, 
                      ACT_LE = -28, ACT_GE = -29, ACT_INDEX = -30, ACT_NEG = -31, ACT_WHILE_START = -32, 
                      ACT_WHILE_JF = -33, ACT_WHILE_END = -34, ACT_IF_JF = -35, ACT_IF_ELSE = -36, ACT_IF_END = -37;

    private readonly int[,] parsingTable = new int[17, 100]; // Увеличили размер для запаса по ID
    private Stack<int> whileStartStack = new Stack<int>(), whileJfStack = new Stack<int>(), ifLabelStack = new Stack<int>();

    public Parser() { InitializeParsingTable(); }

    private void InitializeParsingTable()
    {
        for (int i = 0; i < 17; i++) for (int j = 0; j < 100; j++) parsingTable[i, j] = -1;

        // Программа и объявления
        parsingTable[NT_P - 100, 1] = 1;  // int
        parsingTable[NT_P - 100, 2] = 2;  // int1
        parsingTable[NT_P - 100, 30] = 3; // begin

        parsingTable[NT_R - 100, 10] = 4; // id
        parsingTable[NT_R - 100, 1] = 0; parsingTable[NT_R - 100, 2] = 0; parsingTable[NT_R - 100, 30] = 0;

        parsingTable[NT_L - 100, 10] = 6; // id
        parsingTable[NT_L - 100, 1] = 0; parsingTable[NT_L - 100, 2] = 0; parsingTable[NT_L - 100, 30] = 0;

        // Список операторов
        foreach (int id in new[] { 10, 3, 6, 8, 9, 30 }) parsingTable[NT_LIST - 100, id] = 43;
        parsingTable[NT_LIST - 100, 31] = 0; // end
        parsingTable[NT_LIST - 100, 5] = 0;  // else

        // Операторы
        parsingTable[NT_A - 100, 10] = 8; parsingTable[NT_A - 100, 3] = 9; parsingTable[NT_A - 100, 6] = 10;
        parsingTable[NT_A - 100, 8] = 11; parsingTable[NT_A - 100, 9] = 12; parsingTable[NT_A - 100, 30] = 42;
        
        parsingTable[NT_X - 100, 22] = 14; // =
        parsingTable[NT_X - 100, 17] = 15; // [

        parsingTable[NT_Y - 100, 10] = 16;
        parsingTable[NT_Y1 - 100, 17] = 17; parsingTable[NT_Y1 - 100, 16] = 0;

        // Ветка Else
        foreach (int id in new[] { 19, 10, 3, 6, 8, 9, 31, 5 }) parsingTable[NT_B - 100, id] = 0;
        parsingTable[NT_B - 100, 5] = 19; 

        // Выражения
        int[] exprStarts = { 10, 29, 15, 12 };
        foreach (int id in exprStarts) {
            parsingTable[NT_S - 100, id] = 21; 
            parsingTable[NT_T - 100, id] = 22; 
            parsingTable[NT_V - 100, id] = 41;
        }

        // Хвосты выражений (Epsilon переходы)
        int[] follows = { 16, 18, 19, 23, 24, 25, 26, 27, 28, 4, 7, 5, 31 };
        foreach (int id in follows) {
            parsingTable[NT_U - 100, id] = 0;
            parsingTable[NT_W - 100, id] = 0;
            parsingTable[NT_F1 - 100, id] = 0;
        }
        // Специально для термов (W) - перед сложением идет умножение
        parsingTable[NT_W - 100, 11] = 0; // +
        parsingTable[NT_W - 100, 12] = 0; // -
        // Специально для F1 - после id может идти + или -
        parsingTable[NT_F1 - 100, 11] = 0;
        parsingTable[NT_F1 - 100, 12] = 0;

        parsingTable[NT_U - 100, 11] = 23; parsingTable[NT_U - 100, 12] = 24;
        parsingTable[NT_W - 100, 13] = 26; parsingTable[NT_W - 100, 14] = 27;

        parsingTable[NT_F - 100, 15] = 29; parsingTable[NT_F - 100, 10] = 30; 
        parsingTable[NT_F - 100, 29] = 31; parsingTable[NT_F - 100, 12] = 32;
        parsingTable[NT_F1 - 100, 17] = 33;
        
        parsingTable[NT_O - 100, 23] = 35; parsingTable[NT_O - 100, 24] = 36; 
        parsingTable[NT_O - 100, 25] = 37; parsingTable[NT_O - 100, 28] = 38; 
        parsingTable[NT_O - 100, 26] = 39; parsingTable[NT_O - 100, 27] = 40;
    }

    public bool Parse(List<Token> tokens, out List<string> rpn)
    {
        rpn = new List<string>();
        Stack<int> gStack = new Stack<int>();
        gStack.Push(NT_P);
        int tIdx = 0;

        while (gStack.Count > 0)
        {
            int top = gStack.Pop();
            if (top < 0) { ProcessAction(top, rpn); continue; }

            Token curr = tIdx < tokens.Count ? tokens[tIdx] : new Token(99, "EOF", 0, 0);
            
            if (top < 100)
            {
                if (top == curr.Id)
                {
                    if (curr.Id == 30 && rpn.Count == 0) rpn.Add("bp");
                    if (curr.Id == 10 || curr.Id == 29) rpn.Add(curr.Value);
                    tIdx++;
                }
                else
                {
                    Console.WriteLine($"[Error] Ожидался токен {top}, но найден {curr.Value} (ID:{curr.Id}) на строке {curr.Line}");
                    return false;
                }
            }
            else
            {
                int rule = parsingTable[top - 100, curr.Id];
                if (rule == -1)
                {
                    Console.WriteLine($"[Error] Нет правила для нетерминала {top} и токена {curr.Value} (ID:{curr.Id}) на строке {curr.Line}");
                    return false;
                }
                if (rule != 0) PushRule(rule, gStack);
            }
        }
        rpn.Add("ret");
        Console.WriteLine("[Parser]: Успешно. ОПС сгенерирована.");
        Console.WriteLine($"ОПС: {string.Join(" ", rpn)}");
        return true;
    }

    private void ProcessAction(int action, List<string> rpn)
    {
        switch (action)
        {
            case ACT_ASSIGN: rpn.Add("="); break;
            case ACT_WRITE: rpn.Add("w"); break;
            case ACT_ADD: rpn.Add("+"); break;
            case ACT_SUB: rpn.Add("-"); break;
            case ACT_MUL: rpn.Add("*"); break;
            case ACT_DIV: rpn.Add("/"); break;
            case ACT_LT: rpn.Add("<"); break;
            case ACT_GT: rpn.Add(">"); break;
            case ACT_EQ: rpn.Add("=="); break;
            case ACT_NE: rpn.Add("!="); break;
            case ACT_LE: rpn.Add("<="); break;
            case ACT_GE: rpn.Add(">="); break;
            case ACT_INDEX: rpn.Add("index"); break;
            case ACT_NEG: rpn.Add("neg"); break;
            case ACT_WHILE_START: whileStartStack.Push(rpn.Count); break;
            case ACT_WHILE_JF: whileJfStack.Push(rpn.Count); rpn.Add("0"); rpn.Add("jf"); break;
            case ACT_WHILE_END: 
                int start = whileStartStack.Pop(), jf = whileJfStack.Pop();
                rpn.Add(start.ToString()); rpn.Add("УП"); rpn[jf] = rpn.Count.ToString(); break;
            case ACT_IF_JF: ifLabelStack.Push(rpn.Count); rpn.Add("0"); rpn.Add("jf"); break;
            case ACT_IF_ELSE:
                int falsePtr = ifLabelStack.Pop();
                ifLabelStack.Push(rpn.Count); rpn.Add("0"); rpn.Add("УП");
                rpn[falsePtr] = rpn.Count.ToString(); break;
            case ACT_IF_END:
                if (ifLabelStack.Count > 0) rpn[ifLabelStack.Pop()] = rpn.Count.ToString(); break;
        }
    }

    private void PushRule(int id, Stack<int> s)
    {
        switch (id)
        {
            case 1: s.Push(NT_P); s.Push(NT_R); s.Push(1); break;
            case 2: s.Push(NT_P); s.Push(NT_L); s.Push(2); break;
            case 3: s.Push(31); s.Push(NT_LIST); s.Push(30); break;
            case 4: s.Push(NT_R); s.Push(19); s.Push(10); break;
            case 6: s.Push(NT_L); s.Push(19); s.Push(18); s.Push(29); s.Push(17); s.Push(10); break;
            case 8: s.Push(19); s.Push(NT_X); s.Push(10); break;
            case 9: s.Push(ACT_IF_END); s.Push(NT_B); s.Push(NT_A); s.Push(4); s.Push(ACT_IF_JF); s.Push(16); s.Push(NT_V); s.Push(15); s.Push(3); break;
            case 10: s.Push(ACT_WHILE_END); s.Push(NT_A); s.Push(7); s.Push(ACT_WHILE_JF); s.Push(16); s.Push(NT_V); s.Push(15); s.Push(ACT_WHILE_START); s.Push(6); break;
            case 11: s.Push(19); s.Push(ACT_READ); s.Push(16); s.Push(NT_Y); s.Push(15); s.Push(8); break;
            case 12: s.Push(19); s.Push(ACT_WRITE); s.Push(16); s.Push(NT_S); s.Push(15); s.Push(9); break;
            case 14: s.Push(ACT_ASSIGN); s.Push(NT_S); s.Push(22); break;
            case 15: s.Push(ACT_ASSIGN); s.Push(NT_S); s.Push(22); s.Push(18); s.Push(ACT_INDEX); s.Push(NT_S); s.Push(17); break;
            case 16: s.Push(NT_Y1); s.Push(10); break;
            case 17: s.Push(18); s.Push(ACT_INDEX); s.Push(NT_S); s.Push(17); break;
            case 19: s.Push(NT_A); s.Push(ACT_IF_ELSE); s.Push(5); break;
            case 21: s.Push(NT_U); s.Push(NT_T); break;
            case 22: s.Push(NT_W); s.Push(NT_F); break;
            case 23: s.Push(NT_U); s.Push(ACT_ADD); s.Push(NT_T); s.Push(11); break;
            case 24: s.Push(NT_U); s.Push(ACT_SUB); s.Push(NT_T); s.Push(12); break;
            case 26: s.Push(NT_W); s.Push(ACT_MUL); s.Push(NT_F); s.Push(13); break;
            case 27: s.Push(NT_W); s.Push(ACT_DIV); s.Push(NT_F); s.Push(14); break;
            case 29: s.Push(16); s.Push(NT_S); s.Push(15); break;
            case 30: s.Push(NT_F1); s.Push(10); break;
            case 31: s.Push(29); break;
            case 32: s.Push(ACT_NEG); s.Push(NT_F); s.Push(12); break;
            case 33: s.Push(18); s.Push(ACT_INDEX); s.Push(NT_S); s.Push(17); break;
            case 35: s.Push(ACT_LT); s.Push(NT_S); s.Push(23); break;
            case 36: s.Push(ACT_GT); s.Push(NT_S); s.Push(24); break;
            case 37: s.Push(ACT_EQ); s.Push(NT_S); s.Push(25); break;
            case 38: s.Push(ACT_NE); s.Push(NT_S); s.Push(28); break;
            case 39: s.Push(ACT_LE); s.Push(NT_S); s.Push(26); break;
            case 40: s.Push(ACT_GE); s.Push(NT_S); s.Push(27); break;
            case 41: s.Push(NT_O); s.Push(NT_S); break;
            case 42: s.Push(31); s.Push(NT_LIST); s.Push(30); break;
            case 43: s.Push(NT_LIST); s.Push(NT_A); break;
        }
    }
}