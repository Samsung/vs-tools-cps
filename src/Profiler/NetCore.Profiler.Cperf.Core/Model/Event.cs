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

namespace NetCore.Profiler.Cperf.Core.Model
{
    /// <summary>
    /// A <see cref="DataContainer"/> data model class for an event (such as garbage collection started)
    /// from a %Core %Profiler log. These events are stored in <see cref="Thread"/> objects.
    /// </summary>
    public class Event
    {
        public object SourceObject { get; }

        public EventType EventType { get; }

        public ulong TimeMilliseconds { get; }

        public string SourceObjectType => SourceObject.GetType().Name;

        public Event(object sourceObject, ulong timeMilliseconds, EventType type)
        {
            SourceObject = sourceObject;
            EventType = type;
            TimeMilliseconds = timeMilliseconds;
        }

        public override string ToString()
        {
            return $"{EventType} at {TimeMilliseconds} ms";
        }
    }
}
