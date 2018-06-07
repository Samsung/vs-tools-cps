using System.Collections.Generic;

namespace NetCore.Profiler.Testing.Core.Model
{
    public class Suite
    {
        public string Name { get; set; }

        public string InputSessionPath { get; set; }

        public List<TestCase> TestCases { get; set; } = new List<TestCase>();
    }
}
