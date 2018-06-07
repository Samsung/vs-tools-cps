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

using System.ComponentModel;

namespace NetCore.Profiler.Cperf.Core.Model
{

    public class Event
    {
        public object SourceObject { get; }

        public EventType EventType { get; }

        public ulong Timestamp { get; }

        public string SourceObjectType => SourceObject.GetType().Name.Substring(2);

        public Event(object sourceObject, ulong timestamp, EventType type)
        {
            EventType = type;
            Timestamp = timestamp;
            SourceObject = sourceObject;
        }

        public override string ToString()
        {
            var result = "PLEvent[";

            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(this))
            {
                switch (descriptor.Name)
                {
                    case "PLObject":
                        break;
                    default:
                        result += "{" + descriptor.Name + ": " + descriptor.GetValue(this) + "}";
                        break;
                }
            }

            result += "]";

            return result;
        }
    }
}
