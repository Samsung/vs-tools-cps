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
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NetCore.Profiler.Analytics.Model;
using NetCore.Profiler.Extension.Commands;
using NetCore.Profiler.Extension.Launcher;
using NetCore.Profiler.Extension.Launcher.Model;
using NetCore.Profiler.Extension.Options;
using NetCore.Profiler.Extension.Session;
using NetCore.Profiler.Extension.UI.AdornedSourceWindow;
using NetCore.Profiler.Extension.UI.MemoryProfilingSessionWindow;
using NetCore.Profiler.Extension.UI.ProfilingProgressWindow;
using NetCore.Profiler.Extension.UI.SessionExplorer;
using NetCore.Profiler.Extension.UI.SessionWindow;
using Tizen.VisualStudio.Tools.Data;
using Tizen.VisualStudio.Tools.DebugBridge;
using Tizen.VisualStudio.Tools.DebugBridge.SDBCommand;
using Tizen.VisualStudio.Tools.ExternalTool;
using Tizen.VisualStudio.Tools.Utilities;
using Tizen.VisualStudio.Utilities;
using Application = System.Windows.Application;
using RunProfilerDialog = NetCore.Profiler.Extension.UI.RunProfilerDialog;

namespace NetCore.Profiler.Extension.VSPackage
{
    /// <summary>
    /// The front-end for the %Tizen %Profiler related part of the plugin.
    /// </summary>
    public sealed class ProfilerPlugin
    {
        public const int RunProfilerCommandId = 0x0115;
        public const int MemoryProfilerCommandId = 0x0116;
        public const int RunHeaptrackCommandId = 0x0117;
        public const int ManagedMemoryProfilerCommandId = 0x0118;

        public const string SettingsCollectionPath = "CoreClrProfiler";

        /// <summary>
        /// A folder (insider a project folder) where the project's profiling sessions are stored.
        /// </summary>
        public const string TizenProfilerDirectory = "TizenProfiler";

        public static ProfilerPlugin Instance { get; private set; }

        private Package _package;

        private IVsOutputWindowPane _outputPaneTizen;
        private IVsThreadedWaitDialogFactory _dialogFactory;

        public Guid OutputPaneTizenGuid = new Guid("CFEE7FDA-3167-422A-971C-6C7BD17DD1AD");

        public ProfileLauncher ProfileLauncher { get; private set; }
        public HeaptrackLauncher HeaptrackLauncher { get; private set; }

        public GeneralOptions GeneralOptions { get; private set; }
        public HeaptrackOptions HeaptrackOptions { get; private set; }

        private SessionExplorerWindow _explorerWindow;
        private ProfilingProgressWindow _profilingProgressWindow;
        private HotLinesToolWindow _hotLinesToolWindow;

        public SessionExplorerWindow ExplorerWindow => _explorerWindow ?? (_explorerWindow = GetToolWindow<SessionExplorerWindow>());

        public ProfilingProgressWindow ProfilingProgressWindow => _profilingProgressWindow ?? (_profilingProgressWindow = GetToolWindow<ProfilingProgressWindow>());

        public HotLinesToolWindow HotLinesToolWindow => _hotLinesToolWindow ?? (_hotLinesToolWindow = GetToolWindow<HotLinesToolWindow>());

        private SolutionSessionsContainer _solutionSessionsContainer;

        private SolutionListener _solutionListener;

        public ISolutionSessionsContainer SessionsContainer => _solutionSessionsContainer;

        public Microsoft.VisualStudio.OLE.Interop.IServiceProvider OLEServiceProvider;

        public IVsUIShell5 VsUiShell5;

        public DBGMODE DebugMode { get; private set; } = DBGMODE.DBGMODE_Design;

        public static void Initialize(Package package, IVsOutputWindowPane outputPaneTizen, IVsThreadedWaitDialogFactory dialogFactory)
        {
            Instance = new ProfilerPlugin(package, outputPaneTizen, dialogFactory);
        }

        public static bool IsTizenVersionSupported(string tizenVersion, bool showDialogIfNot)
        {
            if (!DeployHelper.IsTizenVersionSupported(tizenVersion))
            {
                if (showDialogIfNot)
                {
                    Instance.ShowError($"Target platform version {tizenVersion} is not supported");
                }
                return false;
            }
            return true;
        }

        private ProfilerPlugin(Package package, IVsOutputWindowPane outputPaneTizen, IVsThreadedWaitDialogFactory dialogFactory)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _outputPaneTizen = outputPaneTizen;
            _dialogFactory = dialogFactory;

            OLEServiceProvider =
                GetService(typeof(Microsoft.VisualStudio.OLE.Interop.IServiceProvider)) as
                    Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

            VsUiShell5 = GetIVsUIShell5();

            ExplorerWindowCommand.Initialize(_package);
            ProfilingProgressWindowCommand.Initialize(_package);

            GeneralOptions = new GeneralOptions(new SettingsStore(_package, SettingsCollectionPath));
            HeaptrackOptions = new HeaptrackOptions(new SettingsStore(_package, SettingsCollectionPath));

            RegisterMenuHandlers();

            ProfileLauncher.Initialize();
            HeaptrackLauncher.Initialize();

            ProfileLauncher = ProfileLauncher.Instance;
            HeaptrackLauncher = HeaptrackLauncher.Instance;
            HeaptrackLauncher.OnSessionFinished += HandleMenuItemRunMemoryProfiler;

            _solutionListener = new SolutionListener(_package)
            {
                AfterOpenSolution = delegate
                {
                    _solutionSessionsContainer.Update();
                    return VSConstants.S_OK;
                },
                AfterCloseSolution = delegate
                {
                    _solutionSessionsContainer.Update();
                    return VSConstants.S_OK;
                }
            };
            _solutionListener.Initialize();

            _solutionSessionsContainer = new SolutionSessionsContainer((DTE2)Package.GetGlobalService(typeof(SDTE)));
        }

        private void RegisterMenuHandlers()
        {
            OleMenuCommandService commandService = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService == null)
            {
                return;
            }

            RegMenuItem(commandService, RunProfilerCommandId, HandleMenuItemRunProfiler);
            RegMenuItem(commandService, MemoryProfilerCommandId, HandleMenuItemRunMemoryProfiler);
            RegMenuItem(commandService, ManagedMemoryProfilerCommandId, HandleMenuItemRunManagedMemoryProfiler);
            RegMenuItem(commandService, RunHeaptrackCommandId, HandleMenuItemRunHeaptrack);
        }

        private MenuCommand RegMenuItem(OleMenuCommandService commandService, int commandID, EventHandler invokeHandler)
        {
            CommandID cmdId = new CommandID(GeneralProperties.CommandSet, commandID);
            MenuCommand mItem = new MenuCommand(invokeHandler, cmdId);

            commandService.AddCommand(mItem);

            return mItem;
        }

        private void HandleMenuItemRunProfiler(object sender, EventArgs e)
        {
            bool isTargetListEmpty = (DeviceManager.DeviceInfoList.Count == 0);
            if (isTargetListEmpty)
            {
                new EmulatorManagerLauncher().Launch();
            }
            else
            {
                try
                {
                    Instance.StartProfiler(false);
                }
                catch (Exception ex)
                {
                    ShowError(ex.Message);
                }
            }
        }

        /// <summary>
        /// Open the %Tizen Memory %Profiler GUI application to view a saved memory profiling (heaptrack) session.
        /// </summary>
        /// <param name="path">A path to the folder of a memory profiling (heaptrack) session.</param>
        /// <param name="args">Additional arguments for %Tizen Memory %Profiler GUI</param>
        public void MemoryProfilerGuiOpenSession(string path, string args = "--managed ")
        {
            StartMemoryProfilerGui(String.Concat(args, Path.Combine(path, "resfile.gz")));
        }

        private void MemoryProfilerGuiStart(string arg)
        {
            string arguments = arg;
            Project project = GetStartupProject();
            if (project != null)
            {
                string[] dirs = Directory.GetDirectories(Path.Combine(Path.GetDirectoryName(project.FullName), TizenProfilerDirectory));
                DateTime minDt = DateTime.MinValue;
                foreach (string dir in dirs)
                {
                    string dname = Path.GetFileName(dir);
                    if (dname.StartsWith("DotNETMP-") || dname.StartsWith("HEAPTRACK-"))
                    {
                        string rname = Path.Combine(dir, "resfile.gz");
                        if (!File.Exists(rname))
                        {
                            rname = Path.Combine(dir, "resfile");
                            if (!File.Exists(rname))
                            {
                                continue;
                            }
                        }
                        DateTime dt = File.GetCreationTimeUtc(rname);
                        if (dt > minDt)
                        {
                            arguments = $"{arg} \"{rname}\"";
                            minDt = dt;
                        }
                    }
                }
            }
            StartMemoryProfilerGui(arguments);
        }

        private void StartMemoryProfilerGui(string arguments)
        {
            string guiPath = ToolsPathInfo.MemoryProfilerPath;
            var startInfo = new ProcessStartInfo(guiPath, arguments);
            try
            {
                System.Diagnostics.Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                if (!msg.Contains(guiPath))
                {
                    msg = StringHelper.CombineMessages(msg, $"\n\nMemory profiler GUI path: \"{guiPath}\"");
                }
                ShowError(msg);
            }
        }

        private void HandleMenuItemRunMemoryProfiler(object sender, EventArgs e)
        {
            MemoryProfilerGuiStart("--managed");
        }

        private void HandleMenuItemRunManagedMemoryProfiler(object sender, EventArgs e)
        {
            MemoryProfilerGuiStart("--hide-unmanaged-stacks --managed");
        }

        private void HandleMenuItemRunHeaptrack(object sender, EventArgs e)
        {
            bool isTargetListEmpty = (DeviceManager.DeviceInfoList.Count == 0);
            if (isTargetListEmpty)
            {
                new EmulatorManagerLauncher().Launch();
            }
            else
            {
                try
                {
                    Instance.StartHeaptrack();
                }
                catch (Exception ex)
                {
                    ShowError(ex.Message);
                }
            }
        }

        public void ActivateTizenOutputPane()
        {
            _outputPaneTizen?.Activate();
        }

        public void WriteToOutput(string message)
        {
            _outputPaneTizen?.OutputStringThreadSafe(message + "\n");
        }

        public void CreateDialogInstance(out IVsThreadedWaitDialog2 dialog,
            string caption, string message, string progressText, string statusBarText)
        {
            dialog = null;
            _dialogFactory?.CreateInstance(out dialog);
            dialog?.StartWaitDialog(
                    caption,
                    message,
                    progressText,
                    null,
                    statusBarText,
                    0, false, true);
        }

        /// <summary>
        /// Build the current Visual Studio solution.
        /// </summary>
        /// <returns>true if built the solution successfully, false otherwise</returns>
        public bool BuildSolution()
        {
            bool result;
            try
            {
                DTE2 dte2 = (DTE2)Package.GetGlobalService(typeof(SDTE));
                //dte2.ExecuteCommand("Build.BuildSolution");
                SolutionBuild sb = dte2.Solution.SolutionBuild;
                sb.Build(true);
                result = (sb.LastBuildInfo == 0);
            }
            catch (Exception e)
            {
                WriteToOutput("Error building the solution. " + e.Message);
                result = false;
            }
            return result;
        }

        public Project GetStartupProject()
        {
            try
            {
                string startPrjName = string.Empty;
                Project startPrj = null;
                DTE2 dte2 = (DTE2)Package.GetGlobalService(typeof(SDTE));
                Property property = dte2.Solution.Properties.Item("StartupProject");
                if (property != null)
                {
                    startPrjName = (string)property.Value;
                }
                foreach (Project prj in dte2.Solution.Projects)
                {
                    if (prj.Name == startPrjName)
                    {
                        startPrj = prj;
                    }
                }
                return startPrj;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private bool IsNetCoreDbgSupported()
        {
            SDBDeviceInfo device = DeviceManager.SelectedDevice;
            bool is_netcoredbg_support = false;
            if (device != null)
            {
                var cap = new SDBCapability(device);
                is_netcoredbg_support =
                    cap.GetAvailabilityByKey("netcoredbg_support");
            }
            return is_netcoredbg_support;
        }

        private bool CanStartProfiler(out Project project)
        {
            project = null;

            if (ProfileLauncher.SessionActive || HeaptrackLauncher.SessionActive)
            {
                ShowError("Another profiling session is active");
                return false;
            }

            project = GetStartupProject();
            if (project == null)
            {
                ShellHelper.ShowMessage(_package, MessageDialogType.Info, "",
                    "No active project");
                return false;
            }

            if (DebugMode != DBGMODE.DBGMODE_Design)
            {
                ShowError("Cannot start profiling while debugging");
                return false;
            }

            if (!IsNetCoreDbgSupported())
            {
                ShellHelper.ShowMessage(_package, MessageDialogType.Info, "",
                    "Profiling is supported starting from Tizen 5.0 version only");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Start a profiling session.
        /// </summary>
        /// <remarks>
        /// A run profiler dialog is shown (if not starting a live profiling session). The dialog allows a user to edit
        /// different profiling options.
        /// </remarks>
        /// <param name="isLiveProfiling">
        /// true in case of live profiling (i.e. combined debugging and profiling), false otherwise
        /// </param>
        /// <returns>true if profiling has been started successfully, false otherwise</returns>
        public bool StartProfiler(bool isLiveProfiling)
        {
            Project project;

            if (!CanStartProfiler(out project) || (project == null))
            {
                return false;
            }

            var sessionConfiguration = new ProfileSessionConfiguration(project, GeneralOptions);

            if (isLiveProfiling)
            {
                var preset = ProfilingPreset.CpuSampling;
                preset.ProfilingSettings.TraceSourceLines = false; // TODO!! example
                sessionConfiguration.ProfilingPreset = preset;
            }
            else
            {
                var dlg = new RunProfilerDialog(sessionConfiguration);
                dlg.ShowDialog();
                if (!(dlg.DialogResult ?? false))
                {
                    return false;
                }
            }

            SDBDeviceInfo device = GetAndCheckSelectedDevice(isLiveProfiling ? RunMode.LiveProfiler : RunMode.CoreProfiler);
            if (device == null)
            {
                return false;
            }

            ProfileSession session = ProfileLauncher.CreateSession(device, sessionConfiguration, isLiveProfiling);
            if (session == null)
            {
                return false;
            }
            ProfilingProgressWindow.SetSession(session);
            ProfileLauncher.StartSession(session);
            return true;
        }

        private static SDBDeviceInfo GetAndCheckSelectedDevice(RunMode runMode)
        {
            SDBDeviceInfo device = DeviceManager.SelectedDevice;
            if (device != null)
            {
                if (!EnsureRootOff(device, runMode))
                {
                    device = null;
                }
            }
            else
            {
                Instance.ShowMessage(MessageDialogType.Warning, "Target device not selected");
            }
            return device;
        }

        public enum RunMode
        {
            NoDebug,
            Debug,
            CoreProfiler,
            LiveProfiler,
            MemoryProfiler
        }

        public static bool EnsureRootOff(SDBDeviceInfo device, RunMode runMode)
        {
            bool isRoot;
            string errorMessage;

			bool isSecureProtocol = (new SDBCapability(DeviceManager.SelectedDevice)).GetAvailabilityByKey("secure_protocol");
			if (isSecureProtocol)
				return true;

            if (!SDBLib.CheckIsRoot(device, out isRoot, out errorMessage))
            {
                Instance.ShowError(StringHelper.CombineMessages("Cannot check if \"root off\" mode set", errorMessage));
                return false;
            }

            if (isRoot)
            {
                string msg = $"Currently \"root on\" mode is used on the \"{device.Name}\" device.\n";
                switch (runMode)
                {
                    case RunMode.Debug:
                        msg +=
                          "Debugging cannot be started until switching the device to \"root off\".\n\n" +
                          "Do you want the plugin to switch the device to \"root off\" for you and continue?\n\n" +
                          "Note: please don't switch to \"root on\" mode manually while debugging.";
                        break;

                    case RunMode.CoreProfiler:
                    case RunMode.LiveProfiler:
                    case RunMode.MemoryProfiler:
                        msg +=
                          "Profiling cannot be started until switching the device to \"root off\".\n\n" +
                          "Do you want the plugin to switch the device to \"root off\" for you and continue?\n\n" +
                          "Note: please don't switch to \"root on\" mode manually while profiling.";
                        break;

                    default:
                        msg +=
                          "An application cannot be started until switching the device to \"root off\".\n\n" +
                          "Do you want the plugin to switch the device to \"root off\" for you and continue?";
                        break;
                }
                if (System.Windows.MessageBox.Show(msg, "Tizen Plugin", MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    return false;
                }
                if (!SDBLib.SwitchToRoot(device, false))
                {
                    ProfilerPlugin.Instance.ShowError($"Cannot switch \"{device.Name}\" to \"root off\" mode");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Called from VsPackage to notify the profiler about the Visual Studio debugging mode change (from design to run,
        /// from run to break, etc.).
        /// </summary>
        /// <param name="dbgmodeNew">The new Visual Studio debugging mode.</param>
        public void OnModeChange(DBGMODE dbgmodeNew)
        {
            DBGMODE lastMode = DebugMode;
            DebugMode = dbgmodeNew;
            ProfileLauncher.OnModeChange(lastMode, dbgmodeNew);
        }

        /// <summary>
        /// Start a memory profiling (heaptrack) session.
        /// </summary>
        public void StartHeaptrack()
        {
            Project project;

            if (!CanStartProfiler(out project) || (project == null))
            {
                return;
            }

            var sessionConfiguration = new HeaptrackSessionConfiguration(project, HeaptrackOptions);

            SDBDeviceInfo device = GetAndCheckSelectedDevice(RunMode.MemoryProfiler);
            if (device == null)
            {
                return;
            }

            HeaptrackSession session = HeaptrackLauncher.CreateSession(device, sessionConfiguration);
            if (session == null)
            {
                return;
            }
            HeaptrackLauncher.StartSession(session);
        }

        public void ShowError(string message)
        {
            ShellHelper.ShowMessage(_package, MessageDialogType.Error, "", message);
        }

        public int ShowMessage(MessageDialogType messageType, string message)
        {
            return ShellHelper.ShowMessage(_package, messageType, "", message);
        }

        private string _explorerWindowCaption;
        public void UpdateExplorerWindowProgress(long current)
        {
            Application.Current.Dispatcher.Invoke(() => ExplorerWindow.Caption = $"{_explorerWindowCaption}: {current}%");
        }

        public void SaveExplorerWindowCaption()
        {
            _explorerWindowCaption = ExplorerWindow.Caption;
        }

        public void RestoreExplorerWindowCaption()
        {
            Application.Current.Dispatcher.Invoke(() => ExplorerWindow.Caption = _explorerWindowCaption);
        }

        public void ShowSourceFile(ISession session, ISourceLineStatistics line, ISourceLinesQueryResult lines)
        {
            var path = session.GetSourceFilePath(line.SourceFileId);
            if (string.IsNullOrEmpty(path))
            {
                ShowMessage(MessageDialogType.Warning, "Source file is missing");
                return;
            }
            if (!File.Exists(Path.GetFullPath(path)))
            {
                if (ShowMessage(MessageDialogType.Question, "Source file not found. Would you like to locate it yourself?") == 6) // Yes
                {
                    var fileName = Path.GetFileName(path);
                    using (var openFileDialog = new OpenFileDialog { Filter = $"{fileName}|{fileName}" })
                    {
                        if (openFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            path = openFileDialog.FileName;
                            session.SetSourceFilePath(line.SourceFileId, path);
                        }
                        else
                        {
                            return;
                        }
                    }
                }
                else
                {
                    return;
                }
            }
            var x = GetToolWindow<HotLinesToolWindow>();
            x.SetInputSource(path, line, lines);
            x.Show();
        }

        public void AddSessionToExplorer()
        {
            //TODO
            //var openFileDialog = new OpenFileDialog { Filter = $"Profiler Session Files|{SessionConstants.SessionPropsFileName}" };
            //if (openFileDialog.ShowDialog() == DialogResult.OK)
            //{
            //    AddSessionToExplorer(openFileDialog.FileName);
            //}
        }

        public T FindToolWindow<T>(int id, bool create) where T : ToolWindowPane
        {
            return _package.FindToolWindow(typeof(T), id, create) as T;
        }

        private T GetToolWindow<T>(int id = 0) where T : ToolWindowPane
        {
            return FindToolWindow<T>(id, true);
        }

        private readonly object _lock = new object();
        public T FindSessionWindow<T>(bool create) where T : ToolWindowPane
        {
            lock (_lock)
            {
                for (int i = 0; i < 100; i++)
                {
                    T window = FindToolWindow<T>(i, false);
                    if (window == null)
                    {
                        if (create)
                        {
                            window = FindToolWindow<T>(i, true);
                            if (window?.Frame == null)
                            {
                                ShowError("Cannot create a profiling session window");
                                return null;
                            }
                        }
                        return window;
                    }
                }
                ShowError("Too many open sessions");
                return null;
            }
        }

        /// <summary>
        /// Open a %Core %Profiler profiling session (asynchronously).
        /// </summary>
        /// <param name="sessionPath">A path to a profiling session</param>
        /// <param name="callBack">
        /// A callback called after the specified profiling session has been opened (or an error has occurred)
        /// </param>
        public void ShowSession(string sessionPath, Action callBack)
        {
            ShowSession<ActiveSession, SessionWindow>(
                () => new ActiveSession(new Session.Session(sessionPath)),
                callBack);
        }

        /// <summary>
        /// Open a %Core %Profiler memory profiling session (asynchronously).
        /// </summary>
        /// <param name="sessionPath">A path to a memory profiling session</param>
        /// <param name="callBack">
        /// A callback called after the specified memory profiling session has been opened (or an error has occurred)
        /// </param>
        public void ShowMemorySession(string sessionPath, Action callBack)
        {
            ShowSession<MemoryProfilingSession, MemoryProfilingSessionWindow>(
                () => new MemoryProfilingSession(sessionPath),
                callBack);
        }

        private void ShowSession<TSession, TSessionWindow>(Func<TSession> sessionCreator, Action callBack)
            where TSessionWindow : ToolWindowPane, ISessionWindow
        {
            System.Threading.Tasks.Task.Run(delegate ()
            {
                try
                {
                    TSession session = sessionCreator();
                    Application.Current.Dispatcher.InvokeAsync(delegate ()
                    {
                        try
                        {
                            TSessionWindow window = FindSessionWindow<TSessionWindow>(true);
                            if (window != null)
                            {
                                window.ShowSession(session);
                            }
                        }
                        finally
                        {
                            callBack();
                        }
                    });
                }
                catch (Exception ex)
                {
                    callBack();
                    ShowError(ex.Message);
                }
            });
        }

        private object GetService(Type serviceType)
        {
            return ((IServiceProvider)_package).GetService(serviceType);
        }

        private IVsUIShell5 GetIVsUIShell5()
        {
            IVsUIShell5 shell5 = null;

            Microsoft.VisualStudio.OLE.Interop.IServiceProvider sp = OLEServiceProvider;
            Type SVsUIShellType = typeof(SVsUIShell);
            IntPtr serviceIntPtr;

            int hr = sp.QueryService(SVsUIShellType.GUID, SVsUIShellType.GUID, out serviceIntPtr);
            if (hr != 0)
            {
                System.Runtime.InteropServices.Marshal.ThrowExceptionForHR(hr);
            }
            else if (!serviceIntPtr.Equals(IntPtr.Zero))
            {
                object serviceObject = System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(serviceIntPtr);

                shell5 = (IVsUIShell5)serviceObject;

                System.Runtime.InteropServices.Marshal.Release(serviceIntPtr);
            }

            return shell5;
        }
    }
}
