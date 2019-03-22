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
using System.Linq;
using NetCore.Profiler.Cperf.Core.Model;

namespace NetCore.Profiler.Cperf.LogAdaptor.Core
{
    /// <summary>
    /// A class used in <see cref = "DebugDataInjectionFilter"/> to represent a function.
    /// </summary>
    internal class Function : IIdentifiable
    {
        public const ulong FakeFunctionId = ulong.MaxValue;

        public ulong InternalId { get; set; }

        public List<FunctionNativeCodeInfo> NativeCodeInfos { get; set; } = new List<FunctionNativeCodeInfo>();

        public List<SourceLine> SourceLines;

        public ulong ModuleId { get; set; }

        public ulong Token { get; set; }

        public Function(ulong internalId, ulong moduleId, ulong token)
        {
            InternalId = internalId;
            ModuleId = moduleId;
            Token = token;
        }

        public CilToNativeMapping FindMappingForNativeAddress(ulong address)
        {
            if (NativeCodeInfos.Count < 1)
            {
                return null;
            }

            var offset = address > NativeCodeInfos[0].StartAddress
                ? (uint)(address - NativeCodeInfos[0].StartAddress)
                : 0;

            return NativeCodeInfos[0].CilToNativeMappings.FirstOrDefault(mapping => offset >= mapping.NativeStartOffset && offset <= mapping.NativeEndOffset);
        }
    }
}
