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

using System;

namespace NetCore.Profiler.Lttng.Core.CTFObject
{

    public class Token
    {
        public enum EnumId
        {
            EOF,
            ALIGN,
            CONST,
            CHAR,
            DOUBLE,
            ENUM,
            EVENT,
            FLOATING_POINT,
            FLOAT,
            INTEGER,
            INT,
            LONG,
            SHORT,
            SIGNED,
            STREAM,
            STRING,
            STRUCT,
            TRACE,
            TYPEALIAS,
            TYPEDEF,
            UNSIGNED,
            VARIANT,
            VOID,
            BOOL,
            COMPLEX,
            IMAGINARY,
            ENV,
            CLOCK,
            NAN,
            INF,
            NINF,
            SEPARATOR,
            COLON,
            ELIPSES,
            ASSIGNMENT,
            TYPE_ASSIGNMENT,
            LT,
            GT,
            OPENBRAC,
            CLOSEBRAC,
            LPAREN,
            RPAREN,
            LCURL,
            RCURL,
            TERM,
            POINTER,
            SIGN,
            ARROW,
            DOT,
            INT_LITERAL,
            STR_LITERAL,
            IDEN
        };

        public string Buffer { get; private set; }
        public EnumId Id { get; private set; }

        public Token(string buffer)
        {
            this.Buffer = buffer;
            // Test keywords and so on
            CheckKeyword();
        }

        private void CheckKeyword()
        {
            Id = EnumId.IDEN;
            switch (Buffer[0])
            {
                case 'a':
                    if (Buffer != "align")
                    {
                        break;
                    }

                    Id = EnumId.ALIGN;
                    return;
                case 'c':
                    if (Buffer == "const")
                    {
                        throw new CTFException(); // Unsupported Id = id.CONST;
                    }
                    else if (Buffer == "char")
                    {
                        throw new CTFException(); // Unsupported Id = id.CHAR;
                    }
                    else if (Buffer == "clock")
                    {
                        Id = EnumId.CLOCK;
                    }
                    else
                    {
                        break;
                    }

                    return;
                case 'd':
                    if (Buffer != "double")
                    {
                        break;
                    }

                    throw new CTFException(); // Unsupported Id = id.DOUBLE;
                case 'e':
                    if (Buffer == "enum")
                    {
                        Id = EnumId.ENUM;
                    }
                    else if (Buffer == "event")
                    {
                        Id = EnumId.EVENT;
                    }
                    else if (Buffer == "env")
                    {
                        Id = EnumId.ENV;
                    }
                    else
                    {
                        break;
                    }

                    return;
                case 'f':
                    if (Buffer == "floating_point")
                    {
                        Id = EnumId.FLOATING_POINT;
                    }
                    else if (Buffer == "float")
                    {
                        throw new CTFException(); // Unsupported Id = id.FLOAT;
                    }
                    else
                    {
                        break;
                    }

                    return;
                case 'i':
                    if (Buffer == "integer")
                    {
                        Id = EnumId.INTEGER;
                    }
                    else if (Buffer == "int")
                    {
                        throw new CTFException(); // Unsupported Id = id.INT;
                    }
                    else
                    {
                        break;
                    }

                    return;
                case 'l':
                    if (Buffer != "long")
                    {
                        break;
                    }

                    Id = EnumId.LONG;
                    return;
                case 's':
                    if (Buffer == "short")
                    {
                        Id = EnumId.SHORT;
                    }
                    else if (Buffer == "signed")
                    {
                        Id = EnumId.SIGNED;
                    }
                    else if (Buffer == "stream")
                    {
                        Id = EnumId.STREAM;
                    }
                    else if (Buffer == "string")
                    {
                        Id = EnumId.STRING;
                    }
                    else if (Buffer == "struct")
                    {
                        Id = EnumId.STRUCT;
                    }
                    else
                    {
                        break;
                    }

                    return;
                case 't':
                    if (Buffer == "trace")
                    {
                        Id = EnumId.TRACE;
                    }
                    else if (Buffer == "typealias")
                    {
                        Id = EnumId.TYPEALIAS;
                    }
                    else if (Buffer == "typedef")
                    {
                        throw new CTFException(); // Unsupported Id
                    }
                    else
                    {
                        break;
                    }

                    return;
                case 'u':
                    if (Buffer != "unsigned")
                    {
                        break;
                    }

                    Id = EnumId.UNSIGNED;
                    return;
                case 'v':
                    if (Buffer == "variant")
                    {
                        Id = EnumId.VARIANT;
                    }
                    else if (Buffer == "void")
                    {
                        throw new CTFException(); // Unsupported Id = id.VOID;
                    }
                    else
                    {
                        break;
                    }

                    return;
                case '_':
                    if (Buffer == "_Bool")
                    {
                        throw new CTFException(); // Unsupported Id = id.BOOL;
                    }
                    else if (Buffer == "_Complex")
                    {
                        throw new CTFException(); // Unsupported Id = id.COMPLEX;
                    }
                    else if (Buffer == "_Imaginary")
                    {
                        throw new CTFException(); // Unsupported Id = id.IMAGINARY;
                    }
                    else
                    {
                        break;
                    }

                default:
                    break;
            }
        }

        public Token(EnumId Id, string buffer)
        {
            this.Id = Id; this.Buffer = buffer;
        }

        public void Output()
        {
            Console.WriteLine("Token : " + Buffer);
        }
    }
}
