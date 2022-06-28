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
using System.IO;
using Microsoft.VisualStudio.Shell.Interop;
using Tizen.VisualStudio.Tools.DebugBridge;
using Tizen.VisualStudio.Tools.DebugBridge.SDBCommand;
using Tizen.VisualStudio.Tools.Utilities;

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

        private bool TargetHasTizenDotNET(SDBCapability cap, out string lastErrorMessage)
        {
            bool isDotnetSupported = false;
            try
            {
                isDotnetSupported = DeployHelper.IsTizenVersionSupported(cap.GetValueByKey("platform_version"));
            }
            catch
            {
            }
            lastErrorMessage = isDotnetSupported ? string.Empty
                : "Failed to identify the .NET support on current platform version. Tizen .NET is supported on Tizen 4.0 or higher.";
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
                // it's ok to fail Delete but if throws exception
                // it won't create the file
                using (File.Create(fileInstall))
                {
                }
            }
            catch (Exception)
            {
            }
        }

        private bool NeedToInstall(SDBDeviceInfo device, string pathTpkFile, bool forceInstall)
        {
            string fileInstall = Path.GetFileNameWithoutExtension(pathTpkFile) + ".tpi";
            string pathInstall = Path.Combine(Path.GetDirectoryName(pathTpkFile), fileInstall);

            //check whether the package is installed on target/emulator or not.
            var sdbLauncher = SDBLauncher.Create(outputPane);
            VsProjectHelper projectHelper = VsProjectHelper.GetInstance;

            if (!sdbLauncher.ConnectBridge())
            {
                return false;
            }

            string curPkgId = projectHelper.GetPackageId(pathTpkFile);//.GetAppId(curProject);

            if (forceInstall || !sdbLauncher.IsPackageDetected(device, curPkgId))
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

        private InstallResult DoInstallTizenPackage(SDBDeviceInfo device, SDBCapability cap, string tpkPath, out string lastErrorMessage, Boolean isDebug)
        {
            var waiter = new InstallWaiter(this);
/*!!
            DeployHelper.InstallTpk(device, tpkPath,
                (bool isStdOut, string line) => waiter.IsWaiterSet(line),
                out lastErrorMessage, TimeSpan.FromMinutes(5));
*/
            InstallResult result;
            int exitCode = 0;

            Version version = Version.Parse(cap.GetValueByKey("platform_version"));

            string cmd;
            if (version >= new Version("5.5"))
            {
                if (isDebug)
                {
                    cmd = "install skip \"";
                }
                else
                {
                    cmd = "install noskip \"";
                }
            }
            else
                cmd = "install \"";

            switch (SDBLib.RunSdbCommand(device, cmd + tpkPath + "\"",
                (bool isStdOut, string line) => waiter.IsWaiterSet(line),
                out exitCode,
                TimeSpan.FromMinutes(5))) // 5 minutes will be enough?
            {
                case SDBLib.SdbRunResult.Success:
                    lastErrorMessage = waiter.LastErrorMessage;
                    result = (exitCode == 0 ? InstallResult.OK : GetInstallResultFromMsg(lastErrorMessage));
                    break;

                case SDBLib.SdbRunResult.CreateProcessError:
                    lastErrorMessage = "Failed to get sdb.exe program";
                    result = InstallResult.INSTALL_NOT_TRIED;
                    break;

                case SDBLib.SdbRunResult.Timeout:
                    lastErrorMessage = "Installation timeout";
                    result = InstallResult.TIMEOUT;
                    break;

                default:
                    lastErrorMessage = "Installation error";
                    lastErrorMessage = (string.IsNullOrEmpty(waiter.LastErrorMessage) ? waiter.LastErrorMessage : "Installation error");
                    result = (exitCode == 0 ? InstallResult.OK : InstallResult.GENERAL_ERROR);
                    break;
            }

            return result;
        }

        public InstallResult InstallTizenPackage(SDBDeviceInfo device, SDBCapability cap, string tpkPath,
                                        IVsOutputWindowPane outputPane,
                                        IVsThreadedWaitDialogFactory dlgFactory,
                                        bool forceInstall,
                                        out string lastErrorMessage,
                                        Boolean isDebugMode = false)
        {
            this.outputPane = outputPane;
            this.outputPane?.Activate();

            if (!TargetHasTizenDotNET(cap, out lastErrorMessage))
            {
                return InstallResult.INSTALL_NOT_TRIED;
            }

            if (!NeedToInstall(device, tpkPath, forceInstall))
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

            int userCancel;

            InstallResult result = DoInstallTizenPackage(device, cap, tpkPath, out lastErrorMessage, isDebugMode);

            this.waitDialog?.EndWaitDialog(out userCancel);

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

    class InstallWaiter
    {
        private ILauncherEvents eventsHandler;
        private int currentPercent;

        public bool InstallResult { get; private set; }

        public string LastErrorMessage { get; private set; }

        public InstallWaiter(ILauncherEvents eventsHandler)
        {
            this.eventsHandler = eventsHandler;
            LastErrorMessage = string.Empty;
            currentPercent = 0;
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

        private static bool HasError(string value, out string lastErrorMessage)
        {
            const string FailedErrorPrefix = "error: failed ";
            const string ProcessingResultPrefix = "processing result : ";
            bool error = value.StartsWith(FailedErrorPrefix, StringComparison.OrdinalIgnoreCase);
            if (error)
            {
                lastErrorMessage = value;
            }
            else if (value.StartsWith(ProcessingResultPrefix, StringComparison.OrdinalIgnoreCase))
            {
                lastErrorMessage = value.Remove(0, ProcessingResultPrefix.Length);
                error = true;
            }
            else
            {
                lastErrorMessage = value;
            }
            return error;
        }

        public bool IsWaiterSet(string value)
        {
            if (!String.IsNullOrEmpty(value))
            {
                if (!value.StartsWith("__return_cb")) // don't show useless progress percent lines
                {
                    VsPackage.outputPaneTizen?.Activate();
                    VsPackage.outputPaneTizen?.OutputStringThreadSafe(string.Format("\t{0}\n", value));
                }

                string lastErrorMessage = "";
                if (value.StartsWith("spend time for pkgcmd is", StringComparison.OrdinalIgnoreCase) ||
                    !(InstallResult = !HasError(value, out lastErrorMessage)))
                {
                    LastErrorMessage = lastErrorMessage;
                    // end this waiting
                    return true;
                }

                if (this.eventsHandler != null)
                {
                    int percent = ParseInstallPercent(value);
                    this.currentPercent = Math.Max(percent, this.currentPercent);
                    this.eventsHandler.OnMessage("Installing...", value);
                    this.eventsHandler.OnUpdateProgress(this.currentPercent);
                }
            }

            return false;
        }
    }

    public interface ILauncherEvents
    {
        void OnMessage(string module, string message);
        void OnUpdateProgress(int percent);
    }
}
