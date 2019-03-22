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
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Linq;
using NetCore.Profiler.Cperf.Core.Model;
using NetCore.Profiler.Cperf.Core.Parser;
using NetCore.Profiler.Cperf.Core.Parser.Model;
using SourceFile = NetCore.Profiler.Cperf.Core.Model.SourceFile;
using SourceLine = NetCore.Profiler.Cperf.Core.Model.SourceLine;

namespace NetCore.Profiler.Cperf.LogAdaptor.Core
{
    /// <summary>
    /// A %Core %Profiler log adapter used to process and extend a %Core %Profiler output data stream during a
    /// profiling session with information loaded from PDB files corresponding to modules of a %Tizen application
    /// being profiled. It’s necessary because the PDB files are not copied to a target %Tizen system.
    /// </summary>
    public class DebugDataInjectionFilter : ILogAdaptor
    {
        public StreamWriter Output { get; set; }

        public string PdbDirectory { get; set; }

        private class PdbReaderData
        {
            public MetadataReaderProvider ReaderProvider;
            public MetadataReader Reader;
        }

        private readonly Dictionary<string, PdbReaderData> _pdbReaders = new Dictionary<string, PdbReaderData>();

        private Dictionary<ulong, Module> Modules { get; } = new Dictionary<ulong, Module>();

        internal Dictionary<ulong, Function> Functions { get; } = new Dictionary<ulong, Function>();

        private Dictionary<ulong, Thread> Threads { get; } = new Dictionary<ulong, Thread>();

        private Dictionary<ulong, SourceFile> SourceFiles { get; } = new Dictionary<ulong, SourceFile>();

        private ulong _nextSourceLineId = 1;

#if DEBUG
        private HashSet<ulong> threadsNotFound = new HashSet<ulong>();
#endif

        public DebugDataInjectionFilter()
        {
            // add root function
            var rootFunc = new Function(ulong.MaxValue, ulong.MaxValue, 0);
            Functions.Add(rootFunc.InternalId, rootFunc);
        }

        public void Process(Func<string> readFunc)
        {
            Process(new CperfParser(), readFunc);
        }

        public void Process(CperfParser parser, Func<string> readFunc)
        {
#if DEBUG
            threadsNotFound.Clear();
#endif
            parser.ModuleLoadFinishedCallback += ModuleLoadFinishedCallback;
            parser.FunctionInfoCallback += FunctionInfoCallback;
            parser.ThreadCreatedCallback += ThreadCreatedCallback;
            parser.StackSampleReadCallback += StackSampleReadCallback;
            parser.AllocationSampleReadCallback += AllocationSampleReadCallback;
            parser.LineReadCallback += LineReadCallback;

            parser.Parse(readFunc);

            parser.ModuleLoadFinishedCallback -= ModuleLoadFinishedCallback;
            parser.FunctionInfoCallback -= FunctionInfoCallback;
            parser.ThreadCreatedCallback -= ThreadCreatedCallback;
            parser.StackSampleReadCallback -= StackSampleReadCallback;
            parser.AllocationSampleReadCallback -= AllocationSampleReadCallback;
            parser.LineReadCallback -= LineReadCallback;
        }

        private void ModuleLoadFinishedCallback(ModuleLoadFinished arg)
        {
            var module = GetModule(arg.ModuleId);
            if (module == null)
            {
                string name = arg.ModuleName;
                if (name.StartsWith("/proc/self/fd/"))
                {
                    name = name.Substring(name.IndexOf('/', 14));
                }
                AddModule(module = new Module { InternalId = arg.ModuleId, AssemblyId = arg.AssemblyId, Name = name });
                InitModulePdbInfo(module);
            }
            else
            {
                module.AssemblyId = arg.AssemblyId;
                module.Name = arg.ModuleName;
            }

            module.ModuleLoadRecords.Add(new ModuleLoadInfo { BaseLoadAddress = arg.BaseLoadAddress });
        }

        private void FunctionInfoCallback(FunctionInfo arg)
        {
            var function = GetFunction(arg.InternalId);
            if (function == null)
            {
                AddFunction(function = new Function(arg.InternalId, arg.ModuleId, arg.FuncToken));
                var codeInfo = arg.CodeInfos.FirstOrDefault();
                if (codeInfo != null)
                {
                    var functionData = new FunctionNativeCodeInfo(codeInfo.StartAddress, codeInfo.Size);

                    foreach (var codeMapping in arg.CodeMappings)
                    {
                        functionData.CilToNativeMappings.Add(new CilToNativeMapping(codeMapping.Offset, codeMapping.NativeStartOffset, codeMapping.NativeEndOffset));
                    }

                    function.NativeCodeInfos.Add(functionData);
                }

                CheckFunctionToken(Modules[function.ModuleId], function);
            }
        }

        private void ThreadCreatedCallback(ThreadCreated arg)
        {
            var thread = GetThread(arg.InternalId);
            if (thread == null)
            {
                thread = new Thread(arg.InternalId);
                thread.InitWithRoot();
                AddThread(thread);
            }
        }

        private void LineReadCallback(string line)
        {
            if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("sam "))
            {
                Output.WriteLine(line);
            }
        }

        private void AllocationSampleReadCallback(AllocationSample arg)
        {
            var thread = Threads[arg.InternalId];

            var result = $"sam mem 0x{arg.InternalId:X8} {arg.Ticks}";

            foreach (var allocation in arg.Allocations)
            {
                result += $" 0x{allocation.ClassIntId:X}:{allocation.AllocationCount}:{allocation.MemorySize}";

                var function = GetFunction(thread.TopFunctionCall.FunctionIntId);
                if (function != null)
                {
                    result += $":0x{GetSourceLineNumberFromPc(function, allocation.Ip):X}";
                }
            }

            Output.WriteLine(result);
        }

        private void StackSampleReadCallback(Cperf.Core.Parser.Model.StackSample arg)
        {
            var sample = new StackSample(arg.Count, arg.StackSize, arg.MatchPrefixSize, arg.InternalId, arg.Ticks, arg.Ip);

            Thread thread;
            if (!Threads.TryGetValue(sample.ThreadId, out thread))
            {
#if DEBUG
                if (!threadsNotFound.Contains(sample.ThreadId))
                {
                    threadsNotFound.Add(sample.ThreadId);
                    Debug.WriteLine(String.Format("Error in {0}.StackSampleReadCallback: cannot find thread {1}",
                        GetType().Name, sample.ThreadId));
                }
#endif
                return;
            }

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
                    return;
                }

                thread.TopFunctionCall = lastFunctionCall;

            }

            //Ip has changed
            if (arg.Ip.HasValue)
            {
                lastFunctionCall.Ip = arg.Ip;
            }

            sample.ParentFunctionIntId = lastFunctionCall.FunctionIntId;

            ProcessFunctionCall(thread, lastFunctionCall, sample, arg.Frames, 0);

            Output.WriteLine(StackSampleFixedRecord(sample));
        }

        private void ProcessFunctionCall(Thread thread, FunctionCall call, StackSample sample, IReadOnlyList<StackSampleFrame> frames, int index)
        {
            if (index >= frames.Count)
            {
                thread.TopFunctionCall = call;
                return;
            }

            var frame = frames[index];
            var child = call.FindChildById(frame.InternalId);

            if (child == null)
            {
                child = new FunctionCall(frame.InternalId)
                {
                    Parent = call,
                };

                call.Children.Add(child);
            }

            child.Ip = frame.Ip;
            sample.FunctionCalls.Add(child);

            //TODO replace recursion with loop when it's ready
            ProcessFunctionCall(thread, child, sample, frames, index + 1);
        }

        private string StackSampleFixedRecord(StackSample sample)
        {
            var result =
                $"sam str 0x{sample.ThreadId:X8} {sample.Timestamp} {sample.Samples} {sample.MatchPrefixSize}:{sample.StackSize}";
            if (sample.Pc.HasValue)
            {
                var function = GetFunction(sample.ParentFunctionIntId);
                if (function != null)
                {
                    result += $":0x{GetSourceLineNumberFromPc(function, sample.Pc.Value):X}";
                }
            }

            return sample.FunctionCalls.Aggregate(result, (current, call) => current + FunctionCallFix(call));
        }

        private string FunctionCallFix(FunctionCall functionCall)
        {
            if (functionCall.FunctionIntId == Cperf.Core.Model.Function.FakeFunctionId)
            {
                return "";
            }

            var function = GetFunction(functionCall.FunctionIntId);
            return function != null
                ? $" 0x{functionCall.FunctionIntId:X}:0x{GetSourceLineNumberFromPc(function, functionCall.Ip):X}"
                : $" 0x{functionCall.FunctionIntId:X}:0x{0}";
        }

        private void AddThread(Thread thread)
        {
            AddToDictionary(Threads, thread);
        }

        private Thread GetThread(ulong id)
        {
            return GetFromDictionary(Threads, id);
        }

        private void AddFunction(Function function)
        {
            AddToDictionary(Functions, function);
        }

        internal Function GetFunction(ulong id)
        {
            return GetFromDictionary(Functions, id);
        }

        private void AddModule(Module module)
        {
            AddToDictionary(Modules, module);
        }

        private Module GetModule(ulong id)
        {
            return GetFromDictionary(Modules, id);
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

        internal ulong GetSourceFileId(string name)
        {
            foreach (var item in SourceFiles)
            {
                if (item.Value.Name == name)
                {
                    return item.Value.InternalId;
                }
            }

            var result = (ulong)SourceFiles.Count;
            var sourceFile = new SourceFile { InternalId = result, Name = name };
            SourceFiles.Add(result, sourceFile);
            WriteSourceFileAdded(sourceFile);
            return result;
        }

        internal ulong FindSourceLineForOffset(List<SourceLine> plSourceLines, uint offset)
        {
            SourceLine best = null;
            foreach (var sl in plSourceLines)
            {
                if (sl.Offset <= offset)
                {
                    best = sl;
                }
            }

            return (best == null) ? 0 : CheckId(best);
        }

        private ulong CheckId(SourceLine line)
        {
            if (line.StartLine == SequencePoint.HiddenLine)
            {
                return 0;
            }

            if (line.InternalId == SourceLine.UndefinedSourceLineId)
            {
                line.InternalId = _nextSourceLineId++;
                WriteSourceLineAdded(line);
            }

            return line.InternalId;
        }

        private void WriteSourceFileAdded(SourceFile sourceFile)
        {
            Output.WriteLine("fil src 0x{0:X8} \"{1}\"", sourceFile.InternalId, sourceFile.Name);
        }

        private void WriteSourceLineAdded(SourceLine sourceLine)
        {
            Output.WriteLine("lin src 0x{0:X8} 0x{1:X8} 0x{2:X8} {3} {4} {5} {6}",
                sourceLine.InternalId,
                sourceLine.SourceFileIntId,
                sourceLine.FunctionIntId,
                sourceLine.StartLine,
                sourceLine.StartColumn,
                sourceLine.EndLine,
                sourceLine.EndColumn);
        }

        private ulong GetSourceLineNumberFromPc(Function function, ulong? pc)
        {
            if (!pc.HasValue || pc.Value == 0 || function.SourceLines == null || function.SourceLines.Count == 0)
            {
                return 0;
            }

            var mapping = function.FindMappingForNativeAddress(pc.Value);

            return mapping != null
                ? FindSourceLineForOffset(function.SourceLines, mapping.Offset)
                : 0;
        }

        private void InitModulePdbInfo(Module module)
        {
            var pdbReader = GetMetadataReader(module.Name);
            if (pdbReader != null)
            {
                return;
            }

            var rname = FindModuleFile(module.Name, PdbDirectory);
            if (rname == null)
            {
                return;
            }

            MetadataReaderProvider provider = null;

            try
            {
                string pdbPath = null;
                using (var fileStream = File.OpenRead(rname))
                {
                    using (var reader = new PEReader(fileStream))
                    {
                        foreach (var entry in reader.ReadDebugDirectory())
                        {
                            if (entry.Type == DebugDirectoryEntryType.CodeView)
                            {
                                pdbPath = reader.ReadCodeViewDebugDirectoryData(entry).Path;
                                break;
                            }
                        }
                    }
                }

                if (pdbPath == null || !File.Exists(pdbPath))
                {
                    return;
                }

                using (var pdbFileStream = File.OpenRead(pdbPath))
                {
                    provider = MetadataReaderProvider.FromPortablePdbStream(pdbFileStream, 
                        MetadataStreamOptions.PrefetchMetadata | MetadataStreamOptions.LeaveOpen);
                    pdbReader = provider.GetMetadataReader();
                }
            }
            catch (Exception)
            {
                pdbReader = null;
            }

            if (pdbReader != null)
            {
                _pdbReaders.Add(module.Name, new PdbReaderData()
                {
                    ReaderProvider = provider,
                    Reader = pdbReader,
                });
            }
        }

        private void CheckFunctionToken(Module module, Function function)
        {
            var pdbReader = GetMetadataReader(module.Name);

            if (pdbReader == null)
            {
                return;
            }

            try
            {
                var mdh = MetadataTokens.MethodDefinitionHandle((int)function.Token);
                var mdi = pdbReader.GetMethodDebugInformation(mdh);

                AddFunctionSourceLines(function, mdi.GetSequencePoints(), pdbReader);
            }
            catch (Exception)
            {
                //Debug.WriteLine(e.Message);
            }
        }

        private static string FindModuleFile(string name, string pdbDir)
        {
            if (File.Exists(name))
            {
                return name;
            }

            var dir = pdbDir;
            if (dir == null)
            {
                return null;
            }

            // start from minimal name
            var fname = Path.GetFileName(name);
            var path = Path.GetDirectoryName(name);
            while (true)
            {
                if (fname == null)
                {
                    return null;
                }

                var dname = Path.Combine(dir, fname);
                if (File.Exists(dname))
                {
                    return dname;
                }

                if (path == null)
                {
                    return null;
                }

                fname = Path.Combine(Path.GetFileName(path), fname);
                path = Path.GetDirectoryName(path);
            }
        }

        private MetadataReader GetMetadataReader(string rname)
        {
            PdbReaderData data;
            _pdbReaders.TryGetValue(rname, out data);
            return data?.Reader;
        }

        private void AddFunctionSourceLines(Function function, SequencePointCollection points, MetadataReader pdbReader)
        {
            var slines = new List<SourceLine>();
            function.SourceLines = slines;
            foreach (var p in points)
            {
                try
                {
                    var sname = pdbReader.GetString(pdbReader.GetDocument(p.Document).Name);
                    var plSourceLine = new SourceLine
                    {
                        InternalId = SourceLine.UndefinedSourceLineId,
                        SourceFileIntId = GetSourceFileId(sname),
                        FunctionIntId = function.InternalId,
                        StartLine = (ulong)p.StartLine,
                        StartColumn = (ulong)p.StartColumn,
                        EndLine = (ulong)p.EndLine,
                        EndColumn = (ulong)p.EndColumn,
                        Offset = (uint)p.Offset,
                        Name = sname
                    };
                    slines.Add(plSourceLine);
                }
                catch (Exception)
                {
                    // Debug.WriteLine(ex.Message);
                }
            }
        }
    }
}
