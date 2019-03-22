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
using System.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;
using NetCore.Profiler.Extension.Launcher.Model;
using NetCore.Profiler.Extension.VSPackage;
using Tizen.VisualStudio.Tools.DebugBridge;

namespace NetCore.Profiler.Extension.Launcher
{
    /// <summary>
    /// A singleton launcher for %Core %Profiler sessions used by <see cref="ProfilerPlugin">.
    /// </summary>
    public class ProfileLauncher
    {
        private ProfileSession _currentSession;

        private ProfileLauncher()
        {
        }

        public static ProfileLauncher Instance { get; private set; }

        public static void Initialize()
        {
            Instance = new ProfileLauncher();
        }

        public bool SessionActive => (_currentSession != null);

        public ProfileSession CreateSession(SDBDeviceInfo device, ProfileSessionConfiguration sessionConfiguration,
            bool isLiveProfiling)
        {
            string details;
            if (ProfilerPlugin.Instance.BuildSolution())
            {
                try
                {
                    return new ProfileSession(device, sessionConfiguration, isLiveProfiling);
                }
                catch (Exception ex)
                {
                    details = ex.Message;
                }
            }
            else
            {
                details = "Solution build failed";
            }
            string errMsg = $"Cannot start profiling session. {details}";
            ProfilerPlugin.Instance.WriteToOutput(errMsg);
            ProfilerPlugin.Instance.ShowError(errMsg);
            return null;
        }

        public void StartSession(ProfileSession session)
        {
            if (SessionActive)
            {
                throw new InvalidOperationException();
            }

            _currentSession = session;
            _currentSession.AddListener(new ProfileSessionListener { OnStateChanged = StateChangedHandler });
            _currentSession.Start();
        }

        private void StateChangedHandler(ProfileSessionState newState)
        {
            if (newState == ProfileSessionState.Finished || newState == ProfileSessionState.Failed)
            {
                ProfileSession s = _currentSession;
                _currentSession = null;
                if (s != null)
                {
                    s.Destroy();
                }
            }
        }

        public void OnModeChange(DBGMODE dbgmodeLast, DBGMODE dbgmodeNew)
        {
            Debug.WriteLine(String.Format($"{GetType().Name}.OnModeChange: {dbgmodeLast}=>{dbgmodeNew}"));

            ProfileSession session = _currentSession;
            if (session != null)
            {
                switch (dbgmodeNew)
                {
                    case DBGMODE.DBGMODE_Break:
                        session.OnDebugStateChanged(true);
                        break;
                    case DBGMODE.DBGMODE_Run:
                        session.OnDebugStateChanged(false);
                        break;
                }
            }
        }
    }
}
