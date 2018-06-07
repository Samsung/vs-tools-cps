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
    public class CTFUnaryExpression
    {
        /* Only the following form of unary-expression is supported
         * unary-expression:
         *  postfix-expresion
         *  
         *  unary-operator is not supported
         *  
         *  
         */
        private CTFPrimaryExpression cpe;
        private CTFUnaryExpression cue; // array size
        private string element;

        private CTFUnaryExpression(CTFPrimaryExpression cpe)
        {
            this.cpe = cpe;
            element = "";
        }

        internal string GetFullName()
        {
            if (cue != null)
            {
                throw new CTFException();
            }

            return cpe.GetName() + element;
        }

        internal ulong GetULong()
        {
            if (element != "" || cue != null)
            {
                throw new CTFException();
            }

            return cpe.GetULong();
        }

        internal bool IsNumber()
        {
            if (element != "" || cue != null)
            {
                return false;
            }

            return cpe.IsNumber();
        }

        internal string GetName()
        {
            if (element != "" || cue != null)
            {
                throw new CTFException();
            }

            return cpe.GetName();
        }

        internal CTFPrimaryExpression GetValue()
        {
            if (element != "" || cue != null)
            {
                throw new CTFException();
            }

            return cpe;
        }

        internal int Calculate()
        {
            if (element != "" || cue != null)
            {
                throw new CTFException();
            }

            return cpe.Calculate();
        }

        internal static CTFUnaryExpression Parse(TokParser tp)
        {
            CTFPrimaryExpression cpe = CTFPrimaryExpression.Parse(tp);
            if (cpe == null)
            {
                return null;
            }

            CTFUnaryExpression cp = new CTFUnaryExpression(cpe);
            while (tp.Match(Token.EnumId.DOT))
            {
                cp.element = cp.element + "." + tp.GetIden();
            }

            if (tp.Match(Token.EnumId.OPENBRAC))
            {
                CTFUnaryExpression cue = Parse(tp);
                if (cue == null)
                {
                    throw new CTFException();
                }

                tp.MustBe(Token.EnumId.CLOSEBRAC);
 
                cp.cue = cue;
            }

            return cp;
        }
    }
}