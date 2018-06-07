using System.Collections.Generic;
using NetCore.Profiler.Analytics.Model;

namespace NetCore.Profiler.Testing.Core.Model
{
    public class ExpectedCallPathTableData
    {
        public ulong Tid { get; set; }

        public List<IHotPath> CallPaths { get; set; }
    }
}