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
using System.Linq;
using NetCore.Profiler.Common;
using NetCore.Profiler.Cperf.Core.Model;
using NetCore.Profiler.Cperf.Core.Parser;
using NetCore.Profiler.Cperf.Core.Parser.Model;
using SourceFile = NetCore.Profiler.Cperf.Core.Model.SourceFile;
using SourceLine = NetCore.Profiler.Cperf.Core.Model.SourceLine;

namespace NetCore.Profiler.Cperf.Core
{
    /// <summary>
    /// A data container used to load and store in memory structures the data from saved (already completed) %Core %Profiler
    /// profiling sessions so they can be used by <see cref="ProfilingDataProvider"/>.
    /// </summary>
    public class DataContainer
    {
        private readonly string _filePath;

        public CpuUtilizationHistory CpuUtilizationHistory { get; private set; }

        public Dictionary<ulong, ApplicationDomain> ApplicationDomains { get; } = new Dictionary<ulong, ApplicationDomain>();

        public Dictionary<ulong, Assembly> Assemblies { get; } = new Dictionary<ulong, Assembly>();

        public Dictionary<ulong, Module> Modules { get; } = new Dictionary<ulong, Module>();

        public Dictionary<ulong, Class> Classes { get; } = new Dictionary<ulong, Class>();

        public Dictionary<ulong, Function> Functions { get; } = new Dictionary<ulong, Function>();

        public class ThreadContainer
        {
            internal ThreadContainer()
            {
                // create & add stub thread, to present common events
                Add(FakeThread = new Thread() { InternalId = Thread.FakeThreadId });
            }

            public void Add(Thread thread)
            {
                threadByInternalId.Add(thread.InternalId, thread);
            }

            public Thread GetByInternalId(ulong internalId)
            {
                Thread result;
                threadByInternalId.TryGetValue(internalId, out result);
                return result;
            }

            public Thread GetByOsThreadId(ulong osThreadId)
            {
                return threadByInternalId.FirstOrDefault(pair => (pair.Value.OsThreadId == osThreadId)).Value;
            }

            public void ClearOsThreadId(ulong osThreadId)
            {
                foreach (var pair in threadByInternalId.Where(p => (p.Value.OsThreadId == osThreadId)))
                {
                    pair.Value.OsThreadId = 0;
                }
            }

            public ICollection<Thread> Collection
            {
                get { return threadByInternalId.Values; }
            }

            public Thread FakeThread { get; private set; }

            private Dictionary<ulong, Thread> threadByInternalId = new Dictionary<ulong, Thread>();
        }

        public ThreadContainer Threads = new ThreadContainer();

        public Dictionary<ulong, SourceFile> SourceFiles { get; } = new Dictionary<ulong, SourceFile>();

        public Dictionary<ulong, SourceLine> SourceLines { get; } = new Dictionary<ulong, SourceLine>();

        public Dictionary<ulong, List<Sample>> Samples { get; } = new Dictionary<ulong, List<Sample>>();

        public ulong TotalSamples { get; private set; }

        public ulong TotalTime { get; private set; }

        public ulong TotalAllocatedMemory { get; private set; }

        private ulong _globalTimeMilliseconds;

        private ulong _resumeTimeMilliseconds;

        private bool _profilingPaused;

        /// <summary>
        /// This value shall be added to profiling events' timestamps to get correct time for display
        /// </summary>
        private ulong _profilingEventsDeltaMilliseconds;

        // key: internal thread Id
        private readonly Dictionary<ulong, ThreadStackInfo> _stackByThread = new Dictionary<ulong, ThreadStackInfo>();

        /// <summary>
        /// Create a data container for the specified profiling session file (but don't load the data at the moment).
        /// </summary>
        /// <param name="filePath">The profiling session file path and name.</param>
        /// <param name="cpuCoreCount">
        /// The target system CPU core count
        /// </param>
        public DataContainer(string filePath, int cpuCoreCount)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException(nameof(filePath));
            }

            _filePath = filePath;

            CpuUtilizationHistory = new CpuUtilizationHistory(cpuCoreCount);

            var rootFunc = new Function { Name = "<ROOT>", Signature = "<EMPTY>" };
            Functions.Add(rootFunc.InternalId, rootFunc);
        }

        /// <summary>
        /// Load the data from the profiling session file set in the constructor.
        /// </summary>
        /// <param name="progressMonitor">Used to report the profiling session file load progress.</param>
        /// <param name="sysInfoStartTime">
        /// The system information start time (used to correct the times of profiling events)
        /// </param>
        /// <param name="profilerStartTime">
        /// The profiler start time (according to a clock of a target Tizen system) is returned in this output parameter.
        /// </param>
        public void Load(ProgressMonitor progressMonitor, DateTime sysInfoStartTime, out DateTime profilerStartTime)
        {
            _globalTimeMilliseconds = 0;
            _profilingEventsDeltaMilliseconds = 0;

            var parser = new CperfParser();

            Action<string> lineReadCallback = (s => progressMonitor.Tick());

            DateTime startTimeFromParser = DateTime.MinValue;
            Action<DateTime> startTimeCallback = (DateTime startTime) =>
            {
                startTimeFromParser = startTime;
                if ((startTime.Kind == DateTimeKind.Utc) && (sysInfoStartTime != DateTime.MinValue) && (startTime > sysInfoStartTime))
                {
                    _profilingEventsDeltaMilliseconds = (ulong)Math.Round((startTime - sysInfoStartTime).TotalMilliseconds);
                }
            };

            parser.LineReadCallback += lineReadCallback;
            parser.StartTimeCallback += startTimeCallback;
            parser.ApplicationDomainCreationFinishedCallback += ApplicationDomainCreationFinishedCallback;
            parser.AssemblyLoadFinishedCallback += AssemblyLoadFinishedCallback;
            parser.ModuleAttachedToAssemblyCallback += ModuleAttachedToAssemblyCallback;
            parser.ModuleLoadFinishedCallback += ModuleLoadFinishedCallback;
            parser.ClassLoadFinishedCallback += ClassLoadFinishedCallback;
            parser.ClassNameReadCallback += ClassNameReadCallback;
            parser.FunctionNameReadCallback += FunctionNameReadCallback;
            parser.FunctionInfoCallback += FunctionInfoCallback;
            parser.JitCompilationStartedCallback += JitCompilationStartedCallback;
            parser.JitCompilationFinishedCallback += JitCompilationFinishedCallback;
            parser.JitCachedFunctionSearchStartedCallback += JitCachedFunctionSearchStartedCallback;
            parser.JitCachedFunctionSearchFinishedCallback += JitCachedFunctionSearchFinishedCallback;
            parser.GarbageCollectionStartedCallback += GarbageCollectionStartedCallback;
            parser.GarbageCollectionFinishedCallback += GarbageCollectionFinishedCallback;
            parser.SourceLineReadCallback += SourceLineReadCallback;
            parser.SourceFileReadCallback += SourceFileReadCallback;
            parser.CpuReadCallback += CpuReadCallback;
            parser.ProfilerTpsReadCallback += ProfilerTpsReadCallback;
            parser.ProfilerTrsReadCallback += ProfilerTrsReadCallback;
            parser.ThreadAssignedToOsThreadCallback += ThreadAssignedToOsThreadCallback;
            parser.ThreadTimesReadCallback += ThreadTimesReadCallback;
            parser.ThreadCreatedCallback += ThreadCreatedCallback;
            parser.ThreadDestroyedCallback += ThreadDestroyedCallback;
            parser.StackSampleReadCallback += StackSampleReadCallback;
            parser.AllocationSampleReadCallback += AllocationSampleReadCallback;

            parser.Parse(_filePath);

            profilerStartTime = startTimeFromParser;

            parser.LineReadCallback -= lineReadCallback;
            parser.StartTimeCallback -= startTimeCallback;
            parser.ApplicationDomainCreationFinishedCallback -= ApplicationDomainCreationFinishedCallback;
            parser.AssemblyLoadFinishedCallback -= AssemblyLoadFinishedCallback;
            parser.ModuleAttachedToAssemblyCallback -= ModuleAttachedToAssemblyCallback;
            parser.ModuleLoadFinishedCallback -= ModuleLoadFinishedCallback;
            parser.ClassLoadFinishedCallback -= ClassLoadFinishedCallback;
            parser.ClassNameReadCallback -= ClassNameReadCallback;
            parser.FunctionNameReadCallback -= FunctionNameReadCallback;
            parser.FunctionInfoCallback -= FunctionInfoCallback;
            parser.JitCompilationStartedCallback -= JitCompilationStartedCallback;
            parser.JitCompilationFinishedCallback -= JitCompilationFinishedCallback;
            parser.JitCachedFunctionSearchStartedCallback -= JitCachedFunctionSearchStartedCallback;
            parser.JitCachedFunctionSearchFinishedCallback -= JitCachedFunctionSearchFinishedCallback;
            parser.GarbageCollectionStartedCallback -= GarbageCollectionStartedCallback;
            parser.GarbageCollectionFinishedCallback -= GarbageCollectionFinishedCallback;
            parser.SourceLineReadCallback -= SourceLineReadCallback;
            parser.SourceFileReadCallback -= SourceFileReadCallback;
            parser.CpuReadCallback -= CpuReadCallback;
            parser.ProfilerTpsReadCallback -= ProfilerTpsReadCallback;
            parser.ProfilerTrsReadCallback -= ProfilerTrsReadCallback;
            parser.ThreadAssignedToOsThreadCallback -= ThreadAssignedToOsThreadCallback;
            parser.ThreadTimesReadCallback -= ThreadTimesReadCallback;
            parser.ThreadCreatedCallback -= ThreadCreatedCallback;
            parser.ThreadDestroyedCallback -= ThreadDestroyedCallback;
            parser.StackSampleReadCallback -= StackSampleReadCallback;
            parser.AllocationSampleReadCallback -= AllocationSampleReadCallback;
        }

        private Event CreateEvent(object sourceObject, ulong timeMilliseconds, EventType type)
        {
            return new Event(sourceObject, timeMilliseconds + _profilingEventsDeltaMilliseconds, type);
        }

        private void ApplicationDomainCreationFinishedCallback(ApplicationDomainCreationFinished arg)
        {
            var applicationDomain = new ApplicationDomain
            {
                InternalId = arg.InternalId,
                Name = arg.Name,
                ProcessId = arg.ProcessId
            };
            AddApplicationDomain(applicationDomain);

            Threads.GetByOsThreadId(applicationDomain.ProcessId).Events
                .Add(CreateEvent(applicationDomain, _globalTimeMilliseconds, EventType.CreationFinished));
        }

        private void AssemblyLoadFinishedCallback(AssemblyLoadFinished arg)
        {
            var assembly = new Assembly(arg.ApplicationDomainId, arg.InternalId, arg.ModuleId, arg.Name);
            AddAssembly(assembly);
            AddGlobalEvent(CreateEvent(assembly, _globalTimeMilliseconds, EventType.LoadFinished));
        }

        private void ModuleAttachedToAssemblyCallback(ModuleAttachedToAssembly arg)
        {
            Module module = GetModule(arg.ModuleId);
            if (module == null)
            {
                AddModule(module = new Module { InternalId = arg.ModuleId, AssemblyId = arg.AssemblyId });
            }
            else
            {
                module.AssemblyId = arg.AssemblyId;
            }

            AddGlobalEvent(CreateEvent(module, _globalTimeMilliseconds, EventType.AttachedToAssembly));
        }

        private void ModuleLoadFinishedCallback(ModuleLoadFinished arg)
        {
            Module module = GetModule(arg.ModuleId);
            if (module == null)
            {
                AddModule(module = new Module { InternalId = arg.ModuleId, AssemblyId = arg.AssemblyId, Name = arg.ModuleName });
            }
            else
            {
                module.AssemblyId = arg.AssemblyId;
                module.Name = arg.ModuleName;
            }

            module.ModuleLoadRecords.Add(new ModuleLoadInfo { BaseLoadAddress = arg.BaseLoadAddress });

            AddGlobalEvent(CreateEvent(module, _globalTimeMilliseconds, EventType.LoadFinished));
        }

        private void ClassLoadFinishedCallback(ClassLoadFinished arg)
        {
            Class @class = GetClass(arg.InternalId);
            if (@class == null)
            {
                AddClass(@class = new Class
                {
                    InternalId = arg.InternalId,
                    Id = arg.Id,
                    ModuleId = arg.ModuleId,
                    Token = arg.ClassToken
                });
            }
            else
            {
                @class.Id = arg.Id;
                @class.ModuleId = arg.ModuleId;
                @class.Token = arg.ClassToken;
            }

            AddGlobalEvent(CreateEvent(@class, _globalTimeMilliseconds, EventType.LoadFinished));
        }

        private void ClassNameReadCallback(ClassName arg)
        {
            Class @class = GetClass(arg.InternalId);
            if (@class == null)
            {
                AddClass(new Class { InternalId = arg.InternalId, Name = arg.Name });
            }
            else
            {
                @class.Name = arg.Name;
            }
        }

        private void FunctionNameReadCallback(FunctionName arg)
        {
            Function function = GetFunction(arg.InternalId);
            if (function == null)
            {
                AddFunction(new Function { InternalId = arg.InternalId, Name = arg.FullName, Signature = arg.ReturnType + arg.Signature });
            }
            else
            {
                function.Name = arg.FullName;
                function.Signature = arg.ReturnType + arg.Signature;
            }
        }

        private void FunctionInfoCallback(FunctionInfo arg)
        {
            Function function = GetFunction(arg.InternalId);
            if (function == null)
            {
                AddFunction(function = new Function { InternalId = arg.InternalId, Id = arg.Id, ClassId = arg.ClassId, ModuleId = arg.ModuleId });
            }
            else
            {
                function.Id = arg.Id;
                function.ClassId = arg.ClassId;
                function.ModuleId = arg.ModuleId;
            }
        }

        private void JitCompilationStartedCallback(JitCompilationStarted arg)
        {
            AddEvent(arg.OsThreadId, CreateEvent(arg, arg.Timestamp, EventType.CompilationStarted));
        }

        private void JitCompilationFinishedCallback(JitCompilationFinished arg)
        {
            AddEvent(arg.OsThreadId, CreateEvent(arg, arg.Timestamp, EventType.CompilationFinished));
        }

        private void JitCachedFunctionSearchStartedCallback(JitCachedFunctionSearchStarted arg)
        {
            AddEvent(arg.OsThreadId, CreateEvent(arg, arg.Timestamp, EventType.CachedFunctionSearchStarted));
        }

        private void JitCachedFunctionSearchFinishedCallback(JitCachedFunctionSearchFinished arg)
        {
            AddEvent(arg.OsThreadId, CreateEvent(arg, arg.Timestamp, EventType.CachedFunctionSearchFinished));
        }

        private void GarbageCollectionStartedCallback(GarbageCollectionStarted arg)
        {
            AddEvent(arg.OsThreadId, CreateEvent(arg, arg.Timestamp, EventType.GarbageCollectionStarted));
        }

        private void GarbageCollectionFinishedCallback(GarbageCollectionFinished arg)
        {
            AddEvent(arg.OsThreadId, CreateEvent(arg, arg.Timestamp, EventType.GarbageCollectionFinished));
        }

        private void SourceLineReadCallback(Parser.Model.SourceLine arg)
        {
            SourceLine sourceLine = GetSourceLine(arg.Id);
            if (sourceLine == null)
            {
                AddSourceLine(new SourceLine
                {
                    InternalId = arg.Id,
                    FunctionIntId = arg.FunctionIntId,
                    SourceFileIntId = arg.SourceFileIntId,
                    StartLine = arg.StartLine,
                    StartColumn = arg.StartColumn,
                    EndLine = arg.EndLine,
                    EndColumn = arg.EndColumn,
                    FunctionName = Functions[arg.FunctionIntId].Name
                });
            }
            else
            {
                sourceLine.FunctionIntId = arg.FunctionIntId;
                sourceLine.SourceFileIntId = arg.SourceFileIntId;
                sourceLine.StartLine = arg.StartLine;
                sourceLine.StartColumn = arg.StartColumn;
                sourceLine.EndLine = arg.EndLine;
                sourceLine.EndColumn = arg.EndColumn;
            }
        }

        private void SourceFileReadCallback(Parser.Model.SourceFile arg)
        {
            SourceFile sourceFile = GetSourceFile(arg.InternalId);
            if (sourceFile == null)
            {
                AddSourceFile(new SourceFile { InternalId = arg.InternalId, Name = arg.Path });
            }
            else
            {
                sourceFile.Name = arg.Path;
            }
        }

        private void CpuReadCallback(Cpu arg)
        {
            CpuUtilizationHistory.RecordCpuUsage(arg.Timestamp, arg.Duration);
        }

        private void ProfilerTpsReadCallback(ProfilerTps arg)
        {
            _profilingPaused = true;
            _resumeTimeMilliseconds = 0;

            CpuUtilizationHistory.RecordProfilingPaused();
            foreach (var thread in Threads.Collection)
            {
                thread.CpuUtilizationHistory.RecordProfilingPaused();
            }
        }

        private void ProfilerTrsReadCallback(ProfilerTrs arg)
        {
            _profilingPaused = false;
            _resumeTimeMilliseconds = arg.Value;

            CpuUtilizationHistory.RecordProfilingResumed();
            foreach (var thread in Threads.Collection)
            {
                thread.CpuUtilizationHistory.RecordProfilingResumed();
            }
        }

        private void ThreadAssignedToOsThreadCallback(ThreadAssignedToOsThread arg)
        {
            Thread thread = GetThreadByInternalId(arg.InternalId);
            if (thread == null)
            {
                thread = new Thread { InternalId = arg.InternalId, OsThreadId = arg.OsThreadId };
                AddThread(thread, _globalTimeMilliseconds);
            }
            else
            {
                // CLR can reassign managed threads to different OS threads
                Threads.ClearOsThreadId(arg.OsThreadId);
                thread.OsThreadId = arg.OsThreadId;
            }

            AddGlobalEvent(CreateEvent(thread, _globalTimeMilliseconds, EventType.AssignedToOsThread));
        }

        private void ThreadTimesReadCallback(ThreadTimes arg)
        {
            //Temporary solution until situation with missing thread create record is cleared
            if (GetThreadByInternalId(arg.InternalId) == null)
            {
                AddThread(new Thread { InternalId = arg.InternalId }, arg.TicksFromStart);
            }

            Threads.GetByInternalId(arg.InternalId).CpuUtilizationHistory.RecordCpuUsage(arg.TicksFromStart, arg.UserTime);
        }

        private void ThreadCreatedCallback(ThreadCreated arg)
        {
            Thread thread = GetThreadByInternalId(arg.InternalId);
            if (thread == null)
            {
                thread = new Thread { InternalId = arg.InternalId, Id = arg.ThreadId };
                AddThread(thread, _globalTimeMilliseconds);
            }
            else
            {
                thread.Id = arg.ThreadId;
            }

            AddGlobalEvent(CreateEvent(thread, _globalTimeMilliseconds, EventType.ThreadCreated));
        }

        private void ThreadDestroyedCallback(ThreadDestroyed arg)
        {
            AddGlobalEvent(CreateEvent(null, _globalTimeMilliseconds, EventType.ThreadDestroyed));
        }

        private void StackSampleReadCallback(StackSample arg)
        {
            if (_profilingPaused)
            {
                return;
            }

            var thread = _stackByThread[arg.InternalId];
            var duration = arg.Ticks - Math.Max(thread.LastSampleTimestamp, _resumeTimeMilliseconds);

            if (UpdateCallTree(arg, thread))
            {
                return;
            }

            if (arg.Count > 0) //Do not account empty records preceding the memory samples just to set stack frame
            {
                TotalSamples += arg.Count;
                TotalTime += duration;

                thread.LastSampleTimestamp = arg.Ticks;

                var sample = new Sample
                {
                    ThreadIntId = arg.InternalId,
                    TimeMilliseconds = arg.Ticks,
                    Samples = arg.Count,
                    Time = duration
                };
                FillSampleStackItems(sample, thread.TopFunctionCall);
                StoreSample(sample);
            }

            _globalTimeMilliseconds = arg.Ticks;
        }

        private bool UpdateCallTree(StackSample arg, ThreadStackInfo thread)
        {
            var lastFunctionCall = thread.TopFunctionCall;

            if (arg.MatchPrefixSize == 0) //Empty stack
            {
                lastFunctionCall = thread.RootFunctionCall;
            }
            else //Unwind stack till last unchanged frame
            {
                var depth = arg.StackSize - arg.MatchPrefixSize;
                while (lastFunctionCall != null && depth > 0)
                {
                    lastFunctionCall = lastFunctionCall.Parent;
                    depth--;
                }

                if (lastFunctionCall == null)
                {
                    //Something's gone wrong
                    return true;
                }

                thread.TopFunctionCall = lastFunctionCall;
            }

            //Ip has changed
            if (arg.Ip.HasValue)
            {
                lastFunctionCall.Ip = arg.Ip;
            }

            ProcessFunctionCall(thread, lastFunctionCall, arg.Frames, 0);

            return false;
        }

        private void ProcessFunctionCall(ThreadStackInfo thread, StackFrame call, IReadOnlyList<StackSampleFrame> frames, int index)
        {
            while (true)
            {
                if (index >= frames.Count)
                {
                    thread.TopFunctionCall = call;
                    return;
                }

                var frame = frames[index];
                var child = new StackFrame
                {
                    Parent = call,
                    FunctionIntId = frame.InternalId,
                    Ip = frame.Ip
                };

                call = child;
                index = index + 1;
            }
        }

        private void AllocationSampleReadCallback(AllocationSample arg)
        {
            if (_profilingPaused)
            {
                return;
            }

            var thread = _stackByThread[arg.InternalId];
            var allocated = arg.Allocations.Aggregate<AllocationSampleInfo, ulong>(0, (current, item) => current + item.MemorySize);

            TotalAllocatedMemory += allocated;

            var sample = new Sample
            {
                ThreadIntId = arg.InternalId,
                TimeMilliseconds = arg.Ticks,
                AllocatedMemory = allocated
            };

            FillSampleStackItems(sample, thread.TopFunctionCall);
            FillSampleAllocationItems(sample, arg.Allocations);
            StoreSample(sample);
        }

        private void StoreSample(Sample sample)
        {
            Samples[sample.ThreadIntId].Add(sample);
        }

        private void FillSampleStackItems(Sample sample, StackFrame top)
        {
            for (var parent = top; parent != null; parent = parent.Parent)
            {
                sample.StackItems.Add(new SampleStackItem
                {
                    FunctionIntId = parent.FunctionIntId,
                    SourceLineId = parent.Ip
                });
            }
        }

        private void FillSampleAllocationItems(Sample sample, IEnumerable<AllocationSampleInfo> allocations)
        {
            foreach (var allocation in allocations)
            {
                sample.AllocationItems.Add(new SampleAllocationItem
                {
                    AllocationCount = allocation.AllocationCount,
                    MemorySize = allocation.MemorySize,
                    SourceLineId = allocation.Ip
                });
            }
        }

        private void AddSourceFile(SourceFile sourceFile)
        {
            AddToDictionary(SourceFiles, sourceFile);
        }

        public SourceFile GetSourceFile(ulong id)
        {
            return GetFromDictionary(SourceFiles, id);
        }

        private void AddSourceLine(SourceLine sourceLine)
        {
            AddToDictionary(SourceLines, sourceLine);
        }

        public SourceLine GetSourceLine(ulong id)
        {
            return GetFromDictionary(SourceLines, id);
        }

        private void AddThread(Thread thread, ulong lastSampleTimestamp)
        {
            Threads.Add(thread);
            if (!_stackByThread.ContainsKey(thread.InternalId))
            {
                var rootStackFrame = new StackFrame
                {
                    FunctionIntId = Function.FakeFunctionId
                };
                _stackByThread.Add(thread.InternalId, new ThreadStackInfo
                {
                    RootFunctionCall = rootStackFrame,
                    TopFunctionCall = rootStackFrame,
                    LastSampleTimestamp = lastSampleTimestamp
                });
            }

            if (!Samples.ContainsKey(thread.InternalId))
            {
                Samples.Add(thread.InternalId, new List<Sample>());
            }
        }

        public Thread GetThreadByInternalId(ulong internalId)
        {
            return Threads.GetByInternalId(internalId);
        }

        private void AddFunction(Function function)
        {
            AddToDictionary(Functions, function);
        }

        public Function GetFunction(ulong id)
        {
            return GetFromDictionary(Functions, id);
        }

        private void AddClass(Class cls)
        {
            AddToDictionary(Classes, cls);
        }

        public Class GetClass(ulong id)
        {
            return GetFromDictionary(Classes, id);
        }

        private void AddModule(Module module)
        {
            AddToDictionary(Modules, module);
        }

        public Module GetModule(ulong id)
        {
            return GetFromDictionary(Modules, id);
        }

        private void AddAssembly(Assembly assembly)
        {
            AddToDictionary(Assemblies, assembly);
        }

        public Assembly GetAssembly(ulong id)
        {
            return GetFromDictionary(Assemblies, id);
        }

        private void AddApplicationDomain(ApplicationDomain domain)
        {
            AddToDictionary(ApplicationDomains, domain);
        }

        public ApplicationDomain GetApplicationDomain(ulong id)
        {
            return GetFromDictionary(ApplicationDomains, id);
        }

        private void AddGlobalEvent(Event evnt)
        {
            Threads.FakeThread.Events.Add(evnt);
        }

#if DEBUG
        // tuple's Item1: osThreadId
        private HashSet<Tuple<ulong, EventType>> threadEventsNotFound = new HashSet<Tuple<ulong, EventType>>();
#endif

        private void AddEvent(ulong osThreadId, Event evnt)
        {
            Thread thread = Threads.GetByOsThreadId(osThreadId);
            if (thread != null)
            {
                thread.Events.Add(evnt);
            }
#if DEBUG
            else
            {
                var t = new Tuple<ulong, EventType>(osThreadId, evnt.EventType);
                if (!threadEventsNotFound.Contains(t))
                {
                    threadEventsNotFound.Add(t);
                    Debug.WriteLine(String.Format("Error in {0}.AddEvent: cannot add event {1} to thread OsThreadId=={2} (thread not found)",
                        GetType().Name, evnt.EventType, osThreadId));
                }
            }
#endif
        }

        private void AddToDictionary<T>(IDictionary<ulong, T> col, T target) where T : IIdentifiable
        {
            if (!col.ContainsKey(target.InternalId))
            {
                col.Add(target.InternalId, target);
            }
        }

        private static T GetFromDictionary<T>(IDictionary<ulong, T> col, ulong key)
        {
            T result;
            col.TryGetValue(key, out result);
            return result;
        }

        private class ThreadStackInfo
        {
            public ulong LastSampleTimestamp { get; set; }

            public StackFrame RootFunctionCall { get; set; }

            public StackFrame TopFunctionCall { get; set; }
        }

        private class StackFrame
        {
            public ulong FunctionIntId { get; set; }

            public StackFrame Parent { get; set; }

            public ulong? Ip { get; set; }
        }
    }
}
