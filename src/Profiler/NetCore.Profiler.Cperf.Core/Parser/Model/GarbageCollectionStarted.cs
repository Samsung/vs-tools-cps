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

using System;

namespace NetCore.Profiler.Cperf.Core.Parser.Model
{
    public enum GarbageCollectionReason
    {
        Unspecified,
        Induced
    }

    [Flags]
    public enum GarbageCollectionGenerations
    {
        None = 0,
        Generation0 = 1,
        Generation1 = 2,
        Generation2 = 4,
        LargeObjectHeap = 8
    }

    /// <summary>
    /// A <see cref="CperfParser"/> data model class for garbage collection started ("gch gcs") %Core %Profiler
    /// trace log records. Notifies that a garbage collection operation has started.
    /// </summary>
    public class GarbageCollectionStarted
    {
        public ulong OsThreadId { get; }

        public ulong Timestamp { get; }

        public GarbageCollectionReason Reason { get; }

        public GarbageCollectionGenerations Generations { get; }

        public GarbageCollectionStarted(ulong osThreadId, ulong timestamp, GarbageCollectionReason reason,
            GarbageCollectionGenerations generations)
        {
            OsThreadId = osThreadId;
            Timestamp = timestamp;
            Reason = reason;
            Generations = generations;
        }
    }
}
