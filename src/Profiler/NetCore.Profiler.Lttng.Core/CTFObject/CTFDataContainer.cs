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
using System.Collections.Generic;

namespace NetCore.Profiler.Lttng.Core.CTFObject
{
    public class CTFDataContainer
    {
        public CTFTClock Clock { get; protected set; }

        public List<CTFThread> CTFThreads { get; protected set; }

        public Dictionary<uint, CTFTEvent> EventTypes { get; protected set; }

        public readonly string FilePath;

        public CTFDataContainer(string filePath)
        {

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("CTFDataContainer");
            }

            this.FilePath = filePath;

            Load();
        }

        public void Load()
        {
            CTFFile tf = new CTFFile();
            CTFThreads = tf.ReadTrace(FilePath);
            EventTypes = tf.Events;
            Clock = tf.Clock;
        }
    }
}
