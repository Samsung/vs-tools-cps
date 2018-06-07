using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NetCore.Profiler.Analytics.DataProvider;
using NetCore.Profiler.Analytics.Model;
using NetCore.Profiler.Common;
using NetCore.Profiler.Cperf.Core;
using NetCore.Profiler.Lttng.Core.BObject;
using NetCore.Profiler.Session.Core;
using NetCore.Profiler.Testing.Core.Model;
using Newtonsoft.Json;

namespace NetCore.Profiler.Testing.TestRunner
{
    internal class TestRunner
    {
        internal string SuiteFile { get; set; }

        private Suite _suite;

        private SavedSession _session;

        private DataProvider _dataProvider;

        private DateTime _startedAt;

        public DateTime StartedAt
        {
            get => _startedAt;
            set
            {
                _startedAt = value;
                StartedNanoseconds = (ulong)value.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds * 1000000;
            }
        }

        public ulong StartedNanoseconds { get; private set; }


        internal void Run()
        {
            Load();
            RunSuite();
        }

        private void Load()
        {
            using (var reader = new StreamReader(SuiteFile))
            {
                var s = reader.ReadToEnd();
                _suite = JsonConvert.DeserializeObject<Suite>(s, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
            }

            _session = new SavedSession()
            {
                SessionFile = _suite.InputSessionPath
            };
            _session.Load();
            InitializeDataProvider();
            _dataProvider.Load();
            FixStartedNanoSeconds(_dataProvider.MinimalStartTime);

        }

        private void RunSuite()
        {
            foreach (var testCase in _suite.TestCases)
            {
                if (testCase is FunctionTableTestCase)
                {
                    RunTestCase(testCase as FunctionTableTestCase);
                }
                else if (testCase is SourceCodeTableTestCase)
                {
                    RunTestCase(testCase as SourceCodeTableTestCase);
                }
                else if (testCase is TotalsTestCase)
                {
                    RunTestCase(testCase as TotalsTestCase);
                }
                else if (testCase is CallTreeTestCase)
                {
                    RunTestCase(testCase as CallTreeTestCase);
                }
            }
        }


        private void RunTestCase(FunctionTableTestCase testCase)
        {
            _dataProvider.BuildStatisticsRaw(testCase.TimeInterval);
            var passed = true;
            foreach (var expectedData in testCase.ExpectedFunctionTables)
            {
                var actualData = _dataProvider.ThreadsStatisticsRaw[expectedData.Tid];
                passed = CompareFunctonTables(expectedData.Functions, actualData.Methods);
                if (!passed)
                {
                    break;
                }
            }

            Console.WriteLine(
                $"FunctionTableTestCase {testCase.TimeInterval.Start}:{testCase.TimeInterval.End} {(passed ? "Passed" : "Failed")}");
        }

        private static bool CompareFunctonTables(IReadOnlyList<IMethodStatistics> expected, IReadOnlyList<IMethodStatistics> actual)
        {
            if (expected.Count != actual.Count)
            {
                return false;
            }

            for (var i = 0; i < expected.Count; i++)
            {
                var expectedFunction = expected[i];
                var actualFunction = actual[i];
                if (expectedFunction.Name != actualFunction.Name)
                {
                    return false;
                }

                if (expectedFunction.Signature != actualFunction.Signature)
                {
                    return false;
                }

                if (expectedFunction.SamplesInclusive != actualFunction.SamplesInclusive)
                {
                    return false;
                }

                if (expectedFunction.SamplesExclusive != actualFunction.SamplesExclusive)
                {
                    return false;
                }

                if (expectedFunction.TimeInclusive != actualFunction.TimeInclusive)
                {
                    return false;
                }

                if (expectedFunction.TimeExclusive != actualFunction.TimeExclusive)
                {
                    return false;
                }

                if (expectedFunction.AllocatedMemoryInclusive != actualFunction.AllocatedMemoryInclusive)
                {
                    return false;
                }

                if (expectedFunction.AllocatedMemoryExclusive != actualFunction.AllocatedMemoryExclusive)
                {
                    return false;
                }
            }

            return true;
        }

        private void RunTestCase(SourceCodeTableTestCase testCase)
        {
            _dataProvider.BuildStatisticsRaw(testCase.TimeInterval);
            var passed = true;
            foreach (var expectedData in testCase.ExpectedSourceCodeTables)
            {
                var actualData = _dataProvider.ThreadsStatisticsRaw[expectedData.Tid];
                passed = CompareSourceTables(expectedData.SourceLines, actualData.Lines);
                if (!passed)
                {
                    break;
                }
            }

            Console.WriteLine(
                $"SourceCodeTableTestCase {testCase.TimeInterval.Start}:{testCase.TimeInterval.End} {(passed ? "Passed" : "Failed")}");
        }

        private static bool CompareSourceTables(IReadOnlyList<ISourceLineStatistics> expected, IReadOnlyList<ISourceLineStatistics> actual)
        {
            if (expected.Count != actual.Count)
            {
                return false;
            }

            for (var i = 0; i < expected.Count; i++)
            {
                var expectedLine = expected[i];
                var actualLine = actual[i];
                if (expectedLine.FunctionName != actualLine.FunctionName)
                {
                    return false;
                }

                if (expectedLine.StartLine != actualLine.StartLine)
                {
                    return false;
                }

                if (expectedLine.StartColumn != actualLine.StartColumn)
                {
                    return false;
                }

                if (expectedLine.EndLine != actualLine.EndLine)
                {
                    return false;
                }

                if (expectedLine.EndColumn != actualLine.EndColumn)
                {
                    return false;
                }

                if (expectedLine.SamplesInclusive != actualLine.SamplesInclusive)
                {
                    return false;
                }

                if (expectedLine.SamplesExclusive != actualLine.SamplesExclusive)
                {
                    return false;
                }

                if (expectedLine.TimeInclusive != actualLine.TimeInclusive)
                {
                    return false;
                }

                if (expectedLine.TimeExclusive != actualLine.TimeExclusive)
                {
                    return false;
                }


                if (expectedLine.AllocatedMemoryInclusive != actualLine.AllocatedMemoryInclusive)
                {
                    return false;
                }

                if (expectedLine.AllocatedMemoryExclusive != actualLine.AllocatedMemoryExclusive)
                {
                    return false;
                }
            }

            return true;
        }

        private void RunTestCase(TotalsTestCase testCase)
        {
            _dataProvider.BuildStatisticsRaw(testCase.TimeInterval);
            var passed = true;
            foreach (var expectedData in testCase.ExpectedTotals)
            {
                var actualData = _dataProvider.ThreadsStatisticsRaw[expectedData.Tid].Totals;
                if (expectedData.Samples != actualData.GetValue(StatisticsType.Sample))
                {
                    passed = false;
                    break;
                }
                else if (expectedData.Time != actualData.GetValue(StatisticsType.Time))
                {
                    passed = false;
                    break;
                }
                else if (expectedData.Memory != actualData.GetValue(StatisticsType.Memory))
                {
                    passed = false;
                    break;
                }
            }

            Console.WriteLine(
                $"TotalsTestCase {testCase.TimeInterval.Start}:{testCase.TimeInterval.End} {(passed ? "Passed" : "Failed")}");
        }

        private void RunTestCase(CallTreeTestCase testCase)
        {
            _dataProvider.BuildStatisticsRaw(testCase.TimeInterval);
            var passed = true;
            foreach (var expectedData in testCase.ExpectedCallTrees)
            {
                passed = CompareCallTrees(expectedData.CallTree, _dataProvider.ThreadsStatisticsRaw[expectedData.Tid].CallTree);
                if (!passed)
                {
                    break;
                }
            }

            Console.WriteLine(
                $"CallTreeTestCase {testCase.TimeInterval.Start}:{testCase.TimeInterval.End} {(passed ? "Passed" : "Failed")}");
        }

        private static bool CompareCallTrees(IReadOnlyList<ICallStatisticsTreeNode> expected, IReadOnlyList<ICallStatisticsTreeNode> actual)
        {
            if (expected.Count != actual.Count)
            {
                return false;
            }

            for (var i = 0; i < expected.Count; i++)
            {
                var expectedNode = expected[i];
                var actualNode = actual[i];
                if (expectedNode.Name != actualNode.Name)
                {
                    return false;
                }

                if (expectedNode.SamplesInclusive != actualNode.SamplesInclusive)
                {
                    return false;
                }

                if (expectedNode.SamplesExclusive != actualNode.SamplesExclusive)
                {
                    return false;
                }

                if (expectedNode.TimeInclusive != actualNode.TimeInclusive)
                {
                    return false;
                }

                if (expectedNode.TimeExclusive != actualNode.TimeExclusive)
                {
                    return false;
                }

                if (expectedNode.AllocatedMemoryInclusive != actualNode.AllocatedMemoryInclusive)
                {
                    return false;
                }

                if (expectedNode.AllocatedMemoryExclusive != actualNode.AllocatedMemoryExclusive)
                {
                    return false;
                }

                if (!CompareCallTrees(expectedNode.Children, actualNode.Children))
                {
                    return false;
                }
            }

            return true;
        }


        private void InitializeDataProvider()
        {
            var sessionProperties = _session.Properties;
            var profilerReportDirectory = sessionProperties.GetProperty("CoreClrProfilerReport", "path");
            var ctfReportDirectory = sessionProperties.GetProperty("CtfReport", "path");
            if (string.IsNullOrEmpty(profilerReportDirectory) || string.IsNullOrEmpty(ctfReportDirectory))
            {
                throw new Exception("Invalid Session Directory");
            }

            var plDataPath = Path.Combine(
                _session.SessionFolder,
                profilerReportDirectory,
                sessionProperties.GetProperty("CoreClrProfilerReport", "name"));

            var ctfDataPath = Path.Combine(
                _session.SessionFolder,
                ctfReportDirectory,
                sessionProperties.GetProperty("CtfReport", "name"));

            using (var file = new StreamReader(plDataPath))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    if (line.StartsWith("prf stm"))
                    {
                        StartedAt = DateTime.ParseExact(line.Substring(8), "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                        break;
                    }
                }
            }
            //TODO Add check if startedAt was found


            var bContainer = new BDataContainer(ctfDataPath);
            var cContainer = new DataContainer(plDataPath);

            bContainer.Load();
            cContainer.Load(new ProgressMonitor
            {
                Start = delegate { },

                Stop = delegate { },

                Tick = delegate { }
            });

            //TODO Find better place for NodeConstructor
            _dataProvider = new DataProvider(bContainer, cContainer, () => new CallTreeNode());
        }


        private void FixStartedNanoSeconds(ulong startTime)
        {
            if (StartedNanoseconds > startTime)
            {
                StartedNanoseconds -= (StartedNanoseconds - startTime) / 1000 / 1000 / 1000 / 3600 * 3600 * 1000 * 1000 * 1000;
            }
            else
            {
                StartedNanoseconds += (startTime - StartedNanoseconds) / 1000 / 1000 / 1000 / 3600 * 3600 * 1000 * 1000 * 1000; // +1 ?
            }
        }

    }
}