using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tizen.VisualStudio.Tools.DebugBridge;

namespace Tizen.VisualStudio.DebugBridge
{
    public class TizenPackageTracer
    {
        public static TizenPackageTracer Instance
        {
            get;
            private set;
        }
        
        private Dictionary<string, string> tpkPathByAppId;

        private TizenPackageTracer()
        {
            tpkPathByAppId = new Dictionary<string, string>();
        }

        static public void Initialize()
        {
            if (Instance == null)
            {
                Instance = new TizenPackageTracer();
                DeviceManager.SubscribeSelectedDevice(OnDeviceChanged);
            }
        }

        private static void OnDeviceChanged(object sender, EventArgs e)
        {
            CleanTpiFiles();
        }

        public static void CleanTpiFiles()
        {
            try
            {
                var dte2 = Package.GetGlobalService(typeof(SDTE)) as DTE2;
                string solDirPath = Path.GetDirectoryName(dte2.Solution.FullName);
                var tpiFiles = Directory.EnumerateFiles(solDirPath, "*.tpi", SearchOption.AllDirectories);

                foreach (string tpiFile in tpiFiles)
                {
                    try
                    {
                        string tpkFile = Path.Combine(Path.GetDirectoryName(tpiFile), Path.GetFileNameWithoutExtension(tpiFile) + ".tpk");
                        if (File.Exists(tpkFile))
                        {
                            File.Delete(tpiFile);
                        }
                    }
                    catch
                    {

                    }
                }
            }
            catch
            {
            }
        }

        public void AddDebugeeApp(string appId, string pkgPath)
        {
            try
            {
                tpkPathByAppId.Remove(appId);
                tpkPathByAppId.Add(appId, pkgPath);
            }
            catch
            {

            }
        }

        public bool IsAppIdOnWaiting(string appId)
        {
            try
            {
                return tpkPathByAppId.ContainsKey(appId);
            }
            catch
            {
                return false;
            }
        }

        public string GetTpkPathByAppId(string appId)
        {
            try
            {
                return tpkPathByAppId[appId];
            }
            catch
            {
                return null;
            }
        }

        public void Clear()
        {
            tpkPathByAppId?.Clear();
        }
    }
}
