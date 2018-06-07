using System.Collections.Generic;
using NetCore.Profiler.Analytics.DataProvider;

namespace NetCore.Profiler.Testing.Core.Model
{
    public class CallPathTableTestCase : TestCase
    {
        public override SessionType Type => SessionType.CallPathTableTestCase;

        public SelectedTimeFrame TimeInterval { get; set; }

        public List<ExpectedCallPathTableData> ExpectedCallPathTables { get; set; }

    }
}
