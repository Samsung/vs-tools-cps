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

using NetCore.Profiler.Common.Helpers;

namespace NetCore.Profiler.Lttng.Core.BObject
{
    public class BJobsPercentage
    {
        public BThread BThread { get; set; }
        public ulong Time { get; set; }
        public ulong Jit { get; set; }
        public ulong GC { get; set; }
        public ulong None { get; set; }

        public double JitPercent => Jit / 10000000.0;

        public double GCPercent => GC / 10000000.0;

        public double NonePercent => (1000000000.0 - Jit - GC) / 10000000.0;

        public string TimeAt => Time.TimeStampToString(0, BThread.GlobalFreq);
    }
}
