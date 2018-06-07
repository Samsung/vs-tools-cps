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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Tizen.VisualStudio.Tools.DebugBridge;
using Tizen.VisualStudio.Tools.Utilities;
using Tizen.VisualStudio.Tools.DebugBridge.SDBCommand;
using Tizen.VisualStudio.Tools.Data;

namespace Tizen.VisualStudio.Debug
{
    public class OnDemandInstaller : IOndemandInstallerEvents
    {
        protected const string lldbVersion = "3.8.1";
        protected string pkgExtension = ".rpm";
        protected const string lldbPkgName = "lldb";

        private const string lldbPrefix = "lldb-";

        private IVsOutputWindowPane outputPane = null;
        //private ToolsInfo toolInfo = ToolsInfo.Instance();

        private string errorMessage;
        public string ErrorMessage
        {
            get
            {
                return errorMessage;
            }

            protected set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    errorMessage = value;
                    OutputDeviceErrorMsg(value);
                }
            }
        }

        public OnDemandInstaller()
        {

        }

        protected virtual string GetLldbDestPath()
        {
            SDBCapability cap = new SDBCapability();
            string lldbPath = cap.GetValueByKey("sdk_toolpath") + @"/on-demand/" + GetLldbPkgName();
            return lldbPath;
        }

        private string GetTargetArch()
        {
            SDBCapability cap = new SDBCapability();
            string arch = cap.GetValueByKey("cpu_arch");
            return arch;
        }

        private bool SetSDBRoot(bool beRoot)
        {
            bool result = true;
            Process process = SDBLib.CreateSdbProcess(true, true);

            if (process == null)
            {
                ErrorMessage = "Failed to get sdb.exe program";
                return false;
            }

            string arguments;
            if (beRoot)
            {
                arguments = DeviceManager.AdjustSdbArgument("root on");
            }
            else
            {
                arguments = DeviceManager.AdjustSdbArgument("root off");
            }

            process.StartInfo.Arguments = arguments;
            ErrorMessage = String.Empty;

            try
            {
                if (!process.Start())
                {
                    ErrorMessage = "Failed to set SDB root";
                    return false;
                }

                process.WaitForExit(5 * 1000);
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                result = false;
            }

            return result;
        }

        private bool RemountRootRW()
        {
            bool result = true;
            Process process = SDBLib.CreateSdbProcess(true, true);
            if (process == null)
            {
                ErrorMessage = "Failed to get sdb.exe program";
                return false;
            }

            string fmt = "shell mount -o remount, rw /";
            string arguments = DeviceManager.AdjustSdbArgument(fmt);


            process.StartInfo.Arguments = arguments;
            ErrorMessage = String.Empty;

            try
            {
                if (!process.Start())
                {
                    ErrorMessage = "Failed to remount RW";
                    return false;
                }

                process.WaitForExit(5 * 1000);
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                result = false;
            }

            return result;
        }

        private bool InstallDebuggerRPM(string rpm)
        {
            bool result = true;
            Process process = SDBLib.CreateSdbProcess(true, true);
            if (process == null)
            {
                ErrorMessage = "Failed to get sdb.exe program";
                return false;
            }

            string fmt = "shell rpm -Uvh \"{0}\" --force";
            string arg = String.Format(fmt, rpm);
            string arguments = DeviceManager.AdjustSdbArgument(arg);

            process.StartInfo.Arguments = arguments;
            ErrorMessage = String.Empty;

            try
            {
                if (!process.Start())
                {
                    ErrorMessage = "Failed to install lldb rpm.";
                    return false;
                }

                process.WaitForExit(5 * 1000);
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                result = false;
            }

            return result;
        }

        protected virtual bool PushDebuggerPackage(string source, string destination)
        {
            DebuggerInstallWaiter waiter = new DebuggerInstallWaiter(this as IOndemandInstallerEvents);
            Process process = SDBLib.CreateSdbProcess(true, true);

            if (process == null)
            {
                ErrorMessage = "Failed to get sdb.exe program";
                return false;
            }

            string fmt = "push \"{0}\" \"{1}\"";
            string arg = String.Format(fmt, source, destination);
            string argument = DeviceManager.AdjustSdbArgument(arg);
            var result = SDBLib.RunSdbProcessAsync(process, argument, true,
                                                   waiter);

            if (waiter.Waiter.WaitOne(300 * 1000)) // 5 minutes will be enough?
            {
                ErrorMessage = waiter.LastErrorMessage;
                return waiter.PushResult;
            }

            ErrorMessage = "SDB Push Timeout.";
            return false;
        }

        protected virtual bool DeployDebuggerPackage(string debugger)
        {
            bool isRooted = SetSDBRoot(true);
            bool isInstalled = isRooted && RemountRootRW() && InstallDebuggerRPM(debugger);
            return isRooted && SetSDBRoot(false) && isInstalled;
        }

        protected void OutputDeviceErrorMsg(string msg)
        {
            DateTime localDate = DateTime.Now;
            string message =
                String.Format("{0} : {1}\n",
                              localDate.ToString(),
                              msg);
            this.outputPane.Activate();
            this.outputPane.OutputString(message);
        }

        protected virtual bool GetPkgInstalledStatus(out bool isInstalled)
        {
            isInstalled = false;
            ErrorMessage = string.Empty;

            PkgInstalledStatusWaiter waiter2 = new PkgInstalledStatusWaiter();
            string argument = DeviceManager.AdjustSdbArgument(
                        "shell \"rpm -qa | grep " + lldbPkgName + "\"");
            Process process = SDBLib.CreateSdbProcess(true, true);
            var result = (process == null) ? null : SDBLib.RunSdbProcessAsync(process, argument, true, waiter2);
            if (!waiter2.Waiter.WaitOne(30000))
            {
                OutputDeviceErrorMsg("GetPkgInstalledStatus failed: " + lldbPkgName);
                return false;
            }

            if (!string.IsNullOrEmpty(waiter2.InstalledStatus))
            {
                Version installedLldbVersion = new Version(waiter2.InstalledStatus.Split('-')[1]);
                Version lldbRpmFileVersion = new Version(lldbVersion);

                if (installedLldbVersion >= lldbRpmFileVersion)
                {
                    OutputDeviceErrorMsg("GetPkgInstalledStatus (already installed): " + lldbPkgName);
                    isInstalled = true;
                }
            }

            return true;
        }

        private string GetLldbPkgName()
        {
            string binaryName = lldbPrefix + lldbVersion;

            switch (GetTargetArch())
            {
                case "x86":
                    binaryName += "-i686";
                    break;
                case "x86_64":
                    binaryName += "-x86_64";
                    break;
                default:
                    binaryName += "-armv7l";
                    break;
            }

            return binaryName + pkgExtension;
        }

        public bool InstallDebugPackage(IVsOutputWindowPane outputPane, IVsThreadedWaitDialogFactory dlgFactory/*uint debugTargetTypeId,*/)
        {
            bool isInstalled;
            string source;
            string destination;
            string ondemandPath = ToolsPathInfo.OndemandFolderPath;

            this.outputPane = outputPane;
            if (this.outputPane != null)
            {
                this.outputPane.Activate();
            }

            // Check the lldb package was installed in previous or not.
            GetPkgInstalledStatus(out isInstalled);
            if (!isInstalled)
            {
                source = ondemandPath + @"\" + GetLldbPkgName();
                destination = GetLldbDestPath();
                if (!PushDebuggerPackage(source, destination))
                {
                    ErrorMessage = "Failed : Debugger Push from " + source + " to " + destination + ".\n";
                }

                // Install Debug package
                
                if (!DeployDebuggerPackage(destination))
                {
                    ErrorMessage = "Failed : Debugger Installation on " + destination + " .\n";
                }
            }

            ErrorMessage = string.Empty;
            return true;
        }

        void IOndemandInstallerEvents.OnMessage(string module, string message)
        {
            if (this.outputPane != null)
            {
                string outmessage;
                outmessage = String.Format("  Ondemand Installer ({0}): {1}\n",
                                           module, message);
                this.outputPane.OutputStringThreadSafe(outmessage);
            }
        }
    }

    class TarGzOnDemandInstaller : OnDemandInstaller
    {
        protected const string signatureName = "signature";

        public TarGzOnDemandInstaller() : base()
        {
            pkgExtension = ".tar.gz";
        }

        protected override string GetLldbDestPath()
        {
            SDBCapability cap = new SDBCapability();
            string lldbPath = cap.GetValueByKey("sdk_toolpath") + @"/on-demand/lldb.tar.gz";
            return lldbPath;
        }

        protected override bool PushDebuggerPackage(string source, string destination)
        {
            string signatureSource = source.Replace(lldbPkgName, signatureName).Replace(pkgExtension, string.Empty);
            string signatureDestination = destination.Replace(lldbPkgName, signatureName).Replace(pkgExtension, string.Empty);

            return base.PushDebuggerPackage(signatureSource, signatureDestination) && base.PushDebuggerPackage(source, destination);
        }

        protected override bool DeployDebuggerPackage(string debugger)
        {
            bool result = true;
            Process process = SDBLib.CreateSdbProcess(true, true);
            if (process == null)
            {
                ErrorMessage = "Failed to get sdb.exe program";
                return false;
            }

            string arg = "shell 0 vs_lldbinstall";
            string arguments = DeviceManager.AdjustSdbArgument(arg);

            process.StartInfo.Arguments = arguments;
            ErrorMessage = String.Empty;

            try
            {
                if (!process.Start())
                {
                    ErrorMessage = "Failed to install lldb.tar.gz";
                    return false;
                }

                process.WaitForExit(5 * 1000);
            }
            catch (Exception e)
            {
                ErrorMessage = e.Message;
                result = false;
            }

            return result;
        }

        protected override bool GetPkgInstalledStatus(out bool isInstalled)
        {
            isInstalled = false;
            ErrorMessage = string.Empty;

            PkgInstalledStatusWaiter waiter = new PkgInstalledStatusWaiter();
            string argument = DeviceManager.AdjustSdbArgument("shell 0 vs_lldbversion");
            Process process = SDBLib.CreateSdbProcess(true, true);
            var result = (process == null) ? null : SDBLib.RunSdbProcessAsync(process, argument, true, waiter);
            if (!waiter.Waiter.WaitOne(30000))
            {
                OutputDeviceErrorMsg("GetPkgInstalledStatus failed: Timeout");
                return false;
            }

            try
            {
                Version installedLldbVersion = new Version(waiter.InstalledStatus);
                Version lldbRpmFileVersion = new Version(lldbVersion);

                if (installedLldbVersion >= lldbRpmFileVersion)
                {
                    OutputDeviceErrorMsg("GetPkgInstalledStatus (already installed): " + lldbPkgName);
                    isInstalled = true;
                }

                return true;
            }
            catch (Exception e)
            {
                OutputDeviceErrorMsg("GetPkgInstalledStatus failed: " + e.Message);
                return false;
            }
        }
    }

    class PkgInstalledStatusWaiter : TizenAutoWaiter
    {
        public string InstalledStatus
        {
            set;
            get;
        }

        public PkgInstalledStatusWaiter()
        {
            this.InstalledStatus = null;
        }

        public override bool IsWaiterSet(string value)
        {
            if (!String.IsNullOrEmpty(value))
            {
                InstalledStatus = value;
                return true;
            }

            return false;
        }

        public override void OnExit()
        {
        }
    }
    
    class DebuggerInstallWaiter : TizenAutoWaiter
    {
        private string lastErrorMessage;
        private IOndemandInstallerEvents eventsHandler;
        bool pushResult;

        public string LastErrorMessage
        {
            get
            {
                return lastErrorMessage;
            }

            set
            {
                lastErrorMessage = value;
            }
        }

        public bool PushResult
        {
            get { return pushResult; }
        }

        public DebuggerInstallWaiter(IOndemandInstallerEvents eventsHandler)
            : base()
        {
            this.eventsHandler = eventsHandler;
        }

        public override bool IsWaiterSet(string value)
        {
            if (!String.IsNullOrEmpty(value))
            {
                if (this.eventsHandler != null)
                {
                    this.eventsHandler.OnMessage("Debugger Installing... ", value);
                }

                if (value.Contains("on-demand"))
                {
                    this.pushResult = true;
                    return true;
                }
            }

            return false;
        }

        public override void OnExit()
        {
        }
    }

    public interface IOndemandInstallerEvents
    {
        void OnMessage(string module, string message);
    }
}