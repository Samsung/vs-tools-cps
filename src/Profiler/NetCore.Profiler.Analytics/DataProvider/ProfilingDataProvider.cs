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
using NetCore.Profiler.Analytics.Model;
using NetCore.Profiler.Cperf.Core;
using NetCore.Profiler.Cperf.Core.Model;
using NetCore.Profiler.Lttng.Core.BObject;

namespace NetCore.Profiler.Analytics.DataProvider
{
    public class ProfilingDataProvider : IDataProvider
    {

        public ulong MinimalStartTime { get; private set; } = ulong.MaxValue;

        public double HotPathThreshold { get; set; } = 0.1;

        public List<CpuUtilization> ApplicationCpuUtilization { get; private set; } = new List<CpuUtilization>();

        private ProfilingStatisticsTotals ApplicationTotals { get; set; }

        public IApplicationStatistics ApplicationStatistics { get; private set; }

        public Dictionary<ulong, IThreadStatistics> ThreadsStatistics { get; private set; }

        public Dictionary<ulong, IThreadStatisticsRaw> ThreadsStatisticsRaw { get; private set; }

        private StatisticsData _applicationData;

        public IEnumerable<ISessionThreadBase> SessionThreads => _sessionThreads.Values.Where(t => t.InternalId != Thread.FakeThreadId);

        private readonly Dictionary<ulong, ISessionThreadBase> _sessionThreads = new Dictionary<ulong, ISessionThreadBase>();

        protected readonly BDataContainer BDataContainer;

        protected readonly DataContainer PDataContainer;

        protected readonly Func<ICallStatisticsTreeNode> NodeConstructor;

        public ProfilingDataProvider(BDataContainer bDataContainer, DataContainer pDataContainer, Func<ICallStatisticsTreeNode> nodeConstructor)
        {
            BDataContainer = bDataContainer;
            PDataContainer = pDataContainer;
            NodeConstructor = nodeConstructor;
        }

        public void Load()
        {
            MinimalStartTime = BDataContainer.BThreads.Count > 0
                ? BDataContainer.BThreads.Min(thread => thread.StartAt)
                : ulong.MaxValue;

            ApplicationTotals = new ProfilingStatisticsTotals(new Dictionary<StatisticsType, ulong>()
            {
                {StatisticsType.Sample, PDataContainer.TotalSamples},
                {StatisticsType.Memory, PDataContainer.TotalAllocatedMemory},
                {StatisticsType.Time, PDataContainer.TotalTime}
            });

            ApplicationCpuUtilization = PDataContainer.CpuUtilizationHistory.CpuList;

            LoadThreads();
        }


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
                    if (s.Timestamp >= timeFrame.Start)
                    {
                        if (s.Timestamp > timeFrame.End)
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
                    if (s.Timestamp >= timeFrame.Start)
                    {
                        if (s.Timestamp > timeFrame.End)
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
            foreach (var thread in PDataContainer.Threads.Values)
            {

                var bthread = BDataContainer.BThreads.Find(bt => (thread.OsThreadId == bt.Tid));
                var clrTasks = bthread == null ? new List<ClrJob>() : BuildClrTasks(bthread);

                _sessionThreads.Add(thread.InternalId, new SessionThreadBase
                {
                    InternalId = thread.InternalId,
                    OsThreadId = thread.OsThreadId,
                    ClrJobs = clrTasks,
                    CpuUtilization = thread.CpuUtilizationHistory.CpuList
                });

            }

        }

        private List<ClrJob> BuildClrTasks(BThread bThread)
        {
            var jobs = bThread.Jits.Where(jit => jit.IsFull)
                .Select(jit => new BThread.SomeJob
                {
                    StartTime = jit.JobStartAt,
                    EndTime = jit.JobEndAt,
                    Type = BThread.SomeJobType.Jit
                }).ToList();


            foreach (var gcItem in bThread.GCItems)
            {
                if (gcItem.IsFull != true)
                {
                    //we don't have full information
                    continue;
                }

                var isAdded = false;
                // search of jit job in which gc job can be place
                for (int i = 0; i < jobs.Count; i++)
                {
                    BThread.SomeJob someJob = jobs[i];
                    if (someJob.Type != BThread.SomeJobType.GC && gcItem.JobStartAt > someJob.StartTime && gcItem.JobStartAt < someJob.EndTime) // if gc job inside jit job
                    {
                        // lets know how it overlaps antoher
                        if (gcItem.JobEndAt < someJob.EndTime) // if gc is smaller than jit (how it must be)
                        {
                            //split large job
                            var firstPart = new BThread.SomeJob { StartTime = someJob.StartTime, EndTime = gcItem.JobStartAt, Type = BThread.SomeJobType.Jit };
                            var secondPart = new BThread.SomeJob { StartTime = gcItem.JobEndAt, EndTime = someJob.EndTime, Type = BThread.SomeJobType.Jit };
                            // create gc job
                            var gcPart = new BThread.SomeJob { StartTime = gcItem.JobStartAt, EndTime = gcItem.JobEndAt, Type = BThread.SomeJobType.GC };
                            // replace old job with new ones
                            jobs.RemoveAt(i);
                            jobs.Insert(i, secondPart);
                            jobs.Insert(i, gcPart);
                            jobs.Insert(i, firstPart);
                            i += 2;
                            isAdded = true;
                        }
                    }
                }

                if (!isAdded)
                {
                    // create gc job
                    var gcJob = new BThread.SomeJob { StartTime = gcItem.JobStartAt, EndTime = gcItem.JobEndAt, Type = BThread.SomeJobType.GC };
                    var i = bThread.GetClosestJob(gcJob, jobs);
                    if (i == -1) // if not found insert to the end
                    {
                        jobs.Add(gcJob);
                    }
                    else // insert at index place
                    {
                        jobs.Insert(i, gcJob);
                    }
                }
            }

            return jobs.Select(job => new ClrJob()
            {
                Type = job.Type == BThread.SomeJobType.GC ? ClrJobType.GarbageCollection : (job.Type == BThread.SomeJobType.Jit ? ClrJobType.JustInTimeCompilation : ClrJobType.None),
                StartTime = job.StartTime,
                EndTime = job.EndTime
            }).ToList();
        }

    }
}
