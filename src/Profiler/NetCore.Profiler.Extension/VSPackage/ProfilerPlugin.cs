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
using Application = System.Windows.Application;
using RunProfilerDialog = NetCore.Profiler.Extension.UI.RunProfilerDialog;
using Thread = System.Threading.Thread;
using Tizen.VisualStudio.Tools.DebugBridge;

namespace NetCore.Profiler.Extension.VSPackage
{
    public sealed class ProfilerPlugin
    {
        public enum MessageType
        {
            Debug,
            Info,
            Warning,
            Error,
            Question
        }

        public const string SettingsCollectionPath = "CoreClrProfiler";

        public static ProfilerPlugin Instance { get; private set; }

        private Package _package;

        private IVsOutputWindowPane _outputPaneTizen;

        public Guid OutputPaneTizenGuid = new Guid("CFEE7FDA-3167-422A-971C-6C7BD17DD1AD");

        public ProfileLauncher ProfileLauncher { get; private set; }

        public GeneralOptions GeneralOptions { get; private set; }


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

        public static void Initialize(Package package, IVsOutputWindowPane outputPaneTizen)
        {
            Instance = new ProfilerPlugin(package, outputPaneTizen);
        }

        private ProfilerPlugin(Package package, IVsOutputWindowPane outputPaneTizen)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _outputPaneTizen = outputPaneTizen;

            OLEServiceProvider =
                GetService(typeof(Microsoft.VisualStudio.OLE.Interop.IServiceProvider)) as
                    Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

            VsUiShell5 = GetIVsUIShell5();

            ExplorerWindowCommand.Initialize(_package);

            GeneralOptions = new GeneralOptions(new SettingsStore(_package, SettingsCollectionPath));

            RunProfilerCommand.Initialize(_package);

            ProfileLauncher.Initialize();
            ProfileLauncher = ProfileLauncher.Instance;

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

        public void WriteToOutput(string message)
        {
            _outputPaneTizen?.OutputString(message + "\n");
        }

        public Project GetStartupProject()
        {
            try
            {
                DTE2 dte2 = (DTE2)Package.GetGlobalService(typeof(SDTE));
                string startPrjName = string.Empty;
                Property property = dte2.Solution.Properties.Item("StartupProject");
                Projects prjs = dte2.Solution.Projects;
                Project startPrj = null;

                if (property != null)
                {
                    startPrjName = (string)property.Value;
                }

                foreach (Project prj in prjs)
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

        public void StartProfiler()
        {
            if (ProfileLauncher.SessionActive)
            {
                ShowError("Run Profiler Error", "Another Session is active");
                return;
            }

            var project = GetStartupProject();
            if (project == null)
            {
                ShowError("Run Profiler Error", "No active project");
                return;
            }

            var sessionConfiguration = new ProfileSessionConfiguration(project, GeneralOptions);

            var dlg = new RunProfilerDialog(sessionConfiguration);
            dlg.ShowDialog();
            if (dlg.DialogResult ?? false)
            {
                var session = ProfileLauncher.CreateSession(sessionConfiguration);
                ProfilingProgressWindow.StartSession(session);
            }
        }

        public void ShowError(string title, string message)
        {
            ShowMessage(MessageType.Error, title, message);
        }

        public int ShowMessage(MessageType messageType, string title, string message)
        {
            if (title == null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            OLEMSGICON icon;
            OLEMSGBUTTON button;

            switch (messageType)
            {
                case MessageType.Debug:
                    icon = OLEMSGICON.OLEMSGICON_NOICON;
                    button = OLEMSGBUTTON.OLEMSGBUTTON_OK;
                    break;
                case MessageType.Info:
                    icon = OLEMSGICON.OLEMSGICON_INFO;
                    button = OLEMSGBUTTON.OLEMSGBUTTON_OK;
                    break;
                case MessageType.Warning:
                    icon = OLEMSGICON.OLEMSGICON_WARNING;
                    button = OLEMSGBUTTON.OLEMSGBUTTON_OK;
                    break;
                case MessageType.Error:
                    icon = OLEMSGICON.OLEMSGICON_CRITICAL;
                    button = OLEMSGBUTTON.OLEMSGBUTTON_OK;
                    break;
                case MessageType.Question:
                    icon = OLEMSGICON.OLEMSGICON_QUERY;
                    button = OLEMSGBUTTON.OLEMSGBUTTON_YESNO;
                    break;
                default:
                    icon = OLEMSGICON.OLEMSGICON_NOICON;
                    button = OLEMSGBUTTON.OLEMSGBUTTON_OK;
                    break;
            }

            return VsShellUtilities.ShowMessageBox(
                _package,
                message,
                title,
                icon,
                button,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
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
                ShowMessage(MessageType.Warning, "File", "Source File is missing.");
                return;
            }

            if (!File.Exists(Path.GetFullPath(path)))
            {
                if (ShowMessage(MessageType.Question, "Source File not found.", "Would you like to locate it yourself") == 6) // Yes
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

        public void ShowSession(string sessionPath, Action callBack)
        {
            new Thread(() =>
            {
                try
                {
                    var session = new Session.Session();
                    session.Initialize(sessionPath);
                    session.Load();
                    Application.Current.Dispatcher.InvokeAsync(delegate()
                    {
                        var window = FindSessionWindow();
                        if (window != null)
                        {
                            window.SetActiveSession(new ActiveSession(session));
                            window.Show();
                        }
                    });
                }
                catch (Exception ex)
                {
                    ShowError("Error", ex.Message);
                }

                callBack();
            }).Start();
        }

        private readonly object _lock = new object();
        private SessionWindow FindSessionWindow()
        {
            lock (_lock)
            {
                int i;
                for (i = 0; i < 100; i++)
                {
                    var window = (SessionWindow)FindToolWindow(typeof(SessionWindow), i, false);
                    if (window == null)
                    {
                        window = (SessionWindow)FindToolWindow(typeof(SessionWindow), i, true);
                        if (window?.Frame == null)
                        {
                            ShowError("Error", "Cannot create tool window");
                            return null;
                        }

                        return window;
                    }
                }

                ShowError("Error", "Too many open sessions");
                return null;
            }
        }


        public void ShowMemorySession(string sessionPath, Action callBack)
        {
            new Thread(() =>
            {
                try
                {
                    var session = new MemoryProfilingSession();
                    session.Initialize(sessionPath);
                    session.Load();
                    Application.Current.Dispatcher.InvokeAsync(delegate()
                    {
                        var window = FindMemorySessionWindow();
                        if (window != null)
                        {
                            window.SetActiveSession(session);
                            window.Show();
                        }
                    });
                }
                catch (Exception ex)
                {
                    ShowError("Error", ex.Message);
                }

                callBack();
            }).Start();
        }

        private MemoryProfilingSessionWindow FindMemorySessionWindow()
        {
            lock (_lock)
            {
                int i;
                for (i = 0; i < 100; i++)
                {
                    var window = (MemoryProfilingSessionWindow)FindToolWindow(typeof(MemoryProfilingSessionWindow), i, false);
                    if (window == null)
                    {
                        window = (MemoryProfilingSessionWindow)FindToolWindow(typeof(MemoryProfilingSessionWindow), i, true);
                        if (window?.Frame == null)
                        {
                            ShowError("Error", "Cannot create tool window");
                            return null;
                        }

                        return window;
                    }
                }

                ShowError("Error", "Too many open sessions");
                return null;
            }
        }


        private T GetToolWindow<T>(int id = 0) where T : ToolWindowPane
        {
            return FindToolWindow(typeof(T), id, true) as T;
        }

        private ToolWindowPane FindToolWindow(Type toolWindowType, int id, bool create)
        {
            return _package.FindToolWindow(toolWindowType, id, create);
        }

        private object GetService(Type serviceType)
        {
            return ((IServiceProvider)_package).GetService(serviceType);
        }

        private IVsUIShell5 GetIVsUIShell5()
        {

            Microsoft.VisualStudio.OLE.Interop.IServiceProvider sp = null;
            Type SVsUIShellType = null;
            int hr = 0;
            IntPtr serviceIntPtr;
            Microsoft.VisualStudio.Shell.Interop.IVsUIShell5 shell5 = null;
            object serviceObject = null;

            sp = OLEServiceProvider;

            SVsUIShellType = typeof(Microsoft.VisualStudio.Shell.Interop.SVsUIShell);

            hr = sp.QueryService(SVsUIShellType.GUID, SVsUIShellType.GUID, out serviceIntPtr);

            if (hr != 0)
            {
                System.Runtime.InteropServices.Marshal.ThrowExceptionForHR(hr);
            }
            else if (!serviceIntPtr.Equals(IntPtr.Zero))
            {
                serviceObject = System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(serviceIntPtr);

                shell5 = (Microsoft.VisualStudio.Shell.Interop.IVsUIShell5)serviceObject;

                System.Runtime.InteropServices.Marshal.Release(serviceIntPtr);
            }

            return shell5;
        }

    }
}
