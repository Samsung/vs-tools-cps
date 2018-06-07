using System.Collections.Generic;
using NetCore.Profiler.Analytics.Model;

namespace NetCore.Profiler.Testing.Core.Model
{
    public class ExpectedFunctionTableData
    {
        public ulong Tid { get; set; }

        public List<IMethodStatistics> Functions { get; set; }
    }
}