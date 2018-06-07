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
using NetCore.Profiler.Cperf.Core.Model;
using NetCore.Profiler.Extension.Launcher.Model;
using NetCore.Profiler.Extension.Session;
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

        private IProfileSession _session;

        private readonly ProfileSessionListener _listener;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfilingProgressWindow"/> class.
        /// </summary>
        public ProfilingProgressWindow() : base(null)
        {
            _listener = new ProfileSessionListener(delegate(ProfileSessionState state)
            {
                switch (state)
                {
                    case ProfileSessionState.Failed:
                    case ProfileSessionState.Finished:
                        _session.RemoveListener(_listener);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ((ProfilingProgressWindowContent)Content).ClearSession();
                            ((IVsWindowFrame)Frame).Hide();
                            if (state == ProfileSessionState.Finished)
                            {
                                ProfilerPlugin.Instance.SessionsContainer.Update();
                            }
                        });
                        break;
                }

            });

            Caption = "Profiling Progress";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.

            // ReSharper disable once VirtualMemberCallInConstructor
            Content = new ProfilingProgressWindowContent();
        }


        public void StartSession(IProfileSession session)
        {
            _session = session;
            session.AddListener(_listener);
            ((IVsWindowFrame)Frame).Show();
            ((ProfilingProgressWindowContent)Content).SetSession(session);
            ProfilerPlugin.Instance.ProfileLauncher.StartSession(session);

        }

        public void Show()
        {
            (Frame as IVsWindowFrame)?.Show();
        }

        public void Hide()
        {
            (Frame as IVsWindowFrame)?.Hide();
        }


        private class ProfileSessionListener : IProfileSessionListener
        {
            internal delegate void StateChangedEventHandler(ProfileSessionState newState);

            private readonly StateChangedEventHandler _stateHandler;

            public ProfileSessionListener(StateChangedEventHandler handler)
            {
                _stateHandler = handler;
            }

            public void StateChanged(ProfileSessionState newState)
            {
                _stateHandler(newState);
            }

            public void SysInfoRead(SysInfoItem siItem)
            {
            }
        }

    }
}
