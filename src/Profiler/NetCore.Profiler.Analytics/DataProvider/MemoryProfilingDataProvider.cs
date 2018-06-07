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
using NetCore.Profiler.Lttng.Core.BObject;

namespace NetCore.Profiler.Analytics.DataProvider
{
    public class MemoryProfilingDataProvider
    {
        private readonly BDataContainer _lttContainer;
        private readonly MemoryProfilingDataContainer _cperfContainer;
        private readonly string _sysInfoPath;
        private readonly ulong _startedNanoseconds;

        public Dictionary<ulong, string> DataTypes { get; protected set; } = new Dictionary<ulong, string>();

        public MemoryProfilingDataProvider(BDataContainer lttContainer, MemoryProfilingDataContainer cperfContainer, string sysInfoPath, ulong startedNanoseconds)
        {
            _lttContainer = lttContainer;
            _cperfContainer = cperfContainer;
            _sysInfoPath = sysInfoPath;
            _startedNanoseconds = startedNanoseconds;
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

        public void Load()
        {
            LoadUnmanagedMemoryInfo();

            LoadManagedMemoryInfo();

            DataTypes = _cperfContainer.DataTypes;

        }

        public List<DataTypeMemoryUsage> GetGarbageCollectorSamples(ulong id)
        {
            return CombinedGarbageCollectorSamples.ContainsKey(id) ? CombinedGarbageCollectorSamples[id] : null;
        }

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
            foreach (var bThread in _lttContainer.BThreads)
            {
                foreach (var gcItem in bThread.GCItems)
                {
                    if (gcItem.IsFull != true)
                    {
                        //we don't have full information
                        continue;
                    }

                    if (gcItem.HeapSize[0] == 0 || gcItem.HeapSize[1] == 0 || gcItem.HeapSize[2] == 0 ||
                        gcItem.HeapSize[3] == 0)
                    {
                        continue;
                    }

                    //HeapStatistics.Add(new MemoryData
                    //{

                    //    Timestamp = (gcItem.JobStartAt - StartedNanoseconds),
                    //    SmallObjectsHeapGeneration0 = gcItem.HeapSizeStart[0],
                    //    SmallObjectsHeapGeneration1 = gcItem.HeapSizeStart[1],
                    //    SmallObjectsHeapGeneration2 = gcItem.HeapSizeStart[2],
                    //    LargeObjectsHeap = gcItem.HeapSizeStart[3],
                    //});


                    HeapStatistics.Add(new MemoryData
                    {
                        Timestamp = (gcItem.JobEndAt - _startedNanoseconds),
                        SmallObjectsHeapGeneration0 = gcItem.HeapSize[0],
                        SmallObjectsHeapGeneration1 = gcItem.HeapSize[1],
                        SmallObjectsHeapGeneration2 = gcItem.HeapSize[2],
                        LargeObjectsHeap = gcItem.HeapSize[3],
                    });

                    ManagedMemoryStatistics.Add(new ManagedMemoryData
                    {
                        Timestamp = (gcItem.JobEndAt - _startedNanoseconds),
                        HeapAllocated = gcItem.HeapSize.Aggregate<ulong, ulong>(0, (current, r) => current + r),
                        HeapReserved = gcItem.ReservedSize.Aggregate<ulong, ulong>(0, (current, r) => current + r)
                    });
                }
            }

            HeapStatistics = HeapStatistics.Distinct(new MemoryDataComparer()).OrderBy(data => data.Timestamp).ToList();
            foreach (var ms in HeapStatistics)
            {
                ms.Timestamp /= 1000000;
            }

            ManagedMemoryStatistics = ManagedMemoryStatistics.Distinct(new ManagedMemoryDataComparer()).OrderBy(data => data.Timestamp).ToList();
            foreach (var ms in ManagedMemoryStatistics)
            {
                ms.Timestamp /= 1000000;
            }
        }

        private void LoadUnmanagedMemoryInfo()
        {
            UnmanagedMemoryStatistics.Clear();
            using (var file = new StreamReader(Path.GetFullPath(_sysInfoPath)))
            {
                string line;
                long startTimestamp = 0;
                while ((line = file.ReadLine()) != null)
                {
                    var sii = SysInfoItem.CreateInstance(line);
                    if (sii != null)
                    {
                        if (startTimestamp == 0)
                        {
                            startTimestamp = sii.Timestamp;
                        }

                        UnmanagedMemoryStatistics.Add(new UnmanagedMemoryData
                        {
                            Timestamp = (ulong)(sii.Timestamp - startTimestamp) * 1000,
                            Unmanaged = (ulong)sii.MemSize
                        });
                    }
                }
            }
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


