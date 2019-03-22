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
using System.Linq;
using NetCore.Profiler.Common;
using NetCore.Profiler.Cperf.Core.Model;
using NetCore.Profiler.Cperf.Core.Parser;
using NetCore.Profiler.Cperf.Core.Parser.Model;

namespace NetCore.Profiler.Cperf.Core
{
    /// <summary>
    /// A data container used to load and store in memory structures the data from saved (already completed) %Core %Profiler
    /// memory profiling sessions so they can be used by <see cref="MemoryProfilingDataProvider"/>.
    /// </summary>
    public class MemoryProfilingDataContainer
    {
        private readonly Dictionary<ulong, Dictionary<ulong, DataTypeMemoryUsage>> _tempDataTypeAllocations = new Dictionary<ulong, Dictionary<ulong, DataTypeMemoryUsage>>();

        private readonly string _filePath;

        /// <summary>
        /// Create a data container for the specified memory profiling session file (but don't load the data at the moment).
        /// </summary>
        /// <param name="filePath">The memory profiling session file path and name.</param>
        public MemoryProfilingDataContainer(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException(nameof(filePath));
            }

            _filePath = filePath;
        }

        public Dictionary<ulong, string> DataTypes { get; } = new Dictionary<ulong, string>();

        public Dictionary<ulong, List<DataTypeMemoryUsage>> DataTypeAllocations { get; } = new Dictionary<ulong, List<DataTypeMemoryUsage>>();

        public Dictionary<ulong, List<DataTypeMemoryUsage>> DataTypeSnapshots { get; } = new Dictionary<ulong, List<DataTypeMemoryUsage>>();

        public List<GarbageCollectorGenerationsSample> GarbageCollectorGenerations { get; } = new List<GarbageCollectorGenerationsSample>();

        public List<ManagedMemoryData> ManagedMemoryStatistics { get; } = new List<ManagedMemoryData>();

        /// <summary>
        /// Load the data from the memory profiling session file set in the constructor.
        /// </summary>
        /// <param name="progressMonitor">Used to report the profiling session file load progress.</param>
        /// <param name="profilerStartTime">
        /// The profiler start time (according to a clock of a target Tizen system) is returned in this output parameter.
        /// </param>
        public void Load(ProgressMonitor progressMonitor, out DateTime profilerStartTime)
        {
            var parser = new CperfParser();

            Action<string> lineReadCallback = (s => progressMonitor.Tick());

            DateTime startTimeFromParser = DateTime.MinValue;
            Action<DateTime> startTimeCallback = (DateTime startTime) =>
            {
                startTimeFromParser = startTime;
            };

            parser.LineReadCallback += lineReadCallback;
            parser.StartTimeCallback += startTimeCallback;
            parser.GarbageCollectorSampleCallback += GarbageCollectorSampleCallback;
            parser.GarbageCollectorGenerationSampleCallback += GarbageCollectorGenerationsSampleCallback;
            parser.ManagedMemorySampleCallback += ManagedMemorySampleCallback;
            parser.ClassNameReadCallback += ClassNameReadCallback;
            parser.AllocationSampleReadCallback += AllocationSampleReadCallback;

            parser.Parse(_filePath);

            profilerStartTime = startTimeFromParser;

            parser.LineReadCallback -= lineReadCallback;
            parser.StartTimeCallback -= startTimeCallback;
            parser.GarbageCollectorSampleCallback -= GarbageCollectorSampleCallback;
            parser.GarbageCollectorGenerationSampleCallback -= GarbageCollectorGenerationsSampleCallback;
            parser.ManagedMemorySampleCallback -= ManagedMemorySampleCallback;
            parser.ClassNameReadCallback -= ClassNameReadCallback;
            parser.AllocationSampleReadCallback -= AllocationSampleReadCallback;

            ProcessDataTypeAllocationSamples();

            _tempDataTypeAllocations.Clear();
        }

        private void ClassNameReadCallback(ClassName arg)
        {
            if (!DataTypes.ContainsKey(arg.InternalId))
            {
                DataTypes.Add(arg.InternalId, arg.Name);
            }
        }

        private void GarbageCollectorGenerationsSampleCallback(GarbageCollectorGenerationsSample sample)
        {
            GarbageCollectorGenerations.Add(sample);
        }

        private void ManagedMemorySampleCallback(ManagedMemoryData sample)
        {
            ManagedMemoryStatistics.Add(sample);
        }

        private void GarbageCollectorSampleCallback(GarbageCollectionSample sample)
        {
            foreach (var snapshot in DataTypeSnapshots)
            {
                var x = sample.Items.FirstOrDefault(item => item.ClassId == snapshot.Key);
                if (x != null)
                {
                    if (snapshot.Value.Count == 0 || snapshot.Value[snapshot.Value.Count - 1].TimeMilliseconds !=
                        sample.Timestamp)
                    {
                        snapshot.Value.Add(new DataTypeMemoryUsage
                        {
                            TimeMilliseconds = sample.Timestamp,
                            ObjectsCount = x.ObjectsCount,
                            MemorySize = x.MemorySize
                        });
                    }
                    else
                    {
                        var mu = snapshot.Value[snapshot.Value.Count - 1];
                        mu.ObjectsCount = x.ObjectsCount;
                        mu.MemorySize = x.MemorySize;
                    }
                }
                else
                {
                    if (snapshot.Value.Count <= 0)
                    {
                        continue;
                    }

                    if (snapshot.Value[snapshot.Value.Count - 1].TimeMilliseconds != sample.Timestamp)
                    {
                        snapshot.Value.Add(new DataTypeMemoryUsage
                        {
                            TimeMilliseconds = sample.Timestamp,
                            ObjectsCount = 0,
                            MemorySize = 0
                        });
                    }
                    else
                    {
                        var mu = snapshot.Value[snapshot.Value.Count - 1];
                        mu.ObjectsCount = 0;
                        mu.MemorySize = 0;
                    }
                }
            }
        }

        private void AllocationSampleReadCallback(AllocationSample arg)
        {
            foreach (var allocation in arg.Allocations)
            {
                RecordAllocation(arg.Ticks, allocation);
                RecordSnapshot(arg.Ticks, allocation);
            }
        }

        private void RecordAllocation(ulong timestamp, AllocationSampleInfo allocation)
        {
            if (!_tempDataTypeAllocations.TryGetValue(allocation.ClassIntId, out Dictionary<ulong, DataTypeMemoryUsage> dict))
            {
                dict = new Dictionary<ulong, DataTypeMemoryUsage>();
                _tempDataTypeAllocations.Add(allocation.ClassIntId, dict);
            }

            if (!dict.TryGetValue(timestamp, out DataTypeMemoryUsage sample))
            {
                sample = new DataTypeMemoryUsage {TimeMilliseconds = timestamp };
                dict.Add(timestamp, sample);
            }

            sample.ObjectsCount += allocation.AllocationCount;
            sample.MemorySize += allocation.MemorySize;
        }

        private void RecordSnapshot(ulong timestamp, AllocationSampleInfo allocation)
        {
            if (!DataTypeSnapshots.TryGetValue(allocation.ClassIntId, out List<DataTypeMemoryUsage> list))
            {
                DataTypeSnapshots.Add(
                    allocation.ClassIntId,
                    new List<DataTypeMemoryUsage>()
                    {
                        new DataTypeMemoryUsage
                        {
                            TimeMilliseconds = timestamp,
                            ObjectsCount = allocation.AllocationCount,
                            MemorySize = allocation.MemorySize
                        }
                    });

                return;
            }

            var mu = list[list.Count - 1];
            if (mu.TimeMilliseconds == timestamp)
            {
                mu.ObjectsCount += allocation.AllocationCount;
                mu.MemorySize += allocation.MemorySize;
            }
            else
            {
                list.Add(new DataTypeMemoryUsage
                {
                    TimeMilliseconds = timestamp,
                    ObjectsCount = mu.ObjectsCount + allocation.AllocationCount,
                    MemorySize = mu.MemorySize + allocation.MemorySize
                });
            }
        }

        private void ProcessDataTypeAllocationSamples()
        {
            foreach (var sample in _tempDataTypeAllocations)
            {
                DataTypeAllocations.Add(sample.Key, sample.Value.Values.ToList());
            }
        }
    }
}
