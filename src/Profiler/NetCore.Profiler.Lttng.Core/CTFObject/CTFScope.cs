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

    public class CTFSymbol
    {

        public string Name { get; private set; }
        public CTFType CtfType { get; private set; }

        public CTFSymbol(string name, CTFType type)
        {
            this.Name = name;
            CtfType = type;
        }

    }

    public class CTFScope
    {
        private const string typePrefix = "$T";
        private const string structPrefix = "$S";

        private Dictionary<string, CTFSymbol> symbols;

        public CTFScope()
        {
            symbols = new Dictionary<string, CTFSymbol>();
        }

        private CTFSymbol FindSymbol(string name)
        {
            CTFSymbol result = null;
            symbols.TryGetValue(name, out result);
            return result;
        }

        public CTFSymbol FindTypeSymbol(string name)
        {
            return FindSymbol(typePrefix + name);
        }

        internal void AddType(CTFSymbol symbol)
        {
            symbols.Add(typePrefix + symbol.Name, symbol);
        }

        internal CTFSymbol FindStructSymbol(string name)
        {
            return FindSymbol(structPrefix + name);
        }

        internal void AddStruct(CTFSymbol symbol)
        {
            symbols.Add(structPrefix + symbol.Name, symbol);
        }
    }
}
