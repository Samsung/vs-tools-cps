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

namespace NetCore.Profiler.Cperf.Core.Parser.Model
{
    /// <summary>
    /// A <see cref="CperfParser"/> data model class for stack trace sample ("sam str") %Core %Profiler
    /// trace log records. This record describes stack changes occurred from the last known stack state.
    /// </summary>
    public class StackSample
    {
        public ulong InternalId { get; set; }

        public ulong Ticks { get; set; }

        public ulong Count { get; set; }

        public int MatchPrefixSize { get; set; }

        public int StackSize { get; set; }

        public ulong? Ip { get; set; }

        public List<StackSampleFrame> Frames { get; set; } = new List<StackSampleFrame>();

        public StackSample(ulong internalId, ulong ticks, ulong count, int matchPrefixSize, int stackSize, ulong? ip)
        {
            InternalId = internalId;
            Ticks = ticks;
            Count = count;
            MatchPrefixSize = matchPrefixSize;
            StackSize = stackSize;
            Ip = ip;
        }
    }
}
