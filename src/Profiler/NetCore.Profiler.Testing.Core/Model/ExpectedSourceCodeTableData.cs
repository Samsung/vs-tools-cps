using System.Collections.Generic;
using NetCore.Profiler.Analytics.Model;

namespace NetCore.Profiler.Testing.Core.Model
{
    public class ExpectedSourceCodeTableData
    {
        public ulong Tid { get; set; }

        public List<ISourceLineStatistics> SourceLines { get; set; }
    }
}