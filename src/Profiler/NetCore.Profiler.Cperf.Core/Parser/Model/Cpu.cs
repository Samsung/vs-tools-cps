﻿/*
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

namespace NetCore.Profiler.Cperf.Core.Parser.Model
{
    /// <summary>
    /// A <see cref="CperfParser"/> data model class for CPU usage dump ("prc cpu") %Core %Profiler
    /// trace log records.
    /// </summary>
    public class Cpu
    {
        /// <summary>
        /// The event timestamp in milliseconds from the start of profiling.
        /// </summary>
        public ulong Timestamp { get; }

        /// <summary>
        /// The CPU usage time in microseconds from a last timestamp.
        /// </summary>
        public ulong Duration { get; }

        public Cpu(ulong timestamp, ulong duration)
        {
            Timestamp = timestamp;
            Duration = duration;
        }
    }
}
