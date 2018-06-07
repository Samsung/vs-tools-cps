using System.Collections.Generic;
using NetCore.Profiler.Analytics.DataProvider;

namespace NetCore.Profiler.Testing.Core.Model
{
    public class SourceCodeTableTestCase : TestCase
    {
        public override SessionType Type => SessionType.SourceCodeTableTestCase;

        public SelectedTimeFrame TimeInterval { get; set; }

        public List<ExpectedSourceCodeTableData> ExpectedSourceCodeTables { get; set; } = new List<ExpectedSourceCodeTableData>();

    }
}
