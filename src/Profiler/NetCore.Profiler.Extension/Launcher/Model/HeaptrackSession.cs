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
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows;
using NetCore.Profiler.Common.Helpers;
using NetCore.Profiler.Extension.Session;
using NetCore.Profiler.Extension.VSPackage;
using NetCore.Profiler.Session.Core;
using Tizen.VisualStudio.Tools.DebugBridge;
using Tizen.VisualStudio.Utilities;

namespace NetCore.Profiler.Extension.Launcher.Model
{
    /// <summary>
    /// A class representing a running memory profiling (heaptrack) session.
    /// </summary>
    public class HeaptrackSession : AbstractSession<HeaptrackSessionListener, HeaptrackSessionState, HeaptrackSessionConfiguration>
    {
        private const string ProfctlLogName = "profctl_heaptrack.log";

        private const int ControlPort = 6005;

        private const int DataPort = 6006;

        private string _targetInstallationDirectory;

        private string _targetShareDirectory;

        private Stream _resFileStream;

        public HeaptrackSession(SDBDeviceInfo device, HeaptrackSessionConfiguration sessionConfiguration) :
            base(device, sessionConfiguration)
        {
            SetState(HeaptrackSessionState.Initial);
            _targetInstallationDirectory = _targetShareDirectory = $"{_sdkToolPath}/heaptrack";
        }

        internal void Start()
        {
            if (State != HeaptrackSessionState.Initial)
            {
                throw new InvalidOperationException();
            }

            DateTime startDateTime = DateTime.Now;

            if (!CheckConfiguration())
            {
                SetState(HeaptrackSessionState.Failed, true);
                return;
            }

            SetState(HeaptrackSessionState.Starting);

            ProfilerPlugin.Instance.ActivateTizenOutputPane();

            WriteToOutput("*** Memory profiling started ***");

            Task.Run(delegate ()
            {
                try
                {
                    if (RunHeaptrackSession())
                    {
                        WriteSessionFiles(startDateTime);
                        SetState(HeaptrackSessionState.Finished);
                    }
                    else
                    {
                        SetState(HeaptrackSessionState.Failed);
                    }

                    CloseOpenStreams();
                }
                catch (Exception ex)
                {
                    DisplaySessionError($"Session run error. {ex.Message}");
                    SetState(HeaptrackSessionState.Failed);
                }

                try
                {
                    DownloadAndDeleteOnTargetLogFile(GetProfctlLogTizenName(), ProfctlLogName);
                }
                catch (Exception ex)
                {
                    WriteToOutput($"Warning: cannot download 'profctl' log file. {ex.Message}");
                }

                if (State == HeaptrackSessionState.Finished)
                {
                    WriteToOutput("=== Memory profiling finished ===");
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        ProfilerPlugin.Instance.SessionsContainer.Update();
                    }));
                }
                else
                {
                    DisplaySessionError("Cannot start memory profiling session");
                    SetState(HeaptrackSessionState.Failed);
                }

                NotifyStatusChange();
            });
        }

        private bool RunHeaptrackSession()
        {
            string errorMessage;
            if (!SDBLib.ForwardTcpPort(_selectedDevice, ControlPort, ControlPort, out errorMessage))
            {
                WriteToOutput($"[ForwardTcpPort] {errorMessage}");
                return false;
            }
            if (!SDBLib.ForwardTcpPort(_selectedDevice, DataPort, DataPort, out errorMessage))
            {
                WriteToOutput($"[ForwardTcpPort] {errorMessage}");
                return false;
            }

            if (!InstallProfiler("profctl", "heaptrack"))
            {
                return false;
            }

            SessionDirectory = GetSessionDirName("DotNETMP-");
            Directory.CreateDirectory(SessionDirectory);

            lock (_logFileLock)
            {
                _resFileStream = new GZipStream(
                    new FileStream(Path.Combine(SessionDirectory, "resfile.gz"), FileMode.CreateNew),
                    CompressionLevel.Optimal);
            }

            // Generate files and copy them to target
            SetState(HeaptrackSessionState.UploadFiles, true);
            if (!PrepareAndCopyFilesToTarget())
            {
                return false;
            }

            // need to start the application first
            if (!StartRemoteApplication("HEAPTRACK"))
            {
                return false;
            }

            SetState(HeaptrackSessionState.Running);

            if (!CommunicateWithControlProcess())
            {
                return false;
            }

            return true;
        }

        private void WriteSessionFiles(DateTime sessionTime)
        {
            double timestamp = (sessionTime.ToUniversalTime() - TimeStampHelper.UnixEpochTime).TotalMilliseconds;
            EnvDTE.Project p = ProfilerPlugin.Instance.GetStartupProject();
            if (p == null)
            {
                return;
            }
            string projectName = ProfilerPlugin.Instance.GetStartupProject().Name;

            var sessionFile = new SessionProperties(Path.Combine(SessionDirectory, SessionConstants.SessionFileName));
            sessionFile.SetProperty("Time", "value", timestamp.ToString(CultureInfo.InvariantCulture));
            sessionFile.SetProperty("ProjectName", "value", projectName);
            sessionFile.SetProperty("ProfilingType", "value", "Memory Profiling");
            sessionFile.SetProperty("DeviceName", "value", DeviceName);

            sessionFile.SetProperty("CoreClrProfilerReport", "name", projectName + ".log");
            sessionFile.SetProperty("CoreClrProfilerReport", "path", "./");
            sessionFile.SetProperty("CtfReport", "name", "metadata");
            sessionFile.SetProperty("CtfReport", "path", "./");
            sessionFile.SetProperty("Proc", "name", "proc.log");
            sessionFile.SetProperty("Proc", "path", "./");

            sessionFile.Save();
        }

        private bool PrepareAndCopyFilesToTarget()
        {
            return InstallTpk();
        }

        private bool CommunicateWithControlProcess()
        {
            string errorMessage;
            StreamReader controlStreamReader = TryConnectToPort(ControlPort, 16, SocketConnectTimeout,
                out errorMessage);
            if (controlStreamReader == null)
            {
                WriteToOutput($"Cannot connect to Control Process (port {ControlPort}). {errorMessage}");
                return false;
            }
            StreamReader dataStreamReader;
            try
            {
                // connect to data port
                dataStreamReader = TryConnectToPort(DataPort, 4096, SocketConnectTimeout, out errorMessage);
                if (dataStreamReader == null)
                {
                    throw new Exception($"Cannot connect to Control Process (port {DataPort}). {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                DisposeHelper.SafeDispose(ref controlStreamReader);
                WriteToOutput($"Read data from socket failed. {ex.Message}");
                return false;
            }

            // read control stream asynchronously
            Task.Run(() =>
            {
                try
                {
                    string line;
                    while ((line = ReadLineIfNotAsyncError(controlStreamReader)) != null)
                    {
                        DebugWriteToOutput($"Reply received from Control Process: \"{line}\"");
                    }
                    DebugWriteToOutput($"Finished reading command stream");
                }
                catch (Exception ex)
                {
                    WriteToOutput($"Read command stream failed. {ex.Message}");
                }
                DisposeHelper.SafeDispose(ref controlStreamReader);
            });

            // read data stream synchronously
            bool result = false;
            try
            {
                DebugWriteToOutput($"Reading heaptrack data from port {DataPort}...");
                ReadHeaptrackOutput(dataStreamReader);
                DebugWriteToOutput($"Finished reading heaptrack data");
                result = !AsyncError;
            }
            catch (Exception ex)
            {
                WriteToOutput($"Read heaptrack data failed. {ex.Message}");
            }
            DisposeHelper.SafeDispose(ref dataStreamReader);
            return result;
        }

        private string GetProfctlLogTizenName()
        {
            return $"{_sdkToolPath}/profctl/{ProfctlLogName}";
        }

        // Read heaptrack output stream and write it to results file
        private bool ReadHeaptrackOutput(StreamReader stream)
        {
            try
            {
                int linesCoped = FileHelper.CopyLines(stream, _resFileStream, skipEmptyLines: true);
                DebugWriteToOutput($"Saved {linesCoped} heaptrack output lines");
                return true;
            }
            catch (Exception ex)
            {
                WriteToOutput($"[ReadHeaptrackOutput] {ex.Message}");
                return false;
            }
        }

        private void CloseOpenStreams()
        {
            if (_resFileStream != null)
            {
                try
                {
                    lock (_logFileLock)
                    {
                        DisposeHelper.SafeDispose(ref _resFileStream);
                    }
                    DebugWriteToOutput("Closed heaptrack data stream");
                }
                catch (Exception ex)
                {
                    WriteToOutput($"[CloseOpenStreams] Error closing heaptrack data stream. {ex.Message}");
                }
            }
        }

        protected override void NotifyStatusChange()
        {
            foreach (var listener in GetListeners())
            {
                listener.OnStateChanged?.Invoke(State);
            }
        }
    }
}
