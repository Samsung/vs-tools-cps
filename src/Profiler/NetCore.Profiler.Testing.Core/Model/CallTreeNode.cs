using System.Collections.Generic;
using NetCore.Profiler.Analytics.Model;

namespace NetCore.Profiler.Testing.Core.Model
{
    public class CallTreeNode : ICallStatisticsTreeNode
    {
        public ICallStatisticsTreeNode Parent
        {
            get => null;
            set { }
        }

        public ulong SamplesExclusive { get; set; }

        public ulong SamplesInclusive { get; set; }

        public ulong TimeExclusive { get; set; }

        public ulong TimeInclusive { get; set; }

        public ulong AllocatedMemoryExclusive { get; set; }

        public ulong AllocatedMemoryInclusive { get; set; }

        public List<ICallStatisticsTreeNode> Children { get; set; } = new List<ICallStatisticsTreeNode>();

        public string Name { get; set; }
    }
}
