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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Tizen.VisualStudio.ExternalTools;
using Tizen.VisualStudio.InstallLauncher;
using Tizen.VisualStudio.Tools.Data;
using Tizen.VisualStudio.Utilities;

namespace Tizen.VisualStudio.Tools.ExternalTool
{
    public abstract class ExternalToolLauncher
    {
        private const string InstallerDownloadPageUrl = "http://download.tizen.org/sdk/vstools/";
        private const string ToolsInfoFlow = "Tools -> Options -> Tizen -> Tools";

        private string externalToolDesc;
        private Process externalToolProcess;
        private ProcessStartInfo procInfo;
        private bool isMultiExecAllowed = false;
        //private ToolsInfo toolsInfo;

        public string FileName
        {
            get
            {
                return procInfo.FileName;
            }
        }

        public bool IsInstalled
        {
            get
            {
                return File.Exists(FileName);
            }
        }

        public ExternalToolLauncher(string externalToolDesc, ProcessStartInfo procInfo, bool isMultiExecAllowed)
        {
            this.externalToolDesc = externalToolDesc;
            this.procInfo = procInfo;
            this.isMultiExecAllowed = isMultiExecAllowed;

            //toolsInfo = ToolsInfo.Instance();
        }

        public void Launch()
        {
            Process runningTool = GetRunningExternalToolProcess();

            bool isLaunchAllowed = this.isMultiExecAllowed || (runningTool == null);

            if (isLaunchAllowed)
            {
                if (CheckToolAvailability())
                {
                    LaunchProcess();
                }
            }
            else
            {
                MessageBox.Show(this.externalToolDesc + " process is running.", this.externalToolDesc);

                foreach (Process proc in JavaProcessUtil.GetProcessesByTitle(externalToolDesc))
                {
                    ShowWindow(proc.MainWindowHandle, 0x09);
                    SetForegroundWindow(proc.MainWindowHandle);
                }

                ShowWindow(runningTool.MainWindowHandle, 0x09);
                SetForegroundWindow(runningTool.MainWindowHandle);
            }
        }

        public void WaitForExit()
        {
            if (this.externalToolProcess != null)
            {
                this.externalToolProcess.WaitForExit();
            }
        }

        public void Kill()
        {
            if (externalToolProcess != null)
            {
                externalToolProcess.Kill();
            }
        }

        [DllImport("user32.dll")]
        internal static extern IntPtr SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        internal static extern int ShowWindow(IntPtr hWnd, uint Msg);

        private void LaunchProcess()
        {
            try
            {
                externalToolProcess = new Process();
                externalToolProcess.StartInfo = procInfo;
                externalToolProcess.Start();
            }
            catch (Exception e)
            {
                MessageBox.Show("Exception occurred : " + e.Message);
            }
        }

        private bool CheckToolAvailability()
        {
            if (string.IsNullOrEmpty(ToolsPathInfo.ToolsRootPath) || !IsInstalled)
            {
                TryInstall();
                return false;
            }

            return true;
        }

        private Process GetRunningExternalToolProcess()
        {
            string path = this.procInfo.FileName;
            string query = @"SELECT * FROM Win32_Process WHERE ExecutablePath='" + path.Replace("\\", @"\\") + "'";

            ManagementObjectCollection processList;
            ManagementObject mainProcessObj = null;

            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
            {
                processList = searcher.Get();
            }

            foreach (ManagementObject obj in processList)
            {
                mainProcessObj = obj;
            }

            if (mainProcessObj != null)
            {
                int processId = (int)(uint)mainProcessObj.GetPropertyValue("ProcessId");
                return Process.GetProcessById(processId);
            }
            else
            {
                return null;
            }
        }

        private void TryInstall()
        {
            PackageManagerLauncher pkgMgr = new PackageManagerLauncher();
            if (pkgMgr.IsInstalled)
            {
                string msg = string.Format("Can not find {0}.\n\nDo you want to install it with {1}?", externalToolDesc, pkgMgr.externalToolDesc);
                if (MessageBox.Show(msg, pkgMgr.externalToolDesc, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    pkgMgr.Launch();
                }

                return;
            }

            InstallWizard iWizard = new InstallWizard();

            if (iWizard.ShowDialog() == true && iWizard.info.NewPath)
            {
                return;
            }
        }

        private void GoToInstallerDownloadPage()
        {
            try
            {
                System.Diagnostics.Process.Start(InstallerDownloadPageUrl);
            }
            catch
            {
                ////DO NOTHING IF PC HAS NO BROWSER, BUT IT CAN NOT BE HAPPEND.
            }
        }
    }
}
