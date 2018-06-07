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

namespace NetCore.Profiler.Cperf.Core.Model
{
    public class Thread : IIdentifiable
    {
        public const ulong FakeThreadId = ulong.MaxValue;
            
        public ulong InternalId { get; set; }

        public ulong Id { get; set; }

        public ulong OsThreadId { get; set; }

        public CpuUtilizationHistory CpuUtilizationHistory { get; } = new CpuUtilizationHistory();

        public List<Event> Events { get; set; } = new List<Event>();

        public override string ToString()
        {
            var result = "Thread[";
            result += "]";

            return result;
        }

    }
}
