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
    public class CTFPrimaryExpression
    {
        private Token t;

        private CTFPrimaryExpression(Token t)
        {
            this.t = t;
        }

        internal static CTFPrimaryExpression Parse(TokParser tp)
        {
            Token t = tp.Token;
            switch (tp.Token.Id)
            {   // All this values can be used in expression
                case Token.EnumId.IDEN:
                case Token.EnumId.ALIGN:
                case Token.EnumId.SIGNED:
                case Token.EnumId.CLOCK:
                case Token.EnumId.EVENT:
                case Token.EnumId.INT_LITERAL:
                case Token.EnumId.STR_LITERAL:
                    tp.Next();
                    return new CTFPrimaryExpression(t);
            }

            return null;
        }

        internal string GetName()
        {
            if (t.Id == Token.EnumId.INT_LITERAL || t.Id == Token.EnumId.STR_LITERAL)
            {
                throw new CTFException();
            }

            return t.Buffer;
        }

        internal ulong GetULong()
        {
            if (t.Id != Token.EnumId.INT_LITERAL)
            {
                throw new CTFException();
            }

            // Convert
            string value = t.Buffer;
            if (value[0] == '0' && value.Length > 1)
            {
                if (value[1] == 'x' || value[1] == 'X')
                {
                    return Convert.ToUInt64(value.Substring(2), 16);
                }

                return Convert.ToUInt64(value, 8);
            }
            else // digital
            {
                return ulong.Parse(value);
            }
        }

        internal bool IsNumber()
        {
            return t.Id == Token.EnumId.INT_LITERAL;
        }

        internal int Calculate()
        {
            if (t.Id != Token.EnumId.INT_LITERAL)
            {
                throw new CTFException();
            }
            // Convert
            string value = t.Buffer;
            if (value[0] == '0' && value.Length > 1)
            {
                if (value[1] == 'x' || value[1] == 'X')
                {
                    return Convert.ToInt32(value.Substring(2), 16);
                }

                return Convert.ToInt32(value, 8);
            }
            else // digital
            {
                return int.Parse(value);
            }
        }

        internal string GetString()
        {
            return t.Buffer;
        }
    }
}