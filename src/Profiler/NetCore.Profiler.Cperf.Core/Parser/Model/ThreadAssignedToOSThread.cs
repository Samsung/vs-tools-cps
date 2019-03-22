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
    /// A <see cref="CperfParser"/> data model class for thread assigned to OS thread ("thr aos")
    /// %Core %Profiler trace log records. Notifies that a managed thread is being implemented
    /// using a particular operating system thread.
    /// </summary>
    public class ThreadAssignedToOsThread
    {
        /// <summary>
        /// An internal ID of a managed thread.
        /// </summary>
        public ulong InternalId { get; }

        /// <summary>
        /// An ID of an operating system thread.
        /// </summary>
        public ulong OsThreadId { get;}

        public ThreadAssignedToOsThread(ulong internalId, ulong osThreadId)
        {
            InternalId = internalId;
            OsThreadId = osThreadId;
        }
    }
}
