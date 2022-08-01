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
using NetCore.Profiler.Analytics.Model;
using NetCore.Profiler.Cperf.Core;
using NetCore.Profiler.Cperf.Core.Model;
using NetCore.Profiler.Cperf.Core.Parser.Model;

namespace NetCore.Profiler.Analytics.DataProvider
{
    /// <summary>
    /// A data provider used for processing the data from saved (already completed) %Core %Profiler profiling sessions
    /// with the aim of generating the resulting analytical and statistical data which can be displayed to end users
    /// in different UI views. Data container class <see cref="DataContainer"/> is used as the source of input data.
    /// </summary>
    public class ProfilingDataProvider : IDataProvider
    {
        public double HotPathThreshold { get; set; } = 0.1;

        public List<CpuUtilization> ApplicationCpuUtilization { get; private set; } = new List<CpuUtilization>();

        private ProfilingStatisticsTotals ApplicationTotals { get; set; }

        public IApplicationStatistics ApplicationStatistics { get; private set; }

        public Dictionary<ulong, IThreadStatistics> ThreadsStatistics { get; private set; }

        public Dictionary<ulong, IThreadStatisticsRaw> ThreadsStatisticsRaw { get; private set; }

        private StatisticsData _applicationData;

        public IEnumerable<ISessionThreadBase> SessionThreads => _sessionThreads.Values.Where(t => t.InternalId != Thread.FakeThreadId);

        private readonly Dictionary<ulong, ISessionThreadBase> _sessionThreads = new Dictionary<ulong, ISessionThreadBase>();

        protected readonly DataContainer PDataContainer;

        protected readonly Func<ICallStatisticsTreeNode> NodeConstructor;

        /// <summary>
        /// Create a profiling data provider using the provided source data container and the node constructor
        /// (but don't process the data at the moment).
        /// </summary>
        /// <param name="pDataContainer">
        /// The input data for the data provider in the form of <see cref="DataContainer"/> which contains parsed data
        /// from a saved (already completed) profiling session.
        /// </param>
        /// <param name="nodeConstructor">
        /// Used for creating function call statistics nodes when building the profiling session analytics.
        /// </param>
        public ProfilingDataProvider(DataContainer pDataContainer, Func<ICallStatisticsTreeNode> nodeConstructor)
        {
            PDataContainer = pDataContainer;
            NodeConstructor = nodeConstructor;
        }

        /// <summary>
        /// Process the data from the data container set in the constructor.
        /// </summary>
        public void Load()
        {
            ApplicationTotals = new ProfilingStatisticsTotals(new Dictionary<StatisticsType, ulong>()
            {
                {StatisticsType.Sample, PDataContainer.TotalSamples},
                {StatisticsType.Memory, PDataContainer.TotalAllocatedMemory},
                {StatisticsType.Time, PDataContainer.TotalTime}
            });

            ApplicationCpuUtilization = PDataContainer.CpuUtilizationHistory.CpuList;

            LoadThreads();
        }

        /// <summary>
        /// Build profiling statistics for a selected time frame.
        /// </summary>
        /// <param name="timeFrame">The selected time frame (specifies a time range)</param>
        public void BuildStatistics(ISelectedTimeFrame timeFrame)
        {
            ApplicationStatistics = new ApplicationStatistics();

            ThreadsStatistics = new Dictionary<ulong, IThreadStatistics>();

            _applicationData = new StatisticsData();

            foreach (var thread in _sessionThreads.Values)
            {
                if (thread.InternalId == Thread.FakeThreadId)
                {
                    continue;
                }

                var threadStatistics = new ThreadStatisticsData
                {
                    CpuUtilization = thread.CpuUtilization
                };
                foreach (var s in PDataContainer.Samples[thread.InternalId])
                {
                    if (s.TimeMilliseconds >= timeFrame.Start)
                    {
                        if (s.TimeMilliseconds > timeFrame.End)
                        {
                            break;
                        }

                        ProcessSample(threadStatistics, s);
                    }
                }

                ThreadsStatistics.Add(thread.InternalId, BuildThreadStatistics(threadStatistics));
            }

            ApplicationStatistics = BuildApplicationStatistics();
        }

        public void BuildStatisticsRaw(ISelectedTimeFrame timeFrame)
        {
            ApplicationStatistics = new ApplicationStatistics();

            ThreadsStatisticsRaw = new Dictionary<ulong, IThreadStatisticsRaw>();

            _applicationData = new StatisticsData();

            foreach (var thread in _sessionThreads.Values)
            {
                if (thread.InternalId == Thread.FakeThreadId)
                {
                    continue;
                }

                var threadStatistics = new ThreadStatisticsData
                {
                    CpuUtilization = thread.CpuUtilization
                };
                foreach (var s in PDataContainer.Samples[thread.InternalId])
                {
                    if (s.TimeMilliseconds >= timeFrame.Start)
                    {
                        if (s.TimeMilliseconds > timeFrame.End)
                        {
                            break;
                        }

                        ProcessSample(threadStatistics, s);
                    }
                }

                ThreadsStatisticsRaw.Add(thread.InternalId, BuildThreadStatisticsRaw(threadStatistics));
            }

            ApplicationStatistics = BuildApplicationStatistics();
        }

        private void ProcessSample(ThreadStatisticsData threadData, Sample sample)
        {
            UpdateStatisticsData(threadData, sample);
            UpdateStatisticsData(_applicationData, sample);
            UpdateCallTree(threadData, sample);
        }

        private void UpdateCallTree(ThreadStatisticsData threadData, Sample sample)
        {
            var call = threadData.CallTreeRoot;
            call.SamplesInclusive += sample.Samples;
            call.TimeInclusive += sample.Time;
            call.AllocatedMemoryInclusive += sample.AllocatedMemory;

            var x = sample.StackItems;
            if (x.Count <= 1)
            {
                call.SamplesExclusive += sample.Samples;
                call.TimeExclusive += sample.Time;
                call.AllocatedMemoryExclusive += sample.AllocatedMemory;
            }

            for (int i = x.Count - 2; i >= 0; i--)
            {
                var si = x[i];
                var child = call.FindChildById(si.FunctionIntId);
                if (child == null)
                {
                    var function = PDataContainer.GetFunction(si.FunctionIntId);
                    if (function == null)
                    {
                        Debug.WriteLine("function is NULL");
                        return;
                    }
                    child = new FunctionCall(si.FunctionIntId, function.Name, function.Signature)
                    {
                        Parent = call,
                    };

                    call.Children.Add(child);
                    threadData.FunctionCalls.Add(child);
                }

                child.SamplesInclusive += sample.Samples;
                child.TimeInclusive += sample.Time;
                child.AllocatedMemoryInclusive += sample.AllocatedMemory;

                if (i == 0)
                {
                    child.SamplesExclusive += sample.Samples;
                    child.TimeExclusive += sample.Time;
                    child.AllocatedMemoryExclusive += sample.AllocatedMemory;
                }

                call = child;
            }
        }

        private void UpdateStatisticsData(StatisticsData data, Sample sample)
        {
            data.SamplesTotal += sample.Samples;
            data.TimeTotal += sample.Time;
            data.MemoryTotal += sample.AllocatedMemory;

            for (int i = 0, e = sample.StackItems.Count; i < e; i++)
            {
                var si = sample.StackItems[i];
                var function = PDataContainer.GetFunction(si.FunctionIntId);
                if (function != null)
                {
                    IMethodStatistics ms;
                    if (!data.Methods.TryGetValue(function.InternalId, out ms))
                    {
                        ms = new MethodStatistics
                        {
                            Name = function.Name,
                            Signature = function.Signature
                        };
                        data.Methods.Add(function.InternalId, ms);
                    }

                    ms.SamplesInclusive += sample.Samples;
                    ms.TimeInclusive += sample.Time;
                    ms.AllocatedMemoryInclusive += sample.AllocatedMemory;

                    if (i == 0)
                    {
                        ms.SamplesExclusive += sample.Samples;
                        ms.TimeExclusive += sample.Time;
                        ms.AllocatedMemoryExclusive += sample.AllocatedMemory;
                    }
                }

                if (si.SourceLineId.HasValue && si.SourceLineId.Value != 0)
                {
                    var line = PDataContainer.GetSourceLine(si.SourceLineId.Value);
                    if (line != null)
                    {
                        ISourceLineStatistics ls;
                        if (!data.Lines.TryGetValue(line.InternalId, out ls))
                        {
                            ls = new SourceLineStatistics
                            {
                                StartLine = line.StartLine,
                                EndLine = line.EndLine,
                                StartColumn = line.StartColumn,
                                EndColumn = line.EndColumn,
                                FunctionName = line.FunctionName,
                                SourceFileId = line.SourceFileIntId
                            };
                            data.Lines.Add(line.InternalId, ls);
                        }

                        ls.SamplesInclusive += sample.Samples;
                        ls.TimeInclusive += sample.Time;
                        ls.AllocatedMemoryInclusive += sample.AllocatedMemory;

                        if (i == 0)
                        {
                            ls.SamplesExclusive += sample.Samples;
                            ls.TimeExclusive += sample.Time;
                            ls.AllocatedMemoryExclusive += sample.AllocatedMemory;
                        }
                    }
                }
            }

            foreach (var ai in sample.AllocationItems)
            {
                if (ai.SourceLineId.HasValue && ai.SourceLineId.Value != 0)
                {
                    var line = PDataContainer.GetSourceLine(ai.SourceLineId.Value);
                    if (line != null)
                    {
                        ISourceLineStatistics ls;
                        if (!data.Lines.TryGetValue(line.InternalId, out ls))
                        {
                            ls = new SourceLineStatistics
                            {
                                StartLine = line.StartLine,
                                EndLine = line.EndLine,
                                StartColumn = line.StartColumn,
                                EndColumn = line.EndColumn,
                                FunctionName = line.FunctionName,
                                SourceFileId = line.SourceFileIntId
                            };
                            data.Lines.Add(line.InternalId, ls);
                        }

                        ls.AllocatedMemoryExclusive += ai.MemorySize;
                    }
                }
            }
        }

        private IApplicationStatistics BuildApplicationStatistics()
        {
            var methods = _applicationData.Methods.Values;
            var lines = _applicationData.Lines.Values;
            return new ApplicationStatistics
            {
                Totals = ApplicationTotals,
                Lines = new Dictionary<StatisticsType, List<ISourceLineStatistics>>
                {
                    {
                        StatisticsType.Sample, lines.Where(line => line.SamplesInclusive != 0)
                            .OrderByDescending(line => line.SamplesInclusive)
                            .ToList()
                    },
                    {
                        StatisticsType.Memory, lines.Where(line => line.AllocatedMemoryInclusive != 0)
                            .OrderByDescending(line => line.AllocatedMemoryInclusive)
                            .ToList()
                    },
                    {
                        StatisticsType.Time, lines.Where(line => line.TimeInclusive != 0)
                            .OrderByDescending(line => line.TimeInclusive)
                            .ToList()
                    }
                },
                Methods = new Dictionary<StatisticsType, List<IMethodStatistics>>
                {
                    {
                        StatisticsType.Sample, methods.Where(method => method.SamplesExclusive != 0)
                            .OrderByDescending(method => method.SamplesExclusive)
                            .ToList()
                    },
                    {
                        StatisticsType.Memory, methods.Where(method => method.AllocatedMemoryExclusive != 0)
                            .OrderByDescending(method => method.AllocatedMemoryExclusive)
                            .ToList()
                    },
                    {
                        StatisticsType.Time, methods.Where(method => method.TimeExclusive != 0)
                            .OrderByDescending(method => method.TimeExclusive)
                            .ToList()
                    }
                }
            };
        }

        private IThreadStatisticsRaw BuildThreadStatisticsRaw(ThreadStatisticsData thread)
        {
            var totals = new ProfilingStatisticsTotals(
                new Dictionary<StatisticsType, ulong>()
                {
                    {StatisticsType.Sample, thread.SamplesTotal},
                    {StatisticsType.Memory,thread.MemoryTotal},
                    {StatisticsType.Time, thread.TimeTotal}
                });

            return new ThreadStatisticsRaw
            {
                Totals = totals,
                Methods = thread.Methods.Values.ToList(),
                Lines = thread.Lines.Values.ToList(),
                CallTree = GetThreadCallTreeNodes(thread),
                CpuUtilization = ApplicationCpuUtilization.Count == 0 ? ApplicationCpuUtilization : thread.CpuUtilization
            };
        }

        private IThreadStatistics BuildThreadStatistics(ThreadStatisticsData thread)
        {

            var totals = new ProfilingStatisticsTotals(
                new Dictionary<StatisticsType, ulong>()
                {
                    {StatisticsType.Sample, thread.SamplesTotal},
                    {StatisticsType.Memory,thread.MemoryTotal},
                    {StatisticsType.Time, thread.TimeTotal}
                });

            var threadMethods = thread.Methods.Values;

            var methods = new Dictionary<StatisticsType, List<IMethodStatistics>>
            {
                {
                    StatisticsType.Memory,
                    threadMethods.Where(method => method.AllocatedMemoryExclusive > 0)
                        .OrderByDescending(method => method.AllocatedMemoryExclusive)
                        .ToList()
                },
                {
                    StatisticsType.Sample,
                    threadMethods.Where(method => method.SamplesExclusive > 0)
                        .OrderByDescending(method => method.SamplesExclusive)
                        .ToList()
                },
                {
                    StatisticsType.Time,
                    threadMethods.Where(method => method.TimeExclusive > 0)
                        .OrderByDescending(method => method.TimeExclusive)
                        .ToList()
                }
            };

            var hotPaths = new Dictionary<StatisticsType, List<IHotPath>>
            {
                {
                    StatisticsType.Memory,
                    BuildThreadHotPaths(thread,StatisticsType.Memory, totals, HotPathThreshold)
                },
                {
                    StatisticsType.Sample,
                    BuildThreadHotPaths(thread, StatisticsType.Sample, totals, HotPathThreshold)
                },
                {
                    StatisticsType.Time,
                    BuildThreadHotPaths(thread, StatisticsType.Time, totals, HotPathThreshold)
                }
            };

            var callTree = GetThreadCallTreeNodes(thread);

            var threadLines = thread.Lines.Values;
            var lines = new Dictionary<StatisticsType, List<ISourceLineStatistics>>
            {
                {
                    StatisticsType.Memory,
                    threadLines.Where(line => line.AllocatedMemoryInclusive > 0)
                        .OrderByDescending(line => line.AllocatedMemoryInclusive)
                        .ToList()
                },
                {
                    StatisticsType.Sample,
                    threadLines.Where(line => line.SamplesInclusive > 0)
                        .OrderByDescending(line => line.SamplesInclusive)
                        .ToList()
                },
                {
                    StatisticsType.Time,
                    threadLines.Where(line => line.TimeInclusive > 0)
                        .OrderByDescending(line => line.TimeInclusive)
                        .ToList()
                }
            };

            return new ThreadStatistics
            {
                Totals = totals,
                Methods = methods,
                CallTree = callTree,
                HotPaths = hotPaths,
                Lines = lines,
                CpuUtilization = ApplicationCpuUtilization.Count == 0 ? ApplicationCpuUtilization : thread.CpuUtilization
            };
        }

        private List<IHotPath> BuildThreadHotPaths(ThreadStatisticsData thread, StatisticsType statisticsType, IProfilingStatisticsTotals totals, double hotPathThreshold)
        {

            var maxValue = totals.GetValue(statisticsType);
            return thread.FunctionCalls
                .Where(call => call.SamplesInclusive > 0 &&
                               ((100.0 * FunctionCallValue(call, statisticsType) / maxValue) >= hotPathThreshold))
                .OrderByDescending(call => FunctionCallValue(call, statisticsType))
                .Select<FunctionCall, IHotPath>(call => new HotPath
                {
                    Name = call.Name,
                    Children = CallPath(call),
                    AllocatedMemoryExclusive = call.AllocatedMemoryExclusive,
                    SamplesExclusive = call.SamplesExclusive,
                    TimeExclusive = call.TimeExclusive,
                })
                .ToList();
        }

        private static ulong FunctionCallValue(FunctionCall call, StatisticsType statisticsType)
        {
            switch (statisticsType)
            {
                case StatisticsType.Memory:
                    return call.AllocatedMemoryExclusive;
                case StatisticsType.Sample:
                    return call.SamplesExclusive;
                case StatisticsType.Time:
                    return call.TimeExclusive;
                default:
                    return 0;
            }
        }

        private static List<IHotPathItem> CallPath(FunctionCall call)
        {
            var result = new List<IHotPathItem>();
            for (var prev = call.Parent; prev != null; prev = prev.Parent)
            {
                result.Add(new HotPathItem{Name = prev.Name});
            }

            return result;
        }

        private List<ICallStatisticsTreeNode> GetThreadCallTreeNodes(ThreadStatisticsData thread)
        {
            var result = new List<ICallStatisticsTreeNode>();
            var childNode = CreateCallTreeNode(thread.CallTreeRoot, null);
            if (childNode != null)
            {
                result.Add(childNode);
            }

            return result;
        }

        private ICallStatisticsTreeNode CreateCallTreeNode(FunctionCall call, ICallStatisticsTreeNode parent)
        {
            if (call.SamplesInclusive == 0)
            {
                return null;
            }

            var node = NodeConstructor();
            if (node == null)
            {
                Debug.WriteLine("node is NULL");
                return null;
            }
            node.Parent = parent;
            node.Name = call.Name;
            node.SamplesInclusive = call.SamplesInclusive;
            node.SamplesExclusive = call.SamplesExclusive;
            node.AllocatedMemoryInclusive = call.AllocatedMemoryInclusive;
            node.AllocatedMemoryExclusive = call.AllocatedMemoryExclusive;
            node.TimeInclusive = call.TimeInclusive;
            node.TimeExclusive = call.TimeExclusive;
            foreach (var child in call.Children)
            {
                var childNode = CreateCallTreeNode(child, node);
                if (childNode != null)
                {
                    node.Children.Add(childNode);
                }
            }

            return node;
        }

        private void LoadThreads()
        {
            foreach (var thread in PDataContainer.Threads.Collection)
            {
                _sessionThreads.Add(thread.InternalId, new SessionThreadBase
                {
                    InternalId = thread.InternalId,
                    OsThreadId = thread.OsThreadId,
                    ClrJobs = BuildClrJobs(thread),
                    CpuUtilization = thread.CpuUtilizationHistory.CpuList
                });
            }
        }

        private List<ClrJob> BuildClrJobs(Thread thread)
        {
            var result = new List<ClrJob>();

            if (thread.Events == null)
            {
                return result;
            }

            JitCompilationStarted jitCompilationStarted = null;
            Event jitCompilationStartedEvent = null;

            JitCachedFunctionSearchStarted jitCachedFunctionSearchStarted = null;
            Event jitCachedFunctionSearchStartedEvent = null;

            GarbageCollectionStarted garbageCollectionStarted = null;
            Event garbageCollectionStartedEvent = null;

            ClrJob prevClrJobJit = null;
            ClrJob prevClrJobGc = null;
            foreach (Event threadEvent in thread.Events)
            {
                ClrJob prevClrJob = null;
                ClrJob clrJob = null;
                ulong startMilliseconds = 0;
                ulong endMilliseconds = 0;
                switch (threadEvent.EventType)
                {
                    case EventType.CompilationStarted:
                        jitCompilationStartedEvent = threadEvent;
                        jitCompilationStarted = (JitCompilationStarted)threadEvent.SourceObject;
                        break;

                    case EventType.CompilationFinished:
                        if (jitCompilationStarted != null)
                        {
                            var jitCompilationFinished = (JitCompilationFinished)threadEvent.SourceObject;
                            if (jitCompilationFinished.FunctionId == jitCompilationStarted.FunctionId)
                            {
                                clrJob = new ClrJob { Type = ClrJobType.JustInTimeCompilation };
                                prevClrJob = prevClrJobJit;
                                startMilliseconds = jitCompilationStartedEvent.TimeMilliseconds;
                                endMilliseconds = threadEvent.TimeMilliseconds;
                            }
                            jitCompilationStarted = null;
                        }
                        break;

                    case EventType.CachedFunctionSearchStarted:
                        jitCachedFunctionSearchStartedEvent = threadEvent;
                        jitCachedFunctionSearchStarted = (JitCachedFunctionSearchStarted)threadEvent.SourceObject;
                        break;

                    case EventType.CachedFunctionSearchFinished:
                        if (jitCachedFunctionSearchStarted != null)
                        {
                            var jitCachedFunctionSearchFinished = (JitCachedFunctionSearchFinished)threadEvent.SourceObject;
                            if (jitCachedFunctionSearchFinished.FunctionId == jitCachedFunctionSearchStarted.FunctionId)
                            {
                                clrJob = new ClrJob { Type = ClrJobType.JustInTimeCompilation };
                                prevClrJob = prevClrJobJit;
                                startMilliseconds = jitCachedFunctionSearchStartedEvent.TimeMilliseconds;
                                endMilliseconds = threadEvent.TimeMilliseconds;
                            }
                            jitCachedFunctionSearchStarted = null;
                        }
                        break;

                    case EventType.GarbageCollectionStarted:
                        garbageCollectionStartedEvent = threadEvent;
                        garbageCollectionStarted = (GarbageCollectionStarted)threadEvent.SourceObject;
                        break;

                    case EventType.GarbageCollectionFinished:
                        if (garbageCollectionStarted != null)
                        {
                            var garbageCollectionFinished = (GarbageCollectionFinished)threadEvent.SourceObject;
                            clrJob = new ClrJob { Type = ClrJobType.GarbageCollection };
                            prevClrJob = prevClrJobGc;
                            startMilliseconds = garbageCollectionStartedEvent.TimeMilliseconds;
                            endMilliseconds = threadEvent.TimeMilliseconds;
                            garbageCollectionStarted = null;
                        }
                        break;
                }
                if (clrJob != null)
                {
                    const int Factor = 1000000;
                    clrJob.StartNanoseconds = startMilliseconds * Factor;
                    clrJob.EndNanoseconds = endMilliseconds * Factor;
                    if (prevClrJob != null)
                    {
                        if (clrJob.StartNanoseconds <= prevClrJob.EndNanoseconds)
                        {
                            // intersecting with previous event
                            if (clrJob.EndNanoseconds <= prevClrJob.EndNanoseconds)
                            {
                                // ignore duplicate events or (unexpected case) events which are contained in previous ones
                                if (clrJob.StartNanoseconds >= prevClrJob.StartNanoseconds)
                                {
                                    continue;
                                }
                                // else unexpected: the next event start is before the previous event start
                            }
                            else // clrJob.EndNanoseconds > prevClrJob.EndNanoseconds
                            {
                                // combine events
                                if (clrJob.StartNanoseconds >= prevClrJob.StartNanoseconds)
                                {
                                    prevClrJob.EndNanoseconds = clrJob.EndNanoseconds;
                                }
                                else
                                {
                                    // unexpected case but support it
                                    prevClrJob.StartNanoseconds = clrJob.StartNanoseconds;
                                }
                                continue;
                            }
                        }
                    }
                    result.Add(clrJob);
                    if (clrJob.Type == ClrJobType.GarbageCollection)
                    {
                        prevClrJobGc = clrJob;
                    }
                    else if (clrJob.Type == ClrJobType.JustInTimeCompilation)
                    {
                        prevClrJobJit = clrJob;
                    }
                }
            } // foreach

            return result;
        }

        /// <summary>
        /// Save CLR jobs (JIT/GC) to file (for debugging)
        /// </summary>
        /// <param name="fileName"></param>
        [Conditional("DEBUG")]
        public void SaveClrJobs(string fileName)
        {
            using (var writer = new StreamWriter(fileName, false))
            {
                var allClrJobs = new List<Tuple<ClrJob, ulong>>();
                foreach (ISessionThreadBase thread in _sessionThreads.Values)
                {
                    allClrJobs.AddRange(thread.ClrJobs.Select(clrJob => new Tuple<ClrJob, ulong>(clrJob, thread.OsThreadId)));
                }
                allClrJobs.Sort((x, y) =>
                {
                    ClrJob job1 = x.Item1;
                    ClrJob job2 = y.Item1;
                    int cmp = job1.Type.CompareTo(job2.Type);
                    if (cmp == 0)
                    {
                        cmp = job1.StartNanoseconds.CompareTo(job2.StartNanoseconds);
                        if (cmp == 0)
                        {
                            cmp = job1.EndNanoseconds.CompareTo(job2.EndNanoseconds);
                        }
                    }
                    return cmp;
                });
                ClrJob prevJob = null;
                int n = 0;
                foreach (var tuple in allClrJobs)
                {
                    ClrJob job = tuple.Item1;
                    if (job == null)
                    {
                        continue;
                    }
                    if ((prevJob != null) && (prevJob.Type != job.Type))
                    {
                        n = 0;
                    }
                    prevJob = job;
                    writer.WriteLine($"#{++n:D4} {job.Type} (TID={tuple.Item2}) " +
                        $"{(job.StartNanoseconds / 1E6):F3} .. {(job.EndNanoseconds / 1E6):F3}");
                }
            }
        }
    }
}
