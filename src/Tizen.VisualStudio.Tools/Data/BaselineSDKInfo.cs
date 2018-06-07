using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Tizen.VisualStudio.Tools.Data
{
    public static class BaselineSDKInfo
    {
        private static string BaselineInstaller32, BaselineInstaller64;
        private static Version BaselineMinVersion;
        static BaselineSDKInfo()
        {
            var dirPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            var InstallerPath = System.IO.Path.Combine(dirPath, "BaselineSDK.info");
            try
            {
                if (File.Exists(InstallerPath))
                {
                    string BaselineSDKInfoString = File.ReadAllText(InstallerPath);
                    string[] ParsedString = BaselineSDKInfoString.Split(new char[] { '=', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                    if (ParsedString[0].Equals("INSTALLER_32BIT_URL"))
                    {
                        BaselineInstaller32 = ParsedString[1];
                    }

                    if (ParsedString[2].Equals("INSTALLER_64BIT_URL"))
                    {
                        BaselineInstaller64 = ParsedString[3];
                    }

                    if (ParsedString[4].Equals("BASELINE_MIN_VER"))
                    {
                        Version.TryParse(ParsedString[5], out BaselineMinVersion);
                    }
                }
            }
            catch
            {

            }
        }

        public static string Get32InstallerURL() => BaselineInstaller32;
        public static string Get64InstallerURL() => BaselineInstaller64;
        public static Version GetBaselineSDKMinVersion() => BaselineMinVersion;
    }
}
