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
using SourceFile = NetCore.Profiler.Cperf.Core.Model.SourceFile;
using SourceLine = NetCore.Profiler.Cperf.Core.Model.SourceLine;

namespace NetCore.Profiler.Cperf.Core
{
    public class DataContainer
    {

        private readonly string _filePath;

        public CpuUtilizationHistory CpuUtilizationHistory { get; } = new CpuUtilizationHistory();

        public Dictionary<ulong, ApplicationDomain> ApplicationDomains { get; } = new Dictionary<ulong, ApplicationDomain>();

        public Dictionary<ulong, Assembly> Assemblies { get; } = new Dictionary<ulong, Assembly>();

        public Dictionary<ulong, Module> Modules { get; } = new Dictionary<ulong, Module>();

        public Dictionary<ulong, Class> Classes { get; } = new Dictionary<ulong, Class>();

        public Dictionary<ulong, Function> Functions { get; } = new Dictionary<ulong, Function>();

        public Dictionary<ulong, Thread> Threads { get; } = new Dictionary<ulong, Thread>();

        public Dictionary<ulong, SourceFile> SourceFiles { get; } = new Dictionary<ulong, SourceFile>();

        public Dictionary<ulong, SourceLine> SourceLines { get; } = new Dictionary<ulong, SourceLine>();

        public Dictionary<ulong, List<Sample>> Samples { get; } = new Dictionary<ulong, List<Sample>>();

        public ulong TotalSamples { get; private set; }

        public ulong TotalTime { get; private set; }

        public ulong TotalAllocatedMemory { get; private set; }

        private ulong _globalTimestamp;

        private ulong _resumeTimestamp;

        private bool _profilingPaused;

        private readonly Dictionary<ulong, ThreadStackInfo> _stackByThread = new Dictionary<ulong, ThreadStackInfo>();

        public DataContainer(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException(nameof(filePath));
            }

            _filePath = filePath;

            var rootFunc = new Function { Name = "<ROOT>", Signature = "<EMPTY>" };
            Functions.Add(rootFunc.InternalId, rootFunc);

            // create & add stub thread, to present common events
            var stubThread = new Thread() { InternalId = Thread.FakeThreadId };
            Threads.Add(stubThread.InternalId, stubThread);
        }

        public void Load(ProgressMonitor progressMonitor)
        {
            _globalTimestamp = 0;
            var parser = new CperfParser
            {
                LineReadCallback = s => progressMonitor.Tick(),
                ApplicationDomainCreationFinishedCallback = ApplicationDomainCreationFinishedCallback,
                AssemblyLoadFinishedCallback = AssemblyLoadFinishedCallback,
                ModuleAttachedToAssemblyCallback = ModuleAttachedToAssemblyCallback,
                ModuleLoadFinishedCallback = ModuleLoadFinishedCallback,
                ClassLoadFinishedCallback = ClassLoadFinishedCallback,
                ClassNameReadCallback = ClassNameReadCallback,
                FunctionNameReadCallback = FunctionNameReadCallback,
                CachedFunctionSearchFinishedCallback = CachedFunctionSearchFinishedCallback,
                CompilationFinishedCallback = CompilationFinishedCallback,
                SourceLineReadCallback = SourceLineReadCallback,
                SourceFileReadCallback = SourceFileReadCallback,
                CpuReadCallback = CpuReadCallback,
                ProfilerTpsReadCallback = ProfilerTpsReadCallback,
                ProfilerTrsReadCallback = ProfilerTrsReadCallback,
                ThreadAssignedToOsThreadCallback = ThreadAssignedToOsThreadCallback,
                ThreadTimesReadCallback = ThreadTimesReadCallback,
                ThreadCreatedCallback = ThreadCreatedCallback,
                ThreadDestroyedCallback = ThreadDestroyedCallback,
                StackSampleReadCallback = StackSampleReadCallback,
                AllocationSampleReadCallback = AllocationSampleReadCallback
            };

            parser.Parse(_filePath);

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

            Threads.Values.ToList().Find(item => item.OsThreadId == applicationDomain.ProcessId).Events
                .Add(new Event(applicationDomain, _globalTimestamp, EventType.CreationFinished));

        }

        private void AssemblyLoadFinishedCallback(AssemblyLoadFinished arg)
        {
            var assembly = new Assembly(arg.ApplicationDomainId, arg.InternalId, arg.ModuleId, arg.Name);
            AddAssembly(assembly);
            AddGlobalEvent(new Event(assembly, _globalTimestamp, EventType.LoadFinished));

        }

        private void ModuleAttachedToAssemblyCallback(ModuleAttachedToAssembly arg)
        {
            var module = GetModule(arg.ModuleId);
            if (module == null)
            {
                AddModule(module = new Module { InternalId = arg.ModuleId, AssemblyId = arg.AssemblyId });
            }
            else
            {
                module.AssemblyId = arg.AssemblyId;
            }

            AddGlobalEvent(new Event(module, _globalTimestamp, EventType.AttachedToAssembly));

        }

        private void ModuleLoadFinishedCallback(ModuleLoadFinished arg)
        {
            var module = GetModule(arg.ModuleId);
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

            AddGlobalEvent(new Event(module, _globalTimestamp, EventType.LoadFinished));

        }

        private void ClassLoadFinishedCallback(ClassLoadFinished arg)
        {
            var @class = GetClass(arg.InternalId);
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

            AddGlobalEvent(new Event(@class, _globalTimestamp, EventType.LoadFinished));

        }

        private void ClassNameReadCallback(ClassName arg)
        {
            var @class = GetClass(arg.InternalId);
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
            var function = GetFunction(arg.InternalId);
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

        private void CachedFunctionSearchFinishedCallback(CachedFunctionSearchFinished arg)
        {
            var function = GetFunction(arg.InternalId);
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

            AddGlobalEvent(new Event(function, _globalTimestamp, EventType.CachedFunctionSearchFinished));

        }

        private void CompilationFinishedCallback(CompilationFinished arg)
        {
            var function = GetFunction(arg.InternalId);
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

            AddGlobalEvent(new Event(function, _globalTimestamp, EventType.CompilationFinished));

        }

        private void SourceLineReadCallback(Parser.Model.SourceLine arg)
        {
            var sourceLine = GetSourceLine(arg.Id);
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
            var sourceFile = GetSourceFile(arg.InternalId);
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
            _resumeTimestamp = 0;

            CpuUtilizationHistory.RecordProfilingPaused();
            foreach (var thread in Threads.Values)
            {
                thread.CpuUtilizationHistory.RecordProfilingPaused();
            }
        }

        private void ProfilerTrsReadCallback(ProfilerTrs arg)
        {
            _profilingPaused = false;
            _resumeTimestamp = arg.Value;

            CpuUtilizationHistory.RecordProfilingResumed();
            foreach (var thread in Threads.Values)
            {
                thread.CpuUtilizationHistory.RecordProfilingResumed();
            }

        }

        private void ThreadAssignedToOsThreadCallback(ThreadAssignedToOsThread arg)
        {
            var thread = GetThread(arg.InternalId);
            if (thread == null)
            {
                thread = new Thread { InternalId = arg.InternalId, OsThreadId = arg.OsThreadId };
                AddThread(thread, _globalTimestamp);
            }
            else
            {
                thread.OsThreadId = arg.OsThreadId;
            }

            AddGlobalEvent(new Event(thread, _globalTimestamp, EventType.AssignedToOsThread));

        }

        private void ThreadTimesReadCallback(ThreadTimes arg)
        {
            //Temporary solution until situation with missing thread create record is cleared
            if (GetThread(arg.InternalId) == null)
            {
                AddThread(new Thread { InternalId = arg.InternalId }, arg.TicksFromStart);
            }

            Threads[arg.InternalId].CpuUtilizationHistory.RecordCpuUsage(arg.TicksFromStart, arg.UserTime);
        }

        private void ThreadCreatedCallback(ThreadCreated arg)
        {
            var thread = GetThread(arg.InternalId);
            if (thread == null)
            {
                thread = new Thread { InternalId = arg.InternalId, Id = arg.ThreadId };
                AddThread(thread, _globalTimestamp);
            }
            else
            {
                thread.Id = arg.ThreadId;
            }

            AddGlobalEvent(new Event(thread, _globalTimestamp, EventType.ThreadCreated));

        }

        private void ThreadDestroyedCallback(ThreadDestroyed arg)
        {
            AddGlobalEvent(new Event(null, _globalTimestamp, EventType.ThreadDestroyed));
        }


        private void StackSampleReadCallback(StackSample arg)
        {
            if (_profilingPaused)
            {
                return;
            }

            var thread = _stackByThread[arg.InternalId];
            var duration = arg.Ticks - Math.Max(thread.LastSampleTimestamp, _resumeTimestamp);

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
                    Timestamp = arg.Ticks,
                    Samples = arg.Count,
                    Time = duration
                };
                FillSampleStackItems(sample, thread.TopFunctionCall);
                StoreSample(sample);
            }

            _globalTimestamp = arg.Ticks;

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
                Timestamp = arg.Ticks,
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

        public void AddSourceFile(SourceFile sourceFile)
        {
            AddToDictionary(SourceFiles, sourceFile);
        }

        public SourceFile GetSourceFile(ulong id)
        {
            return GetFromDictionary(SourceFiles, id);
        }

        public void AddSourceLine(SourceLine sourceLine)
        {
            AddToDictionary(SourceLines, sourceLine);
        }

        public SourceLine GetSourceLine(ulong id)
        {
            return GetFromDictionary(SourceLines, id);
        }

        public void AddThread(Thread thread, ulong lastSampleTimestamp)
        {
            AddToDictionary(Threads, thread);
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

        public Thread GetThread(ulong id)
        {
            return GetFromDictionary(Threads, id);
        }

        public void AddFunction(Function function)
        {
            AddToDictionary(Functions, function);
        }

        public Function GetFunction(ulong id)
        {
            return GetFromDictionary(Functions, id);
        }

        public void AddClass(Class cls)
        {
            AddToDictionary(Classes, cls);
        }

        public Class GetClass(ulong id)
        {
            return GetFromDictionary(Classes, id);
        }

        public void AddModule(Module module)
        {
            AddToDictionary(Modules, module);
        }

        public Module GetModule(ulong id)
        {
            return GetFromDictionary(Modules, id);
        }

        public void AddAssembly(Assembly assembly)
        {
            AddToDictionary(Assemblies, assembly);
        }

        public Assembly GetAssembly(ulong id)
        {
            return GetFromDictionary(Assemblies, id);
        }

        public void AddApplicationDomain(ApplicationDomain domain)
        {
            AddToDictionary(ApplicationDomains, domain);
        }

        public ApplicationDomain GetApplicationDomain(ulong id)
        {
            return GetFromDictionary(ApplicationDomains, id);
        }

        public void AddGlobalEvent(Event evnt)
        {
            Threads[Thread.FakeThreadId].Events.Add(evnt);
        }

        public void AddEvent(ulong threadId, Event evnt)
        {
            Threads[threadId].Events.Add(evnt);
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
