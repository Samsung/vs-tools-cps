/*
 * Copyright 2017 (c) Samsung Electronics Co., Ltd  All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * 	http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

namespace NetCore.Profiler.Lttng.Core.CTFObject
{
    class Flex
    {
        private enum Classes
        {
            EOF = -1,
            Unknown,
            Whitespace,
            MayStartId,
            Digit,
            Zero
        };

        public int lc;
        public string Buffer { get; private set; }
        private static Classes[] Table;
        private static bool[] Iden;
        CTFMetaReader Stream;

        public Flex(CTFMetaReader stream)
        {
            this.Stream = stream;
            lc = stream.Read();
        }

        static Flex()
        {
            Table = new Classes[256];
            Iden = new bool[256];
            for (int i = 'a'; i <= 'z'; i++)
            {
                Table[i] = Classes.MayStartId;
                Iden[i] = true;
            }

            for (int i = 'A'; i <= 'Z'; i++)
            {
                Table[i] = Classes.MayStartId;
                Iden[i] = true;
            }

            Table['_'] = Classes.MayStartId; Iden['_'] = true;

            for (int i = '0'; i <= '9'; i++)
            {
                Table[i] = Classes.Digit;
                Iden[i] = true;
            }

            Table[' '] = Classes.Whitespace;
            Table['\t'] = Classes.Whitespace;
            Table['\r'] = Classes.Whitespace;
            Table['\n'] = Classes.Whitespace;
            Table['0'] = Classes.Zero;
        }

        private static bool IsIden(int c)
        {
            if (c < 0 || c >= 128)
            {
                return false;
            }

            return Iden[c];
        }


        private static bool IsHex(int c)
        {
            return ((c >= '0') && (c <= '9')) || ((c >= 'a') && (c <= 'f')) || ((c >= 'A') && (c <= 'F'));
        }

        private Classes GetClass(int c)
        {
            if (c == -1)
            {
                return Classes.EOF;
            }

            if (c >= 0 && c < 256)
            {
                return Table[c];
            }

            return Classes.Unknown;
        }

        private void Collect()
        {
            Buffer += (char)lc;
            lc = Stream.Read();
        }

        private void Skip()
        {
            lc = Stream.Read();
        }

        public Token CollectNext()
        {
            Buffer = "";
            while (true)
            {
                switch (GetClass(lc))
                {
                    case Classes.EOF:
                        return new Token(Token.EnumId.EOF, Buffer); ;
                    case Classes.Whitespace:
                        // Skip all whitespaces
                        do
                        {
                            Skip();
                        }
                        while (GetClass(lc) == Classes.Whitespace);

                        break;
                    case Classes.MayStartId:
                        // Try to collect identifier or keyword
                        do
                        {
                            Collect();
                        }
                        while (IsIden(lc));

                        return new Token(Buffer);
                    case Classes.Zero: // octal or hexadecimal
                        Collect();
                        if (lc == 'x' || lc == 'X')
                        {
                            // must be hexadecimal
                            Collect();
                            if (!IsHex(lc))
                            {
                                return null; // Something wrong
                            }

                            do
                            {
                                Collect();
                            }
                            while (IsHex(lc));

                            return new Token(Token.EnumId.INT_LITERAL, Buffer);
                        }
                        else
                        {
                            // must be octal
                            while ((lc >= '0') && (lc <= '7'))
                            {
                                Collect();
                            }

                            return new Token(Token.EnumId.INT_LITERAL, Buffer);
                        }

                    case Classes.Digit:
                        do
                        {
                            Collect();
                        }
                        while (lc >= '0' && lc <= '9');

                        return new Token(Token.EnumId.INT_LITERAL, Buffer);
                    default:
                        switch (lc)
                        {
                            case ',': Skip();  return new Token(Token.EnumId.SEPARATOR, ",");
                            case '.':
                                Skip();
                                if (lc == '.')
                                {
                                    Skip();
                                    if (lc != '.')
                                    {
                                        return null;
                                    }

                                    Skip();
                                    return new Token(Token.EnumId.ELIPSES, "...");
                                }

                                return new Token(Token.EnumId.DOT, "...");
                            case '=': Collect(); return new Token(Token.EnumId.ASSIGNMENT, Buffer);
                            case ':':
                                Skip();
                                if (lc != '=')
                                {
                                    return new Token(Token.EnumId.COLON, ":");
                                }

                                Skip();
                                return new Token(Token.EnumId.TYPE_ASSIGNMENT, ":=");
                            case '<': Collect(); return new Token(Token.EnumId.LT, Buffer);
                            case '>': Collect(); return new Token(Token.EnumId.GT, Buffer);
                            case '[': Collect(); return new Token(Token.EnumId.OPENBRAC, Buffer);
                            case ']': Collect(); return new Token(Token.EnumId.CLOSEBRAC, Buffer);
                            case '(': Collect(); return new Token(Token.EnumId.LPAREN, Buffer);
                            case ')': Collect(); return new Token(Token.EnumId.RPAREN, Buffer);
                            case '{': Collect(); return new Token(Token.EnumId.LCURL, Buffer);
                            case '}': Collect(); return new Token(Token.EnumId.RCURL, Buffer);
                            case ';': Collect(); return new Token(Token.EnumId.TERM, Buffer);
                            case '*': //collect(); return new Token(Token.id.POINTER, buffer);
                            case '+': //collect(); return new Token(Token.id.SIGN, buffer);
                            case '-':
                                throw new CTFException(); // Unsupported
                            case '/': // It must be comment only !
                                Skip();
                                if (lc != '*')
                                {
                                    return null; // must be exception
                                }

                                Skip();
                                for (;;)
                                {
                                    if (lc == -1)
                                    {
                                        return null;
                                    }

                                    int pc = lc;
                                    Skip();
                                    if (pc != '*')
                                    {
                                        continue;
                                    }

                                    if (lc != '/')
                                    {
                                        continue;
                                    }

                                    Skip();
                                    break;
                                }

                                continue;
                            case '"':
                                for (;;)
                                {
                                    Collect();
                                    if (lc == -1)
                                    {
                                        return null;
                                    }

                                    if (lc == '"')
                                    {
                                        break;
                                    }
                                }

                                Collect();
                                return new Token(Token.EnumId.STR_LITERAL, Buffer);
                            default:
                                return null;
                        }
                        //return null; // Really add exceptions
                }
            }
        }
    }
}
