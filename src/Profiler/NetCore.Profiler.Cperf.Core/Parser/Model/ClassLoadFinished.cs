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

namespace NetCore.Profiler.Cperf.Core.Parser.Model
{
    /// <summary>
    /// A <see cref="CperfParser"/> data model class for class load finished ("cls ldf")
    /// %Core %Profiler trace log records. Notifies that a class has finished loading.
    /// </summary>
    public class ClassLoadFinished
    {
        public ulong Id { get; }

        public ulong InternalId { get; }

        public ulong ModuleId { get; }

        public int ClassToken { get; }

        public ulong Status { get; }

        public ClassLoadFinished(ulong id, ulong internalId, ulong moduleId, int classToken, ulong status)
        {
            Id = id;
            InternalId = internalId;
            ModuleId = moduleId;
            ClassToken = classToken;
            Status = status;
        }
    }
}
