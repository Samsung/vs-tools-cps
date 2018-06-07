using System.Collections.Generic;
using NetCore.Profiler.Analytics.DataProvider;

namespace NetCore.Profiler.Testing.Core.Model
{
    public class TotalsTestCase : TestCase
    {
        public override SessionType Type => SessionType.TotalsTestCase;

        public SelectedTimeFrame TimeInterval { get; set; }

        public List<ExpectedTotalsData> ExpectedTotals { get; set; } = new List<ExpectedTotalsData>();

    }
}
