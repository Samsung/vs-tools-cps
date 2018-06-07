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
    public class CTFStructOrVariantDeclaration
    {
        public CTFDeclarator Cd { get; private set; }
        public List<CTFTypeSpecifier> List { get; private set; }

        private CTFStructOrVariantDeclaration(List<CTFTypeSpecifier> list, CTFDeclarator cd)
        {
            this.List = list;
            this.Cd = cd;
        }

        internal static List<CTFStructOrVariantDeclaration> ParseList(CTFScope scope, TokParser tp)
        {
            List<CTFStructOrVariantDeclaration> list = new List<CTFStructOrVariantDeclaration>();
            for (;;)
            {
                CTFStructOrVariantDeclaration item = Parse(scope, tp);
                if (item == null)
                {
                    break;
                }

                tp.MustBe(Token.EnumId.TERM);
                list.Add(item);
            }

            if (list.Count == 0)
            {
                return null;
            }

            return list;
        }

        private static CTFStructOrVariantDeclaration Parse(CTFScope scope, TokParser tp)
        {
            List<CTFTypeSpecifier> list = new List<CTFTypeSpecifier>();
            for (;;)
            {
                CTFTypeSpecifier s = CTFITypeSpecifier.ParseTypeSpecifier(scope, tp);
                if (s == null)
                {
                    break;
                }

                list.Add(s);
            }

            if (list.Count == 0)
            {
                return null;
            }

            CTFDeclarator cd = CTFDeclarator.Parse(tp);
            return new CTFStructOrVariantDeclaration(list, cd);
        }
    }
}