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
using System.Globalization;
using System.IO;
using System.Linq;
using NetCore.Profiler.Analytics.DataProvider;
using NetCore.Profiler.Analytics.Model;
using NetCore.Profiler.Common;
using NetCore.Profiler.Cperf.Core;
using NetCore.Profiler.Cperf.Core.Model;
using NetCore.Profiler.Extension.VSPackage;
using NetCore.Profiler.Lttng.Core.BObject;
using NetCore.Profiler.Session.Core;

namespace NetCore.Profiler.Extension.Session
{
    public class MemoryProfilingSession : IMemoryProfilingSession
    {

        private SessionProperties _sessionProperties;

        private MemoryProfilingDataProvider _profilingDataProvider;

        private DateTime _startedAt;

        public string ProjectFolder { get; set; } = "";

        public string SessionFolder { get; protected set; } = "";

        public DateTime CreatedAt { get; protected set; }

        public string ProjectName { get; private set; }

        public string SessionFile { get; private set; }

        public ISessionProperties Properties => _sessionProperties;

        public string Label => $"(M) {CreatedAt.ToLocalTime()} - {ProjectName}";

        public Dictionary<ulong, string> DataTypes => _profilingDataProvider.DataTypes;

        public List<MemoryData> HeapStatistics => _profilingDataProvider.HeapStatistics;

        public List<ManagedMemoryData> ManagedMemoryStatistics => _profilingDataProvider.ManagedMemoryStatistics;

        public List<UnmanagedMemoryData> UnmanagedMemoryStatistics => _profilingDataProvider.UnmanagedMemoryStatistics;

        public List<DataTypeMemoryStatistics> DataTypeMemoryStatistics => _profilingDataProvider
            .DataTypeMemoryStatistics.Values.OrderByDescending(statistics => statistics.MemorySizeAvg).ToList();

        public List<DataTypeAllocationStatistics> DataTypeAllocationStatistics => _profilingDataProvider
            .DataTypeAllocationStatistics.Values.OrderByDescending(statistics => statistics.MemorySize).ToList();

        public List<DataTypeMemoryUsage> GetGarbageCollectorSamples(ulong id)
        {
            return _profilingDataProvider.GetGarbageCollectorSamples(id);
        }

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

            ProjectName = _sessionProperties.GetProperty("ProjectName", "value");

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
                PrepareData();
            }
            finally
            {
                progressMonitor.Stop();
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

            var lttContainer = new BDataContainer(ctfDataPath);
            var cperfContainer = new MemoryProfilingDataContainer(plDataPath);


            lttContainer.Load();
            cperfContainer.Load(progressMonitor);

            FixStartedNanoSeconds(lttContainer.BThreads.Count > 0
                ? lttContainer.BThreads.Min(thread => thread.StartAt)
                : ulong.MaxValue);

            _profilingDataProvider = new MemoryProfilingDataProvider(lttContainer, cperfContainer, Path.Combine(SessionFolder, SessionConstants.ProcFileName), StartedNanoseconds);
            _profilingDataProvider.Load();

        }


        private void PrepareData()
        {
            var selectedTimeframe = new SelectedTimeFrame();
            _profilingDataProvider.BuildStatistics(selectedTimeframe);
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