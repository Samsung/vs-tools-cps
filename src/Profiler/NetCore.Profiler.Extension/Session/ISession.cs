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
    public interface ISession : IBaseSession
    {
        int CpuCoreCount { get; }

        string ProfilingType { get; }

        IEnumerable<ISessionThread> Threads { get; }

        string GetSourceFilePath(ulong sourceFileId);

        void SetSourceFilePath(ulong sourceFileId, string path);

        ICallTreeQueryResult GetCallTree(ulong threadId);

        IMethodsQueryResult GetTopMethods(ulong threadId, StatisticsType statisticsType);

        IHotPathsQueryResult GetHotPathes(ulong threadId, StatisticsType statisticsType);

        ISourceLinesQueryResult GetTopLines(ulong threadId, StatisticsType statisticsType);

        List<CpuUtilization> GetCpuUtilization(ulong threadId);

        List<CpuUtilization> GetApplicationCpuUtilization();

        List<ClrJob> GetClrJobs(ulong threadId);

        IMethodsQueryResult GetTopMethodsByMemory();

        IMethodsQueryResult GetTopMethodsByTime();

        ISourceLinesQueryResult GetTopLinesBySamples();

        ISourceLinesQueryResult GetTopLinesByMemory();

        List<SysInfoItem> GetSysInfoItems();

        void BuildStatistics(ISelectedTimeFrame timeFrame);
    }
}
