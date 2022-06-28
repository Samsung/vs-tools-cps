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
    using Tizen.VisualStudio.Command;
    using NetCore.Profiler.Extension.VSPackage;
    using NetCore.Profiler.Extension.UI.AdornedSourceWindow;
    using NetCore.Profiler.Extension.UI.ProfilingProgressWindow;
    using NetCore.Profiler.Extension.UI.SessionExplorer;
    using NetCore.Profiler.Extension.UI.SessionWindow;

    using System.Threading;
    using System.Threading.Tasks;
    using System.Runtime.CompilerServices;
    using System.IO;
    using Microsoft.VisualStudio.Threading;
    using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
    using Task = System.Threading.Tasks.Task;
    using System.Xml.Linq;
    using Tizen.VisualStudio.ProjectSystem.VS.Debug;
    using Tizen.VisualStudio.Tidl;
    using Tizen.VisualStudio.Workload;

    /// <summary>
    /// This class implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// This package is required if you want to define adds custom commands (ctmenu)
    /// or localized resources for the strings that appear in the New Project and Open Project dialogs.
    /// Creating project extensions or project types does not actually require a VSPackage.
    /// </remarks>
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(ActivationContextGuid, PackageAutoLoadFlags.BackgroundLoad)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideUIContextRule(ActivationContextGuid,
        name: "Load Tizen Project Package",
        expression: "TizenNET | TizenNative",
        termNames: new[] { "TizenNET", "TizenNative" },
        termValues: new[] { "SolutionHasProjectCapability: Tizen.NET & CSharp & CPS", "SolutionHasProjectCapability: TizenNative" })]
    [Description("Tizen project type")]
    [Guid(VsPackage.PackageGuid)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(Tizen.VisualStudio.OptionPages.Certificate), "Tizen", "Certification", 0, 0, true)]
    [ProvideOptionPage(typeof(Tizen.VisualStudio.ToolsOption.TizenOptionPageViewModel), "Tizen", "Tools", 0, 0, true)]
    [ProvideOptionPage(typeof(Tizen.VisualStudio.OptionPages.Tidl), "Tizen", "TIDL", 0, 0, true)]

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

    public sealed class VsPackage : AsyncPackage, IVsEventsHandler, IVsDebuggerEvents
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
        private SolutionEvents SEvents = null;
        private DocumentEvents DocumentEvents = null;
        private Solution solution = null;
        private IVsSolution solutionIV = null;
        private IVsDebugger monitoredDebugger = null;
        private uint monitorCookie = 0;

        private static VsPackage instance;
        private IVsMonitorSelection MonitorSelection = null;

        private DTE dte = null;

        public bool ErrorReporting { get; private set; }

        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);

            // TODO : Can remove?
            VsProjectHelper.Initialize();

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            //await PrepareWindowsAsync();
            PrepareWidnows();

            //Install Workload in async mode
            WorkloadInstaller installer = WorkloadInstaller.GetInstance();
            var workloadTask =  Task.Run(() => installer?.InstallWorkload());

            //Load Template lists in async mode
            VsProjectHelper projectHelper = VsProjectHelper.GetInstance;
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            projectHelper.TemplateTaskSource = source;
            projectHelper.TemplateTask = Task.Run(() => projectHelper.LoadTemplatesAsync(), token);

            MonitorSelection = await GetServiceAsync(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;

            VsEvents.Initialize(this as IVsEventsHandler, await GetServiceAsync(typeof(SVsSolution)) as IVsSolution, MonitorSelection);

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
            DTE2 dte2 = await GetServiceAsync(typeof(SDTE)) as DTE2;
            CEvents = dte2.Events.CommandEvents[guidVSstd97, cmdidStartupPrj];
            CEvents.AfterExecute += SetStartup_AfterExecute;

            SEvents = dte2.Events.SolutionEvents;
            DocumentEvents = dte2.Events.DocumentEvents;

            solution = dte2.Application.Solution;

            solutionIV = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;
            SEvents.ProjectAdded += OnProjectAdded;
            SEvents.Opened += OnSolutionOpen;
            SEvents.AfterClosing += OnSolutionClose;
            SEvents.BeforeClosing += BeforeSolutionClose;

            dte = await GetServiceAsync(typeof(SDTE)) as DTE;

            ProfilerPlugin.Initialize(this, outputPaneTizen, dialogFactory);

            instance = this;
        }

        private void OnProjectAdded(Project Project)
        {
            VsProjectHelper vsProjectHelper = VsProjectHelper.GetInstance;
            if (vsProjectHelper.IsHaveTizenManifest(Project) && Project.Kind == vsProjectHelper.CPPBasedProject)
                vsProjectHelper.RemoveActiveDebuggerEntry(Project);
        }

        private void BeforeSolutionClose()
        {
            // ToDo : remove this workaround with proper fix
            VsProjectHelper.Initialize();
            VsProjectHelper vsProjectHelper = VsProjectHelper.GetInstance;
            if (vsProjectHelper.IsTizenNativeProject())
                dte.Properties["TextEditor", "C/C++ Specific"].Item("DisableErrorReporting").Value = ErrorReporting;
        }

        private void OnDocumentSave(Document Document)
        {
            if (!HotReloadLaunchProvider.IsHotreloadStarted())
                return;

            if (!solution.IsOpen)
                return;

            //Check for tizen project
            Projects ListOfProjectsInSolution = solution.Projects;

            var rootDir = Path.GetDirectoryName(Document.ProjectItem.ContainingProject.FullName);
            var projName = Document.ProjectItem.ContainingProject.Name;

            bool InOpenedSolution = false;

            foreach (Project project in ListOfProjectsInSolution)
            {
                if (project.Name.Equals(projName))
                {
                    InOpenedSolution = true;
                    break;
                }
            }

            if (!InOpenedSolution)
                return;

            HotReloadLaunchProvider.OnFileChanged(Document.FullName, rootDir);

        }

        private void OnSolutionClose()
        {
            DocumentEvents.DocumentSaved -= OnDocumentSave;
            HotReloadLaunchProvider.SetIsXamlProject(false);
        }

        private void OnSolutionOpen()
        {
            if (solution == null || !solution.IsOpen)
                return;

            //Check for tizen project
            Projects ListOfProjectsInSolution = solution.Projects;
            if (ListOfProjectsInSolution == null)
                return;
            bool solutionHasTizenProject = false;
            VsProjectHelper.Initialize();
            VsProjectHelper vsProjectHelper = VsProjectHelper.GetInstance;

            if (vsProjectHelper.IsTizenWebProject())
            {
                vsProjectHelper.UpdateYaml(vsProjectHelper.getSolutionFolderPath(), "chrome_path:", ToolsPathInfo.ChromePath);
                HotReloadLaunchProvider.SetIsXamlProject(false);
                return;
            }
            if (vsProjectHelper.IsTizenNativeProject())
            {
                //Workaround for Debugger Label not displayed
                vsProjectHelper.RemoveActiveDebuggerEntry();
                // ToDo : remove this workaround with proper fix
                ErrorReporting = (bool)dte.Properties["TextEditor", "C/C++ Specific"].Item("DisableErrorReporting").Value;
                dte.Properties["TextEditor", "C/C++ Specific"].Item("DisableErrorReporting").Value = true;
                HotReloadLaunchProvider.SetIsXamlProject(false);
                return;
            }
            foreach (Project project in ListOfProjectsInSolution)
            {
                 solutionHasTizenProject = vsProjectHelper.IsHaveTizenManifest(project);
                if (solutionHasTizenProject)
                    break;
            }
            if (!solutionHasTizenProject)
            {
                HotReloadLaunchProvider.SetIsXamlProject(false);
                return;
            }

            //Check for Xamarin project

            bool hasXamarinProject = false;

            var projectsEnumerator = ListOfProjectsInSolution.GetEnumerator();
            while (projectsEnumerator.MoveNext() && !hasXamarinProject)
            {
                var proj = projectsEnumerator.Current as Project;
                if (proj == null)
                {
                    continue;
                }

                if (proj.Kind == "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}")
                {
                    XDocument xmldoc = XDocument.Load(proj.FullName);
                    XDocument xd = xmldoc.Document;

                    foreach (XElement element in xd.Descendants("ProjectTypeGuids"))
                    {
                        // code here to identify values
                        var val = element.Value;

                        if (val != null && val.ToUpper().Contains("{B484D2DE-331C-4CA2-B931-2B4BDAD9945F}"))
                        {
                            hasXamarinProject = true;
                            break;
                        }
                    }

                    // This code is not working, check again
                    /* var hierarchy = vsProjectHelper.GetHierachyFromProjectUniqueName(proj.UniqueName);
                     IVsAggregatableProjectCorrected ap = hierarchy as IVsAggregatableProjectCorrected;
                     if (hierarchy is IVsAggregatableProjectCorrected aggregatableProjectCorrected)
                     {
                         aggregatableProjectCorrected.GetAggregateProjectTypeGuids(out var projTypeGuids);
                         if (projTypeGuids.ToUpper().Contains("{B484D2DE-331C-4CA2-B931-2B4BDAD9945F}"))
                         {
                             hasXamarinProject = true;
                             break;
                         }
                     }*/
                }
            }
            HotReloadLaunchProvider.SetIsXamlProject(hasXamarinProject);
            DocumentEvents.DocumentSaved += OnDocumentSave;

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
            TidlCommandOption.Initialize(this);
            WebSimulatorCommand.Initialize(this);
            AddTizenProjectCommand.Initialize(this);
            AddTizenDependencyCommand.Initialize(this);
            TizenSettingsCommand.Initialize(this);
            OptionPages.Tools.Initialize(this);
            OptionPages.Tidl.Initialize(this);
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
            if (project != null)
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
            // feature moved to build task tizen
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
                                        if (profilingProgressWindow.Frame != null)
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
                    {
                        TizenPackageTracer.Instance.Clear();
                        VsPackage.outputPaneTizen?.Activate();
                        VsPackage.outputPaneTizen?.OutputStringThreadSafe("debug stoped, stoppig observer \n");
                        ProjectSystem.VS.Debug.HotReloadLaunchProvider.StopHotReloadObserver();
                    }
                    break;
                default:
                    break;
            }

            ProfilerPlugin.Instance.OnModeChange(dbgmodeNew);

            return VSConstants.S_OK;
        }
    }
}
