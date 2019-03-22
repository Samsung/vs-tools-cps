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
using System.Globalization;
using System.Text.RegularExpressions;

namespace NetCore.Profiler.Cperf.Core.Model
{
    /// <summary>
    /// A system information log (proc.log) item.
    /// </summary>
    public class SysInfoItem
    {
        public double TimeSeconds { get; set; }

        public int CoreNum { get; set; }

        public long UserLoad { get; set; }

        public long SysLoad { get; set; }

        public long MemTotal { get; set; }

        public long MemFree { get; set; }

        public long MemSize { get; set; }

        public long UserSys => (UserLoad + SysLoad);

        public long MemLoad => (MemTotal - MemFree);

        public string ProfilerStatus { get; set; }

        private SysInfoItem() { }

        private static readonly Regex FirstLineRegTempl = new Regex(@"psize ([0-9.]+) ncpu ([0-9]+)");

        public static int GetCoreNumber(string firstLine)
        {
            var m = FirstLineRegTempl.Match(firstLine);
            if (m.Success)
            {
                int result;
                if (int.TryParse(m.Groups[2].Value, out result))
                {
                    return result;
                }
            }
            return -1;
        }

        private static readonly Regex SysInfoRegTempl =
            new Regex(@"([0-9.]+) ([0-9]+) ([0-9]+) ([0-9]+) ([0-9]+) ([0-9]+) ([0-9]+) ([0-9]+) ([0-9]+)( [0-9]+)? (.*)$");

        public static SysInfoItem CreateInstance(string line, int coreNum)
        {
            SysInfoItem sii = null;
            var m = SysInfoRegTempl.Match(line);
            if (m.Success)
            {
                sii = new SysInfoItem()
                {
                    TimeSeconds = Convert.ToDouble(m.Groups[1].Value, CultureInfo.InvariantCulture),
                    CoreNum = coreNum,
                    UserLoad = Convert.ToInt64(m.Groups[2].Value),
                    SysLoad = Convert.ToInt64(m.Groups[3].Value),
                    MemTotal = Convert.ToInt64(m.Groups[7].Value),
                    MemFree = Convert.ToInt64(m.Groups[8].Value),
                    ProfilerStatus = m.Groups[11].Value
                };
                Group group = m.Groups[10];
                sii.MemSize = string.IsNullOrEmpty(group.Value) ? (sii.MemTotal - sii.MemFree) : Convert.ToInt64(group.Value);
            }
            return sii;
        }
    }
}
