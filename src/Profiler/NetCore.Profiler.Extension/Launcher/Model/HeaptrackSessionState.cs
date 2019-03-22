using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetCore.Profiler.Extension.Launcher.Model
{
    public enum HeaptrackSessionState
    {
        Initial,
        Starting,
        UploadFiles,
        Running,
        DownloadFiles,
        Stopping,
        Finished,
        Failed
    }
}
