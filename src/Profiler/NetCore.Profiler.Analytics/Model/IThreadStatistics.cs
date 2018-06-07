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
using NetCore.Profiler.Cperf.Core.Model;

namespace NetCore.Profiler.Analytics.Model
{
    public interface IThreadStatistics
    {
        IProfilingStatisticsTotals Totals { get; }

        Dictionary<StatisticsType, List<ISourceLineStatistics>> Lines { get; }

        Dictionary<StatisticsType, List<IMethodStatistics>> Methods { get; }

        List<ICallStatisticsTreeNode> CallTree { get; }

        Dictionary<StatisticsType, List<IHotPath>> HotPaths { get; }

        List<CpuUtilization> CpuUtilization { get; }
    }
}
