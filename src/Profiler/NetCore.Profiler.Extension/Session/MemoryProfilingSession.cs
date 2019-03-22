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
using System.IO;
using System.Linq;
using NetCore.Profiler.Analytics.DataProvider;
using NetCore.Profiler.Analytics.Model;
using NetCore.Profiler.Common;
using NetCore.Profiler.Cperf.Core;
using NetCore.Profiler.Cperf.Core.Model;

namespace NetCore.Profiler.Extension.Session
{
    /// <summary>
    /// A class representing a completed (saved previously) %Core %Profiler memory profiling session loaded
    /// for viewing in UI components provided by the plugin.
    /// </summary>
    public class MemoryProfilingSession : BaseSession, IMemoryProfilingSession
    {
        private MemoryProfilingDataProvider _profilingDataProvider;

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

        public MemoryProfilingSession(string path) : base(path) { }

        protected override void LoadData(ProgressMonitor progressMonitor)
        {
            InitializeDataProvider(progressMonitor);
            PrepareData();
        }

        private void InitializeDataProvider(ProgressMonitor progressMonitor)
        {
            var cperfContainer = new MemoryProfilingDataContainer(GetProfilerDataFileName());
            DateTime profilerStartTime;
            cperfContainer.Load(progressMonitor, out profilerStartTime);
            if (profilerStartTime == DateTime.MinValue)
            {
                throw new Exception("Invalid session log");
            }

            _profilingDataProvider = new MemoryProfilingDataProvider(cperfContainer, Path.Combine(SessionFolder, SessionConstants.ProcFileName));
            _profilingDataProvider.Load();
        }

        private void PrepareData()
        {
            _profilingDataProvider.BuildStatistics(new SelectedTimeFrame());
        }
    }
}
