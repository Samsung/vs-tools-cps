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
    /// A <see cref="CperfParser"/> data model class for a garbage collection sample ("gch alt") %Core %Profiler
    /// trace log records. Used to dump the GC allocation table that describes how many objects of different classes
    /// remain in the heap after a garbage collection has completed and how many memory is used by them.
    /// </summary>
    public class GarbageCollectionSample
    {
        public ulong Timestamp { get; }

        public List<GarbageCollectionSampleItem> Items { get; } = new List<GarbageCollectionSampleItem>();

        public GarbageCollectionSample(ulong timestamp)
        {
            Timestamp = timestamp;
        }
    }
}
