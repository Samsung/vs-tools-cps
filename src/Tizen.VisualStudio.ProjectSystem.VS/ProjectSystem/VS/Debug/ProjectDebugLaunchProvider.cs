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
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
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
using Tizen.VisualStudio.Utilities;

namespace Tizen.VisualStudio.ProjectSystem.VS.Debug
{
    [ExportDebugger(ProjectDebugger.SchemaName)]
    [AppliesTo(MyUnconfiguredProject.UniqueCapability)]
    [Order(100)]
    public class ProjectDebugLaunchProvider : DebugLaunchProviderBase
    {
        private const string WaitCaption = "Installing Tizen Application";
        private const string StatusBarText = "Tizen app install in progress...";
        private const string ProgressText = "Preparing...";
        private static readonly Guid MIEngineId = new Guid("{3352D8EC-AE86-41F2-BB8A-90DA85ABCA05}");
        private string lastErrorMessage;
        private bool isRootModeOff;
        private SDBCapability cap;

        private Task DoNothing = Task.Run(() => { });

        private TizenDebugLaunchOptions tDebugLaunchOptions;

        private string debugEngineOptions;

        private VsProjectHelper projHelper;

        private ITizenLaunchSettingsProvider _tizenLaunchSettingsProvider { get; }

        [ImportingConstructor]
        public ProjectDebugLaunchProvider(ConfiguredProject configuredProject)
            : base(configuredProject)
        {
            projHelper = VsProjectHelper.GetInstance;
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
            if (commandValue == null)
                return false;
            //TODO : Check the presence of SDB and pop Install Wizard in case of its absence

            return File.Exists(commandValue) || ((DeviceManager.DeviceInfoList != null) ? (DeviceManager.DeviceInfoList.Count == 0) : false);
        }

        private async void HandledWebAppLaunchByVS(bool isDebugMode, SDBDeviceInfo device, Project debuggeeProj)
        {
            OutputDebugLaunchMessage("<<< web app >>>");
            var waitPopup = new WaitDialogUtil();

            String workspacePath = projHelper.getSolutionFolderPath();
            if (workspacePath == null)
            {
                OutputDebugLaunchMessage("<<< unable to get workspace path >>>");
                return;
            }

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";

            if (!isDebugMode)
            {
                waitPopup.ShowPopup(WaitCaption,
                        "Please wait while the new app is being installed and launched...",
                        ProgressText, StatusBarText);

                // Install and Launch the Tizen web app
                if (InstallApp(device, debuggeeProj) != 0)
                {
                    OutputDebugLaunchMessage("<<<  Failed to install the Web package.  >>>");
                    waitPopup.ClosePopup();
                    return;
                }

                SDBLauncher.Create(VsPackage.outputPaneTizen).LaunchApplication(device, projHelper.GetWebAppId(debuggeeProj));
                waitPopup.ClosePopup();
                return;
            }
            else
            {
                // Check if chrome path is set in Tools->Options->Tizen
                if (string.IsNullOrWhiteSpace(ToolsPathInfo.ChromePath))
                {
                    string errormsg = string.Format("Chrome path (Tools->Options->Tizen) not set for Debugging.");

                    VsPackage.ShowMessage(MessageDialogType.Error, null, errormsg);
                    return;
                }
                waitPopup.ShowPopup(WaitCaption,
                        "Please wait while the new app is being installed...",
                        ProgressText, StatusBarText);

                // Launch the Tizen web app in debug mode
                if (InstallApp(device, debuggeeProj) != 0)
                {
                    OutputDebugLaunchMessage("<<<  Failed to install the Web app for debugging. >>>");
                    waitPopup.ClosePopup();
                    return;
                }
                else
                {
                    OutputDebugLaunchMessage("<<< Successfully installed the Web app for debugging. >>>");
                    waitPopup.ClosePopup();
                }

                String output, errMsg;
                int i, port;
                var appId = projHelper.GetWebAppId(debuggeeProj);
                string prfName = cap.GetValueByKey("profile_name");

                if (prfName.Equals("tv"))
                    SDBLib.RunSdbCommandAndGetFirstNonEmptyLine(device, "shell 0 debug " + appId, out output, out errMsg);
                else
                    SDBLib.RunSdbCommandAndGetFirstNonEmptyLine(device, "shell app_launcher -w --start " + appId, out output, out errMsg);

                i = output.LastIndexOf(':') + 2;
                port = 0;
                while (i < output.Length)
                {
                    port = port * 10 + (output[i] - '0');
                    i++;
                }

                String command = ToolsPathInfo.ToolsRootPath + "\\tools\\sdb.exe forward tcp:" + port + " tcp:" + port;// + " tcp:26099";
                startInfo.Arguments = "/C " + command;
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    string errormsg = string.Format("Failed to do forward tcp ports.");

                    VsPackage.ShowMessage(MessageDialogType.Error, null, errormsg);
                    return;
                }

                var client = new HttpClient();
                String baseUrl = "http://127.0.0.1:";
                String url = baseUrl + port + "/json";
                var json = await client.GetStringAsync(url);
                JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
                dynamic dobj = jsonSerializer.Deserialize<dynamic>(json);
                string inspectorPath = dobj[0]["devtoolsFrontendUrl"].ToString();

                if (inspectorPath[0] != '/')
                {
                    inspectorPath = "/" + inspectorPath;
                }

                waitPopup.ShowPopup("Launching Tizen Web App",
                        "Please wait while the app is being installed and launched in Debug mode...",
                        ProgressText, "Tizen web app launch in progress...");
                var debugUrl = baseUrl + port + inspectorPath;
                command = "\"" + ToolsPathInfo.ChromePath + "\" --no-first-run --activate-on-launch --no-default-browser-check --allow-file-access-from-files --disable-web-security --disable-translate --proxy-auto-detect --proxy-bypass-list=127.0.0.1 --app=" + debugUrl;
                startInfo.Arguments = "/C " + command;
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
                waitPopup.ClosePopup();

                if (process.ExitCode != 0)
                {
                    string errormsg = string.Format("Failed to launch Web app Chrome debugging.");

                    VsPackage.ShowMessage(MessageDialogType.Error, null, errormsg);
                }
                else
                {
                    OutputDebugLaunchMessage("<<< Successfully launched Web app debugging. >>>");
                }

                return;
            }
        }

        private int InstallApp(SDBDeviceInfo device, Project debuggeeProj)
        {
            String workspacePath = projHelper.getSolutionFolderPath();
            if (workspacePath == null)
            {
                OutputDebugLaunchMessage("<<< unable to get workspace path >>>");
                return -1;
            }

            var executor = new TzCmdExec();

            string message = executor.RunTzCmnd(string.Format("/c tz install \"{0}\" -s \"{1}\"", workspacePath, device.Serial));

            if (message.Contains("error:"))
            {
                OutputDebugLaunchMessage(message);
                return -1;
            }
            else
            {
                return 0;
            }

        }

        private async Task HandledNativeAppLaunchByVS(bool isDebugMode, SDBDeviceInfo device, Project debuggeeProj, DebugLaunchOptions launchOptions)
        {
            OutputDebugLaunchMessage("<<< Native app >>>");
            var waitPopup = new WaitDialogUtil();
            String workspacePath = projHelper.getSolutionFolderPath();

            if (workspacePath == null)
            {
                OutputDebugLaunchMessage("<<< unable to get workspace path >>>");
                return;
            }

            string appCat = projHelper.GetAppCategory(debuggeeProj);
            string appType = projHelper.GetAppType(debuggeeProj);

            if (appCat.Equals("http://tizen.org/category/ime"))
            {
                OutputDebugLaunchMessage("<<< info:Certain application categories, such as \"ime\" cannot be launched by \"Run\" >>>");
                System.Windows.MessageBox.Show("<<< info:Certain application categories, such as \"ime\" cannot be launched by \"Run\" >>>");
                return;
            }

            waitPopup.ShowPopup(WaitCaption,
                "Please wait while the new app is being installed and launched...",
                ProgressText, StatusBarText);

            // Install the Tizen Native app
            if (InstallApp(device, debuggeeProj) != 0)
            {
                OutputDebugLaunchMessage("<<<  Failed to install the Native app.  >>>");
                waitPopup.ClosePopup();
                return;
            }

            if (!isDebugMode)
            {
                // Launch the App
                SDBLauncher.Create(VsPackage.outputPaneTizen).LaunchApplication(device, projHelper.GetAppId(debuggeeProj));
                waitPopup.ClosePopup();
                return;
            }
            else
            {
                // Port Forwarding
                var proc = StartSdbProcess("-s " + device.Serial + " forward --remove tcp:1234",
                  "Removing port forward...", false);
                WaitProcessExit(proc, 5000);
                proc = StartSdbProcess("-s " + device.Serial + " forward tcp:1234 tcp:4711",
                    "Forwarding port...");
                WaitProcessExit(proc, 5000);

                // Debug Launch
                string AppId = projHelper.GetAppId(debuggeeProj);
                proc = StartSdbProcess("-s " + device.Serial + " launch -a " + AppId + " -p -e -m debug -P 4711",
                  "Launching App in Debug Mode...", false);
                //WaitProcessExit(proc, 5000);

                TizenNativeDebugLaunchOptions tDebugLaunchOptions = new TizenNativeDebugLaunchOptions(device, cap, isDebugMode, debuggeeProj);
                debugEngineOptions = tDebugLaunchOptions.DebugEngineOptions;
                await base.LaunchAsync(launchOptions);
                waitPopup.ClosePopup();
            }
        }

        private void HandledWebAppLaunchByTZ(bool isDebugMode, SDBDeviceInfo device)
        {
            OutputDebugLaunchMessage("<<< web app >>>");
            String message;
            var waitPopup = new WaitDialogUtil();

            String workspacePath = projHelper.getSolutionFolderPath();
            if (workspacePath == null)
            {
                OutputDebugLaunchMessage("<<< unable to get workspace path >>>");
                return;
            }

            var executor = new TzCmdExec();

            if (!isDebugMode)
            {
                waitPopup.ShowPopup(WaitCaption,
                        "Please wait while the new app is being installed...",
                        ProgressText, StatusBarText);
                // Install and Launch the Tizen web app
                message = executor.RunTzCmnd(string.Format("/c tz run \"{0}\" -s \"{1}\"", workspacePath, device.Serial));

                if (message.Contains("error:"))
                {
                    OutputDebugLaunchMessage(message);
                }
                waitPopup.ClosePopup();
                return;
            }
            else
            {
                // Check if chrome path is set in Tools->Options->Tizen
                if (string.IsNullOrWhiteSpace(ToolsPathInfo.ChromePath))
                {
                    string errormsg = string.Format("Chrome path (Tools->Options->Tizen) not set for Debugging.");

                    VsPackage.ShowMessage(MessageDialogType.Error, null, errormsg);
                    return;
                }
                waitPopup.ShowPopup(WaitCaption,
                        "Please wait while the app is being installed and launched in Debug mode...",
                        ProgressText, StatusBarText);

                // Launch the Tizen web app in debug mode
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmd.exe";
                String command = ToolsPathInfo.ToolsRootPath + "\\tools\\tizen-core\\tz.exe run -d \"" + workspacePath + "\"" + " -s " + device.Serial;
                startInfo.Arguments = "/C " + command;
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();

                waitPopup.ClosePopup();

                if (process.ExitCode != 0)
                {
                    OutputDebugLaunchMessage("<<<  Failed to debug the Web app. >>>");
                }
                else
                {
                    OutputDebugLaunchMessage("<<< Successfully launched the Web app for debugging. >>>");
                }

                return;
            }
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

            if (DeviceManager.SdbCapsMap.ContainsKey(device.Serial))
            {
                cap = DeviceManager.SdbCapsMap[device.Serial];
            }
            else
            {
                cap = new SDBCapability(device);
                DeviceManager.SdbCapsMap.Add(device.Serial, cap);
            }

            string tizenVersion = cap.GetValueByKey("platform_version");
            if (!ProfilerPlugin.IsTizenVersionSupported(tizenVersion, true))
            {
                return;
            }

            bool isDebugMode = !launchOptions.Equals(DebugLaunchOptions.NoDebug);

            Project debuggeeProj = VsHierarchy.GetDTEProject();
            string debugeeProjType = projHelper.getProjectType(Path.GetDirectoryName(debuggeeProj.FullName));

            if (debugeeProjType.Equals("web"))
            {
                // Code for Launching Tizen Web app
                HandledWebAppLaunchByTZ(isDebugMode, device);
            }
            else if (debugeeProjType.Equals("native"))
            {
                // Code for Launching Tizen Native app
                await HandledNativeAppLaunchByVS(isDebugMode, device, debuggeeProj, launchOptions);
            }
            else
            {
                bool isSecureProtocol = cap.GetAvailabilityByKey("secure_protocol");
                bool useNetCoreDbg = cap.GetAvailabilityByKey("netcoredbg_support");
                bool useLiveProfiler = isDebugMode && useNetCoreDbg && DebuggerInfo.UseLiveProfiler;
                bool useHotReloder = isDebugMode && !useLiveProfiler && HotReloadInfo.UseHotReload;

                // check the root mode is off
                if (!ProfilerPlugin.EnsureRootOff(device, cap,
                    isDebugMode ?
                        (useLiveProfiler ? ProfilerPlugin.RunMode.LiveProfiler : ProfilerPlugin.RunMode.Debug)
                        : ProfilerPlugin.RunMode.NoDebug))
                {
                    return;
                }
                else
                {
                    isRootModeOff = true;
                }


                tDebugLaunchOptions = isSecureProtocol ?
                    (useNetCoreDbg ?
                        new SecuredTizenNetCoreDbgLaunchOptions(device, cap, isDebugMode, debuggeeProj, _tizenLaunchSettingsProvider.TizenLaunchSetting.ExtraArguments) :
                        new SecuredTizenDebugLaunchOptions(device, cap, isDebugMode, debuggeeProj, _tizenLaunchSettingsProvider.TizenLaunchSetting.ExtraArguments)) :
                    (useNetCoreDbg ?
                        new TizenNetCoreDbgLaunchOptions(device, cap, isDebugMode, debuggeeProj, useHotReloder, _tizenLaunchSettingsProvider.TizenLaunchSetting.ExtraArguments) :
                        new TizenDebugLaunchOptions(device, cap, isDebugMode, debuggeeProj, _tizenLaunchSettingsProvider.TizenLaunchSetting.ExtraArguments));


                debugEngineOptions = tDebugLaunchOptions.DebugEngineOptions;

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
                        // TODO!! remove OnDemandDebuggerInstaller.cs after checking OnDemandInstallers

                        string packageapiversion = projHelper.GetPackageAPIVersion(tDebugLaunchOptions.TpkPath);
                        if (string.IsNullOrEmpty(packageapiversion))
                        {
                            string errormsg = string.Format("Incorrect manifest values.");

                            VsPackage.ShowMessage(MessageDialogType.Error, null, errormsg);
                            return;
                        }

                        if (string.Compare(tizenVersion, packageapiversion) == -1)
                        {
                            string errormsg = string.Format("Tizen version of device is lower than that of the project.\n" +
                                "Please install {0} platform using Tizen Package manager.", packageapiversion);

                            VsPackage.ShowMessage(MessageDialogType.Error, null, errormsg);
                            return;
                        }

                        if (!DeviceManager.isDebuggerInstalled)
                        {
                            string errormsg = string.Format("Debugger not properly installed.");

                            VsPackage.ShowMessage(MessageDialogType.Error, null, errormsg);
                            return;
                        }

                        if (useHotReloder)
                        {
                            await HotReloadLaunchProvider.StartHotReloadObserverAsync(device, tDebugLaunchOptions.AppId, debuggeeProj.FullName);
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
            settings.Options = debugEngineOptions;

            if (!projHelper.IsTizenNativeProject())
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

            InstallResult installResult = Launcher.Create().InstallTizenPackage(device, cap, tDebugLaunchOptions.TpkPath,
                null, VsPackage.dialogFactory, false, out lastErrorMessage, tDebugLaunchOptions.IsDebugMode);

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

            if (proc == null)
            {
                return null;
            }
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

            bool startLiveProfiler = tDebugLaunchOptions.IsDebugMode && cap.GetAvailabilityByKey("netcoredbg_support") && DebuggerInfo.UseLiveProfiler;
            if (startLiveProfiler)
            {
                switch (tDebugLaunchOptions.AppType)
                {
                    case "ui-application":
                    case "service-application":
                    case "component-based-application":
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
                if (isRootModeOff) // check the root mode is off
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
            protected SDBCapability cap;

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

            public TizenDebugLaunchOptions(SDBDeviceInfo device, SDBCapability c, bool isDebugMode, Project proj, string extraArgs, bool enableHotreload = false)
            {
                _device = device;
                cap = c;

                IsDebugMode = isDebugMode;

                AppId = projHelper.GetAppId(proj);//projHelper.GetManifestApplicationId(proj);
                AppType = projHelper.GetAppType(proj);
                TpkPath = GetDebugeeTpkPath(AppId, proj);//pkgTracer.GetTpkPathByAppId(AppId) ?? projHelper.GetTizenPackagePath(proj);
                string debugLaunchPadArgs = string.Empty;
                string launchCommand = string.Empty;
                Parameters debugEngineLaunchParameters = GetDebugEngineLaunchParameters();
                var setHotreload = enableHotreload && !HotReloadLaunchProvider.GetIsXamlProject();

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
                        "\" MIMode=\"" + debugEngineLaunchParameters.MiMode +
                        "\" Hotreload=\"" + setHotreload + "\" TargetArchitecture=\"" +
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
                    case "component-based-application":
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
                string arch = cap.GetValueByKey("cpu_arch");

                switch (arch)
                {
                    case "x86":
                        return "X86";
                    case "x86_64":
                        return "X64";
                    case "armv8":
                        return "arm64";
                    case "aarch64":
                        return "arm64";
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


        private class TizenNativeDebugLaunchOptions
        {
            public string DebugEngineOptions { get; }

            public TizenNativeDebugLaunchOptions(SDBDeviceInfo device, SDBCapability cap, bool isDebugMode, Project proj)
            {
                VsProjectHelper projHelp = VsProjectHelper.GetInstance;
                string debuggerPath = GetDebuggerPath(cap).Replace("\\", "/");
                string arch = GetTargetArch(cap);
                string buildFolderName = "native_build";
                string tpkFolder = projHelp.getTPKFolder(projHelp.getSolutionFolderPath(), buildFolderName);
                string exePath = Path.Combine(tpkFolder, "bin", projHelp.GetAppExec(proj));
                string optionFormat = "<LocalLaunchOptions xmlns=\"http://schemas.microsoft.com/vstudio/MDDDebuggerOptions/2014\"\r\n" +
                                              "MIDebuggerPath = \"{0}\" " +
                                              "MIDebuggerServerAddress = \"127.0.0.1:1234\" " +
                                              "ExePath = \"{1}\" " +
                                              "MIMode = \"gdb\" " +
                                              "TargetArchitecture = \"{2}\" " +
                                              "/>";
                DebugEngineOptions = String.Format(optionFormat, debuggerPath, exePath, arch);
            }

            public string GetTpkPath(Project prj)
            {
                string projectFolder = Path.GetDirectoryName(prj.FullName);
                string projectOutput = Path.Combine(projectFolder, "debug");
                string tpkFilePath = Path.Combine(projectOutput, "tpk");
                return tpkFilePath;
            }

            private string GetTargetArch(SDBCapability cap)
            {
                // Only X86 and ARM are support now. 64bit are not supported.
                string arch = cap.GetValueByKey("cpu_arch");
                switch (arch)
                {
                    case "x86":
                        return "X86";
                    default:
                        return "arm";
                }
            }

            private string GetDebuggerPath(SDBCapability cap)
            {
                string sdkToolPath = Path.Combine(ToolsPathInfo.ToolsRootPath, "tools");
                string arch = GetTargetArch(cap);
                string tizenVersion = cap.GetValueByKey("platform_version");
                Version version = Version.Parse(tizenVersion);
                string debuggerPath = getGDBPath(sdkToolPath, arch, version);
                return debuggerPath;
            }

            private string getGDBPath(string sdkToolPath, string arch, Version version)
            {
                if (version.Major >= 6)
                {
                    if (arch == "X86")
                    {
                        return Path.Combine(sdkToolPath, "i586-linux-gnueabi-gdb-8.3.1", "bin", "i586-linux-gnueabi-gdb.exe");
                    }
                    else
                    {
                        return Path.Combine(sdkToolPath, "arm-linux-gnueabi-gdb-8.3.1", "bin", "arm-linux-gnueabi-gdb.exe");
                    }
                }
                else
                {
                    if (arch == "X86")
                    {
                        return Path.Combine(sdkToolPath, "i386-linux-gnueabi-gdb-7.8", "bin", "i386-linux-gnueabi-gdb.exe");
                    }
                    else
                    {
                        return Path.Combine(sdkToolPath, "arm-linux-gnueabi-gdb-7.8", "bin", "arm-linux-gnueabi-gdb.exe");
                    }
                }
            }
        }

        private class SecuredTizenDebugLaunchOptions : TizenDebugLaunchOptions
        {
            public SecuredTizenDebugLaunchOptions(SDBDeviceInfo device, SDBCapability cap, bool isDebugMode, Project proj, string extraArgs)
                : base(device, cap, isDebugMode, proj, extraArgs)
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
                    case "component-based-application":
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
            public TizenNetCoreDbgLaunchOptions(SDBDeviceInfo device, SDBCapability cap, bool isDebugMode, Project proj, bool enableHotreload, string extraArgs)
                : base(device, cap, isDebugMode, proj, extraArgs, enableHotreload)
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
            public SecuredTizenNetCoreDbgLaunchOptions(SDBDeviceInfo device, SDBCapability cap, bool isDebugMode, Project proj, string extraArgs)
                : base(device, cap, isDebugMode, proj, extraArgs)
            {
            }

            protected override Parameters GetDebugEngineLaunchParameters()
            {
                string launchPadArguments = string.Empty;
                string pluginVersion = cap.GetValueByKey("sdbd_plugin_version");
                string[] version = pluginVersion.Split('.');
                if (version.Length == 3)
                {
                    int major = Int32.Parse(version[0]);
                    int minor = Int32.Parse(version[1]);
                    if ((major < 3) || (major == 3 && minor < 7))
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
