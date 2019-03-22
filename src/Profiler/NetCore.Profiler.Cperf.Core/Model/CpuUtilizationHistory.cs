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

namespace NetCore.Profiler.Cperf.Core.Model
{
    /// <summary>
    /// A container for <see cref="CpuUtilization"/> objects.
    /// </summary>
    public class CpuUtilizationHistory
    {
        public List<CpuUtilization> CpuList { get; } = new List<CpuUtilization>();

        private int _cpuCoreCount;

        private bool _profilingResumed;

        public CpuUtilizationHistory(int cpuCoreCount)
        {
            _cpuCoreCount = Math.Max(cpuCoreCount, 1);
        }

        public void RecordCpuUsage(ulong timestamp, ulong duration)
        {
            int cpuListCount = CpuList.Count;
            double cpu = (cpuListCount > 0) ? CalculateCpuUtilization(CpuList[cpuListCount - 1], timestamp, duration) : 0;
            if (_profilingResumed && (cpu < 0))
            {
                cpu = 0;
            }
            if (cpu >= 0)
            {
                cpu = Math.Min(cpu, 100);
                CpuList.Add(new CpuUtilization
                {
                    TimeMilliseconds = timestamp,
                    Utilization = cpu,
                    ProfilingWasResumed = _profilingResumed
                });
            }
            _profilingResumed = false;
        }

        public void RecordProfilingPaused()
        {
            _profilingResumed = false;

            if (CpuList.Count > 0)
            {
                CpuList[CpuList.Count - 1].ProfilingWasPaused = true;
            }
        }

        public void RecordProfilingResumed()
        {
            _profilingResumed = true;

            //Compatibility with old implementation. Useless though
            if (CpuList.Count > 0)
            {
                CpuList[CpuList.Count - 1].ProfilingWasResumed = true;
            }
        }

        private double CalculateCpuUtilization(CpuUtilization prev, ulong timestamp, ulong duration)
        {
            //TODO. To prevent errors for log records like the following
            //thr cpu 0x00000000 85403 18446744073709551586
            if (duration >= int.MaxValue)
            {
                return -1;
            }

            ulong interval = timestamp - prev.TimeMilliseconds;
            if (interval <= 0)
            {
                return -1;
            }

            return (duration / (interval * 1000.0)) * 100 / _cpuCoreCount;
        }
    }
}
