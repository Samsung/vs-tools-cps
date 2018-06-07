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
using NetCore.Profiler.Cperf.Core.Model;

namespace NetCore.Profiler.Cperf.LogAdaptor.Core
{
    internal class StackSample
    {

        public int StackSize { get; }

        public int MatchPrefixSize { get; }

        public ulong Samples { get; }

        public ulong ThreadId { get; }

        public ulong Timestamp { get; }

        public ulong? Pc { get; set; }

        public ulong ParentFunctionIntId { get; set; }

        public List<FunctionCall> FunctionCalls { get; } = new List<FunctionCall>();

        public StackSample(ulong samples, int stackSize, int matchPrefixSize, ulong threadId, ulong timestamp, ulong? pc)
        {
            Samples = samples;
            StackSize = stackSize;
            MatchPrefixSize = matchPrefixSize;
            ThreadId = threadId;
            Timestamp = timestamp;
            Pc = pc;
        }
    }
}
