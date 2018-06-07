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

using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Tizen.VisualStudio.Tools.DebugBridge;

namespace Tizen.VisualStudio.Tools.Data
{
    public abstract class ToolsPathInfo : UIElementDialogPage, INotifyPropertyChanged
    {
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
        public event PropertyChangedEventHandler PropertyChanged;
        public static bool isDirty = false;

        private static FileSystemWatcher sdbWatcher;

        private static string toolsRootPath;
        public static string ToolsRootPath
        {
            get => toolsRootPath;
            set
            {
                bool isChanged = !string.IsNullOrEmpty(value) && !value.Equals(toolsRootPath);
                toolsRootPath = value;

                if (isChanged)
                {
                    if (!IsAccessiblePath(value))
                    {
                        MessageBox.Show(
                            "Selected Tizen SDK path is inaccessible with current permission. " +
                            "Admin privilege can be required to use Tizen SDK.",
                            string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    DeviceManager.StartDeviceMonitor();
                    StartToolsUpdateMonitor();
                }
            }
        }

        public string ToolsPath
        {
            get => ToolsRootPath;
            set
            {
                ToolsRootPath = value;
                OnPropertyChanged();
            }
        }

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static string GenToolPath(string relPath) => string.IsNullOrEmpty(ToolsRootPath) ? string.Empty : Path.Combine(ToolsRootPath, relPath);

        public static string OndemandFolderPath => GenToolPath(OndemandRelativePath);

        public static string PlatformPath => GenToolPath(PlatformPathTizen40RelativePath);

        public static string SdkInfoFilePath => Path.Combine(ToolsRootPath, SdkInfoRelativePath, SdkInfoFileName);

        public static string PkgMgrPath => GenToolPath(Path.Combine(PkgMgrRelativePath, PkgMgrExeName));

        public static string EmulatorMgrPath => GenToolPath(Path.Combine(EmulatorMgrRelativePath, EmulatorMgrExeName));

        public static string CertificateMgrPath => GenToolPath(Path.Combine(CertificateMgrRelativePath, CertificateMgrExeName));

        public static string DeviceMgrPath => GenToolPath(Path.Combine(DeviceMgrRelativePath, DeviceMgrExeName));

        public static string SDBPath => GenToolPath(Path.Combine(SDBRelativePath, SDBExeName));

        public static string DefaultCertPath
        {
            get
            {
                string defaultCertPath = Path.Combine(UserDataFolderPath, DefaultCertProfileRelativePath, DefaultCertProfileName);

                return File.Exists(defaultCertPath) ? defaultCertPath : string.Empty;
            }
        }

        public static string UserDataFolderPath
        {
            get
            {
                if (string.IsNullOrEmpty(ToolsRootPath))
                {
                    return "";
                }
                else
                {
                    // get tizen sdk user data path from sdk.info file
                    string sdkInfoPath = Path.Combine(ToolsRootPath, SdkInfoFilePath);

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

        private static bool IsAccessiblePath(string directoryPath)
        {
            try
            {
                using (FileStream fs = File.Create(
                    Path.Combine(directoryPath, Path.GetRandomFileName()),
                    1,
                    FileOptions.DeleteOnClose))
                {

                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void StartToolsUpdateMonitor()
        {
            StartSDBUpdateMonitor();
        }

        private static void StartSDBUpdateMonitor()
        {
            string sdbPath = Path.Combine(toolsRootPath, SDBRelativePath);

            if (Directory.Exists(sdbPath))
            {
                sdbWatcher?.Dispose();
                sdbWatcher = new FileSystemWatcher();
                sdbWatcher.Path = sdbPath;
                sdbWatcher.Filter = SDBExeName;
                sdbWatcher.NotifyFilter = NotifyFilters.LastWrite;

                sdbWatcher.Changed += new FileSystemEventHandler(OnSDBExeUpdated);

                sdbWatcher.EnableRaisingEvents = true;
            }
        }

        private static void OnSDBExeUpdated(object source, FileSystemEventArgs e)
        {
            DeviceManager.ResetDeviceMonitorRetry();
            DeviceManager.StartDeviceMonitor();
        }
    }
}
