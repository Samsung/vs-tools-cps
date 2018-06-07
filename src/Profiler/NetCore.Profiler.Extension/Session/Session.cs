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
using System.Globalization;
using System.IO;
using System.Linq;
using NetCore.Profiler.Analytics.DataProvider;
using NetCore.Profiler.Analytics.Model;
using NetCore.Profiler.Common;
using NetCore.Profiler.Cperf.Core;
using NetCore.Profiler.Cperf.Core.Model;
using NetCore.Profiler.Extension.UI.CallTree;
using NetCore.Profiler.Extension.VSPackage;
using NetCore.Profiler.Lttng.Core.BObject;
using NetCore.Profiler.Session.Core;

namespace NetCore.Profiler.Extension.Session
{
    public class Session : ISession
    {
        private const int TopCount = 20;

        public string ProjectFolder { get; set; } = "";

        public string SessionFolder { get; protected set; } = "";

        public DateTime CreatedAt { get; protected set; }

        public DateTime StartedAt
        {
            get => _startedAt;
            set
            {
                _startedAt = value;
                StartedNanoseconds = (ulong)value.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds * 1000000;
            }
        }

        public ulong StartedNanoseconds { get; private set; }

        public string ProjectName { get; private set; }

        public string SessionFile { get; private set; }

        public ulong Size { get; private set; }

        public string ProfilingType { get; private set; }

        public ISessionProperties Properties => _sessionProperties;

        public IEnumerable<ISessionThread> Threads => SessionThreads.Values.Where(st => st.InternalId != Thread.FakeThreadId);

        private Dictionary<ulong, SessionThread> SessionThreads { get; } = new Dictionary<ulong, SessionThread>();

        private SessionProperties _sessionProperties;

        private DataContainer _dataContainer;

        private ProfilingDataProvider _profilingDataProvider;

        private IApplicationStatistics _applicationStatistics;

        private DateTime _startedAt;

        private readonly List<SysInfoItem> _sysInfoItems = new List<SysInfoItem>();


        /// <summary>
        /// Perform basic initialization. Check for file existance, read properties etc.
        /// </summary>
        /// <remarks>
        /// Trace Data is not loaded at this moment. It's done in <code>Load</code> method.
        /// </remarks>
        /// <param name="path">Path to the session properties file </param>
        public void Initialize(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            SessionFolder = Path.GetDirectoryName(path);

            if (SessionFolder == null || !File.Exists(Path.GetFullPath(path)))
            {
                throw new Exception("Session File does not exist");
            }

            SessionFile = Path.GetFullPath(path);

            _sessionProperties = new SessionProperties(SessionFile);
            _sessionProperties.Load();

            CreatedAt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                .AddMilliseconds(Convert.ToDouble(_sessionProperties.GetProperty("Time", "value")
                .Replace(',', '.'), //Temporay Fix to read sessions created before changing the format
                CultureInfo.InvariantCulture));

            ProfilingType = _sessionProperties.GetProperty("ProfilingType", "value");

            ProjectName = _sessionProperties.GetProperty("ProjectName", "value");

            Size = (ulong)Directory.GetFiles(SessionFolder, "*", SearchOption.AllDirectories).Sum(fileName => (new FileInfo(fileName).Length));


            foreach (var property in new List<string> { "CoreClrProfilerReport", "CoreClrProfilerReport", "CtfReport", "Proc" })
            {
                if (!_sessionProperties.PropertyExists(property))
                {
                    throw new Exception($"{property} Session Property not found");
                }
            }
        }

        /// <summary>
        /// Load profiling data. <code>Initialize</code> must be called prior to this.
        /// </summary>
        public void Load()
        {

            var startTime = DateTime.Now;
            var lastTime = startTime;
            ulong cnt = 0;
            var progressMonitor = new ProgressMonitor()
            {
                Start = delegate
                {
                    ProfilerPlugin.Instance.SaveExplorerWindowCaption();
                    ProfilerPlugin.Instance.UpdateExplorerWindowProgress(0);
                },

                Stop = delegate
                {
                    ProfilerPlugin.Instance.RestoreExplorerWindowCaption();
                },

                Tick = delegate
                {
                    if (++cnt % 1000 == 0)
                    {
                        var now = DateTime.Now;
                        if ((now - lastTime).TotalSeconds >= 0.5)
                        {
                            ProfilerPlugin.Instance.UpdateExplorerWindowProgress((long)Math.Min(((now - startTime).TotalSeconds) * 5, 99));
                            lastTime = now;
                        }
                    }
                }

            };
            try
            {
                progressMonitor.Start();
                InitializeDataProvider(progressMonitor);
                _profilingDataProvider.Load();
                FixStartedNanoSeconds(_profilingDataProvider.MinimalStartTime);
                ParseSysInfoLog(Path.Combine(SessionFolder, SessionConstants.ProcFileName));
                PrepareData();
            }
            finally
            {
                progressMonitor.Stop();
            }
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

        private void InitializeDataProvider(ProgressMonitor progressMonitor)
        {
            var profilerReportDirectory = _sessionProperties.GetProperty("CoreClrProfilerReport", "path");
            var ctfReportDirectory = _sessionProperties.GetProperty("CtfReport", "path");
            if (string.IsNullOrEmpty(profilerReportDirectory) || string.IsNullOrEmpty(ctfReportDirectory))
            {
                throw new Exception("Invalid Session Directory");
            }

            var plDataPath = Path.Combine(
                SessionFolder,
                profilerReportDirectory,
                _sessionProperties.GetProperty("CoreClrProfilerReport", "name"));

            var ctfDataPath = Path.Combine(
                SessionFolder,
                ctfReportDirectory,
                _sessionProperties.GetProperty("CtfReport", "name"));

            using (var file = new StreamReader(plDataPath))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    if (line.StartsWith("prf stm"))
                    {
                        StartedAt = DateTime.ParseExact(line.Substring(8), "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                        break;
                    }
                }
            }
            //TODO Add check if startedAt was found


            var bContainer = new BDataContainer(ctfDataPath);
            _dataContainer = new DataContainer(plDataPath);

            bContainer.Load();
            _dataContainer.Load(progressMonitor);

            //TODO Find better place for NodeConstructor
            _profilingDataProvider = new ProfilingDataProvider(bContainer, _dataContainer, () => new CallStatisticsTreeNode());
        }

        private void ParseSysInfoLog(string path)
        {
            _sysInfoItems.Clear();

            using (var file = new StreamReader(Path.GetFullPath(path)))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    var sii = SysInfoItem.CreateInstance(line);
                    if (sii != null)
                    {
                        _sysInfoItems.Add(sii);
                    }
                }
            }
        }

        private void PrepareData()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            CreateThreads();

            var selectedTimeframe = new SelectedTimeFrame();

            _profilingDataProvider.BuildStatistics(selectedTimeframe);

            _applicationStatistics = _profilingDataProvider.ApplicationStatistics;

            foreach (var thread in SessionThreads.Values)
            {
                UpdateSessionThreadData(thread);
            }

            stopwatch.Stop();
            Debug.WriteLine("=====================================================================");
            Debug.WriteLine("Prepare Data time elapsed: {0}", stopwatch.Elapsed);
            Debug.WriteLine("=====================================================================");
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

        private void FixStartedNanoSeconds(ulong startTime)
        {
            if (StartedNanoseconds > startTime)
            {
                StartedNanoseconds -= (StartedNanoseconds - startTime) / 1000 / 1000 / 1000 / 3600 * 3600 * 1000 * 1000 * 1000;
            }
            else
            {
                StartedNanoseconds += (startTime - StartedNanoseconds) / 1000 / 1000 / 1000 / 3600 * 3600 * 1000 * 1000 * 1000; // +1 ?
            }
        }
    }
}