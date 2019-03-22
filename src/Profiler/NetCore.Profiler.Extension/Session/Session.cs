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
using System.Diagnostics;
using System.IO;
using System.Linq;
using NetCore.Profiler.Analytics.DataProvider;
using NetCore.Profiler.Analytics.Model;
using NetCore.Profiler.Common;
using NetCore.Profiler.Common.Helpers;
using NetCore.Profiler.Cperf.Core;
using NetCore.Profiler.Cperf.Core.Model;
using NetCore.Profiler.Extension.UI.CallTree;

namespace NetCore.Profiler.Extension.Session
{
    /// <summary>
    /// A class representing a completed (saved previously) %Core %Profiler profiling session loaded
    /// for viewing in UI components provided by the plugin.
    /// </summary>
    public class Session : BaseSession, ISession
    {
        private const int TopCount = 20;

        public int CpuCoreCount { get; private set; } = 1;

        public string ProfilingType { get; private set; }

        public IEnumerable<ISessionThread> Threads => SessionThreads.Values.Where(st => st.InternalId != Thread.FakeThreadId);

        private Dictionary<ulong, SessionThread> SessionThreads { get; } = new Dictionary<ulong, SessionThread>();

        private DataContainer _dataContainer;

        private ProfilingDataProvider _profilingDataProvider;

        private IApplicationStatistics _applicationStatistics;

        private readonly List<SysInfoItem> _sysInfoItems = new List<SysInfoItem>();

        public Session(string path) : base(path) { }

        protected override void Initialize(string path)
        {
            base.Initialize(path);

            ProfilingType = _sessionProperties.GetProperty("ProfilingType", "value");
        }

        protected override void LoadData(ProgressMonitor progressMonitor)
        {
            ParseSysInfoLog(Path.Combine(SessionFolder, SessionConstants.ProcFileName));
            DateTime sysInfoStartTime = DateTime.MinValue;
            SysInfoItem firstSysInfo = _sysInfoItems.FirstOrDefault();
            if (firstSysInfo != null)
            {
                sysInfoStartTime = firstSysInfo.TimeSeconds.UnixTimeToDateTime();
                CpuCoreCount = Math.Max(firstSysInfo.CoreNum, 1);
            }
            InitializeDataProvider(progressMonitor, sysInfoStartTime, CpuCoreCount);
            _profilingDataProvider.Load();
//            _profilingDataProvider.SaveClrJobs(Path.Combine(SessionFolder, "clr_jobs.log"));
            PrepareData();
        }

        public string GetSourceFilePath(ulong sourceFileId)
        {
            if (_dataContainer != null)
            {
                if (_dataContainer.SourceFiles.ContainsKey(sourceFileId))
                {
                    return _dataContainer.SourceFiles[sourceFileId].Name;
                }
            }

            return null;
        }

        public void SetSourceFilePath(ulong sourceFileId, string path)
        {
            if (_dataContainer != null)
            {
                if (_dataContainer.SourceFiles.ContainsKey(sourceFileId))
                {
                    _dataContainer.SourceFiles[sourceFileId].Name = path;
                }
            }
        }

        public ICallTreeQueryResult GetCallTree(ulong threadId)
        {
            return SessionThreads.TryGetValue(threadId, out SessionThread x) ?
                new CallTreeQueryResult { Totals = x.Totals, CallTree = x.CallTree }
                : new CallTreeQueryResult();
        }

        public IMethodsQueryResult GetTopMethods(ulong threadId, StatisticsType statisticsType)
        {
            return SessionThreads.TryGetValue(threadId, out SessionThread x) ?
                new MethodsQueryResult() { StatisticsType = statisticsType, Totals = x.Totals, Methods = x.Methods[statisticsType].Take(TopCount).ToList() }
                : new MethodsQueryResult();
        }

        public IHotPathsQueryResult GetHotPathes(ulong threadId, StatisticsType statisticsType)
        {
            return SessionThreads.TryGetValue(threadId, out SessionThread x) ?
                new HotPathsQueryResult() { StatisticsType = statisticsType, Totals = x.Totals, Methods = x.HotPaths[statisticsType].Take(TopCount).ToList() }
                : new HotPathsQueryResult();
        }

        public ISourceLinesQueryResult GetTopLines(ulong threadId, StatisticsType statisticsType)
        {
            return SessionThreads.TryGetValue(threadId, out SessionThread x) ?
                new SourceLinesQueryResult() { StatisticsType = statisticsType, Totals = x.Totals, Lines = x.Lines[statisticsType].Take(TopCount).ToList() }
                : new SourceLinesQueryResult();
        }

        public List<CpuUtilization> GetCpuUtilization(ulong threadId)
        {
            return SessionThreads.TryGetValue(threadId, out SessionThread x) ? x.CpuUtilization : new List<CpuUtilization>();
        }

        public List<CpuUtilization> GetApplicationCpuUtilization()
        {
            return _profilingDataProvider.ApplicationCpuUtilization;
        }

        public List<ClrJob> GetClrJobs(ulong threadId)
        {
            return SessionThreads.TryGetValue(threadId, out SessionThread x) ? x.ClrJobs : new List<ClrJob>();
        }

        public IMethodsQueryResult GetTopMethodsByMemory()
        {
            return new MethodsQueryResult
            {
                StatisticsType = StatisticsType.Memory,
                Totals = _applicationStatistics.Totals,
                Methods = _applicationStatistics.Methods[StatisticsType.Memory].Take(TopCount).ToList()
            };
        }

        public IMethodsQueryResult GetTopMethodsByTime()
        {
            return new MethodsQueryResult
            {
                StatisticsType = StatisticsType.Time,
                Totals = _applicationStatistics.Totals,
                Methods = _applicationStatistics.Methods[StatisticsType.Time].Take(TopCount).ToList()
            };
        }

        public ISourceLinesQueryResult GetTopLinesBySamples()
        {
            return new SourceLinesQueryResult
            {
                StatisticsType = StatisticsType.Sample,
                Totals = _applicationStatistics.Totals,
                Lines = _applicationStatistics.Lines[StatisticsType.Sample].Take(TopCount).ToList()
            };
        }

        public ISourceLinesQueryResult GetTopLinesByMemory()
        {
            return new SourceLinesQueryResult
            {
                StatisticsType = StatisticsType.Memory,
                Totals = _applicationStatistics.Totals,
                Lines = _applicationStatistics.Lines[StatisticsType.Memory].Take(TopCount).ToList()
            };
        }

        public List<SysInfoItem> GetSysInfoItems()
        {
            return _sysInfoItems;
        }

        public void BuildStatistics(ISelectedTimeFrame timeFrame)
        {
            _profilingDataProvider.BuildStatistics(timeFrame);
            foreach (var thread in SessionThreads.Values)
            {
                UpdateSessionThreadData(thread);
            }
        }

        private void InitializeDataProvider(ProgressMonitor progressMonitor, DateTime sysInfoStartTime, int cpuCoreCount)
        {
            _dataContainer = new DataContainer(GetProfilerDataFileName(), cpuCoreCount);
            DateTime profilerStartTime;
            _dataContainer.Load(progressMonitor, sysInfoStartTime, out profilerStartTime);
            if (profilerStartTime == DateTime.MinValue)
            {
                throw new Exception("Invalid session log");
            }
/*
            DateTime startTime = profilerStartTime;
            if ((sysInfoStartTime != DateTime.MinValue) && (sysInfoStartTime < startTime))
            {
                startTime = sysInfoStartTime;
            }
            // Subtract doesn't care of DateTime.Kind which is exactly what we want here
            StartNanoseconds = (ulong)(Math.Round(startTime.Subtract(TimeStampHelper.UnixEpochTime).TotalMilliseconds)) * 1000000;
*/
            _profilingDataProvider = new ProfilingDataProvider(_dataContainer, () => new CallStatisticsTreeNode());
        }

        private void ParseSysInfoLog(string path)
        {
            _sysInfoItems.Clear();

            using (var file = new StreamReader(Path.GetFullPath(path)))
            {
                string line = file.ReadLine();
                if (line != null)
                {
                    int coreNum = SysInfoItem.GetCoreNumber(line);
                    if (coreNum < 0)
                    {
                        return;
                    }
                    while ((line = file.ReadLine()) != null)
                    {
                        var sii = SysInfoItem.CreateInstance(line, coreNum);
                        if (sii != null)
                        {
                            _sysInfoItems.Add(sii);
                        }
                    }
                }
            }
        }

        private void PrepareData()
        {
#if DEBUG
            var stopwatch = new Stopwatch();
            stopwatch.Start();
#endif
            CreateThreads();

            var selectedTimeframe = new SelectedTimeFrame();

            _profilingDataProvider.BuildStatistics(selectedTimeframe);

            _applicationStatistics = _profilingDataProvider.ApplicationStatistics;

            foreach (var thread in SessionThreads.Values)
            {
                UpdateSessionThreadData(thread);
            }
#if DEBUG
            stopwatch.Stop();
            Debug.WriteLine("=====================================================================");
            Debug.WriteLine("Prepare Data time elapsed: {0}", stopwatch.Elapsed);
            Debug.WriteLine("=====================================================================");
#endif
        }

        private void CreateThreads()
        {
            foreach (var thread in _profilingDataProvider.SessionThreads)
            {
                SessionThreads.Add(thread.InternalId, new SessionThread(thread));
            }
        }

        private void UpdateSessionThreadData(SessionThread thread)
        {
            thread.UpdateStatistics(_profilingDataProvider.ThreadsStatistics[thread.InternalId]);
        }
    }
}
