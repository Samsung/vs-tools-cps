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
    internal class CTFVaraintSpecifier : CTFTypeSpecifier
    {

        public List<CTFStructOrVariantDeclaration> List { get; private set; }
        public CTFUnaryExpression Cue { get; protected set; }

        private CTFVaraintSpecifier(Token.EnumId id, CTFUnaryExpression cue, List<CTFStructOrVariantDeclaration> list) 
        {
            this.Cue = cue;
            this.List = list;
        }

        internal static CTFTypeSpecifier Parse(CTFScope scope, TokParser tp)
        {
            tp.MustBe(Token.EnumId.VARIANT);
            tp.MustBe(Token.EnumId.LT);
            CTFUnaryExpression cue = CTFUnaryExpression.Parse(tp);
            if (cue == null)
            {
                throw new CTFException();
            }

            tp.MustBe(Token.EnumId.GT);
            tp.MustBe(Token.EnumId.LCURL);
            List<CTFStructOrVariantDeclaration> list = CTFStructOrVariantDeclaration.ParseList(scope, tp);
            tp.MustBe(Token.EnumId.RCURL);
            return new CTFVaraintSpecifier(Token.EnumId.VARIANT, cue, list);
        }

        public override CTFType GetType(CTFScope scope)
        {
            return new CTFVariantType(scope, this);
        }
    }
}