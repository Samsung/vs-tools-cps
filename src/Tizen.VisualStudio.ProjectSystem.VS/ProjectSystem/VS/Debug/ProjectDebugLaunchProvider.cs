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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Debug;
using Microsoft.VisualStudio.ProjectSystem.VS.Debug;
using NetCore.Profiler.Extension.VSPackage;
using Tizen.VisualStudio.Debug;
using Tizen.VisualStudio.DebugBridge;
using Tizen.VisualStudio.ProjectSystem.Debug;
using Tizen.VisualStudio.ProjectSystem.VS.Extensibility;
using Tizen.VisualStudio.Tools.Data;
using Tizen.VisualStudio.Tools.DebugBridge;
using Tizen.VisualStudio.Tools.DebugBridge.SDBCommand;
using Tizen.VisualStudio.Tools.ExternalTool;
using Tizen.VisualStudio.Tools.Utilities;
using Tizen.VisualStudio.Utilities;

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

        public override async Task LaunchAsync(DebugLaunchOptions launchOptions)
        {
            if (ProfilerPlugin.Instance.ProfileLauncher.SessionActive ||
                ProfilerPlugin.Instance.HeaptrackLauncher.SessionActive)
            {
                ProfilerPlugin.Instance.ShowError("Cannot start debugging: a profiling session is active");
                return;
            }

            SDBDeviceInfo device = DeviceManager.SelectedDevice;
            if (device == null)
            {
                new EmulatorManagerLauncher().Launch();
                return;
            }

            var cap = new SDBCapability(device);
            string tizenVersion = cap.GetValueByKey("platform_version");
            if (!ProfilerPlugin.IsTizenVersionSupported(tizenVersion, true))
            {
                return;
            }

            bool isSecureProtocol = cap.GetAvailabilityByKey("secure_protocol");
            bool useNetCoreDbg = cap.GetAvailabilityByKey("netcoredbg_support");
            bool isDebugMode = !launchOptions.Equals(DebugLaunchOptions.NoDebug);
            bool useLiveProfiler = isDebugMode && useNetCoreDbg && DebuggerInfo.UseLiveProfiler;

            // check the root mode is off
            if (!ProfilerPlugin.EnsureRootOff(device,
                isDebugMode ?
                    (useLiveProfiler ? ProfilerPlugin.RunMode.LiveProfiler : ProfilerPlugin.RunMode.Debug)
                    : ProfilerPlugin.RunMode.NoDebug))
            {
                return;
            }

            Project debuggeeProj = VsHierarchy.GetDTEProject();

            tDebugLaunchOptions = isSecureProtocol ?
                (useNetCoreDbg ?
                    new SecuredTizenNetCoreDbgLaunchOptions(device, isDebugMode, debuggeeProj, _tizenLaunchSettingsProvider.TizenLaunchSetting.ExtraArguments) :
                    new SecuredTizenDebugLaunchOptions(device, isDebugMode, debuggeeProj, _tizenLaunchSettingsProvider.TizenLaunchSetting.ExtraArguments)) :
                (useNetCoreDbg ?
                    new TizenNetCoreDbgLaunchOptions(device, isDebugMode, debuggeeProj, _tizenLaunchSettingsProvider.TizenLaunchSetting.ExtraArguments) :
                    new TizenDebugLaunchOptions(device, isDebugMode, debuggeeProj, _tizenLaunchSettingsProvider.TizenLaunchSetting.ExtraArguments));

            string msg = $"Start {(isDebugMode ? "" : "without ")}debugging \"{tDebugLaunchOptions.AppId}\"";
            if (isSecureProtocol)
            {
                msg += " (secure protocol)";
            }
            OutputDebugLaunchMessage($"<<< {msg} >>>");

            bool isDebugNeeded = InstallTizenPackage(device, tDebugLaunchOptions);

            if (isDebugNeeded)
            {
                if (isDebugMode)
                {
/*
                    OnDemandDebuggerInstaller debuggerInstaller = isSecureProtocol ?
                        (useNetCoreDbg ?
                            new OnDemandDebuggerInstallerSecure("netcoredbg", "1.0.0") :
                            new OnDemandDebuggerInstallerSecure("lldb-tv", "3.8.1")) :
                        (useNetCoreDbg ?
                            new OnDemandDebuggerInstaller("netcoredbg", "1.0.0") :
                            new OnDemandDebuggerInstaller("lldb", "3.8.1"));

                    isDebugNeeded = debuggerInstaller.InstallPackage(tizenVersion, VsPackage.outputPaneTizen, VsPackage.dialogFactory);
*/
                    // TODO!! remove OnDemandDebuggerInstaller.cs after checking OnDemandInstaller

                    var installer = new OnDemandInstaller(device, supportRpms: false, supportTarGz: true,
                        onMessage: (s) => ProfilerPlugin.Instance.WriteToOutput(s));

                    isDebugNeeded = installer.Install(useNetCoreDbg ? "netcoredbg" :
                        (isSecureProtocol ? "lldb-tv" : "lldb"));

                    if (!isDebugNeeded)
                    {
                        ProfilerPlugin.Instance.ShowError(StringHelper.CombineMessages(
                            "Cannot check/install the debugger package.\n", installer.ErrorMessage));
                    }
                }
                if (isDebugNeeded)
                {
                    isDebugNeeded = LaunchApplication(device, tDebugLaunchOptions) && isDebugMode;
                    if (isDebugNeeded)
                    {
                        await base.LaunchAsync(launchOptions);
                    }
                }
            }
        }

        public override async Task<IReadOnlyList<IDebugLaunchSettings>> QueryDebugTargetsAsync(DebugLaunchOptions launchOptions)
        {
            // The properties that are available via DebuggerProperties are determined by the property XAML files in your project.
            var settings = new DebugLaunchSettings(launchOptions);
            var debuggerProperties = await this.DebuggerProperties.GetProjectDebuggerPropertiesAsync();

            settings.CurrentDirectory = await debuggerProperties.Debugger1WorkingDirectory.GetEvaluatedValueAtEndAsync();
            settings.LaunchOperation = DebugLaunchOperation.CreateProcess;
            settings.Executable = await DebugLaunchDataStore.SDBTaskAsync();
            settings.Arguments = "";
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

        private void ShowInstallError(InstallResult installResult)
        {
            string msg = string.Format(
                "Failed to install {0}.\n\n{1}",
                tDebugLaunchOptions.AppId,
                ResourcesInstallMessage.ResourceManager.GetString(installResult.ToString()));

            VsPackage.ShowMessage(MessageDialogType.Error, null, msg);
        }

        private bool InstallTizenPackage(SDBDeviceInfo device, TizenDebugLaunchOptions tDebugLaunchOptions)
        {
            SDBLauncher.Create(VsPackage.outputPaneTizen).TerminateApplication(device, tDebugLaunchOptions.AppId);

            InstallResult installResult = Launcher.Create().InstallTizenPackage(device, tDebugLaunchOptions.TpkPath,
                null, VsPackage.dialogFactory, false, out lastErrorMessage);

            bool isInstallSucceeded = (installResult == InstallResult.OK);

            if (!isInstallSucceeded)
            {
                OutputDebugLaunchMessage(lastErrorMessage);
                ShowInstallError(installResult);
                TizenPackageTracer.CleanTpiFiles();
            }

            return isInstallSucceeded;
        }

        private bool LaunchApplication(SDBDeviceInfo device, TizenDebugLaunchOptions tDebugLaunchOptions)
        {
            return tDebugLaunchOptions.IsDebugMode ?
                LaunchDebugModeApplication(device, tDebugLaunchOptions) :
                SDBLauncher.Create(VsPackage.outputPaneTizen).LaunchApplication(device, tDebugLaunchOptions.AppId);
        }

        private System.Diagnostics.Process StartSdbProcess(string arguments, string message, bool showStdErr = true)
        {
            var proc = SDBLib.CreateSdbProcess();
            proc.StartInfo.Arguments = arguments;
            proc.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    OutputDebugLaunchMessage(e.Data);
                }
            });
            if (showStdErr)
            {
                proc.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
                {
                    if (!String.IsNullOrEmpty(e.Data))
                    {
                        OutputDebugLaunchMessage($"[StdErr] {e.Data}");
                    }
                });
            }
            OutputDebugLaunchMessage(message);
            System.Diagnostics.Debug.WriteLine("{0} {1} StartSdbProcess command '{2}'", DateTime.Now, this.ToString(), proc.StartInfo.Arguments);
            proc.Start();
            proc.BeginOutputReadLine();
            if (showStdErr)
            {
                proc.BeginErrorReadLine();
            }
            return proc;
        }

        private bool WaitProcessExit(System.Diagnostics.Process proc, int milliseconds)
        {
            bool success = proc.WaitForExit(milliseconds);
            if (success)
            {
                proc.WaitForExit(); // process stdout and stderr
            }
            else
            {
                proc.CancelOutputRead();
                proc.CancelErrorRead();
            }
            proc.Close();
            return success;
        }

        private bool LaunchDebugModeApplication(SDBDeviceInfo device, TizenDebugLaunchOptions tDebugLaunchOptions)
        {
            #region W/A launch due to not-implemented parameter(for debug) of runapp protocol
            switch (tDebugLaunchOptions.AppType)
            {
                case "watch-application":
                case "widget-application":
                    SDBLauncher.Create(VsPackage.outputPaneTizen).LaunchApplication(device, DebugLaunchDataStore.WidgetViewerSdkAppId);
                    break;
            }
            var cap = new SDBCapability(device);
            bool startLiveProfiler = tDebugLaunchOptions.IsDebugMode && cap.GetAvailabilityByKey("netcoredbg_support") && DebuggerInfo.UseLiveProfiler;
            if (startLiveProfiler)
            {
                switch (tDebugLaunchOptions.AppType)
                {
                    case "ui-application":
                    case "service-application":
                        if (tDebugLaunchOptions is SecuredTizenDebugLaunchOptions)
                        {
                            startLiveProfiler = false;
                        }
                        break;
                    default:
                        startLiveProfiler = false;
                        break;
                }
            }

            bool ok = true;
            if (startLiveProfiler)
            {
                var proc = StartSdbProcess("-s " + device.Serial + " forward --remove tcp:4712",
                    "Removing port forward...", false);
                WaitProcessExit(proc, 5000);
                proc = StartSdbProcess("-s " + device.Serial + " forward tcp:4712 tcp:4711",
                    "Forwarding port...");
                WaitProcessExit(proc, 5000);
                ok = ProfilerPlugin.Instance.StartProfiler(true); // StartProfiler checks whether the root mode is off
            }
            else
            {
                if (ProfilerPlugin.EnsureRootOff(device, ProfilerPlugin.RunMode.Debug)) // check the root mode is off
                {
                    foreach (var arg in tDebugLaunchOptions.LaunchSequence)
                    {
                        var proc = StartSdbProcess(arg.Args, arg.Message);
                        if (arg.Timeout != 0)
                        {
                            if (!WaitProcessExit(proc, arg.Timeout))
                            {
                                // TODO!! show diagnostics
                                ok = false;
                                break;
                            }
                        }
                    }
                }
            }

            return ok;
            #endregion
        }

        private void OutputDebugLaunchMessage(string rawMsg)
        {
            if (string.IsNullOrEmpty(rawMsg))
            {
                return;
            }

            //Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            string message = String.Format($"{DateTime.Now} : {rawMsg}\n");

            VsPackage.outputPaneTizen?.Activate();

            VsPackage.outputPaneTizen?.OutputStringThreadSafe(message);
        }

        private class DebugLaunchDataStore
        {
            public const string DotNetLauncher = "/usr/bin/dotnet-launcher";
            public const string AppInstallPath = "/home/owner/apps_rw";
            public const string TpkRoot = "tpkroot";
            public const string WidgetViewerSdkAppId = "org.tizen.widget_viewer_sdk";

//!!            public static string LldbMi => (new SDBCapability().GetValueByKey("sdk_toolpath")) + @"/lldb/bin/lldb-mi";
            public static Task<string> SDBTaskAsync() => Task.Run(() =>
            {
                return SDBLib.GetSdbFilePath();
            });
        }

        private class TizenDebugLaunchOptions
        {
            public class SdbArgs
            {
                public string Args { get; }
                public string Message { get; }
                public int Timeout { get; }

                public SdbArgs(string args, string message, int timeout)
                {
                    Args = args;
                    Message = message;
                    Timeout = timeout;
                }
            }

            public IEnumerable<SdbArgs> LaunchSequence { get; }
            public string DebugEngineOptions { get; }
            public string AppId { get; }
            public string AppType { get; }
            public string TpkPath { get; }
            public bool IsDebugMode { get; }

            protected SDBDeviceInfo _device;

            protected const string niDisableOption = " COMPlus_ZapDisable 1 ";
            protected VsProjectHelper projHelper = VsProjectHelper.GetInstance;

            private TizenPackageTracer pkgTracer = TizenPackageTracer.Instance;

            protected class Parameters
            {
                public string PipePath { get; }
                public string PipeArguments { get; }
                public string MiMode { get; }
                public string AdditionalOptions { get; }
                public string LaunchCommand { get; }
                public string LaunchpadArgs { get; }

                public Parameters(string pipePath, string pipeArguments, string miMode, string additionalOptions, string launchCommand, string launchpadArgs)
                {
                    PipePath = pipePath;
                    PipeArguments = pipeArguments;
                    MiMode = miMode;
                    AdditionalOptions = additionalOptions;
                    LaunchCommand = launchCommand;
                    LaunchpadArgs = launchpadArgs;
                }
            }

            public TizenDebugLaunchOptions(SDBDeviceInfo device, bool isDebugMode, Project proj, string extraArgs)
            {
                _device = device;

                IsDebugMode = isDebugMode;

                AppId = projHelper.GetAppId(proj);//projHelper.GetManifestApplicationId(proj);
                AppType = projHelper.GetAppType(proj);
                TpkPath = GetDebugeeTpkPath(AppId, proj);//pkgTracer.GetTpkPathByAppId(AppId) ?? projHelper.GetTizenPackagePath(proj);
                string debugLaunchPadArgs = string.Empty;
                string launchCommand = string.Empty;
                Parameters debugEngineLaunchParameters = GetDebugEngineLaunchParameters();

                if (IsDebugMode)
                {
                    DebugEngineOptions =
                        "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<PipeLaunchOptions PipePath=\"" +
                        debugEngineLaunchParameters.PipePath +
                        "\" PipeArguments=\"" +
                        debugEngineLaunchParameters.PipeArguments +
                        "\" PipeCwd=\"" +
                        projHelper.GetValidRootPath() +
                        "\" ExePath=\"" +
                        DebugLaunchDataStore.DotNetLauncher +
                        "\" MIMode=\"" + debugEngineLaunchParameters.MiMode + "\" TargetArchitecture=\"" +
                        GetTargetArch() +
                        "\" WorkingDirectory=\"" +
                        DebugLaunchDataStore.AppInstallPath + "/" + projHelper.GetPackageId(TpkPath) + "/bin" +
                        "\" AdditionalSOLibSearchPath=\"\" xmlns=\"http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014\" >" +
                        debugEngineLaunchParameters.AdditionalOptions +
                        "</PipeLaunchOptions>";

                    launchCommand = debugEngineLaunchParameters.LaunchCommand;
                    debugLaunchPadArgs = debugEngineLaunchParameters.LaunchpadArgs;
                }
                else
                {
                    DebugEngineOptions = string.Empty;
                    launchCommand = debugEngineLaunchParameters.LaunchCommand;
                    debugLaunchPadArgs = " __AUL_SDK__ dotnet-launcher ";
                }
                LaunchSequence = GetLaunchSequence(AppId, proj, extraArgs, launchCommand, debugLaunchPadArgs);
            }

            protected virtual Parameters GetDebugEngineLaunchParameters()
            {
                return new Parameters(
                    pipePath: SDBLib.GetSdbFilePath(),
                    pipeArguments: GetLldbArguments(),
                    miMode: "lldb",
                    additionalOptions: "",
                    launchCommand: "launch_app",
                    launchpadArgs: " __AUL_SDK__ LLDB-SERVER __DLP_DEBUG_ARG__ g,--platform=host,*:1234,-- CORECLR_GDBJIT " + GetDebuggeeDllList() + niDisableOption
                );
            }

            private string GetDebugeeTpkPath(string appId, Project proj)
            {
                if (pkgTracer.IsAppIdOnWaiting(appId))
                {
                    string waitingTpkPath = pkgTracer.GetTpkPathByAppId(appId);
                    string waitingTpkPkgId = projHelper.GetPackageId(waitingTpkPath);

                    if (SDBLauncher.Create(null).IsPackageDetected(_device, waitingTpkPkgId))
                    {
                        return waitingTpkPath;
                    }
                }

                return projHelper.GetTizenPackagePath(proj);
            }

            protected virtual IEnumerable<SdbArgs> GetLaunchSequence(string appId, Project proj, string extraArgs,
                string launchCommand, string debugLaunchPadArgs, int timeout = 0)
            {
                string hostCmd = string.Join(" ", new string[] { "-s", _device.Serial, "shell", "sh", "-c" });
                string targetShellCmd = "exit";

                switch (AppType)
                {
                    case "ui-application":
                    case "service-application":
                        targetShellCmd = string.Join(" ", new string[] { launchCommand, appId, extraArgs, debugLaunchPadArgs });
                        break;
                    case "watch-application":
                    case "widget-application":
                        targetShellCmd = string.Join(" ", new string[] { launchCommand, DebugLaunchDataStore.WidgetViewerSdkAppId, "widget_id", appId, extraArgs, debugLaunchPadArgs });
                        break;
                    case "ime-application":
                    default:
                        break;
                }

                return new List<SdbArgs> {
                    new SdbArgs(args: string.Format("{0} \"{1}\"", hostCmd, targetShellCmd), message: "Launching " + appId, timeout: timeout)
                };
            }

            protected virtual string GetLldbArguments() => " -s " + _device.Serial + " shell sh -c 'launch_debug " + this.AppId + " __AUL_SDK__ LLDB-MI __LAUNCH_APP_MODE__ SYNC'";
            // Older emulator images do not support __AUL_SDK__ LLDB-MI:
            //protected virtual string GetLldbArguments() => " -s " + DeviceManager.SelectedDevice.Serial + " shell sh -c '" + DebugLaunchDataStore.LldbMi + "'";

            private string GetTargetArch()
            {
                var cap = new SDBCapability(_device);
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

                string binPath = Path.Combine(tpkDirPath, DebugLaunchDataStore.TpkRoot, "bin");

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
            public SecuredTizenDebugLaunchOptions(SDBDeviceInfo device, bool isDebugMode, Project proj, string extraArgs)
                : base(device, isDebugMode, proj, extraArgs)
            {
            }

            protected override Parameters GetDebugEngineLaunchParameters()
            {
                return new Parameters(
                    pipePath: SDBLib.GetSdbFilePath(),
                    pipeArguments: GetLldbArguments(),
                    miMode: "lldb",
                    additionalOptions: "",
                    launchCommand: "0 vs_debug",
                    launchpadArgs: " " + GetDebuggeeDllList() + niDisableOption
                );
            }

            protected override IEnumerable<SdbArgs> GetLaunchSequence(string appId, Project proj, string extraArgs, string launchCommand, string debugLaunchPadArgs, int timeout)
            {
                string hostCmd = string.Join(" ", new string[] { "-s", _device.Serial, "shell" });
                string targetShellCmd = "exit";

                switch (AppType)
                {
                    case "ui-application":
                    case "service-application":
                        if (extraArgs == string.Empty)
                        {
                            targetShellCmd = string.Join(" ", new string[] { launchCommand, appId, debugLaunchPadArgs });
                        }
                        else
                        {
                            targetShellCmd = string.Join(" ", new string[] { launchCommand, appId, extraArgs, debugLaunchPadArgs });
                        }
                        break;
                    case "watch-application":
                    case "widget-application":
                        targetShellCmd = string.Join(" ", new string[] { launchCommand, DebugLaunchDataStore.WidgetViewerSdkAppId, "widget_id", appId, extraArgs, debugLaunchPadArgs });
                        break;
                    default:
                        break;
                }

                return new List<SdbArgs> {
                    new SdbArgs(args: string.Format("{0} \"{1}\"", hostCmd, targetShellCmd), message: "Launching " + appId, timeout: timeout)
                };
            }

            protected override string GetLldbArguments() => " -s " + _device.Serial + " shell 0 vs_lldblaunch";
        }

        private class TizenNetCoreDbgLaunchOptions : TizenDebugLaunchOptions
        {
            public TizenNetCoreDbgLaunchOptions(SDBDeviceInfo device, bool isDebugMode, Project proj, string extraArgs)
                : base(device, isDebugMode, proj, extraArgs)
            {
            }

            protected override Parameters GetDebugEngineLaunchParameters()
            {
                return new Parameters(
                    pipePath: Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "NetCat.exe"),
                    pipeArguments: String.Format("127.0.0.1 4712"),
                    miMode: "clrdbg",
                    additionalOptions: "<CustomLaunchSetupCommands/>" + // empty because we pass launch command through arguments
                                       "<LaunchCompleteCommand>exec-run</LaunchCompleteCommand>",
                    launchCommand: "launch_app",
                    launchpadArgs: " __AUL_SDK__ NETCOREDBG __DLP_DEBUG_ARG__ --server=4711,-- "
                );
            }

            protected override IEnumerable<SdbArgs> GetLaunchSequence(string appId, Project proj, string extraArgs, string launchCommand,
                string debugLaunchpadArgs, int timeout)
            {
                if (IsDebugMode)
                {
                    return new List<SdbArgs> {
                        new SdbArgs(args: string.Format("-s {0} forward --remove tcp:4712", _device.Serial), message: "Removing port forward...", timeout: 5000),
                        new SdbArgs(args: string.Format("-s {0} forward tcp:4712 tcp:4711", _device.Serial), message: "Forwarding port...", timeout: 5000)
                    }.Concat(base.GetLaunchSequence(appId, proj, extraArgs, launchCommand, debugLaunchpadArgs, 30000));
                }
                else
                {
                    return base.GetLaunchSequence(appId, proj, extraArgs, launchCommand, debugLaunchpadArgs);
                }
            }
        }

        private class SecuredTizenNetCoreDbgLaunchOptions : SecuredTizenDebugLaunchOptions
        {
            public SecuredTizenNetCoreDbgLaunchOptions(SDBDeviceInfo device, bool isDebugMode, Project proj, string extraArgs)
                : base(device, isDebugMode, proj, extraArgs)
            {
            }

            protected override Parameters GetDebugEngineLaunchParameters()
            {
                string launchPadArguments = string.Empty;
                var cap = new SDBCapability(_device);
                string pluginVersion = cap.GetValueByKey("sdbd_plugin_version");
                string[] version = pluginVersion.Split('.');
                if (version.Length == 3)
                {
                    int major = Int32.Parse(version[0]);
                    int minor = Int32.Parse(version[1]);
                    if ((major < 3) || (major == 3 && minor <= 5))
                    {
                        launchPadArguments = " __DLP_DEBUG_ARG__ --server=4711,-- ";
                    }
                    else
                    {
                        launchPadArguments = "0";
                    }
                }
                return new Parameters(
                    pipePath: Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "NetCat.exe"),
                    pipeArguments: String.Format("127.0.0.1 4712"),
                    miMode: "clrdbg",
                    additionalOptions: "<CustomLaunchSetupCommands/>" + // empty because we pass launch command through arguments
                                       "<LaunchCompleteCommand>exec-run</LaunchCompleteCommand>",
                    launchCommand: "0 vs_sdklaunch NETCOREDBG",
                    launchpadArgs: launchPadArguments
                );
            }

            protected override IEnumerable<SdbArgs> GetLaunchSequence(string appId, Project proj, string extraArgs, string launchCommand, string debugLaunchpadArgs, int timeout)
            {
                return new List<SdbArgs> {
                    new SdbArgs(args: string.Format("-s {0} forward --remove tcp:4712", _device.Serial), message: "Removing port forward...", timeout: 5000),
                    new SdbArgs(args: string.Format("-s {0} forward tcp:4712 tcp:4711", _device.Serial), message: "Forwarding port...", timeout: 5000)
                }.Concat(base.GetLaunchSequence(appId, proj, extraArgs, launchCommand, debugLaunchpadArgs, 30000));
            }
        }
    }
}
