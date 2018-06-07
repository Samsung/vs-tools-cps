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

using System.Collections.Generic;

namespace NetCore.Profiler.Lttng.Core.CTFObject
{
    internal class CTFStructSpecifier : CTFTypeSpecifier
    {

        private CTFType ct;

        public CTFStructSpecifier(CTFType cftType)
        {
            ct = cftType;
        }

        internal static CTFTypeSpecifier Parse(CTFScope scope, TokParser tp)
        {
            string name = null;
            List<CTFStructOrVariantDeclaration> list = null;
            if (!tp.Match(Token.EnumId.STRUCT))
            {
                return null;
            }

            if (tp.Token.Id == Token.EnumId.IDEN)
            {
                name = tp.Token.Buffer;
                tp.Next();
            }

            if (tp.Match(Token.EnumId.LCURL))
            {
                list = CTFStructOrVariantDeclaration.ParseList(scope, tp);
                tp.MustBe(Token.EnumId.RCURL);
            }

            int align = 0;
            if (tp.Match(Token.EnumId.ALIGN))
            {
                tp.MustBe(Token.EnumId.LPAREN);
                CTFUnaryExpression cue = CTFUnaryExpression.Parse(tp);
                tp.MustBe(Token.EnumId.RPAREN);
                align = cue.Calculate();
            }

            CTFType ct = CTFType.GetStructType(scope, name, list, align);
            return new CTFStructSpecifier(ct);
        }

        public override CTFType GetType(CTFScope scope) => ct;

    }
}