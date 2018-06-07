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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows.Forms;
using Tizen.VisualStudio.Tools.Data;
using Tizen.VisualStudio.Tools.DebugBridge;
using Tizen.VisualStudio.Utilities;

namespace Tizen.VisualStudio.ExternalTools
{
    public class SdkInstaller
    {
        public const string InstallerTitle = "Baseline SDK Installer";//"Tizen SDK Installer";//Title to pick the JAVA process.

        private const string linkForWin32 = "http://download.tizen.org/sdk/Installer/tizen-studio_2.1/Baseline_Tizen_Studio_2.1_windows-32.exe";//Installer URL
        private const string linkForWin64 = "http://download.tizen.org/sdk/Installer/tizen-studio_2.1/Baseline_Tizen_Studio_2.1_windows-64.exe";

        private string BaselineInstaller32 = "";
        private string BaselineInstaller64 = "";

        private Uri installerLink;
        private string downloadedInstallerPath;
        private WebClient webClient;

        //private ToolsInfo toolInfo = ToolsInfo.Instance();

        public SdkInstaller()
        {
            BaselineInstaller32 = BaselineSDKInfo.Get32InstallerURL();
            BaselineInstaller64 = BaselineSDKInfo.Get64InstallerURL();
            installerLink = new Uri(GetLinkForHostArch());
            downloadedInstallerPath = Downloader.GetLocalPathByUri(Path.GetTempPath(), installerLink);
        }

        public bool IsDownloadNeeded()
        {
            if (File.Exists(downloadedInstallerPath))
            {
                string msg = string.Format("{0} is detected on `{1}`, but it can be an incomplete file.\n\nDo you want to use an existing file anyway?", InstallerTitle, downloadedInstallerPath);

                if (MessageBox.Show(msg, InstallerTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    return false;
                }
            }

            return true;
        }

        public bool StartDownload(DownloadProgressChangedEventHandler OnProgressChanged, AsyncCompletedEventHandler OnDownloadComplete)
        {
            try
            {
                string localPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(installerLink.AbsolutePath));

                webClient = new WebClient();
                webClient.DownloadFileAsync(installerLink, localPath);
                webClient.DownloadProgressChanged += OnProgressChanged;
                webClient.DownloadFileCompleted += OnDownloadComplete;

                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to download : " + e.Message, InstallerTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public bool LaunchInstaller()
        {
            try
            {
                using (Process launcher = new Process())
                {
                    launcher.StartInfo.FileName = downloadedInstallerPath;
                    launcher.StartInfo.Arguments = "--automatic-installation \"" + ToolsPathInfo.ToolsRootPath + "\"";
                    launcher.StartInfo.UseShellExecute = true;
                    launcher.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    launcher.StartInfo.Verb = "runas";
                    //launcher.Exited += OnInstallerExited;
                    launcher.Start();
                    launcher.WaitForExit();

                    if (launcher.ExitCode == 0)
                    {
                        Process installerWindow = JavaProcessUtil.GetLastProcessByTitleAwait(launcher, InstallerTitle);
                        JavaProcessUtil.AttachExitEvent(installerWindow, OnInstallerExited);

                        //CLIExecutor ex = new CLIExecutor(proc, waitDialogDesc, null, OnInstallerCanceled, OnInstallerExited);
                        //IsInstallingNow = ex.Execute();

                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to install Tizen tools : " + e.Message, InstallerTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return false;
        }

        public void CancelDownload()
        {
            webClient?.CancelAsync();
        }

        private string GetLinkForHostArch()
        {
            return Environment.Is64BitOperatingSystem ? BaselineInstaller64 : BaselineInstaller32;
        }

        private void OnInstallerExited(/*object sender, EventArgs e*/)
        {
            //InstallEmulator();
            InstallDotnetCliExt();
            ToolsPathInfo.StartToolsUpdateMonitor();
            StartDeviceMonitor();
        }

        private void StartDeviceMonitor()
        {
            if (File.Exists(ToolsPathInfo.SDBPath))
            {
                DeviceManager.ResetDeviceMonitorRetry();
                DeviceManager.StartDeviceMonitor();
            }
        }

        private void InstallDotnetCliExt()
        {
            using (Process launcher = new Process())
            {
                launcher.StartInfo.FileName = ToolsPathInfo.PkgMgrPath;
                launcher.StartInfo.Arguments = "--automatic-installation DOTNET-CLI-EXT";
                launcher.StartInfo.UseShellExecute = true;
                launcher.StartInfo.Verb = "runas";
                launcher.Start();
            }
        }

        private void InstallEmulator()
        {
            const string pkgMgrCliExeName = "package-manager-cli.exe";

            DeviceManager.StopDeviceMonitor();

            string pkgMgrCliPath = string.Empty;
            bool isPkgMgrAvailable = !string.IsNullOrEmpty(ToolsPathInfo.PkgMgrPath) && File.Exists(pkgMgrCliPath = Path.Combine(Path.GetDirectoryName(ToolsPathInfo.PkgMgrPath), pkgMgrCliExeName));

            if (!isPkgMgrAvailable)
            {
                return;
            }

            try
            {
                Process proc = new Process();
                proc.StartInfo.FileName = pkgMgrCliPath;
                proc.StartInfo.Arguments = "install --accept-license MOBILE-4.0-Emulator";
                proc.EnableRaisingEvents = true;
                proc.Exited += (object sender, EventArgs e) =>
                {
                    DeviceManager.StartDeviceMonitor();
                };
                proc.Start();
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to install Tizen Emulator : " + e.Message, InstallerTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
