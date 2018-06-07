using System.Collections.Generic;
using NetCore.Profiler.Analytics.DataProvider;

namespace NetCore.Profiler.Testing.Core.Model
{
    public class FunctionTableTestCase : TestCase
    {
        public override SessionType Type => SessionType.FunctionTableTestCase;

        public SelectedTimeFrame TimeInterval { get; set; }

        public List<ExpectedFunctionTableData> ExpectedFunctionTables { get; set; } = new List<ExpectedFunctionTableData>();

    }
}
