namespace NetCore.Profiler.Testing.Core.Model
{
    public class SessionDataTestCase : TestCase
    {
        public override SessionType Type => SessionType.SessionDataTestCase;

        public SessionData ExpectedSessionData { get; set; }

    }
}
