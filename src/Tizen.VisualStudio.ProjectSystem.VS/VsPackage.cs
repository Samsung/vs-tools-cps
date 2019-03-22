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

using NetCore.Profiler.Extension.UI.MemoryProfilingSessionWindow;

namespace Tizen.VisualStudio
{
    using System;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using EnvDTE;
    using EnvDTE80;
    using Tizen.VisualStudio.APIChecker;
    using Tizen.VisualStudio.DebugBridge;
    using Tizen.VisualStudio.LogViewer;
    using Tizen.VisualStudio.ManifestEditor;
    using Tizen.VisualStudio.ResourceManager;
    using Tizen.VisualStudio.Tools.Data;
    using Tizen.VisualStudio.Tools.DebugBridge;
    using Tizen.VisualStudio.Utilities;
    using NetCore.Profiler.Extension.VSPackage;
    using NetCore.Profiler.Extension.UI.AdornedSourceWindow;
    using NetCore.Profiler.Extension.UI.ProfilingProgressWindow;
    using NetCore.Profiler.Extension.UI.SessionExplorer;
    using NetCore.Profiler.Extension.UI.SessionWindow;

    /// <summary>
    /// This class implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// This package is required if you want to define adds custom commands (ctmenu)
    /// or localized resources for the strings that appear in the New Project and Open Project dialogs.
    /// Creating project extensions or project types does not actually require a VSPackage.
    /// </remarks>
    [ProvideAutoLoad(ActivationContextGuid)]
    [ProvideUIContextRule(ActivationContextGuid,
        name: "Load Tizen Project Package",
        expression: "TizenNET | TizenNative",
        termNames: new[] { "TizenNET", "TizenNative" },
        termValues: new[] { "SolutionHasProjectCapability: Tizen.NET & CSharp & CPS", "SolutionHasProjectCapability: TizenNative" })]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [Description("Tizen project type")]
    [Guid(VsPackage.PackageGuid)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(Tizen.VisualStudio.OptionPages.Certificate), "Tizen", "Certification", 0, 0, true)]
    [ProvideOptionPage(typeof(Tizen.VisualStudio.ToolsOption.TizenOptionPageViewModel), "Tizen", "Tools", 0, 0, true)]

    [ProvideToolWindow(typeof(Tizen.VisualStudio.LogViewer.LogViewer))]
    [ProvideToolWindow(typeof(Tizen.VisualStudio.ResourceManager.ResourceManager))]
    [ProvideToolWindow(typeof(SessionExplorerWindow), Style = VsDockStyle.Tabbed, Window = ToolWindowGuids.SolutionExplorer)]
    [ProvideToolWindow(typeof(ProfilingProgressWindow), Style = VsDockStyle.Tabbed, Window = ToolWindowGuids.SolutionExplorer)]
    [ProvideToolWindow(typeof(SessionWindow), Transient = true, Style = VsDockStyle.MDI, MultiInstances = true)]
    [ProvideToolWindow(typeof(HotLinesToolWindow), Transient = true, Style = VsDockStyle.MDI, MultiInstances = true)]
    [ProvideToolWindow(typeof(MemoryProfilingSessionWindow), Transient = true, Style = VsDockStyle.MDI, MultiInstances = true)]

    #region ManifestEditor
    [ProvideXmlEditorChooserDesignerView("TizenManifest", "xml", LogicalViewID.Designer, 0x60,
                                         DesignerLogicalViewEditor = typeof(ManifestEditorFactory),
                                         Namespace = "http://tizen.org/ns/packages",
                                         MatchExtensionAndNamespace = false)]
    [ProvideEditorExtension(typeof(ManifestEditorFactory), ManifestEditorFactory.Extension, 0x40, NameResourceID = 106)]
    // We register that our editor supports LOGVIEWID_Designer logical view
    // [ProvideEditorLogicalView(typeof(ManifestEditorFactory), LogicalViewID.Designer)]
    //[EditorFactoryNotifyForProject("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", ManifestEditorFactory.Extension, @"{32CC8DFA-2D70-49b2-94CD-22D57349B778}")]
    #endregion

    public sealed class VsPackage : Package, IVsEventsHandler, IVsDebuggerEvents//AsyncPackage
    {
        public const string ActivationContextGuid = "9BF1CB95-137C-4489-A80D-B3399395318F";
        /// <summary>
        /// The GUID for this package.
        /// </summary>
        public const string PackageGuid = "f6276019-8d92-4a6e-8db3-ab820a4f0ab6";

        /// <summary>
        /// The GUID for this project type.  It is unique with the project file extension and
        /// appears under the VS registry hive's Projects key.
        /// </summary>
        public const string ProjectTypeGuid = "ba2e1a58-11e7-41aa-8cc2-7407d2f01588";

        /// <summary>
        /// The file extension of this project type.  No preceding period.
        /// </summary>
        public const string ProjectExtension = "csproj";

        /// <summary>
        /// The default namespace this project compiles with, so that manifest
        /// resource names can be calculated for embedded resources.
        /// </summary>
        internal const string DefaultNamespace = "Tizen.VisualStudio";

        // OutputPane for Tizen
        public const string OutputPaneTizenGuidString =
            "CFEE7FDA-3167-422A-971C-6C7BD17DD1AD";
        public Guid OutputPaneTizenGuid = new Guid(OutputPaneTizenGuidString);

        public static IVsOutputWindowPane outputPaneTizen = null;
        public static IVsThreadedWaitDialogFactory dialogFactory = null;
        private CommandEvents CEvents = null;

        private IVsDebugger monitoredDebugger = null;
        private uint monitorCookie = 0;

        private static VsPackage instance;

        protected override void Initialize() //async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            base.Initialize();

            // TODO : Can remove?
            VsProjectHelper.Initialize();

            //await PrepareWindowsAsync();
            PrepareWidnows();

            VsEvents.Initialize(this as IVsEventsHandler, GetService(typeof(SVsSolution)) as IVsSolution);

            DeviceManager.Initialize(VsPackage.outputPaneTizen);

            DeviceManager.ResetDeviceMonitorRetry();
            DeviceManager.StartDeviceMonitor();

            TizenPackageTracer.Initialize();

            PrepareToolsWindows();
            StartDebuggerMonitoring();

            base.RegisterEditorFactory(new ManifestEditorFactory(this));
            APICheckerCommand.Initialize(this, VsPackage.outputPaneTizen);

            string guidVSstd97 = "{5efc7975-14bc-11cf-9b2b-00aa00573819}".ToUpper();
            int cmdidStartupPrj = 246;
            DTE2 dte2 = GetService(typeof(SDTE)) as DTE2;
            CEvents = dte2.Events.CommandEvents[guidVSstd97, cmdidStartupPrj];
            CEvents.AfterExecute += SetStartup_AfterExecute;

            ProfilerPlugin.Initialize(this, outputPaneTizen, dialogFactory);

            instance = this;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            instance = null;
        }

        public static int ShowMessage(MessageDialogType messageType, string title, string message)
        {
            var vsPackage = instance;
            if (vsPackage != null)
            {
                return ShellHelper.ShowMessage(vsPackage, messageType, title, message);
            }
            if (messageType != MessageDialogType.Question)
            {
                MessageBox.Show(message, title);
            }
            return -1;
        }

        protected override int QueryClose(out bool canClose)
        {
            int hr = VSConstants.S_OK;
            canClose = true;

            try
            {
                if (monitoredDebugger != null)
                {
                    if (monitorCookie != 0)
                    {
                        hr = monitoredDebugger.UnadviseDebuggerEvents(monitorCookie);
                        ErrorHandler.ThrowOnFailure(hr);
                        monitorCookie = 0;
                    }
                }

                hr = base.QueryClose(out canClose);
                ErrorHandler.ThrowOnFailure(hr);
            }
            catch
            {
            }

            return hr;
        }

        private void PrepareToolsWindows()
        {
            LogViewerCommand.Initialize(this);
            ResourceManagerCommand.Initialize(this);
        }

        private void PrepareWidnows() //async System.Threading.Tasks.Task PrepareWindowsAsync()
        {
            //await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IServiceProvider serviceProvider = this as IServiceProvider;
            IVsOutputWindow outputWindow = serviceProvider.GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            IVsOutputWindowPane pane;
            outputWindow.CreatePane(ref OutputPaneTizenGuid, "Tizen", 1, 1);
            outputWindow.GetPane(ref OutputPaneTizenGuid, out pane);
            VsPackage.outputPaneTizen = pane;

            IVsThreadedWaitDialogFactory dialogFactory;
            dialogFactory = GetService(typeof(SVsThreadedWaitDialogFactory)) as IVsThreadedWaitDialogFactory;
            VsPackage.dialogFactory = dialogFactory;

            ToolsMenu.Initialize(this);

            OptionPages.Tools.Initialize(this);
            ToolsOption.TizenOptionPageViewModel.Initialize(this);
            OptionPages.Certificate.Initialize(this);
        }

        private IVsDebugger StartDebuggerMonitoring()
        {
            DBGMODE[] modeArray = new DBGMODE[1];

            monitoredDebugger = GetService(typeof(SVsShellDebugger)) as IVsDebugger;

            if (monitoredDebugger != null)
            {
                int hr = monitoredDebugger.AdviseDebuggerEvents(this, out monitorCookie);
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(hr);

                hr = monitoredDebugger.GetMode(modeArray);
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(hr);
            }

            return monitoredDebugger;
        }

        public Events GetDTEEvents()
        {
            DTE2 dte2 = GetService(typeof(SDTE)) as DTE2;
            return dte2.Events;
        }

        public void OnVsEventSolutionBeforeClose()
        {
            ResourceManagerLauncher launcher = ResourceManagerLauncher.getInstance();
            launcher.CloseAllResourceManagerWindow(this);
        }

        public void OnVsEventBeforeCloseProject(Project project, int fRemoved)
        {
            if(project != null)
            {
                ResourceManagerLauncher launcher = ResourceManagerLauncher.getInstance();
                launcher.CloseProjectResourceManagerWindow(this, project);
            }
        }

        void IVsEventsHandler.OnVsEventBuildProjectDone(string Project, string ProjectConfig, string Platform,
            string SolutionConfig, bool Success, bool scdproperty, string arch)
        {
            Project currentProject = VsProjectHelper.GetInstance.GetCurrentProjectFromUniqueName(Project);
            // Create res.xml file while packaging
            if (currentProject != null && VsProjectHelper.GetInstance.IsHaveTizenManifest(currentProject))
            {
                XmlWriter.updateResourceXML(currentProject);
            }
        }

        void SetStartup_AfterExecute(string Guid, int ID, object CustomIn, object CustomOut)
        {
            DeviceManager.UpdateDebugTargetList(false);
            //LogViewerControl.CreateLogTab();
        }

        public int OnModeChange(DBGMODE dbgmodeNew)
        {
            switch (dbgmodeNew)
            {
                case DBGMODE.DBGMODE_Run:
                    {
                        // hide Profiling Progress window if not in live profiler mode
                        if (!DebuggerInfo.UseLiveProfiler &&
                            (ProfilerPlugin.Instance.DebugMode == DBGMODE.DBGMODE_Design))
                        {
                            Action closeProfilingProgressWindow = () =>
                            {
                                System.Windows.Application.Current.Dispatcher.Invoke(delegate ()
                                {
                                    var profilingProgressWindow = ProfilerPlugin.Instance.FindToolWindow<ProfilingProgressWindow>(0, false);
                                    if (profilingProgressWindow != null)
                                    {
                                        ((IVsWindowFrame)(profilingProgressWindow.Frame)).CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
                                    }
                                });
                            };
                            System.Threading.Tasks.Task.Run(() =>
                            {
                                closeProfilingProgressWindow();
                                System.Threading.Thread.Sleep(200); // we are in situation of races so try again after a small delay
                                closeProfilingProgressWindow();
                            });
                        }
                    }
                    break;
                case DBGMODE.DBGMODE_Design:
                    TizenPackageTracer.Instance.Clear();
                    break;
                default:
                    break;
            }

            ProfilerPlugin.Instance.OnModeChange(dbgmodeNew);

            return VSConstants.S_OK;
        }
    }
}
