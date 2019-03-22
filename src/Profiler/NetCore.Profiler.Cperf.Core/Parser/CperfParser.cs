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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NetCore.Profiler.Common.Helpers;
using NetCore.Profiler.Cperf.Core.Parser.Model;

namespace NetCore.Profiler.Cperf.Core.Parser
{
    /// <summary>
    /// A parser for %Core %Profiler data files.
    /// </summary>
    /// <remarks>
    /// CperfParser accepts a file or a text stream containing %Core %Profiler output data and parses it.
    /// The %Core %Profiler output is a sequence of one line statements of different types containing the profiling data.
    /// The parser provides a number of events allowing its users (clients) to process the types of statements they need.
    /// Every time the parser finishes processing a valid source line it invokes an event and passes the extracted data
    /// to the event handler.
    /// </remarks>
    public class CperfParser
    {
        /// <summary>
        /// Event called after reading every source line (called before parsing).
        /// </summary>
        /// <remarks>
        /// This special event is the only one which reports not the parsed data but the source line itself (it may be empty or invalid).
        /// </remarks>
        /// <param name="line">The source line</param>
        public event Action<string> LineReadCallback;

        /// <summary>
        /// Event for reporting the profiler start time (from "prf stm" statement).
        /// </summary>
        /// <remarks>
        /// Reports the profiler start time which is a part of "prf stm" statement, e.g.
        /// "prf stm 2018-06-25 07:24:28.049 +09:00".
        /// </remarks>
        /// <param name="startTime">The profiler start time</param>
        public event Action<DateTime> StartTimeCallback;

        /// <summary>
        /// Event for reporting the application domain creation finished record ("apd crf" statement).
        /// </summary>
        public event Action<ApplicationDomainCreationFinished> ApplicationDomainCreationFinishedCallback;

        public event Action<AssemblyLoadFinished> AssemblyLoadFinishedCallback;

        public event Action<ModuleAttachedToAssembly> ModuleAttachedToAssemblyCallback;

        public event Action<ModuleLoadFinished> ModuleLoadFinishedCallback;

        public event Action<ClassLoadFinished> ClassLoadFinishedCallback;

        public event Action<ClassName> ClassNameReadCallback;

        public event Action<FunctionName> FunctionNameReadCallback;

        public event Action<FunctionInfo> FunctionInfoCallback;

        public event Action<JitCompilationStarted> JitCompilationStartedCallback;

        public event Action<JitCompilationFinished> JitCompilationFinishedCallback;

        public event Action<JitCachedFunctionSearchStarted> JitCachedFunctionSearchStartedCallback;

        public event Action<JitCachedFunctionSearchFinished> JitCachedFunctionSearchFinishedCallback;

        public event Action<GarbageCollectionStarted> GarbageCollectionStartedCallback;

        public event Action<GarbageCollectionFinished> GarbageCollectionFinishedCallback;

        public event Action<SourceLine> SourceLineReadCallback;

        public event Action<SourceFile> SourceFileReadCallback;

        public event Action<Cpu> CpuReadCallback;

        public event Action<ProfilerTps> ProfilerTpsReadCallback;

        public event Action<ProfilerTrs> ProfilerTrsReadCallback;

        public event Action<ThreadAssignedToOsThread> ThreadAssignedToOsThreadCallback;

        public event Action<ThreadTimes> ThreadTimesReadCallback;

        public event Action<ThreadCreated> ThreadCreatedCallback;

        public event Action<ThreadDestroyed> ThreadDestroyedCallback;

        public event Action<StackSample> StackSampleReadCallback;

        public event Action<AllocationSample> AllocationSampleReadCallback;

        public event Action<GarbageCollectionSample> GarbageCollectorSampleCallback;

        public event Action<GarbageCollectorGenerationsSample> GarbageCollectorGenerationSampleCallback;

        public event Action<ManagedMemoryData> ManagedMemorySampleCallback;

        public event Action<string> UnrecognizedStringCallback;

        private static readonly Regex ApdCrf =
            new Regex(@"(apd) (crf) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+) (.*)$");

        private static readonly Regex AsmLdf =
            new Regex(@"(asm) (ldf) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+) (.*)$");

        private static readonly Regex ModLdf =
            new Regex(@"(mod) (ldf) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+) (.*)$");

        private static readonly Regex ModAta = new Regex(@"(mod) (ata) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+)$");

        private static readonly Regex ClsNam = new Regex(@"(cls) (nam) (0x[A-Fa-f0-9]+) (.*)$");

        private static readonly Regex ClsLdf = new Regex(
            @"(cls) (ldf) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+)$");

        private static readonly Regex FunNam = new Regex("(fun) (nam) (0x[A-Fa-f0-9]+) (\".*\") (\".*\") (\".*\")$");

        private static readonly Regex FunInf = new Regex(
            @"(fun) (inf) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+)(.*)$");

        private static readonly Regex JitCms = new Regex(@"(jit) (cms) (\d+) (\d+) (0x[A-Fa-f0-9]+)$");

        private static readonly Regex JitCmf = new Regex(@"(jit) (cmf) (\d+) (\d+) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+)$");

        private static readonly Regex JitCss = new Regex(@"(jit) (css) (\d+) (\d+) (0x[A-Fa-f0-9]+)$");

        private static readonly Regex JitCsf = new Regex(@"(jit) (csf) (\d+) (\d+) (0x[A-Fa-f0-9]+)$");

        private static readonly Regex GchGcs = new Regex(@"(gch) (gcs) (\d+) (\d+) (induced|\?) (t|f)(t|f)(t|f)(t|f)$");

        private static readonly Regex GchGcf = new Regex(@"(gch) (gcf) (\d+) (\d+)$");

        private static readonly Regex LinSrc = new Regex(
            @"(lin) (src) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+) (\d+) (\d+) (\d+) (\d+)$");

        private static readonly Regex FilSrc = new Regex(@"(fil) (src) (0x[A-Fa-f0-9]+) (.*)$");

        private static readonly Regex PrcCpu = new Regex(@"(prc) (cpu) (\d+) (\d+)$");

        private static readonly Regex PrfTps = new Regex(@"(prf) (tps) (\d+)$");

        private static readonly Regex PrfTrs = new Regex(@"(prf) (trs) (\d+)$");

        private static readonly Regex ThrCrt = new Regex(@"(thr) (crt) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+)$");

        private static readonly Regex ThrDst = new Regex(@"(thr) (dst) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+)$");

        private static readonly Regex ThrAos = new Regex(@"(thr) (aos) (0x[A-Fa-f0-9]+) (\d+)$");

        private static readonly Regex ThrCpu = new Regex(@"(thr) (cpu) (0x[A-Fa-f0-9]+) (\d+) (\d+)$");

        public void Parse(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            using (var streamReader = File.OpenText(fileName))
            {
                Parse(() => streamReader.ReadLine());
            }
        }

        public void Parse(Func<string> readFunc)
        {
            DateTime startTime = DateTime.MinValue;
            int lineNumber = 0;
            string inputString = "";
            try
            {
                while ((inputString = readFunc()) != null)
                {
                    ++lineNumber;

                    LineReadCallback?.Invoke(inputString);

                    if (inputString.Length < 7)
                    {
                        continue;
                    }

                    if ((startTime == DateTime.MinValue) && (StartTimeCallback != null))
                    {
                        if (GetProfilerStartTime(inputString, out startTime))
                        {
                            StartTimeCallback(startTime);
                        }
                    }

                    var match = SamStr.Match(inputString);
                    if (match.Success)
                    {
                        StackSampleReadCallback?.Invoke(ParseSamStr(match));
                        continue;
                    }

                    match = SamMem.Match(inputString);
                    if (match.Success)
                    {
                        AllocationSampleReadCallback?.Invoke(ParseSamMem(match));
                        continue;
                    }

                    match = ApdCrf.Match(inputString);
                    if (match.Success)
                    {
                        ApplicationDomainCreationFinishedCallback?.Invoke(new ApplicationDomainCreationFinished(
                            match.Groups[3].HexToUInt64(),
                            match.Groups[6].Value,
                            match.Groups[4].HexToUInt64()));
                        continue;
                    }

                    match = AsmLdf.Match(inputString);
                    if (match.Success)
                    {
                        AssemblyLoadFinishedCallback?.Invoke(new AssemblyLoadFinished(match.Groups[3].HexToUInt64(), match.Groups[4].HexToUInt64(),
                            match.Groups[5].HexToUInt64(), match.Groups[7].Value));
                        continue;
                    }

                    match = ModAta.Match(inputString);
                    if (match.Success)
                    {
                        ModuleAttachedToAssemblyCallback?.Invoke(new ModuleAttachedToAssembly(
                            match.Groups[3].HexToUInt64(),
                            match.Groups[4].HexToUInt64()));
                        continue;
                    }

                    match = ModLdf.Match(inputString);
                    if (match.Success)
                    {
                        ModuleLoadFinishedCallback?.Invoke(new ModuleLoadFinished(
                            match.Groups[3].HexToUInt64(),
                            match.Groups[4].HexToUInt64(),
                            match.Groups[5].HexToUInt64(),
                            match.Groups[6].HexToUInt64(),
                            match.Groups[7].Value.Replace("\"", string.Empty)));
                        continue;
                    }

                    match = ClsNam.Match(inputString);
                    if (match.Success)
                    {
                        ClassNameReadCallback?.Invoke(new ClassName(match.Groups[3].HexToUInt64(), match.Groups[4].Value.Replace("\"", string.Empty)));
                        continue;
                    }

                    match = ClsLdf.Match(inputString);
                    if (match.Success)
                    {
                        ClassLoadFinishedCallback?.Invoke(new ClassLoadFinished(match.Groups[3].HexToUInt64(), match.Groups[4].HexToUInt64(),
                            match.Groups[5].HexToUInt64(),
                            match.Groups[6].HexToInt32(), match.Groups[7].HexToUInt64()));
                        continue;
                    }

                    match = FunInf.Match(inputString);
                    if (match.Success)
                    {
                        ParseFunctionInfo(match);
                        continue;
                    }

                    match = FunNam.Match(inputString);
                    if (match.Success)
                    {
                        FunctionNameReadCallback?.Invoke(new FunctionName(match.Groups[3].HexToUInt64(), match.Groups[4].Value.Replace("\"", string.Empty),
                            match.Groups[5].Value.Replace("\"", string.Empty), match.Groups[6].Value.Replace("\"", string.Empty)));
                        continue;
                    }

                    match = JitCms.Match(inputString);
                    if (match.Success)
                    {
                        JitCompilationStartedCallback?.Invoke(new JitCompilationStarted(
                            match.Groups[3].ToUInt64(),
                            match.Groups[4].ToUInt64(),
                            match.Groups[5].HexToUInt64()));

                        continue;
                    }

                    match = JitCmf.Match(inputString);
                    if (match.Success)
                    {
                        JitCompilationFinishedCallback?.Invoke(new JitCompilationFinished(
                            match.Groups[3].ToUInt64(),
                            match.Groups[4].ToUInt64(),
                            match.Groups[5].HexToUInt64(),
                            match.Groups[6].HexToUInt64()));
                        continue;
                    }

                    match = JitCss.Match(inputString);
                    if (match.Success)
                    {
                        JitCachedFunctionSearchStartedCallback?.Invoke(new JitCachedFunctionSearchStarted(
                            match.Groups[3].ToUInt64(),
                            match.Groups[4].ToUInt64(),
                            match.Groups[5].HexToUInt64()));
                        continue;
                    }

                    match = JitCsf.Match(inputString);
                    if (match.Success)
                    {
                        JitCachedFunctionSearchFinishedCallback?.Invoke(new JitCachedFunctionSearchFinished(
                            match.Groups[3].ToUInt64(),
                            match.Groups[4].ToUInt64(),
                            match.Groups[5].HexToUInt64()));
                        continue;
                    }

                    match = GchGcs.Match(inputString);
                    if (match.Success)
                    {
                        ParseGarbageCollectionStarted(match);
                        continue;
                    }

                    match = GchGcf.Match(inputString);
                    if (match.Success)
                    {
                        GarbageCollectionFinishedCallback?.Invoke(new GarbageCollectionFinished(
                            match.Groups[3].ToUInt64(),
                            match.Groups[4].ToUInt64()));
                        continue;
                    }

                    match = LinSrc.Match(inputString);
                    if (match.Success)
                    {
                        SourceLineReadCallback?.Invoke(new SourceLine(
                            match.Groups[3].HexToUInt64(),
                            match.Groups[4].HexToUInt64(),
                            match.Groups[5].HexToUInt64(),
                            match.Groups[6].ToUInt64(),
                            match.Groups[7].ToUInt64(),
                            match.Groups[8].ToUInt64(),
                            match.Groups[9].ToUInt64()));
                        continue;
                    }

                    match = FilSrc.Match(inputString);
                    if (match.Success)
                    {
                        SourceFileReadCallback?.Invoke(new SourceFile(match.Groups[3].HexToUInt64(), match.Groups[4].Value.Replace("\"", string.Empty)));
                        continue;
                    }

                    match = PrcCpu.Match(inputString);
                    if (match.Success)
                    {
                        CpuReadCallback?.Invoke(new Cpu(match.Groups[3].ToUInt64(), match.Groups[4].ToUInt64()));
                        continue;
                    }

                    match = PrfTps.Match(inputString);
                    if (match.Success)
                    {
                        ProfilerTpsReadCallback?.Invoke(new ProfilerTps(match.Groups[3].ToUInt64()));
                        continue;
                    }

                    match = PrfTrs.Match(inputString);
                    if (match.Success)
                    {
                        ProfilerTrsReadCallback?.Invoke(new ProfilerTrs(match.Groups[3].ToUInt64()));
                        continue;
                    }

                    match = ThrAos.Match(inputString);
                    if (match.Success)
                    {
                        ThreadAssignedToOsThreadCallback?.Invoke(new ThreadAssignedToOsThread(match.Groups[3].HexToUInt64(), match.Groups[4].ToUInt64()));
                        continue;
                    }

                    match = ThrCpu.Match(inputString);
                    if (match.Success)
                    {
                        ThreadTimesReadCallback?.Invoke(new ThreadTimes(match.Groups[3].HexToUInt64(), match.Groups[4].ToUInt64(), match.Groups[5].ToUInt64()));
                        continue;
                    }

                    match = ThrCrt.Match(inputString);
                    if (match.Success)
                    {
                        ThreadCreatedCallback?.Invoke(new ThreadCreated(match.Groups[3].HexToUInt64(), match.Groups[4].HexToUInt64()));
                        continue;
                    }

                    match = ThrDst.Match(inputString);
                    if (match.Success)
                    {
                        ThreadDestroyedCallback?.Invoke(new ThreadDestroyed(match.Groups[3].HexToUInt64()));
                        continue;
                    }

                    match = SamGc.Match(inputString);
                    if (match.Success)
                    {
                        ParseSamGc(match);
                        continue;
                    }

                    match = GchGen.Match(inputString);
                    if (match.Success)
                    {
                        ParseGchGen(match);
                        continue;
                    }

                    UnrecognizedStringCallback?.Invoke(inputString);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("Error in {0}.Parse when parsing line {1} ({2}). {3}",
                    GetType().Name, lineNumber, inputString, e.Message));
            }
        }

        private static bool GetProfilerStartTime(string line, out DateTime startTime)
        {
            if (line.StartsWith("prf stm "))
            {
                startTime = ParseProfilerStartTime(line.Substring(8));
                return true;
            }
            startTime = DateTime.MinValue;
            return false;
        }

        private static DateTime ParseProfilerStartTime(string profilerTimeText)
        {
            const string DateTimeFormatLocal = "yyyy-MM-dd HH:mm:ss.fff";
            const string DateTimeFormatUtc = DateTimeFormatLocal + " zzz";
            DateTime result = DateTime.MinValue;
            if (profilerTimeText.Length == DateTimeFormatLocal.Length)
            {
                DateTime.TryParseExact(profilerTimeText.Substring(0, DateTimeFormatLocal.Length), DateTimeFormatLocal,
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
            }
            else
            {
                DateTime.TryParseExact(profilerTimeText, DateTimeFormatUtc, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal, out result);
            }
            return result;
        }

        private static readonly Regex CodeInfo = new Regex(@"\s(0x[A-Fa-f0-9]+):(0x[A-Fa-f0-9]+)(\s|$)");
        private static readonly Regex CodeMapping = new Regex(@"\s(0x[A-Fa-f0-9]+):(0x[A-Fa-f0-9]+):(0x[A-Fa-f0-9]+)(\s|$)");

        private void ParseFunctionInfo(Match match)
        {
            var result = new FunctionInfo(match.Groups[3].HexToUInt64(),
                match.Groups[4].HexToUInt64(),
                match.Groups[5].HexToUInt64(), match.Groups[6].HexToUInt64(), match.Groups[7].HexToUInt64());

            result.CodeInfos.AddRange(ParseCodeInfos(match.Groups[8].Value));
            result.CodeMappings.AddRange(ParseCodeMappings(match.Groups[8].Value));

            FunctionInfoCallback?.Invoke(result);
        }

        private void ParseGarbageCollectionStarted(Match match)
        {
            GarbageCollectionGenerations generations = GarbageCollectionGenerations.None;
            if (match.Groups[6].Value == "t")
            {
                generations |= GarbageCollectionGenerations.Generation0;
            }
            if (match.Groups[7].Value == "t")
            {
                generations |= GarbageCollectionGenerations.Generation1;
            }
            if (match.Groups[8].Value == "t")
            {
                generations |= GarbageCollectionGenerations.Generation2;
            }
            if (match.Groups[9].Value == "t")
            {
                generations |= GarbageCollectionGenerations.LargeObjectHeap;
            }
            var result = new GarbageCollectionStarted(
                match.Groups[3].ToUInt64(),
                match.Groups[4].ToUInt64(),
                (match.Groups[5].Value == "?")
                    ? GarbageCollectionReason.Unspecified
                    : GarbageCollectionReason.Induced,
                generations);
            GarbageCollectionStartedCallback?.Invoke(result);
        }

        private IEnumerable<CodeInfo> ParseCodeInfos(string input)
        {
            return (from Match match in CodeInfo.Matches(input)
                    select new CodeInfo(match.Groups[1].HexToUInt64(), match.Groups[2].HexToUInt32()));
        }

        private IEnumerable<CodeMapping> ParseCodeMappings(string input)
        {
            return (from Match match in CodeMapping.Matches(input)
                    select new CodeMapping(match.Groups[1].HexToUInt32(), match.Groups[2].HexToUInt32(),
                        match.Groups[3].HexToUInt32()));
        }

        //------------------------------------------------------------- threadIid.id    ticks count  PfSz:stSz(:ip)
        private static readonly Regex SamStr = new Regex(@"(sam) (str) (0x[A-Fa-f0-9]+) (\d+) (\d+) (\d+):(\d+)(:(0x[A-Fa-f0-9]+|\?))?(.*)$");
        private static readonly Regex StrFrame = new Regex(@" (0x[A-Fa-f0-9]+)(:(0x[A-Fa-f0-9]+))?");

        private StackSample ParseSamStr(Match match)
        {
            var stackSample = new StackSample(
                match.Groups[3].HexToUInt64(),
                match.Groups[4].ToUInt64(),
                match.Groups[5].ToUInt64(),
                match.Groups[6].ToInt32(),
                match.Groups[7].ToInt32(),
                match.Groups[8].Value.Length > 0
                ? (match.Groups[9].Value != "?" ? match.Groups[9].Value.HexToUInt64() : 0)
                : (ulong?)null);
            foreach (Match frameMatch in StrFrame.Matches(match.Groups[10].Value))
            {
                stackSample.Frames.Add(new StackSampleFrame(frameMatch.Groups[1].HexToUInt64(),
                    frameMatch.Groups[2].Value.Length > 0 ? frameMatch.Groups[3].Value.HexToUInt64() : (ulong?)null));
            }

            return stackSample;
        }

        //------------------------------------------------------------- threadIid.id,   ticks
        private static readonly Regex SamMem = new Regex(@"(sam) (mem) (0x[A-Fa-f0-9]+) (\d+)(.*)$");
        private static readonly Regex AllocInfo = new Regex(@" (0x[A-Fa-f0-9]+):(\d+):(\d+)(:(0x[A-Fa-f0-9]+))?");

        private AllocationSample ParseSamMem(Match match)
        {
            var memSample = new AllocationSample(match.Groups[3].HexToUInt64(), match.Groups[4].ToUInt64());
            foreach (Match allocMatch in AllocInfo.Matches(match.Groups[5].Value))
            {
                memSample.Allocations.Add(new AllocationSampleInfo(
                    allocMatch.Groups[1].HexToUInt64(),
                    allocMatch.Groups[2].ToUInt64(),
                    allocMatch.Groups[3].ToUInt64(),
                    allocMatch.Groups[4].Value.Length > 0
                        ? allocMatch.Groups[5].Value.HexToUInt64()
                        : (ulong?)null));
            }

            return memSample;
        }

        private static readonly Regex SamGc = new Regex(@"(gch) (alt) (\d+)(.*)$");
        private static readonly Regex GcInfo = new Regex(@"(0x[A-Fa-f0-9]+):(\d+):(\d+)");

        private void ParseSamGc(Match match)
        {
            var sample = new GarbageCollectionSample(match.Groups[3].ToUInt64());
            foreach (Match allocMatch in GcInfo.Matches(match.Groups[4].Value))
            {
                sample.Items.Add(new GarbageCollectionSampleItem(allocMatch.Groups[1].HexToUInt64(),
                    allocMatch.Groups[2].ToUInt64(), allocMatch.Groups[3].ToUInt64()));
            }

            GarbageCollectorSampleCallback?.Invoke(sample);
        }

        private static readonly Regex GchGen = new Regex(@"(gch) (gen) (\d+)(.*)$");
        private static readonly Regex GchGenInfo = new Regex(@"(\w+):(\d+):(\d+)");

        private void ParseGchGen(Match match)
        {
            var sample_gen = new GarbageCollectorGenerationsSample();
            var sample_mm = new ManagedMemoryData();

            var parts = match.Groups[0].Value.Split(' ');
            var timestamp = Convert.ToUInt64(parts[2]);
            sample_gen.Timestamp = timestamp;
            sample_mm.Timestamp = timestamp;
            var matchInfos = GchGenInfo.Matches(match.Groups[0].Value);

            var loh = matchInfos[0].Value;
            var loh_alloc = Convert.ToUInt64(loh.Split(':')[1]);
            var loh_reserved = Convert.ToUInt64(loh.Split(':')[2]);
            sample_gen.LargeObjectsHeap = loh_reserved;

            var gen2 = matchInfos[1].Value;
            var gen2_alloc = Convert.ToUInt64(gen2.Split(':')[1]);
            var gen2_reserved = Convert.ToUInt64(gen2.Split(':')[2]);
            sample_gen.SmallObjectsHeapGeneration2 = gen2_reserved;

            var gen1 = matchInfos[2].Value;
            var gen1_alloc = Convert.ToUInt64(gen1.Split(':')[1]);
            var gen1_reserved = Convert.ToUInt64(gen1.Split(':')[2]);
            sample_gen.SmallObjectsHeapGeneration1 = gen1_reserved;

            var gen0 = matchInfos[3].Value;
            var gen0_alloc = Convert.ToUInt64(gen0.Split(':')[1]);
            var gen0_reserved = Convert.ToUInt64(gen0.Split(':')[2]);
            sample_gen.SmallObjectsHeapGeneration0 = gen0_reserved;

            sample_mm.HeapAllocated = loh_alloc + gen2_alloc + gen1_alloc + gen0_alloc;
            sample_mm.HeapReserved = sample_mm.HeapAllocated + loh_reserved + gen2_reserved + gen1_reserved + gen0_reserved;

            GarbageCollectorGenerationSampleCallback?.Invoke(sample_gen);
            ManagedMemorySampleCallback?.Invoke(sample_mm);
        }
    }
}
