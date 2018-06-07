namespace NetCore.Profiler.Testing.Core.Model
{
    public abstract class TestCase
    {
        public enum SessionType
        {
            SessionDataTestCase,
            FunctionTableTestCase,
            CallPathTableTestCase,
            SourceCodeTableTestCase,
            TotalsTestCase,
            CallTreeTestCase
        }

        public abstract SessionType Type { get; }
    }
}