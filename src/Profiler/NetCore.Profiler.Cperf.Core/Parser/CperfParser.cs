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
using System.Text.RegularExpressions;
using NetCore.Profiler.Common.Helpers;
using NetCore.Profiler.Cperf.Core.Parser.Model;

namespace NetCore.Profiler.Cperf.Core.Parser
{
    public class CperfParser
    {

        public Action<ApplicationDomainCreationFinished> ApplicationDomainCreationFinishedCallback { get; set; }

        public Action<AssemblyLoadFinished> AssemblyLoadFinishedCallback { get; set; }

        public Action<ModuleAttachedToAssembly> ModuleAttachedToAssemblyCallback { get; set; }

        public Action<ModuleLoadFinished> ModuleLoadFinishedCallback { get; set; }

        public Action<ClassLoadFinished> ClassLoadFinishedCallback { get; set; }

        public Action<ClassName> ClassNameReadCallback { get; set; }

        public Action<FunctionName> FunctionNameReadCallback { get; set; }

        public Action<CachedFunctionSearchFinished> CachedFunctionSearchFinishedCallback { get; set; }

        public Action<CompilationFinished> CompilationFinishedCallback { get; set; }

        public Action<SourceLine> SourceLineReadCallback { get; set; }

        public Action<SourceFile> SourceFileReadCallback { get; set; }

        public Action<Cpu> CpuReadCallback { get; set; }

        public Action<ProfilerTps> ProfilerTpsReadCallback { get; set; }

        public Action<ProfilerTrs> ProfilerTrsReadCallback { get; set; }

        public Action<ThreadAssignedToOsThread> ThreadAssignedToOsThreadCallback { get; set; }

        public Action<ThreadTimes> ThreadTimesReadCallback { get; set; }

        public Action<ThreadCreated> ThreadCreatedCallback { get; set; }

        public Action<ThreadDestroyed> ThreadDestroyedCallback { get; set; }

        public Action<StackSample> StackSampleReadCallback { get; set; }

        public Action<AllocationSample> AllocationSampleReadCallback { get; set; }

        public Action<GarbageCollectionSample> GarbageCollectorSampleCallback { get; set; }

        public Action<string> UnrecognizedStringCallback { get; set; }

        public Action<string> LineReadCallback { get; set; }

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

        private static readonly Regex FunCmf =
                new Regex(
                    @"(fun) (cmf) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+)(.*)$")
            ;

        private static readonly Regex FunCsf =
                new Regex(
                    @"(fun) (csf) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+) (0x[A-Fa-f0-9]+)(.*)$")
            ;

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
                Parse(streamReader);
            }
        }

        public void Parse(TextReader streamReader)
        {

            try
            {
                string inputString;
                while ((inputString = streamReader.ReadLine()) != null)
                {
                    LineReadCallback?.Invoke(inputString);

                    if (inputString.Length < 7)
                    {
                        continue;
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

                    match = FunCmf.Match(inputString);
                    if (match.Success)
                    {
                        ParseCompilationFinished(match);
                        continue;
                    }

                    match = FunCsf.Match(inputString);
                    if (match.Success)
                    {
                        ParseCachedFunctionSearchFinished(match);
                        continue;
                    }

                    match = FunNam.Match(inputString);
                    if (match.Success)
                    {
                        FunctionNameReadCallback?.Invoke(new FunctionName(match.Groups[3].HexToUInt64(), match.Groups[4].Value.Replace("\"", string.Empty),
                            match.Groups[5].Value.Replace("\"", string.Empty), match.Groups[6].Value.Replace("\"", string.Empty)));
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

                    UnrecognizedStringCallback?.Invoke(inputString);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }



        private static readonly Regex CodeInfo = new Regex(@"\s(0x[A-Fa-f0-9]+):(0x[A-Fa-f0-9]+)(\s|$)");
        private static readonly Regex CodeMapping = new Regex(@"\s(0x[A-Fa-f0-9]+):(0x[A-Fa-f0-9]+):(0x[A-Fa-f0-9]+)(\s|$)");

        private void ParseCachedFunctionSearchFinished(Match match)
        {
            var result = new CachedFunctionSearchFinished(match.Groups[3].HexToUInt64(),
                match.Groups[4].HexToUInt64(),
                match.Groups[5].HexToUInt64(), match.Groups[6].HexToUInt64(), match.Groups[7].HexToUInt64());

            result.CodeInfos.AddRange(ParseCodeInfos(match.Groups[8].Value));
            result.CodeMappings.AddRange(ParseCodeMappings(match.Groups[8].Value));

            CachedFunctionSearchFinishedCallback?.Invoke(result);
        }

        private void ParseCompilationFinished(Match match)
        {
            var result = new CompilationFinished(match.Groups[3].HexToUInt64(),
                match.Groups[4].HexToUInt64(),
                match.Groups[5].HexToUInt64(), match.Groups[6].HexToUInt64(), match.Groups[7].HexToInt32(),
                match.Groups[8].HexToUInt64());

            result.CodeInfos.AddRange(ParseCodeInfos(match.Groups[9].Value));
            result.CodeMappings.AddRange(ParseCodeMappings(match.Groups[9].Value));

            CompilationFinishedCallback?.Invoke(result);
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
        private static readonly Regex SamStr = new Regex(@"(sam) (str) (0x[A-Fa-f0-9]+) (\d+) (\d+) (\d+):(\d+)(:([A-Fa-f0-9]+|\?))?(.*)$");
        private static readonly Regex StrFrame = new Regex(@" (0x[A-Fa-f0-9]+)(:([A-Fa-f0-9]+))?");

        private StackSample ParseSamStr(Match match)
        {
            var stackSample = new StackSample(
                match.Groups[3].HexToUInt64(),
                match.Groups[4].ToUInt64(),
                match.Groups[5].ToUInt64(),
                match.Groups[6].ToInt32(),
                match.Groups[7].ToInt32(),
                match.Groups[8].Value.Length > 0
                ? (match.Groups[9].Value != "?" ? Convert.ToUInt64(match.Groups[9].Value, 16) : 0)
                : (ulong?)null);
            foreach (Match frameMatch in StrFrame.Matches(match.Groups[10].Value))
            {
                stackSample.Frames.Add(new StackSampleFrame(frameMatch.Groups[1].HexToUInt64(),
                    frameMatch.Groups[2].Value.Length > 0 ? Convert.ToUInt64(frameMatch.Groups[3].Value, 16) : (ulong?)null));
            }

            return stackSample;
        }

        //------------------------------------------------------------- threadIid.id,   ticks
        private static readonly Regex SamMem = new Regex(@"(sam) (mem) (0x[A-Fa-f0-9]+) (\d+)(.*)$");
        private static readonly Regex AllocInfo = new Regex(@" (0x[A-Fa-f0-9]+):(\d+):(\d+)(:([A-Fa-f0-9]+))?");

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
                        ? Convert.ToUInt64(allocMatch.Groups[5].Value, 16)
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


    }

}
