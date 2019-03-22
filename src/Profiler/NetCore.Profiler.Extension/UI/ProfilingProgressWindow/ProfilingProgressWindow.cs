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

using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NetCore.Profiler.Extension.Launcher.Model;
using NetCore.Profiler.Extension.VSPackage;

namespace NetCore.Profiler.Extension.UI.ProfilingProgressWindow
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("b2a1d943-c8fa-46d1-b5b2-cf362c8f0672")]
    public class ProfilingProgressWindow : ToolWindowPane
    {
        private ProfileSession _session;

        private readonly ProfileSessionListener _listener;

        private volatile ProfileSessionState _profileSessionState;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfilingProgressWindow"/> class.
        /// </summary>
        public ProfilingProgressWindow() : base(null)
        {
            _listener = new ProfileSessionListener { OnStateChanged = StateChangedHandler };

            Caption = "Profiling Progress";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.

            // ReSharper disable once VirtualMemberCallInConstructor
            Content = new ProfilingProgressWindowContent();
        }

        private void StateChangedHandler(ProfileSessionState newState)
        {
            _profileSessionState = newState;

            switch (newState)
            {
                case ProfileSessionState.Running:
                    Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
                    {
                        // sometimes the window is not visible after Show in SetSession
                        System.Threading.Thread.Sleep(2000);
                        if (_profileSessionState == ProfileSessionState.Running) // still running?
                        {
                            Show();
                        }
                    }));
                    break;

                case ProfileSessionState.Failed:
                case ProfileSessionState.Finished:
                    _session.RemoveListener(_listener);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // hide the window and clear the session
                        Hide();
                        ((ProfilingProgressWindowContent)Content).ClearSession();
                        if (newState == ProfileSessionState.Finished)
                        {
                            ProfilerPlugin.Instance.SessionsContainer.Update();
                        }
                    });
                    break;
            }
        }

        public void SetSession(ProfileSession session)
        {
            _session = session;
            Show();
            ((ProfilingProgressWindowContent)Content).SetSession(session);
            // listener shall be added after SetSession to allow ProfilingProgressWindowContent processing events first
            session.AddListener(_listener);
            Show(); // why double Show: trying to fix the problem - window not visible
        }

        public void Show()
        {
            (Frame as IVsWindowFrame)?.Show();
        }

        public void Hide()
        {
            (Frame as IVsWindowFrame)?.Hide();
        }
    }
}
