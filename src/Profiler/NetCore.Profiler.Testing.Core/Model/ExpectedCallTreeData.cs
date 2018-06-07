using System.Collections.Generic;
using NetCore.Profiler.Analytics.Model;

namespace NetCore.Profiler.Testing.Core.Model
{
    public class ExpectedCallTreeData
    {
        public ulong Tid { get; set; }

        public List<ICallStatisticsTreeNode> CallTree { get; set; }
    }
}