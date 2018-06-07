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
    public class CTFAssignmentExpression
    {
        private CTFUnaryExpression Dst;
        public CTFUnaryExpression Src { get; private set; }
        private CTFTypeSpecifier Cts;

        private CTFAssignmentExpression(CTFUnaryExpression cue, CTFUnaryExpression cue2)
        {
            Dst = cue;
            Src = cue2;
        }

        private CTFAssignmentExpression(CTFUnaryExpression cue, CTFTypeSpecifier cts)
        {
            Dst = cue;
            this.Cts = cts;
        }

        internal string GetName() => Dst.GetName();
        internal string GetFullName() => Dst.GetFullName();

        public CTFType GetType(CTFScope scope) => Cts.GetType(scope);

        internal static List<CTFAssignmentExpression> ParseList(CTFScope scope, TokParser tp)
        {
            List<CTFAssignmentExpression> cael = new List<CTFAssignmentExpression>();
            while (true)
            {
                CTFAssignmentExpression cae = Parse(scope, tp);
                if (cae == null)
                {
                    break;
                }

                tp.MustBe(Token.EnumId.TERM);
                cael.Add(cae);
            }

            if (cael.Count == 0)
            {
                return null;
            }

            return cael;
        }

        private static CTFAssignmentExpression Parse(CTFScope scope, TokParser tp)
        {
            CTFUnaryExpression cue = CTFUnaryExpression.Parse(tp);
            if (cue != null)
            {
                if (tp.Match(Token.EnumId.ASSIGNMENT))
                {
                    CTFUnaryExpression cue2 = CTFUnaryExpression.Parse(tp);
                    if (cue2 == null)
                    {
                        throw new CTFException();
                    }

                    return new CTFAssignmentExpression(cue, cue2);
                }
           
                if (tp.Match(Token.EnumId.TYPE_ASSIGNMENT))
                {
                    CTFTypeSpecifier cs = CTFITypeSpecifier.ParseTypeSpecifier(scope, tp);
                    if (cs == null)
                    {
                        throw new CTFException();
                    }

                    return new CTFAssignmentExpression(cue, cs);
                }
            }

            return null;
        }
    }
}