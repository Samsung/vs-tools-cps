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

namespace NetCore.Profiler.Lttng.Core.CTFObject
{
    public class CTFThread
    {
        public ulong Pid { get; set; }
        public ulong Tid { get; set; }

        public List<CTFERecord> Records { get; set; } = new List<CTFERecord>();

        public List<CTFELostRecord> LostRecords { get; set; } = new List<CTFELostRecord>();

        public static CTFThread FirstOrCreateCTFThreadById(ulong pid, ulong tid, List<CTFThread> threads)
        {
            CTFThread result = null;
            foreach (CTFThread thread in threads)
            {
                if (thread.Pid == pid && thread.Tid == tid)
                {
                    result = thread;
                    break;
                }
            }

            if (result == null)
            {
                result = new CTFThread { Pid = pid, Tid = tid };
                threads.Add(result);
            }

            return result;
        }
    }
}
