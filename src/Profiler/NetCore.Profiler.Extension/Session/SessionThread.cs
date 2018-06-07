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
using NetCore.Profiler.Analytics.Model;
using NetCore.Profiler.Cperf.Core.Model;

namespace NetCore.Profiler.Extension.Session
{
    class SessionThread : ISessionThread
    {
        public ulong OsThreadId { get; set; }

        public ulong InternalId { get; set; }

        public List<CpuUtilization> CpuUtilization { get; set; }

        public List<ClrJob> ClrJobs { get; set; }

        public IProfilingStatisticsTotals Totals { get; set; }


        public List<ICallStatisticsTreeNode> CallTree { get; set; }

        public Dictionary<StatisticsType, List<IMethodStatistics>> Methods { get; set; }

        public Dictionary<StatisticsType, List<IHotPath>> HotPaths { get; set; }

        public Dictionary<StatisticsType, List<ISourceLineStatistics>> Lines { get; set; }

        public SessionThread(ISessionThreadBase baseThread)
        {
            InternalId = baseThread.InternalId;

            OsThreadId = baseThread.OsThreadId;

            CpuUtilization = baseThread.CpuUtilization;

            ClrJobs = baseThread.ClrJobs;
        }

        public void UpdateStatistics(IThreadStatistics statistics)
        {
            Totals = statistics.Totals;
            CallTree = statistics.CallTree;
            Methods = statistics.Methods;
            HotPaths = statistics.HotPaths;
            Lines = statistics.Lines;
            CpuUtilization = statistics.CpuUtilization;
        }
    }
}
