using System;

namespace NetCore.Profiler.Testing.TestRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Syntax: <suite file>");
                return;
            }

            var testRunner = new TestRunner
            {
                SuiteFile = args[0]
            };

            testRunner.Run();

        }
    }
}
