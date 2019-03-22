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
using Tizen.VisualStudio.Tools.Data;
using Tizen.VisualStudio.Tools.DebugBridge;
using Tizen.VisualStudio.Tools.DebugBridge.SDBCommand;
using Tizen.VisualStudio.Tools.Utilities;
using Tizen.VisualStudio.Utilities;

namespace Tizen.VisualStudio.Debug
{
    // not used, leaved for reference so far
    [Obsolete("Use Tizen.VisualStudio.Tools.Utilities.OnDemandInstaller instead")]
    public class OnDemandDebuggerInstaller
    {
        public const string LLDBPACKAGE = "lldb";
        protected string pkgName { get; }
        protected Version sdkPkgVersion { get; }
        protected const string pkgExtension = ".tar.gz";

        protected IVsOutputWindowPane outputPane = null;

        private string errorMessage;
        public string ErrorMessage
        {
            get
            {
                return errorMessage;
            }
            protected set
            {
                if (!String.IsNullOrEmpty(value))
                {
                    errorMessage = value;
                    OutputDeviceErrorMsg(value);
                }
            }
        }

        protected SDBCapability sdbCapability;

        public OnDemandDebuggerInstaller(string name, string version)
        {
            pkgName = name;
            sdkPkgVersion = Version.Parse(version);
            sdbCapability = new SDBCapability();
        }

        private string GetVersionFileMask()
        {
            string toolPath = sdbCapability.GetValueByKey("sdk_toolpath");
            return (pkgName == LLDBPACKAGE)
                ? $"{toolPath}/lldb/bin/lldb-server-*"
                : $"{toolPath}/{pkgName}/version-*";
        }

        protected virtual string GetPackageDestinationPath()
        {
            return sdbCapability.GetValueByKey("sdk_toolpath") + "/on-demand/" + GetPackageFileName();
        }

        protected string GetTargetArch()
        {
            return sdbCapability.GetValueByKey("cpu_arch");
        }

        private bool InstallPackage(string tarPath, out string errorMessage)
        {
            string sdkToolPath = sdbCapability.GetValueByKey("sdk_toolpath");
            string errorString;
            SDBLib.RunSdbShellCommand(null, $"rm -rf {sdkToolPath}/{pkgName}", null, out errorString); // remove old files (if any)
            return DeployHelper.ExtractTarGzip(null, tarPath, sdkToolPath, out errorMessage);
        }

        protected virtual bool PushPackage(string source, string destination, out string errorMessage)
        {
            return DeployHelper.PushFile(null, source, destination,
                (isStdOut, line) => { OnMessage("SDK Tool Installing... ", line); return false; },
                out errorMessage);
        }

        protected virtual bool DeployPackage(string debugger)
        {
            string errorMessage;
            bool result = InstallPackage(debugger, out errorMessage);
            if (!result)
            {
                ErrorMessage = $"Failed to deploy debugger. {errorMessage}";
            }
            return result;
        }

        protected void OutputDeviceErrorMsg(string msg)
        {
            DateTime localDate = DateTime.Now;
            string message = String.Format("{0} : {1}\n", localDate.ToString(), msg);
            this.outputPane.Activate();
            this.outputPane.OutputStringThreadSafe(message);
        }

        protected virtual bool IsPackageInstalled()
        {
            string errorMessage;
            Version installedVersion = DeployHelper.GetInstalledPackageVersion(null, GetVersionFileMask(), out errorMessage);
            if (installedVersion == null)
            {
                if (!(String.IsNullOrEmpty(errorMessage) || errorMessage.Contains("No such file")))
                {
                    ErrorMessage = errorMessage;
                    OutputDeviceErrorMsg($"IsPackageInstalled({pkgName}) failed. " + errorMessage);
                }
                return false;
            }

            System.Diagnostics.Debug.WriteLine($"Installed version of {pkgName}: {installedVersion}");

            bool result = false;
            if (installedVersion >= sdkPkgVersion)
            {
                result = true;
            }

            OutputDeviceErrorMsg($"IsPackageInstalled({pkgName}): {result}. Installed version: {installedVersion}. SDK version: {sdkPkgVersion}");

            return result;
        }

        protected string GetPackageFileName()
        {
            return $"{pkgName}-{sdkPkgVersion}-{DeployHelper.GetPackageSuffix(GetTargetArch())}{pkgExtension}";
        }

        public virtual bool InstallPackage(string tizenVersion, IVsOutputWindowPane outputPane,
            IVsThreadedWaitDialogFactory dlgFactory/*uint debugTargetTypeId,*/)
        {
            this.outputPane = outputPane;
            if (this.outputPane != null)
            {
                this.outputPane.Activate();
            }

            // Check the lldb package was installed in previous or not.
            // Check lldb.tar.gz extractor.
            //GetPkgInstalledStatus(out isInstalled);

            try
            {
                // Changed the logic from commanding rpm -qa to ls command to checkout the version.
                if (!IsPackageInstalled())
                {
                    //source = ondemandPath + @"\" + GetLldbPkgName();
                    // chagned by Sebeom to suport Tizen 4.0 debugger.

                    string onDemandPath = ToolsPathInfo.GetOnDemandFolderPath(tizenVersion);
                    string source = Path.Combine(onDemandPath, GetPackageFileName());
                    string destination = GetPackageDestinationPath();
                    string errorMessage;
                    if (!PushPackage(source, destination, out errorMessage))
                    {
                        ErrorMessage = errorMessage;
                        string msg =
                            "Cannot push package\n" +
                            $"\"{source}\"\n" +
                            "to\n" +
                            $"\"{destination}\".";
                        if (!String.IsNullOrEmpty(errorMessage))
                        {
                            if (errorMessage.Contains(source) && errorMessage.Contains(destination))
                            {
                                msg = errorMessage;
                            }
                            else
                            {
                                msg += "\n\n";
                                msg += errorMessage;
                            }
                        }
                        VsPackage.ShowMessage(MessageDialogType.Error, null, msg);
                        return false;
                    }

                    // Install Debug package
                    if (!DeployPackage(destination))
                    {
                        ErrorMessage = $"Cannot deploy package \"{destination}\"";
                        VsPackage.ShowMessage(MessageDialogType.Error, null, ErrorMessage);
                        return false;
                    }
                }
            }
            finally
            {
                //
            }

            return true;
        }

        private void OnMessage(string module, string message)
        {
            if (this.outputPane != null)
            {
                string outmessage = String.Format("  Ondemand Installer ({0}): {1}\n", module, message);
                this.outputPane.OutputStringThreadSafe(outmessage);
            }
        }
    }

    class OnDemandDebuggerInstallerSecure : OnDemandDebuggerInstaller
    {
        public const string LLDBTVPACKAGE = "lldb-tv";

        public OnDemandDebuggerInstallerSecure(string name, string version) : base(name, version) { }

        protected override string GetPackageDestinationPath()
        {
            string toolPath = sdbCapability.GetValueByKey("sdk_toolpath");
            string pkgFolder = (pkgName == LLDBTVPACKAGE) ? "lldb" : pkgName;
            return $"{toolPath}/on-demand/{pkgFolder}{pkgExtension}";
        }

        protected override bool PushPackage(string source, string destination, out string errorMessage)
        {
            string signatureSource, signatureDestination;
            if (pkgName == LLDBTVPACKAGE)
            {
                // lldb-tv-3.8.1-armv7l.tar.gz -> lldb.tar.gz
                // signature-3.8.1-armv7l -> signature
                const string SignatureName = "signature";
                int i = source.LastIndexOf('\\');
                signatureSource = source.Substring(0, i + 1) + source.Substring(i + 1).Replace(pkgName, SignatureName).Replace(pkgExtension, string.Empty);
                i = destination.LastIndexOf('/');
                signatureDestination = destination.Substring(0, i + 1) + SignatureName;
            }
            else
            {
                // mypackage-1.0.0-armv7l.tar.gz -> mypackage.tar.gz
                // mypackage-1.0.0-armv7l.tar.gz.signature -> mypackage.tar.gz.signature
                const string SignatureExt = ".signature";
                signatureSource = source + SignatureExt;
                signatureDestination = destination + SignatureExt;
            }
            return
                base.PushPackage(signatureSource, signatureDestination, out errorMessage) &&
                base.PushPackage(source, destination, out errorMessage);
        }

        protected override bool DeployPackage(string tarPath)
        {
            bool result;
            string outputLine, errorMessage;
            if (pkgName == LLDBTVPACKAGE)
            {
                result = DeployHelperSecure.RunCommand(null, "shell 0 vs_lldbinstall", out outputLine, out errorMessage);
            }
            else
            {
                result = DeployHelperSecure.InstallPackage(null, pkgName, out errorMessage);
            }
            if (!result)
            {
                ErrorMessage = StringHelper.CombineMessages($"Failed to deploy {pkgName}", errorMessage);
            }
            return result;
        }

        protected override bool IsPackageInstalled()
        {
            Version installedVersion;
            string errorMessage;
            if (pkgName == LLDBTVPACKAGE)
            {
                installedVersion = DeployHelperSecure.RunGetVersionCommand(null, "shell 0 vs_lldbversion", out errorMessage);
            }
            else
            {
                installedVersion = DeployHelperSecure.GetInstalledPackageVersion(null, pkgName, out errorMessage);
            }
            if (installedVersion == null)
            {
                if (errorMessage != "")
                {
                    ErrorMessage = $"IsPackageInstalled({pkgName}) failed. {errorMessage}";
                }
                return false;
            }
            return (installedVersion >= sdkPkgVersion);
        }
    }
}
