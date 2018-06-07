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

namespace NetCore.Profiler.Testing.TestDataWriter
{
    internal class DataWriter
    {
        internal string SessionFile { get; set; }

        internal string OutputFIle { get; set; }

        internal List<SelectedTimeFrame> Intervals { get; set; }


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


        internal void Write()
        {
            Load();
            var suite = new Suite
            {
                Name = "TestSuite",
                InputSessionPath = SessionFile
            };

            foreach (var interval in Intervals)
            {
                _dataProvider.BuildStatisticsRaw(interval);
                suite.TestCases.Add(CreateFunctionTableTestCase(interval));
                suite.TestCases.Add(CreateSourceCodeTableTestCase(interval));
                suite.TestCases.Add(CreateTotalsTestCase(interval));
                suite.TestCases.Add(CreateCallTreeTestCase(interval));
            }

            var s = JsonConvert.SerializeObject(suite, Formatting.Indented, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
            using (var writer = new StreamWriter(OutputFIle))
            {
                writer.Write(s);
            }
        }

        private FunctionTableTestCase CreateFunctionTableTestCase(SelectedTimeFrame interval)
        {
            var result = new FunctionTableTestCase
            {
                TimeInterval = interval
            };

            foreach (var thread in _dataProvider.SessionThreads)
            {
                result.ExpectedFunctionTables.Add(new ExpectedFunctionTableData
                {
                    Tid = thread.InternalId,
                    Functions = _dataProvider.ThreadsStatisticsRaw[thread.InternalId].Methods
                });
            }

            return result;
        }

        private SourceCodeTableTestCase CreateSourceCodeTableTestCase(SelectedTimeFrame interval)
        {
            var result = new SourceCodeTableTestCase
            {
                TimeInterval = interval
            };

            foreach (var thread in _dataProvider.SessionThreads)
            {
                result.ExpectedSourceCodeTables.Add(new ExpectedSourceCodeTableData
                {
                    Tid = thread.InternalId,
                    SourceLines = _dataProvider.ThreadsStatisticsRaw[thread.InternalId].Lines
                });
            }

            return result;
        }

        private TotalsTestCase CreateTotalsTestCase(SelectedTimeFrame interval)
        {
            var result = new TotalsTestCase
            {
                TimeInterval = interval
            };

            foreach (var thread in _dataProvider.SessionThreads)
            {
                var totals = _dataProvider.ThreadsStatisticsRaw[thread.InternalId].Totals;
                result.ExpectedTotals.Add(new ExpectedTotalsData
                {
                    Tid = thread.InternalId,
                    Samples = totals.GetValue(StatisticsType.Sample),
                    Time = totals.GetValue(StatisticsType.Time),
                    Memory = totals.GetValue(StatisticsType.Memory)
                });
            }

            return result;
        }

        private CallTreeTestCase CreateCallTreeTestCase(SelectedTimeFrame interval)
        {
            var result = new CallTreeTestCase
            {
                TimeInterval = interval
            };

            foreach (var thread in _dataProvider.SessionThreads)
            {
                result.ExpectedCallTrees.Add(new ExpectedCallTreeData
                {
                    Tid = thread.InternalId,
                    CallTree = _dataProvider.ThreadsStatisticsRaw[thread.InternalId].CallTree
                });
            }

            return result;
        }

        private void Load()
        {
            _session = new SavedSession()
            {
                ProjectFolder = Path.GetDirectoryName(Path.GetDirectoryName(SessionFile)),
                SessionFile = SessionFile
            };
            _session.Load();
            InitializeDataProvider();
            _dataProvider.Load();
            FixStartedNanoSeconds(_dataProvider.MinimalStartTime);

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