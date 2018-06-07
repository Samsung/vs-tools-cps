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
using System.Text.RegularExpressions;

namespace NetCore.Profiler.Cperf.Core.Model
{
    public class SysInfoItem
    {
        public long Timestamp { get; set; }

        public int CoreNum { get; set; }

        public long UserLoad { get; set; }

        public long SysLoad { get; set; }

        public long MemTotal { get; set; }

        public long MemFree { get; set; }

        public long MemSize { get; set; }

        public long UserSys
        {
            get
            {
                return UserLoad + SysLoad;
            }
        }

        public long MemLoad
        {
            get
            {
                return MemTotal - MemFree;
            }
        }

        public string ProfilerStatus { get; set; }

        private static Regex RegTempl =
                        new Regex(@"([0-9]+) ([0-9]+) ([0-9]+) ([0-9]+) ([0-9]+) ([0-9]+) ([0-9]+) ([0-9]+) ([0-9]+) ([0-9]+)( [0-9]+)? (.*)$");

        private SysInfoItem(long timestamp, int coreNum, long userLoad, long sysLoad, long memTotal, long memFree, long memSize, string profilerStatus)
        {
            Timestamp = timestamp;
            CoreNum = coreNum;
            UserLoad = userLoad;
            SysLoad = SysLoad;
            MemTotal = memTotal;
            MemFree = memFree;
            MemSize = memSize;
            ProfilerStatus = profilerStatus;
        }

        public static SysInfoItem CreateInstance(string str)
        {
            SysInfoItem sii = null;
            var m = RegTempl.Match(str);
            if (m.Success)
            {
                var timestamp = Convert.ToInt64(m.Groups[1].Value);
                var coreNum = Convert.ToInt32(m.Groups[2].Value);
                var userLoad = Convert.ToInt64(m.Groups[3].Value);
                var sysLoad = Convert.ToInt64(m.Groups[4].Value);
                var memTotal = Convert.ToInt64(m.Groups[8].Value);
                var memFree = Convert.ToInt64(m.Groups[9].Value);
                var memSize = string.IsNullOrEmpty(m.Groups[11].Value) ? (memTotal - memFree) : Convert.ToInt64(m.Groups[11].Value);
                var profilerStatus = m.Groups[12].Value;
                sii = new SysInfoItem(timestamp, coreNum, userLoad, sysLoad, memTotal, memFree, memSize, profilerStatus);
            }

            return sii;
        }
    }
}
