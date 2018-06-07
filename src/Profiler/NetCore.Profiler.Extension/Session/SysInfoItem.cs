using System;
using System.Text.RegularExpressions;

namespace NetCore.Profiler.Extension.Session
{
    public class SysInfoItem
    {
        public long Timestamp { get; set; }     // 0
        public int CoreNum { get; set; }        // 1
        public long UserLoad { get; set; }      // 2
        public long SysLoad { get; set; }       // 3
        public long MemTotal { get; set; }      // 7    
        public long MemFree { get; set; }       // 8
        public long UserSys { get { return this.UserLoad + this.SysLoad; } }
        public long MemLoad { get { return this.MemTotal - this.MemFree; } }
        public string ProfilerStatus { get; set; } // 10

        private static Regex RegTempl =
                        new Regex(@"([0-9]+) ([0-9]+) ([0-9]+) ([0-9]+) ([0-9]+) ([0-9]+) ([0-9]+) ([0-9]+) ([0-9]+) ([0-9]+) (.*)$");

        private SysInfoItem(long timestamp, int coreNum, long userLoad, long sysLoad, long memTotal, long memFree, string profilerStatus)
        {
            this.Timestamp = timestamp;
            this.CoreNum = coreNum;
            this.UserLoad = userLoad;
            this.SysLoad = SysLoad;
            this.MemTotal = memTotal;
            this.MemFree = memFree;
            this.ProfilerStatus = profilerStatus;
        }

        public static SysInfoItem CreateInstance(string str)
        {
            SysInfoItem sii = null;
            Match m = RegTempl.Match(str);
            if (m.Success)
            {
                long timestamp = Convert.ToInt64(m.Groups[1].Value);
                int coreNum = Convert.ToInt32(m.Groups[2].Value);
                long userLoad = Convert.ToInt64(m.Groups[3].Value);
                long sysLoad = Convert.ToInt64(m.Groups[4].Value);
                long memTotal = Convert.ToInt64(m.Groups[8].Value);
                long memFree = Convert.ToInt64(m.Groups[9].Value);
                string profilerStatus = m.Groups[11].Value;
                sii = new SysInfoItem(timestamp, coreNum, userLoad, sysLoad, memTotal, memFree, profilerStatus);
            }
            return sii;
        }
    }
}
