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
    public class CTFTypeSpecifier
    {
        public virtual CTFType GetType(CTFScope scope)
        {
            throw new CTFException();
        }
    }

    public class CTFITypeSpecifier : CTFTypeSpecifier
    {

        static Dictionary<Token.EnumId, CTFTypeSpecifier> cache;      
        
        public Token.EnumId Id { get; private set; }

        public CTFITypeSpecifier(Token.EnumId id)
        {
            this.Id = id;
        }

        public static void Init()
        {
            cache = new Dictionary<Token.EnumId, CTFTypeSpecifier>();
            cache.Add(Token.EnumId.STRING, new CTFSTypeSpecifier());
        }

        public static CTFTypeSpecifier Get(Token.EnumId id)
        {
            CTFTypeSpecifier value;
            cache.TryGetValue(id, out value);
            if (value == null)
            {
                value = new CTFITypeSpecifier(id);
                cache.Add(id, value);
            }

            return value;
        }


        internal static CTFTypeSpecifier ParseTypeSpecifier(CTFScope scope, TokParser tp)
        {
            Token.EnumId id = tp.Token.Id;
            switch (id)
            {
                case Token.EnumId.VOID:
                case Token.EnumId.CHAR:
                case Token.EnumId.SHORT:
                case Token.EnumId.INT:
                case Token.EnumId.FLOAT:
                case Token.EnumId.DOUBLE:
                case Token.EnumId.SIGNED:
                case Token.EnumId.BOOL:
                case Token.EnumId.COMPLEX:
                case Token.EnumId.IMAGINARY:
                    throw new CTFException(); // unsupported

                case Token.EnumId.LONG:
                case Token.EnumId.UNSIGNED:
                    tp.Next();
                    return Get(id);
                case Token.EnumId.STRUCT:
                    return CTFStructSpecifier.Parse(scope, tp);
                case Token.EnumId.VARIANT:
                    return CTFVaraintSpecifier.Parse(scope, tp);
                case Token.EnumId.ENUM:
                    return CTFEnumSpecifier.Parse(scope, tp);
                // ctf-type-specifier
                case Token.EnumId.FLOATING_POINT:
                case Token.EnumId.INTEGER:
                    tp.Next();
                    tp.MustBe(Token.EnumId.LCURL);
                    List<CTFAssignmentExpression> cael = CTFAssignmentExpression.ParseList(scope, tp);
                    tp.MustBe(Token.EnumId.RCURL);
                    return new CTFATypeSpecifier(id, cael);
                case Token.EnumId.STRING:
                    tp.Next();
                    return Get(Token.EnumId.STRING);
                case Token.EnumId.IDEN: // must be a type in a scope
                    CTFTypeSpecifier ct = CTFNTypeSpecifier.Get(scope, tp.Token.Buffer);
                    if (ct == null)
                    {
                        return null;
                    }

                    tp.Next();
                    return ct;
            }

            return null;
        }

        internal string GetName()
        {
            switch (Id)
            {
                case Token.EnumId.UNSIGNED:
                    return "unsigned";
                case Token.EnumId.LONG:
                    return "long";
            }

            throw new CTFException();
        }

        internal static List<CTFTypeSpecifier> ParseList(CTFScope scope, TokParser tp)
        {
            List<CTFTypeSpecifier> specifiers = new List<CTFTypeSpecifier>();
            for (;;)
            {
                CTFTypeSpecifier cts = ParseTypeSpecifier(scope, tp);
                if (cts == null)
                {
                    break;
                }

                specifiers.Add(cts);
            }

            if (specifiers.Count == 0)
            {
                return null;
            }

            return specifiers;
        }
    }

    public class CTFSTypeSpecifier : CTFTypeSpecifier
    {
        public CTFSTypeSpecifier()
        {
        }

        public override CTFType GetType(CTFScope scope) => CTFType.StringType;
    }

    public class CTFNTypeSpecifier : CTFTypeSpecifier
    {
        private string name;
        private CTFType ct;

        public CTFNTypeSpecifier(string name, CTFType ct)
        {
            this.name = name;
            this.ct = ct;
        }

        public static CTFNTypeSpecifier Get(CTFScope scope, string name)
        {
            CTFSymbol s = scope.FindTypeSymbol(name);
            if (s == null)
            {
                return null;
            }

            return new CTFNTypeSpecifier(name, s.CtfType);
        }

        public override CTFType GetType(CTFScope scope) => ct;

    }

    public class CTFATypeSpecifier : CTFITypeSpecifier
    {
        private List<CTFAssignmentExpression> cael;

        public CTFATypeSpecifier(Token.EnumId id, List<CTFAssignmentExpression> cael) : base(id)
        {
            this.cael = cael;
        }

        public override CTFType GetType(CTFScope scope)
        {
            switch (Id)
            {
                case Token.EnumId.INTEGER:
                    {
                        return new CTFIntType(cael);
                    }

                case Token.EnumId.FLOATING_POINT:
                    {
                        return new CTFFloatType(cael);
                    }
            }

            throw new CTFException();
        }
    }
}