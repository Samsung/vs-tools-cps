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
    public class CTFEnumElem
    {
        public string Name { get; private set; }
        public int First { get; private set; }        
        public int Second { get; private set; }

        private CTFEnumElem(string name, int first)
        {
            this.Name = name;
            this.First = first;
            Second = first;
        }

        internal static CTFEnumElem Parse(TokParser tp)
        {
            string name = tp.GetIden();
            tp.MustBe(Token.EnumId.ASSIGNMENT);
            CTFUnaryExpression first = CTFUnaryExpression.Parse(tp);
            CTFEnumElem ce = new CTFEnumElem(name, first.Calculate());

            if (tp.Match(Token.EnumId.ELIPSES))
            {
                ce.Second = CTFUnaryExpression.Parse(tp).Calculate();
            }

            return ce;
        }
    }

    internal class CTFEnumSpecifier : CTFITypeSpecifier
    {
        public List<CTFTypeSpecifier> Cds { get; private set; }
        public List<CTFEnumElem> List { get; private set; }

        private CTFEnumSpecifier(Token.EnumId id, List<CTFTypeSpecifier> cds, List<CTFEnumElem> list) : base(id)
        {
            this.Cds = cds;
            this.List = list;
        }

        internal static CTFITypeSpecifier Parse(CTFScope scope, TokParser tp)
        {
            if (!tp.Match(Token.EnumId.ENUM))
            {
                return null;
            }

            tp.MustBe(Token.EnumId.COLON);

            List<CTFTypeSpecifier> cds = ParseList(scope, tp);

            tp.MustBe(Token.EnumId.LCURL);
            
            List<CTFEnumElem> list = new List<CTFEnumElem>();
            do
            {
                CTFEnumElem ce = CTFEnumElem.Parse(tp);
                list.Add(ce);
            }
            while (tp.Match(Token.EnumId.SEPARATOR));

            tp.MustBe(Token.EnumId.RCURL);

            return new CTFEnumSpecifier(Token.EnumId.ENUM, cds, list);
        }

        public override CTFType GetType(CTFScope scope)
        {
            return new CTFEnumType(scope, this);
        }
    }
}