using System.Collections.Generic;
using NetCore.Profiler.Analytics.DataProvider;

namespace NetCore.Profiler.Testing.Core.Model
{
    public class CallTreeTestCase : TestCase
    {
        public override SessionType Type => SessionType.CallTreeTestCase;

        public SelectedTimeFrame TimeInterval { get; set; }

        public List<ExpectedCallTreeData> ExpectedCallTrees { get; set; } = new List<ExpectedCallTreeData>();

    }
}
