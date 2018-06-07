/*
 * Copyright 2017 (c) Samsung Electronics Co., Ltd  All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * 	http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Tizen.VisualStudio.Tools.Data
{
    public class ToolsInfo
    {
        //private const string InstallerRelativePath = @"SDK";

        private const string SdkInfoFileName = "sdk.info";
        private const string PkgMgrExeName = "package-manager.exe";
        private const string EmulatorMgrExeName = "emulator-manager.exe";
        private const string CertificateMgrExeName = "certificate-manager.exe";
        private const string DeviceMgrExeName = "device-manager.exe";
        private const string SDBExeName = "sdb.exe";
        private const string DefaultCertProfileName = "profiles.xml";

        private const string SdkInfoRelativePath = @"";
        private const string PkgMgrRelativePath = @"package-manager";
        private const string EmulatorMgrRelativePath = @"tools\emulator\bin";
        private const string CertificateMgrRelativePath = @"tools\certificate-manager\";
        //private const string DeviceMgrRelativePFPath = @"tools\device-manager";
        private const string DeviceMgrRelativePath = @"tools\device-manager\bin";
        private const string SDBRelativePath = @"tools\";
        private const string DefaultCertProfileRelativePath = @"profile";
        private const string PlatformPathTizen40RelativePath = @"platforms\tizen-4.0\";
        private const string OndemandRelativePath = @"platforms\tizen-4.0\common\on-demand";

        //private static string DefaultCertProfilePath = "";

        private static string toolsFolderPath = "";

        private static ToolsInfo instance;
        private static object syncLock = new object();

        public delegate void OnToolsDirChangedEventHandler(string toolsDirPath);
        public event OnToolsDirChangedEventHandler OnToolsDirChanged;

        private ToolsInfo()
        {
        }

        public static ToolsInfo Instance()
        {
            if (instance == null)
            {
                lock (syncLock)
                {
                    instance = new ToolsInfo();
                }
            }

            return instance;
        }

        public string ToolsFolderPath
        {
            get
            {
                return toolsFolderPath;
            }

            set
            {
                bool isUpdated = !value.Equals(toolsFolderPath);

                if (isUpdated)
                {
                    toolsFolderPath = value;
                    UpdateToolPath(toolsFolderPath);
                }
            }
        }

        private void UpdateToolPath(string rootPath)
        {
            try
            {
                OnToolsDirChanged(rootPath);
            }
            catch
            {
            }

            DebugBridge.DeviceManager.StartDeviceMonitor();
        }

        public string OndemandFolderPath
        {
            get
            {
                return GenToolPath(OndemandRelativePath);
            }
        }

        public string PlatformPath
        {
            get
            {
                return GenToolPath(PlatformPathTizen40RelativePath);
            }
        }

        private static string SdkInfoFilePath
        {
            get
            {
                return Path.Combine(toolsFolderPath, SdkInfoRelativePath, SdkInfoFileName);
            }
        }

        private string UserDataFolderPath
        {
            get
            {
                if (string.IsNullOrEmpty(toolsFolderPath))
                {
                    return "";
                }
                else
                {
                    // get tizen sdk user data path from sdk.info file
                    string sdkInfoPath = Path.Combine(toolsFolderPath, SdkInfoFilePath);

                    if (!File.Exists(sdkInfoPath))
                    {
                        return "";
                    }

                    var data = new Dictionary<string, string>();

                    foreach (var row in File.ReadAllLines(sdkInfoPath))
                    {
                        data.Add(row.Split('=')[0], string.Join("=", row.Split('=').Skip(1).ToArray()));
                    }

                    string value;
                    string userDataFolderPath = "";

                    if (data.TryGetValue("TIZEN_SDK_DATA_PATH", out value))
                    {
                        userDataFolderPath = value;
                    }

                    return userDataFolderPath;
                }
            }
        }

        public string PkgMgrPath
        {
            get
            {
                return GenToolPath(Path.Combine(PkgMgrRelativePath, PkgMgrExeName));
            }
        }

        public string EmulatorMgrPath
        {
            get
            {
                return GenToolPath(Path.Combine(EmulatorMgrRelativePath, EmulatorMgrExeName));
            }
        }

        public string CertificateMgrPath
        {
            get
            {
                return GenToolPath(Path.Combine(CertificateMgrRelativePath, CertificateMgrExeName));
            }
        }

        public string DeviceMgrPath
        {
            get
            {
                return GenToolPath(Path.Combine(DeviceMgrRelativePath, DeviceMgrExeName));
            }
        }

        public string SDBPath
        {
            get
            {
                return GenToolPath(Path.Combine(SDBRelativePath, SDBExeName));
            }
        }

        public string DefaultCertPath
        {
            get
            {
                string defaultCertPath = Path.Combine(UserDataFolderPath, DefaultCertProfileRelativePath, DefaultCertProfileName);

                return File.Exists(defaultCertPath) ? defaultCertPath : string.Empty;

                /*if (string.IsNullOrEmpty(toolsFolderPath))
                {
                    return "";
                }
                else
                {
                    return DefaultCertProfilePath;
                }*/
            }
        }

        private string GenToolPath(string relPath)
        {
            return string.IsNullOrEmpty(toolsFolderPath) ? string.Empty : Path.Combine(toolsFolderPath, relPath);
        }
    }
}
