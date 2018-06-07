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
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Debug;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using System.IO;
using Tizen.VisualStudio.Tools.DebugBridge.SDBCommand;
using Tizen.VisualStudio.Tools.DebugBridge;
using Tizen.VisualStudio.Debug;
using System.Threading.Tasks.Dataflow;
using System.Windows.Threading;
using Tizen.VisualStudio.ProjectSystem;
using System.Linq;
using System.Collections.ObjectModel;
using Tizen.VisualStudio.ProjectSystem.Debug;
using Tizen.VisualStudio.Tools.ExternalTool;
using Tizen.VisualStudio.DebugBridge;
using System.Windows.Forms;
using System.Diagnostics;
using Tizen.VisualStudio.ProjectSystem.VS.Extensibility;

namespace Tizen.VisualStudio.ProjectSystem.VS.Debug
{
    [ExportDebugger(ProjectDebugger.SchemaName)]
    [AppliesTo(MyUnconfiguredProject.UniqueCapability)]
    [Order(100)]
    public class ProjectDebugLaunchProvider : DebugLaunchProviderBase
    {
        private static readonly Guid MIEngineId = new Guid("{3352D8EC-AE86-41F2-BB8A-90DA85ABCA05}");
        private string lastErrorMessage;

        private Task DoNothing = Task.Run(() => { });

        private TizenDebugLaunchOptions tDebugLaunchOptions;

        private ITizenLaunchSettingsProvider _tizenLaunchSettingsProvider { get; }

        [ImportingConstructor]
        public ProjectDebugLaunchProvider(ConfiguredProject configuredProject)
            : base(configuredProject)
        {
            _tizenLaunchSettingsProvider = configuredProject.Services.ExportProvider.GetExportedValue<ITizenLaunchSettingsProvider>();
        }

        // TODO: Specify the assembly full name here
        [AppliesTo(MyUnconfiguredProject.UniqueCapability)]
        private object DebuggerXaml
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets project properties that the debugger needs to launch.
        /// </summary>
        [Import]
        private ProjectProperties DebuggerProperties { get; set; }

        public override async Task<bool> CanLaunchAsync(DebugLaunchOptions launchOptions)
        {
            string commandValue = await DebugLaunchDataStore.SDBTaskAsync();
            //TODO : Check the presence of SDB and pop Install Wizard in case of its absence.
            return File.Exists(commandValue) || (DeviceManager.DeviceInfoList.Count == 0);
        }

        public override Task LaunchAsync(DebugLaunchOptions launchOptions)
        {
            bool isTargetListEmpty = (DeviceManager.DeviceInfoList.Count == 0);

            if (isTargetListEmpty)
            {
                new EmulatorManagerLauncher().Launch();
                return DoNothing;
            }

            bool isSecureProtocol = new SDBCapability().GetAvailabilityByKey("secure_protocol");
            bool isDebugMode = !launchOptions.Equals(DebugLaunchOptions.NoDebug);
            Project debuggeeProj = VsHierarchy.GetDTEProject();

            tDebugLaunchOptions = isSecureProtocol ?
                new SecuredTizenDebugLaunchOptions(isDebugMode, debuggeeProj, _tizenLaunchSettingsProvider.TizenLaunchSetting.ExtraArguments) :
                new TizenDebugLaunchOptions(isDebugMode, debuggeeProj, _tizenLaunchSettingsProvider.TizenLaunchSetting.ExtraArguments);

            OnDemandInstaller lldbInstaller = isSecureProtocol ? new TarGzOnDemandInstaller() : new OnDemandInstaller();

            bool isDebugNeeded =
                InstallTizenPackage(tDebugLaunchOptions) &&
                (isDebugMode ? lldbInstaller.InstallDebugPackage(VsPackage.outputPaneTizen, VsPackage.dialogFactory) : true) &&
                LaunchApplication(tDebugLaunchOptions) &&
                isDebugMode;

            return isDebugNeeded ? base.LaunchAsync(launchOptions) : DoNothing;
        }

        public override async Task<IReadOnlyList<IDebugLaunchSettings>> QueryDebugTargetsAsync(DebugLaunchOptions launchOptions)
        {
            // The properties that are available via DebuggerProperties are determined by the property XAML files in your project.
            var settings = new DebugLaunchSettings(launchOptions);
            var debuggerProperties = await this.DebuggerProperties.GetProjectDebuggerPropertiesAsync();

            settings.CurrentDirectory = await debuggerProperties.Debugger1WorkingDirectory.GetEvaluatedValueAtEndAsync();
            settings.LaunchOperation = DebugLaunchOperation.CreateProcess;
            settings.Executable = await DebugLaunchDataStore.SDBTaskAsync();
            settings.Arguments = tDebugLaunchOptions.LldbArguments;
            settings.LaunchDebugEngineGuid = MIEngineId;
            settings.Options = tDebugLaunchOptions.DebugEngineOptions;

            TraceTizenPackage();

            return new IDebugLaunchSettings[] { settings };
        }

        private void TraceTizenPackage()
        {
            TizenPackageTracer pkgTracer = TizenPackageTracer.Instance;

            if (pkgTracer.IsAppIdOnWaiting(tDebugLaunchOptions.AppId))
            {
                string msg = string.Format("'{0}' is dependent on '{1}'.", tDebugLaunchOptions.AppId, Path.GetFileName(tDebugLaunchOptions.TpkPath));
                OutputDebugLaunchMessage(msg);
            }
            else
            {
                foreach (var appId in VsProjectHelper.GetInstance.GetAppIds(tDebugLaunchOptions.TpkPath))
                {
                    pkgTracer.AddDebugeeApp(appId, tDebugLaunchOptions.TpkPath);
                }
            }
        }

        private DialogResult ShowInstallError(InstallResult installResult)
        {
            string msg = string.Format(
                "Failed to install {0}.\n\n{1}",
                tDebugLaunchOptions.AppId,
                ResourcesInstallMessage.ResourceManager.GetString(installResult.ToString()));

            return MessageBox.Show(msg, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private bool InstallTizenPackage(TizenDebugLaunchOptions tDebugLaunchOptions)
        {
            SDBLauncher.Create(VsPackage.outputPaneTizen).TerminateApplication(tDebugLaunchOptions.AppId);

            InstallResult installResult = Launcher.Create().InstallTizenPackage(
                tDebugLaunchOptions.TpkPath,
                null,
                VsPackage.dialogFactory,
                false,
                out lastErrorMessage);

            bool isInstallSucceeded = (installResult == InstallResult.OK);

            if (!isInstallSucceeded)
            {
                OutputDebugLaunchMessage(lastErrorMessage);
                ShowInstallError(installResult);
                TizenPackageTracer.CleanTpiFiles();
            }

            return isInstallSucceeded;
        }

        private bool LaunchApplication(TizenDebugLaunchOptions tDebugLaunchOptions)
        {
            return tDebugLaunchOptions.IsDebugMode ?
                LaunchDebugModeApplication(tDebugLaunchOptions) :
                SDBLauncher.Create(VsPackage.outputPaneTizen).LaunchApplication(tDebugLaunchOptions.AppId);
        }

        private bool LaunchDebugModeApplication(TizenDebugLaunchOptions tDebugLaunchOptions)
        {
            #region W/A launch due to not-implemented parameter(for debug) of runapp protocol
            switch (tDebugLaunchOptions.AppType)
            {
                case "watch-application":
                case "widget-application":
                    SDBLauncher.Create(VsPackage.outputPaneTizen).LaunchApplication(DebugLaunchDataStore.WidgetViewerSdkAppId);
                    break;
            }

            var proc = SDBLib.CreateSdbProcess(true, true);
            proc.StartInfo.Arguments = tDebugLaunchOptions.LaunchAppArguments;
            proc.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                OutputDebugLaunchMessage(e.Data);
            });
            OutputDebugLaunchMessage("Launching " + tDebugLaunchOptions.AppId);
            proc.Start();
            proc.BeginOutputReadLine();

            return true;
            #endregion
        }

        private void OutputDebugLaunchMessage(string rawMsg)
        {
            if (string.IsNullOrEmpty(rawMsg))
            {
                return;
            }

            //Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            VsPackage.outputPaneTizen?.Activate();

            DateTime localDate = DateTime.Now;

            string message = String.Format("{0} : {1}\n",
                localDate.ToString(),
                rawMsg);

            VsPackage.outputPaneTizen?.OutputStringThreadSafe(message);
        }

        private class DebugLaunchDataStore
        {
            public const string DotNetLauncher = "/usr/bin/dotnet-launcher";
            public const string AppInstallPath = "/home/owner/apps_rw";
            public const string TpkRoot = "tpkroot";
            public const string WidgetViewerSdkAppId = "org.tizen.widget_viewer_sdk";

            public static string LldbMi => (new SDBCapability().GetValueByKey("sdk_toolpath")) + @"/lldb/bin/lldb-mi";
            public static Task<string> SDBTaskAsync() => Task.Run(() =>
            {
                return SDBLib.GetSdbFilePath();
            });
        }

        private class TizenDebugLaunchOptions
        {
            public string LaunchAppArguments { get; }
            public string LldbArguments { get; }
            public string DebugEngineOptions { get; }
            public string AppId { get; }
            public string AppType { get; }
            public string TpkPath { get; }
            public bool IsDebugMode { get; }

            protected const string niDisableOption = " COMPlus_ZapDisable 1 ";
            protected VsProjectHelper projHelper = VsProjectHelper.GetInstance;

            private TizenPackageTracer pkgTracer = TizenPackageTracer.Instance;

            public TizenDebugLaunchOptions(bool isDebugMode, Project proj, string extraArgs)
            {
                IsDebugMode = isDebugMode;

                AppId = projHelper.GetAppId(proj);//projHelper.GetManifestApplicationId(proj);
                AppType = projHelper.GetAppType(proj);
                TpkPath = GetDebugeeTpkPath(AppId, proj);//pkgTracer.GetTpkPathByAppId(AppId) ?? projHelper.GetTizenPackagePath(proj);
                LaunchAppArguments = GetLaunchArguments(AppId, proj, extraArgs);

                if (IsDebugMode)
                {
                    LldbArguments = GetLldbArguments();

                    DebugEngineOptions =
                        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<PipeLaunchOptions PipePath=\"" +
                        SDBLib.GetSdbFilePath() +
                        "\" PipeArguments=\"" +
                        LldbArguments +
                        "\" PipeCwd=\"" +
                        projHelper.GetValidRootPath() +
                        "\" ExePath=\"" +
                        DebugLaunchDataStore.DotNetLauncher +
                        "\" MIMode=\"lldb\" TargetArchitecture=\"" +
                        GetTargetArch() +
                        "\" WorkingDirectory=\"" +
                        DebugLaunchDataStore.AppInstallPath + "/" + projHelper.GetPackageId(TpkPath) + "/bin" +
                        "\" AdditionalSOLibSearchPath=\"\" xmlns=\"http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014\" />";
                }
            }

            private string GetDebugeeTpkPath(string appId, Project proj)
            {
                if (pkgTracer.IsAppIdOnWaiting(appId))
                {
                    string waitingTpkPath = pkgTracer.GetTpkPathByAppId(appId);
                    string waitingTpkPkgId = projHelper.GetPackageId(waitingTpkPath);

                    if (SDBLauncher.Create(null).IsPackageDetected(waitingTpkPkgId))
                    {
                        return waitingTpkPath;
                    }
                }

                return projHelper.GetTizenPackagePath(proj);
            }

            protected virtual string GetLaunchArguments(string appId, Project proj, string extraArgs)
            {
                string hostCmd = string.Join(" ", new string[] { "-s", DeviceManager.SelectedDevice.Serial, "shell", "sh", "-c" });
                string targetShellCmd = "exit";
                string DebugLaunchPadArgs = IsDebugMode ? " __AUL_SDK__ LLDB-SERVER __DLP_DEBUG_ARG__ g,--platform=host,*:1234,-- CORECLR_GDBJIT " + GetDebuggeeDllList() + niDisableOption : string.Empty;

                switch (AppType)
                {
                    case "ui-application":
                    case "service-application":
                        targetShellCmd = string.Join(" ", new string[] { "launch_app", appId, extraArgs, DebugLaunchPadArgs });
                        break;
                    case "watch-application":
                    case "widget-application":
                        targetShellCmd = string.Join(" ", new string[] { "launch_app", DebugLaunchDataStore.WidgetViewerSdkAppId, "widget_id", appId, extraArgs, DebugLaunchPadArgs });
                        break;
                    case "ime-application":
                    default:
                        break;
                }

                return string.Format("{0} \"{1}\"", hostCmd, targetShellCmd);
            }

            protected virtual string GetLldbArguments() => " -s " + DeviceManager.SelectedDevice.Serial + " shell sh -c '" + DebugLaunchDataStore.LldbMi + "'";

            private string GetTargetArch()
            {
                SDBCapability cap = new SDBCapability();
                string arch = cap.GetValueByKey("cpu_arch");

                switch (arch)
                {
                    case "x86":
                        return "X86";
                    case "x86_64":
                        return "X64";
                    default:
                        return "arm";
                }
            }

            protected string GetDebuggeeDllList()
            {
                string tpkDirPath = Path.GetDirectoryName(TpkPath);

                string binPath =
                    Path.Combine(
                        tpkDirPath,
                        DebugLaunchDataStore.TpkRoot,
                        "bin");

                IEnumerable<string> pdbFilePathCollection = Directory.EnumerateFiles(binPath, "*.pdb", SearchOption.AllDirectories);

                IEnumerable<string> dllFilePathCollection =
                    from filePath in pdbFilePathCollection
                    let value = Path.GetFileNameWithoutExtension(filePath) + ".dll"
                    select value;

                return string.Join(",", dllFilePathCollection);
            }
        }

        private class SecuredTizenDebugLaunchOptions : TizenDebugLaunchOptions
        {
            public SecuredTizenDebugLaunchOptions(bool isDebugMode, Project proj, string extraArgs) : base(isDebugMode, proj, extraArgs)
            {
            }

            protected override string GetLaunchArguments(string appId, Project proj, string extraArgs)
            {
                string hostCmd = string.Join(" ", new string[] { "-s", DeviceManager.SelectedDevice.Serial, "shell" });
                string targetShellCmd = "exit";

                switch (AppType)
                {
                    case "ui-application":
                    case "service-application":
                        targetShellCmd = string.Join(" ", new string[] { "0 vs_debug", appId, extraArgs, GetDebuggeeDllList() });
                        break;
                    case "watch-application":
                    case "widget-application":
                        targetShellCmd = string.Join(" ", new string[] { "0 vs_debug", DebugLaunchDataStore.WidgetViewerSdkAppId, "widget_id", appId, extraArgs, GetDebuggeeDllList() });
                        break;
                    default:
                        break;
                }

                targetShellCmd += niDisableOption;

                return string.Format("{0} \"{1}\"", hostCmd, targetShellCmd);
            }

            protected override string GetLldbArguments() => " -s " + DeviceManager.SelectedDevice.Serial + " shell 0 vs_lldblaunch";
        }
    }
}
