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
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Tizen.VisualStudio.Tools.DebugBridge;
using Tizen.VisualStudio.Tools.Utilities;
using Tizen.VisualStudio.Tools.DebugBridge.SDBCommand;
using EnvDTE;
using System.Text.RegularExpressions;

namespace Tizen.VisualStudio.Debug
{
    public enum InstallResult
    {
        UNDEFINED_BY_VS_ERROR = 0,
        OK = 1,
        INSTALL_NOT_TRIED = 1 << 1,
        CHECK_CERTIFICATE_ERROR = 1 << 2,
        MANIFEST_NOT_FOUND = 1 << 3,
        REGISTER_APPLICATION_ERROR = 1 << 4,
        SIGNATURE_ERROR = 1 << 5,
        GENERAL_ERROR = 1 << 30,
        TIMEOUT = 1 << 31
    }

    public class Launcher : ILauncherEvents
    {
        private static readonly Guid MIEngineId =
            new Guid("{3352D8EC-AE86-41F2-BB8A-90DA85ABCA05}");
        // new Guid("{ea6637c6-17df-45b5-a183-0951c54243bc}");
        // original miengine

        private IVsOutputWindowPane outputPane = null;
        private IVsThreadedWaitDialog2 waitDialog = null;

        public static Launcher Create()
        {
            return new Debug.Launcher();
        }

        private Launcher()
        {

        }

        private bool TargetHasTizenDotNET(out string lastErrorMessage)
        {
            bool isDotnetSupported = false;

            try
            {
                Version platformVersion = new SDBCapability().GetVersionByKey("platform_version");
                isDotnetSupported = platformVersion >= new Version("4.0");
            }
            catch
            {
            }

            lastErrorMessage = isDotnetSupported ? string.Empty : "Failed to identify the .NET support on current platform version. Tizen .NET is supported on Tizen 4.0 or higher.";

            return isDotnetSupported;
        }

        private void ClearInstallCheckFile(string fileInstall)
        {
            // this should be called also when debug target changes
            // or VS won't install for the new target
            try
            {
                File.Delete(fileInstall);
            }
            catch (Exception)
            {
            }
        }

        private void TouchInstallCheckFile(string fileInstall)
        {
            ClearInstallCheckFile(fileInstall);

            try
            {
                // don't put this in same try block of Delete
                // it's ok to fail Delete but if throws excpetion
                // it won't create the file
                using (File.Create(fileInstall))
                {

                }
            }
            catch (Exception)
            {
            }
        }

        private bool NeedToInstall(string pathTpkFile, bool forceInstall)
        {
            string pathInstall;
            string fileInstall;

            pathInstall = Path.GetDirectoryName(pathTpkFile);
            fileInstall = Path.GetFileNameWithoutExtension(pathTpkFile) + ".tpi";
            pathInstall = Path.Combine(pathInstall, fileInstall);

            //check whether the package is installed on target/emaultor or not.
            SDBLauncher sdbLauncher = SDBLauncher.Create(outputPane);
            VsProjectHelper projectHelper = VsProjectHelper.GetInstance;

            if (!sdbLauncher.ConnectBridge())
            {
                return false;
            }

            string curPkgId = projectHelper.GetPackageId(pathTpkFile);//.GetAppId(curProject);

            if (forceInstall || !sdbLauncher.IsPackageDetected(curPkgId))
            {
                ClearInstallCheckFile(pathInstall);
            }

            if (!File.Exists(pathInstall))
            {
                TouchInstallCheckFile(pathInstall);
                return true;
            }

            DateTime tpkTime = File.GetLastWriteTime(pathTpkFile);
            DateTime insTime = File.GetLastWriteTime(pathInstall);
            if (tpkTime <= insTime)
            {
                return false;
            }

            TouchInstallCheckFile(pathInstall);
            return true;
        }

        private bool DoInstallSetRootOff(out string lastErrorMessage)
        {
            bool result = true;
            System.Diagnostics.Process process = SDBLib.CreateSdbProcess(true, true);
            if (process == null)
            {
                lastErrorMessage = "Failed to get sdb.exe program";
                return false;
            }

            string arguments = DeviceManager.AdjustSdbArgument("root off");

            process.StartInfo.Arguments = arguments;
            lastErrorMessage = String.Empty;

            try
            {
                if (!process.Start())
                {
                    lastErrorMessage = "Failed to set SDB root off. App can not be installed by root.";
                    return false;
                }

                process.WaitForExit(5 * 1000);
            }
            catch (Exception e)
            {
                lastErrorMessage = e.Message;
                result = false;
            }

            return result;
        }

        private InstallResult GetInstallResultFromMsg(string lastErrorMessage)
        {
            try
            {
                string errorName = lastErrorMessage.Remove(lastErrorMessage.IndexOf('[') - 1).Replace(" ", "_").ToUpper();
                Enum.TryParse(errorName, true, out InstallResult installResult);

                return installResult;
            }
            catch
            {
            }

            return InstallResult.UNDEFINED_BY_VS_ERROR;
        }

        private InstallResult DoInstallTizenPackage(string tpkPath,
                                           out string lastErrorMessage)
        {
            InstallWaiter waiter = new InstallWaiter(this as ILauncherEvents);
            System.Diagnostics.Process process = SDBLib.CreateSdbProcess(true, true);
            if (process == null)
            {
                lastErrorMessage = "Failed to get sdb.exe program";
                return InstallResult.INSTALL_NOT_TRIED;
            }

            string argument = DeviceManager.AdjustSdbArgument("install \"" + tpkPath + "\"");
            var sdbTaskRet = SDBLib.RunSdbProcessAsync(process, argument, true, waiter);

            if (waiter.Waiter.WaitOne(300 * 1000)) // 5 minutes will be enough?
            {
                lastErrorMessage = waiter.LastErrorMessage;

                return string.IsNullOrEmpty(lastErrorMessage) ? InstallResult.OK : GetInstallResultFromMsg(lastErrorMessage);
            }

            lastErrorMessage = "Installation Timeout.";
            return InstallResult.TIMEOUT;
        }

        public InstallResult InstallTizenPackage(string tpkPath,
                                        IVsOutputWindowPane outputPane,
                                        IVsThreadedWaitDialogFactory dlgFactory,
                                        bool forceInstall,
                                        out string lastErrorMessage)
        {
            this.outputPane = outputPane;
            this.outputPane?.Activate();

            if (!TargetHasTizenDotNET(out lastErrorMessage) || !DoInstallSetRootOff(out lastErrorMessage))
            {
                return InstallResult.INSTALL_NOT_TRIED;
            }

            if (!NeedToInstall(tpkPath, forceInstall))
            {
                lastErrorMessage = string.Format("  Skip install ({0})", tpkPath);
                return InstallResult.OK;
            }

            dlgFactory?.CreateInstance(out this.waitDialog);//waitDialog can be null and dialog can not be displayed by VS without some reasons.
            this.waitDialog?.StartWaitDialog(
                    "Install Tizen Application",
                    "Please wait while the new application is being installed...",
                    "Preparing...",
                    null,
                    "Tizen application install in progress...",
                    0, false, true);

            int usercancel;

            SDBAppCmd appUninstallCmd = new SDBAppCmd(SDBProtocol.uninstall, VsProjectHelper.GetInstance.GetPackageId(tpkPath));
            InstallResult result = DoInstallTizenPackage(tpkPath, out lastErrorMessage);

            this.waitDialog?.EndWaitDialog(out usercancel);

            return result;
        }

        void ILauncherEvents.OnMessage(string module, string message)
        {
            if (this.outputPane != null)
            {
                string outmessage;
                outmessage = string.Format("\tDebug Launcher ({0}) : {1}\n", module, message);
                this.outputPane.OutputStringThreadSafe(outmessage);
            }
        }

        void ILauncherEvents.OnUpdateProgress(int percent)
        {
            if (this.waitDialog != null)
            {
                bool pfCanceled = false;
                string progress = String.Format("Installing {0}%...", percent);
                this.waitDialog.UpdateProgress(null, progress, null,
                                               percent, 100,
                                               false, out pfCanceled);
            }
        }
    }

    class InstallWaiter : TizenAutoWaiter
    {
        private ILauncherEvents eventsHandler;
        private bool installResult;
        private string lastErrorMessage;
        private int currentPercent;

        public bool InstallResult
        {
            get { return installResult; }
        }

        public string LastErrorMessage
        {
            get { return lastErrorMessage; }
        }

        public InstallWaiter(ILauncherEvents eventsHandler)
            : base()
        {
            this.eventsHandler = eventsHandler;
            this.installResult = false;
            this.lastErrorMessage = string.Empty;
            this.currentPercent = 0;
        }

        private int ParseInstallPercent(string value)
        {
            // this is a simple way to parse in C style
            // would be better to handle in C# way
            int percent = 0;
            char[] delimiterChars = { ' ' };
            string[] words = value.Split(delimiterChars);

            if (words.Length >= 6 && words[4] == "key[install_percent]")
            {
                string valuePercent = words[5];
                char[] delimiterChars2 = { '[', ']' };
                string[] words2 = valuePercent.Split(delimiterChars2);

                if (words2.Length >= 2 && words2[0] == "val")
                {
                    percent = Int32.Parse(words2[1]);
                }
            }

            return percent;
        }

        private bool HasError(string value, out string lastErrorMessage)
        {
            const string errorPrefix = "processing result : ";
            const string closeErrorString = "error: failed to close";

            bool hasGeneralError = value.StartsWith(errorPrefix, StringComparison.OrdinalIgnoreCase);
            bool hasCloseError = value.StartsWith(closeErrorString, StringComparison.OrdinalIgnoreCase);

            lastErrorMessage = hasCloseError ? value :
                (hasGeneralError ? value.Remove(0, errorPrefix.Length) : string.Empty);

            return hasCloseError || hasGeneralError;
        }

        public override bool IsWaiterSet(string value)
        {
            if (!String.IsNullOrEmpty(value))
            {
                VsPackage.outputPaneTizen?.Activate();
                VsPackage.outputPaneTizen?.OutputStringThreadSafe(string.Format("\t{0}\n", value));

                if (this.eventsHandler != null)
                {
                    int percent = ParseInstallPercent(value);
                    this.currentPercent = Math.Max(percent, this.currentPercent);
                    this.eventsHandler.OnMessage(Modules.Installing, value);
                    this.eventsHandler.OnUpdateProgress(this.currentPercent);
                }
                
                if (value.StartsWith("spend time for pkgcmd is", StringComparison.OrdinalIgnoreCase) ||
                    !(installResult = !HasError(value, out lastErrorMessage)))
                {
                    // end this waiting
                    return true;
                }
            }

            return false;
        }

        public override void OnExit()
        {
        }
    }

    public interface ILauncherEvents
    {
        void OnMessage(string module, string message);
        void OnUpdateProgress(int percent);
    }

    public class Modules
    {
        public const string Installing = "Installing...";

        private static Modules instance;

        protected Modules()
        {
        }

        public static Modules Instance()
        {
            instance = new Modules();
            return instance;
        }
    }
}