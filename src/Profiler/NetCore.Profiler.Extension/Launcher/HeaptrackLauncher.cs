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
using System.Threading.Tasks;
using NetCore.Profiler.Cperf.Core.Model;
using NetCore.Profiler.Extension.Launcher.Model;
using NetCore.Profiler.Extension.VSPackage;
using Tizen.VisualStudio.Tools.DebugBridge;

namespace NetCore.Profiler.Extension.Launcher
{
    /// <summary>
    /// A singleton launcher for memory profiling (heaptrack) sessions used by <see cref="ProfilerPlugin"/>.
    /// </summary>
    public class HeaptrackLauncher
    {
        private HeaptrackSession _currentSession;

        private HeaptrackLauncher()
        {
        }

        public event EventHandler OnSessionFinished;

        public static HeaptrackLauncher Instance { get; private set; }

        public static void Initialize()
        {
            Instance = new HeaptrackLauncher();
        }

        public bool SessionActive => _currentSession != null;

        public HeaptrackSession CreateSession(SDBDeviceInfo device, HeaptrackSessionConfiguration sessionConfiguration)
        {
            string details;
            if (ProfilerPlugin.Instance.BuildSolution())
            {
                try
                {
                    return new HeaptrackSession(device, sessionConfiguration);
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
            string errMsg = $"Cannot start memory profiling session. {details}";
            ProfilerPlugin.Instance.WriteToOutput(errMsg);
            ProfilerPlugin.Instance.ShowError(errMsg);
            return null;
        }

        public void StartSession(HeaptrackSession session)
        {
            if (SessionActive)
            {
                throw new InvalidOperationException();
            }

            _currentSession = session;
            _currentSession.AddListener(new HeaptrackSessionListener { OnStateChanged = StateChangedEventHandler });
            _currentSession.Start();
        }

        private void StateChangedEventHandler(HeaptrackSessionState newState)
        {
            if (newState == HeaptrackSessionState.Finished || newState == HeaptrackSessionState.Failed)
            {
                if (newState == HeaptrackSessionState.Finished)
                {
                    Task.Run(() => OnSessionFinished?.Invoke(this, new EventArgs()));
                }

                HeaptrackSession s = _currentSession;
                _currentSession = null;
                if (s != null)
                {
                    s.Destroy();
                }
            }
        }
    }
}
