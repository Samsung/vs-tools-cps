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
using NetCore.Profiler.Analytics.Model;
using NetCore.Profiler.Cperf.Core;
using NetCore.Profiler.Cperf.Core.Model;

namespace NetCore.Profiler.Analytics.DataProvider
{
    /// <summary>
    /// A data provider used for processing the data from saved (already completed) %Core %Profiler memory profiling sessions
    /// with the aim of generating the resulting analytical and statistical data which can be displayed to end users
    /// in different UI views. Data container class <see cref="MemoryDataContainer"/> is used as the source of input data.
    /// </summary>
    public class MemoryProfilingDataProvider
    {
        private readonly MemoryProfilingDataContainer _cperfContainer;
        private readonly string _sysInfoPath;

        public Dictionary<ulong, string> DataTypes { get; protected set; } = new Dictionary<ulong, string>();

        /// <summary>
        /// Create a memory profiling data provider using the provided source data container and the target system
        /// information file name (but don't process the data at the moment).
        /// </summary>
        /// <param name="cperfContainer">
        /// The input data for the data provider in the form of <see cref="MemoryProfilingDataContainer"/> which contains
        /// parsed data from a saved (already completed) memory profiling session.
        /// </param>
        /// <param name="sysInfoPath">The target system information file name.</param>
        public MemoryProfilingDataProvider(MemoryProfilingDataContainer cperfContainer, string sysInfoPath)
        {
            _cperfContainer = cperfContainer;
            _sysInfoPath = sysInfoPath;
        }

        public List<MemoryData> HeapStatistics { get; private set; } = new List<MemoryData>();

        public List<UnmanagedMemoryData> UnmanagedMemoryStatistics { get; } = new List<UnmanagedMemoryData>();

        public List<ManagedMemoryData> ManagedMemoryStatistics { get; private set; } = new List<ManagedMemoryData>();

        public Dictionary<ulong, DataTypeMemoryStatistics> DataTypeMemoryStatistics { get; } =
            new Dictionary<ulong, DataTypeMemoryStatistics>();

        public Dictionary<ulong, DataTypeAllocationStatistics> DataTypeAllocationStatistics { get; } =
            new Dictionary<ulong, DataTypeAllocationStatistics>();

        public Dictionary<ulong, List<DataTypeMemoryUsage>> CombinedGarbageCollectorSamples => _cperfContainer
            .DataTypeSnapshots;

        /// <summary>
        /// Load the unmanaged memory information from the target system information file and initialize some other
        /// data from the data container set in the constructor.
        /// </summary>
        public void Load()
        {
            LoadUnmanagedMemoryInfo();
            LoadManagedMemoryInfo();
            LoadHeapStatistics();

            DataTypes = _cperfContainer.DataTypes;
        }

        public List<DataTypeMemoryUsage> GetGarbageCollectorSamples(ulong id)
        {
            return CombinedGarbageCollectorSamples.ContainsKey(id) ? CombinedGarbageCollectorSamples[id] : null;
        }

        /// <summary>
        /// Build memory profiling statistics for a selected time frame [the time frame is not used currently].
        /// </summary>
        /// <param name="timeFrame">The selected time frame (specifies a time range)</param>
        public void BuildStatistics(SelectedTimeFrame selectedTimeframe)
        {
            DataTypeAllocationStatistics.Clear();

            DataTypeMemoryStatistics.Clear();

            foreach (var sr in CombinedGarbageCollectorSamples)
            {
                var name = DataTypes[sr.Key];
                var list = sr.Value;
                DataTypeMemoryStatistics.Add(sr.Key, new DataTypeMemoryStatistics
                {
                    DataTypeId = sr.Key,
                    DataTypeName = name,
                    ObjectsCountMax = list.Max(sample => sample.ObjectsCount),
                    ObjectsCountAvg = Math.Round(list.Average(sample => (double)sample.ObjectsCount)),
                    MemorySizeMax = list.Max(sample => sample.MemorySize),
                    MemorySizeAvg = Math.Round(list.Average(sample => (double)sample.MemorySize)),
                });
            }

            foreach (var sr in _cperfContainer.DataTypeAllocations)
            {
                var name = DataTypes[sr.Key];
                var list = sr.Value;
                DataTypeAllocationStatistics.Add(sr.Key, new DataTypeAllocationStatistics
                {
                    DataTypeName = name,
                    MemorySize = (ulong)list.Sum(sample => (double)sample.MemorySize),
                    ObjectsCount = (ulong)list.Sum(sample => (double)sample.ObjectsCount),
                });
            }
        }

        private void LoadManagedMemoryInfo()
        {
            ManagedMemoryStatistics.Clear();

            _cperfContainer.ManagedMemoryStatistics.ForEach(
                x => ManagedMemoryStatistics.Add(
                    new ManagedMemoryData() {
                        Timestamp = x.Timestamp,
                        HeapAllocated = x.HeapAllocated,
                        HeapReserved = x.HeapReserved })
            );
        }

        private void LoadHeapStatistics()
        {
            HeapStatistics.Clear();

            _cperfContainer.GarbageCollectorGenerations.ForEach(
                x => HeapStatistics.Add(
                    new MemoryData() {
                        Timestamp = x.Timestamp,
                        LargeObjectsHeap = x.LargeObjectsHeap,
                        SmallObjectsHeapGeneration0 = x.SmallObjectsHeapGeneration0,
                        SmallObjectsHeapGeneration1 = x.SmallObjectsHeapGeneration1,
                        SmallObjectsHeapGeneration2 = x.SmallObjectsHeapGeneration2 })
            );
        }

        private void LoadUnmanagedMemoryInfo()
        {
            UnmanagedMemoryStatistics.Clear();

            List<UnmanagedMemoryData> stat = new List<UnmanagedMemoryData>();
            using (var file = new StreamReader(Path.GetFullPath(_sysInfoPath)))
            {
                double startTimeSeconds = 0;
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
                            if (startTimeSeconds == 0)
                            {
                                startTimeSeconds = sii.TimeSeconds;
                            }
                            else if (sii.TimeSeconds < startTimeSeconds)
                            {
                                continue;
                            }

                            stat.Add(new UnmanagedMemoryData
                            {
                                Timestamp = (ulong)Math.Round((sii.TimeSeconds - startTimeSeconds) * 1000),
                                Unmanaged = (ulong)sii.MemSize
                            });
                        }
                    }
                }
            }

            stat = stat.OrderBy(x => x.Timestamp).ToList();

            stat.ForEach(x => UnmanagedMemoryStatistics.Add(x));
        }

        private class MemoryDataComparer : IEqualityComparer<MemoryData>
        {
            public bool Equals(MemoryData x, MemoryData y)
            {
                return x.Timestamp == y.Timestamp;
            }

            public int GetHashCode(MemoryData obj)
            {
                if (obj == null)
                {
                    return 0;
                }

                return obj.Timestamp.GetHashCode();
            }
        }

        private class ManagedMemoryDataComparer : IEqualityComparer<ManagedMemoryData>
        {
            public bool Equals(ManagedMemoryData x, ManagedMemoryData y)
            {
                return x.Timestamp == y.Timestamp;
            }

            public int GetHashCode(ManagedMemoryData obj)
            {
                if (obj == null)
                {
                    return 0;
                }

                return obj.Timestamp.GetHashCode();
            }
        }
    }
}
