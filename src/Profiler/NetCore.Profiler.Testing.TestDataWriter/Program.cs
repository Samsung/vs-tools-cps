using System;
using System.Collections.Generic;
using NetCore.Profiler.Analytics.DataProvider;

namespace NetCore.Profiler.Testing.TestDataWriter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length <= 2 || (args.Length - 2) % 2 != 0)
            {
                Console.WriteLine("Syntax: <session file> <output file> {<intrval start> <interval end>}+");
                return;
            }

            var intervals = new List<SelectedTimeFrame>();
            for (var i = 0; i < (args.Length - 2) / 2; i++)
            {
                intervals.Add(new SelectedTimeFrame
                {
                    Start = Convert.ToUInt64(args[2 + i * 2]),
                    End = Convert.ToUInt64(args[3 + i * 2])
                });
            }

            var writer = new DataWriter
            {
                SessionFile = args[0],

                OutputFIle = args[1],

                Intervals = intervals
            };

            writer.Write();

        }
    }
}
