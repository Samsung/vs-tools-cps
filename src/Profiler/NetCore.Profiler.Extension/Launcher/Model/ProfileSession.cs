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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using NetCore.Profiler.Common.Helpers;
using NetCore.Profiler.Cperf.Core.Model;
using NetCore.Profiler.Cperf.Core.Parser;
using NetCore.Profiler.Cperf.Core.Parser.Model;
using NetCore.Profiler.Cperf.LogAdaptor.Core;
using NetCore.Profiler.Extension.Session;
using NetCore.Profiler.Extension.VSPackage;
using NetCore.Profiler.Session.Core;
using Tizen.VisualStudio.Tools.DebugBridge;
using Tizen.VisualStudio.Tools.Utilities;
using Tizen.VisualStudio.Utilities;

namespace NetCore.Profiler.Extension.Launcher.Model
{
    /// <summary>
    /// A class representing a running %Core %Profiler session.
    /// </summary>
    public class ProfileSession : AbstractSession<ProfileSessionListener, ProfileSessionState, ProfileSessionConfiguration>//, IProfileSession
    {
        private const int ControlPort = 6001;

        private const int DataPort = 6002;

        private const int StatisticsPort = 6003;

        public ProfileSessionConfiguration Configuration => _sessionConfiguration;

        public bool IsLiveProfiling => _isLiveProfiling;

        public DateTime ProfilerStartTimeUtc { get; private set; }

        private const string ProfilerConfigName = "profiler.config";

        private string _targetInstallationDirectory;

        private string _targetShareDirectory;

        private NetworkStream _commandStream;

        private StreamWriter _procLogStreamWriter;

        private readonly bool _isLiveProfiling;

        private ulong _appPid;

        private ulong _traceLinesRead;

        private bool _lastBreakState;

        public ProfileSession(SDBDeviceInfo device, ProfileSessionConfiguration sessionConfiguration, bool isLiveProfiling)
            : base(device, sessionConfiguration)
        {
            SetState(ProfileSessionState.Initial);
            _isLiveProfiling = isLiveProfiling;
            _targetInstallationDirectory = _targetShareDirectory = $"{_sdkToolPath}/coreprofiler";
        }

        private bool IsProfilingInProgress()
        {
            return (State == ProfileSessionState.Waiting || State == ProfileSessionState.Running || State == ProfileSessionState.Paused);
        }

        public void Stop()
        {
            if (IsProfilingInProgress())
            {
                if (_appPid == 0)
                {
                    DebugWriteToOutput("Cannot stop the application: PID not known");
                    return;
                }
                DebugWriteToOutput("Stopping the application");
                SetState(ProfileSessionState.Stopping, true);
                SendCommandToControlProcess($"kill {_appPid}");
            }
        }

        public void Pause()
        {
            if (State != ProfileSessionState.Running)
            {
                throw new InvalidOperationException();
            }

            bool success = DoPause();

            if (success)
            {
                SetState(ProfileSessionState.Paused, true);
                WriteToOutput("Profiling paused");
            }
        }

        private bool DoPause()
        {
            if (_appPid == 0)
            {
                DebugWriteToOutput("Cannot pause the application: PID not known");
                return false;
            }
            return SendCommandToControlProcess($"stop {_appPid}");
        }

        public void Resume()
        {
            if ((State != ProfileSessionState.Waiting) && (State != ProfileSessionState.Paused))
            {
                throw new InvalidOperationException();
            }

            bool success = DoResume();

            if (success)
            {
                SetState(ProfileSessionState.Running, true);
                WriteToOutput("Profiling resumed");
            }
            else
            {
                //TODO Handle error
            }
        }

        private bool DoResume()
        {
            if (_appPid == 0)
            {
                DebugWriteToOutput("Cannot resume the application: PID not known");
                return false;
            }
            return SendCommandToControlProcess($"start {_appPid}");
        }

#if DEBUG
        System.Diagnostics.Stopwatch lastBreakTimer;
#endif

        public void OnDebugStateChanged(bool isBreakState)
        {
#if DEBUG
            if (isBreakState)
            {
                if (lastBreakTimer == null)
                {
                    lastBreakTimer = new System.Diagnostics.Stopwatch();
                }
                lastBreakTimer.Restart();
            }
            string msg = $"Debug break {(isBreakState ? "started" : "finished")}";
            if (!isBreakState && (lastBreakTimer != null) && lastBreakTimer.IsRunning)
            {
                msg += $" (duration: {lastBreakTimer.Elapsed})";
            }
            DebugWriteToOutput(msg);
#endif
            NotifyDebugStateChanged(isBreakState);
            if (State == ProfileSessionState.Running)
            {
                if (isBreakState)
                {
                    DoPause();
                }
                else if (_lastBreakState)
                {
                    DoResume();
                }
                _lastBreakState = isBreakState;
            }
        }

        internal void Start()
        {
            if (State != ProfileSessionState.Initial)
            {
                throw new InvalidOperationException();
            }

            DateTime startDateTime = DateTime.Now;

            if (!CheckConfiguration())
            {
                SetState(ProfileSessionState.Failed, true);
                return;
            }

            SetState(ProfileSessionState.Starting);

            ProfilerPlugin.Instance.ActivateTizenOutputPane();

            IVsThreadedWaitDialog2 waiter;
            ProfilerPlugin.Instance.CreateDialogInstance(out waiter,
                "Preparation for profiling",
                "Please wait while the preparation is being done...",
                "Preparing...",
                "Preparation for profiling in progress...");

            WriteToOutput("*** Profiling started ***");

            bool prepared = Prepare();

            int userCancel;
            waiter?.EndWaitDialog(out userCancel);

            Task.Run(delegate ()
            {
                try
                {
                    if (prepared && CommunicateWithControlProcess())
                    {
                        SetState(ProfileSessionState.WritingSession, true);
                        WriteSessionFiles(startDateTime);
                        SetState(ProfileSessionState.Finished);
                    }
                    else
                    {
                        DisplaySessionError("Cannot start profiling session");
                        SetState(ProfileSessionState.Failed);
                    }

                    CloseOpenStreams();
                }
                catch (Exception ex)
                {
                    DisplaySessionError($"Session run error. {ex.Message}");
                    SetState(ProfileSessionState.Failed);
                }

                try
                {
                    DownloadAndDeleteOnTargetLogFile(GetProfctlLogTizenName(), GetProfctlLogName());
                }
                catch (Exception ex)
                {
                    WriteToOutput($"Warning: cannot download 'profctl' log file. {ex.Message}");
                }

                if (State == ProfileSessionState.Finished)
                {
                    DebugWriteToOutput($"Profiler trace log lines read: {_traceLinesRead}");
                    if (_traceLinesRead > 0)
                    {
                        WriteToOutput("=== Profiling finished ===");
                    }
                    else
                    {
                        DisplaySessionError("Cannot read profiler trace log");
                        SetState(ProfileSessionState.Failed);
                    }
                }

                NotifyStatusChange();
            });
        }

        private bool Prepare()
        {
            try
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
                if (!SDBLib.ForwardTcpPort(_selectedDevice, StatisticsPort, StatisticsPort, out errorMessage))
                {
                    WriteToOutput($"[ForwardTcpPort] {errorMessage}");
                    return false;
                }

                if (!InstallProfiler("profctl", "coreprofiler"))
                {
                    return false;
                }

                SessionDirectory = GetSessionDirName("DotNET-");
                Directory.CreateDirectory(SessionDirectory);

                _procLogStreamWriter = new StreamWriter(
                    Path.Combine(SessionDirectory, "proc.log"), false, Encoding.ASCII, 4096);

                // Generate files and copy them to target
                SetState(ProfileSessionState.UploadFiles, true);
                if (!PrepareAndCopyFilesToTarget())
                {
                    return false;
                }

                SetState(ProfileSessionState.StartHost, true);

                if (!StartRemoteApplication(_isLiveProfiling ? "LIVEPROFILER" : "COREPROFILER"))
                {
                    return false;
                }

                SetState(_sessionConfiguration.ProfilingSettings.DelayedStart
                    ? ProfileSessionState.Waiting : ProfileSessionState.Running, true);
            }
            catch (Exception ex)
            {
                DisplaySessionError($"Session prepare error. {ex.Message}");
                return false;
            }

            return true;
        }

        private bool PrepareAndCopyFilesToTarget()
        {
            if (!_isLiveProfiling)
            {
                if (!InstallTpk())
                {
                    return false;
                }
            }

            string tempFileName = Path.GetTempFileName();
            try
            {
                if (!WriteProfilerConfigFile(_sessionConfiguration.TargetDll, _sessionConfiguration.AppId, tempFileName))
                {
                    WriteToOutput("[PrepareAndCopyFilesToTarget] WriteProfilerConfigFile failed");
                    return false;
                }

                string destination = $"{_targetShareDirectory}/{ProfilerConfigName}";
                DebugWriteToOutput($"Uploading \"{tempFileName}\" to {destination}...");
                string errorMessage;
                if (!DeployHelper.PushFile(_selectedDevice, tempFileName, destination, null, out errorMessage))
                {
                    WriteToOutput(errorMessage);
                    return false;
                }
            }
            finally
            {
                try { File.Delete(tempFileName); } catch { }
            }

            return true;
        }

        private bool WriteProfilerConfigFile(string binPath, string appId, string filePath)
        {
            return _sessionConfiguration.ProfilingSettings.WriteCperfStartFile(filePath);
        }

        //private bool AdjustAttributes()
        //{
        //    WriteToOutput("-> Adjust script attributes");
        //    var process = SDBLib.CreateSdbProcess();
        //    SDBLib.RunSdbProcess(process,
        //        DeviceManager.AdjustSdbArgument($"shell \"chmod +x {_sessionConfiguration.ProjectTargetPath}/cperfstart_{_hostId}\""));
        //    var rc = process.ExitCode;
        //    process.Close();

        //    process = SDBLib.CreateSdbProcess();
        //    SDBLib.RunSdbProcess(process,
        //        DeviceManager.AdjustSdbArgument($"shell \"chsmack -a '_' -e 'System::Privileged' {_sessionConfiguration.ProjectTargetPath}/cperfstart_{_hostId}\""));
        //    var rc1 = process.ExitCode;
        //    process.Close();
        //    return rc != 0 || rc1 != 0;
        //}

        protected override string GetSdbLaunchArguments()
        {
            return _isLiveProfiling ? "__DLP_DEBUG_ARG__ --server=4711,--" : "";
        }

        private string GetProfctlLogTizenName()
        {
            return $"{_sdkToolPath}/profctl/{GetProfctlLogName()}";
        }

        private string GetProfctlLogName()
        {
            return $"profctl_{(_isLiveProfiling ? "liveprofiler" : "coreprofiler")}.log";
        }

        private bool CommunicateWithControlProcess()
        {
            StreamReader commandStreamReader;
            string errorMessage;
            if (!TryConnectToPort(ControlPort, 16, SocketConnectTimeout, out _commandStream, out commandStreamReader,
                out errorMessage))
            {
                WriteToOutput($"Cannot connect to Control Process (port {ControlPort}). {errorMessage}");
                return false;
            }

            // read control stream asynchronously
            Task.Run(() =>
            {
                try
                {
                    string line;
                    while ((line = ReadLineIfNotAsyncError(commandStreamReader)) != null)
                    {
                        DebugWriteToOutput($"Reply received from Control Process: \"{line}\"");
                    }
                    DebugWriteToOutput("Finished reading command stream");
                }
                catch (Exception ex)
                {
                    string msg = $"Cannot read command stream. {ex.Message}";
                    if (!AsyncError)
                    {
                        SetAsyncError();
                        WriteToOutput(msg);
                    }
                    else
                    {
                        DebugWriteToOutput(msg);
                    }
                }
                DisposeHelper.SafeDispose(ref commandStreamReader);
                DisposeHelper.SafeDispose(ref _commandStream);
            });

            // read statistics stream asynchronously
            Task.Run(() =>
            {
                try
                {
                    StreamReader statisticsStreamReader = TryConnectToPort(StatisticsPort, 1024, SocketConnectTimeout,
                        out errorMessage);
                    if (statisticsStreamReader == null)
                    {
                        throw new Exception(errorMessage);
                    }
                    using (statisticsStreamReader)
                    {
                        ProcessControlProcessStatisticsStream(statisticsStreamReader);
                    }
                }
                catch (Exception ex)
                {
                    string msg = $"Cannot read statistics stream. {ex.Message}";
                    if (!AsyncError)
                    {
                        SetAsyncError();
                        WriteToOutput(msg);
                    }
                    else
                    {
                        DebugWriteToOutput(msg);
                    }
                }
            });

            // read data stream synchronously
            bool result = false;
            try
            {
                ProcessProfilerLog(SessionDirectory);
                DebugWriteToOutput("Finished reading profiler trace log");
                result = !AsyncError;
            }
            catch (Exception ex)
            {
                string msg = $"Cannot process profiler trace log. {ex.Message}";
                if (!AsyncError)
                {
                    SetAsyncError();
                    WriteToOutput(msg);
                }
                else
                {
                    DebugWriteToOutput(msg);
                }
            }
            return result;
        }

        private bool SendCommandToControlProcess(string command)
        {
            bool result = SendCommandToControlProcess(_commandStream, command);
            if (!result)
            {
                SetAsyncError();
            }
            return result;
        }

        private void CloseOpenStreams()
        {
            if (_procLogStreamWriter != null)
            {
                DebugWriteToOutput("Finishing process log");
                try
                {
                    lock (_logFileLock)
                    {
                        DisposeHelper.SafeDispose(ref _procLogStreamWriter);
                    }
                }
                catch (Exception ex)
                {
                    WriteToOutput($"Error closing process log. {ex.Message}");
                }
            }
        }

        private void ProcessControlProcessStatisticsStream(StreamReader reader)
        {
            bool firstLine = true;
            bool writeProcLog = true;
            string line;
            int coreNum = -1;
            while ((line = ReadLineIfNotAsyncError(reader)) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (firstLine)
                {
                    coreNum = SysInfoItem.GetCoreNumber(line);
                }

                line = $"{line} {StateToLabel()}";

                if (writeProcLog && (_procLogStreamWriter != null))
                {
                    try
                    {
                        lock (_logFileLock)
                        {
                            _procLogStreamWriter?.WriteLine(line);
                        }
                    }
                    catch (Exception ex)
                    {
                        writeProcLog = false;
                        WriteToOutput($"Error writing Control Process log. {ex.Message}");
                    }
                }

                if (!firstLine && IsProfilingInProgress())
                {
                    var si = SysInfoItem.CreateInstance(line, coreNum);
                    if (si != null)
                    {
                        NotifySystemInfo(si);
                    }
                }

                firstLine = false;
            }
        }

        private string StateToLabel()
        {
            switch (State)
            {
                case ProfileSessionState.Waiting:
                case ProfileSessionState.Paused:
                    return "W";
                case ProfileSessionState.Running:
                    return "R";
                default:
                    return "U";
            }
        }

        private void ProcessProfilerLog(string traceLocation)
        {
            string outputLogPath = Path.Combine(traceLocation, _sessionConfiguration.ProjectName + ".log");
            var outputLogWriter = new StreamWriter(outputLogPath, false, Encoding.UTF8, 8192);
            try
            {
                using (outputLogWriter)
                {
                    string errorMessage;
                    StreamReader traceStreamReader = TryConnectToPort(DataPort, 8192, SocketConnectTimeout,
                        out errorMessage);
                    if (traceStreamReader == null)
                    {
                        throw new Exception(errorMessage);
                    }
                    try
                    {
                        DebugWriteToOutput($"Reading profiler trace log from port {DataPort}...");
                        ProcessProfilerLog(traceStreamReader, outputLogWriter);
                    }
                    finally
                    {
                        DisposeHelper.SafeDispose(ref traceStreamReader);
                    }
                }
            }
            catch
            {
                try { File.Delete(outputLogPath); } catch { }
                throw;
            }
        }

        private void ProcessProfilerLog(StreamReader source, StreamWriter destination)
        {
            var parser = new CperfParser();

            parser.LineReadCallback += LineReadCallback;
            parser.StartTimeCallback += StartTimeCallback;
            parser.JitCompilationStartedCallback += JitCompilationStartedCallback;
            parser.JitCompilationFinishedCallback += JitCompilationFinishedCallback;
            parser.JitCachedFunctionSearchStartedCallback += JitCachedFunctionSearchStartedCallback;
            parser.JitCachedFunctionSearchFinishedCallback += JitCachedFunctionSearchFinishedCallback;
            parser.GarbageCollectionStartedCallback += GarbageCollectionStartedCallback;
            parser.GarbageCollectionFinishedCallback += GarbageCollectionFinishedCallback;
            parser.ThreadAssignedToOsThreadCallback += ThreadAssignedToOsThreadCallback;

            new DebugDataInjectionFilter
            {
                Output = destination,
                PdbDirectory = _sessionConfiguration.ProjectHostBinPath
            }
            .Process(
                parser,
                () => ReadLineIfNotAsyncError(source));

            parser.StartTimeCallback -= StartTimeCallback;
            parser.JitCompilationStartedCallback -= JitCompilationStartedCallback;
            parser.JitCompilationFinishedCallback -= JitCompilationFinishedCallback;
            parser.JitCachedFunctionSearchStartedCallback -= JitCachedFunctionSearchStartedCallback;
            parser.JitCachedFunctionSearchFinishedCallback -= JitCachedFunctionSearchFinishedCallback;
            parser.GarbageCollectionStartedCallback -= GarbageCollectionStartedCallback;
            parser.GarbageCollectionFinishedCallback -= GarbageCollectionFinishedCallback;
            parser.ThreadAssignedToOsThreadCallback -= ThreadAssignedToOsThreadCallback;
        }

        private void LineReadCallback(string line)
        {
            ++_traceLinesRead;
        }

        private void StartTimeCallback(DateTime startTime)
        {
            if (startTime.Kind == DateTimeKind.Utc)
            {
                ProfilerStartTimeUtc = startTime;
                DebugWriteToOutput($"Profiler start time: {startTime.ToDebugString()}");
            }
        }

        private void JitCompilationStartedCallback(JitCompilationStarted arg)
        {
            NotifyProfilerEvent(new Event(arg, arg.Timestamp, EventType.CompilationStarted));
        }

        private void JitCompilationFinishedCallback(JitCompilationFinished arg)
        {
            NotifyProfilerEvent(new Event(arg, arg.Timestamp, EventType.CompilationFinished));
        }

        private void JitCachedFunctionSearchStartedCallback(JitCachedFunctionSearchStarted arg)
        {
            NotifyProfilerEvent(new Event(arg, arg.Timestamp, EventType.CachedFunctionSearchStarted));
        }

        private void JitCachedFunctionSearchFinishedCallback(JitCachedFunctionSearchFinished arg)
        {
            NotifyProfilerEvent(new Event(arg, arg.Timestamp, EventType.CachedFunctionSearchFinished));
        }

        private void GarbageCollectionStartedCallback(GarbageCollectionStarted arg)
        {
            NotifyProfilerEvent(new Event(arg, arg.Timestamp, EventType.GarbageCollectionStarted));
        }

        private void GarbageCollectionFinishedCallback(GarbageCollectionFinished arg)
        {
            NotifyProfilerEvent(new Event(arg, arg.Timestamp, EventType.GarbageCollectionFinished));
        }

        private void ThreadAssignedToOsThreadCallback(ThreadAssignedToOsThread arg)
        {
            if (_appPid == 0)
            {
                _appPid = arg.OsThreadId;
                Task.Run(() =>
                {
                    if (!SendCommandToControlProcess($"test {_appPid}"))
                    {
                        SetAsyncError();
                    }
                });
            }
        }

        private void WriteSessionFiles(DateTime dt)
        {
            double timestamp = (dt.ToUniversalTime() - TimeStampHelper.UnixEpochTime).TotalMilliseconds;

            var sessionFile = new SessionProperties(Path.Combine(SessionDirectory, SessionConstants.SessionFileName));
            sessionFile.SetProperty("CoreClrProfilerReport", "name", _sessionConfiguration.ProjectName + ".log");
            sessionFile.SetProperty("CoreClrProfilerReport", "path", "./");

            sessionFile.SetProperty("Proc", "name", "proc.log");
            sessionFile.SetProperty("Proc", "path", "./");
            sessionFile.SetProperty("Time", "value", timestamp.ToString(CultureInfo.InvariantCulture));
            sessionFile.SetProperty("ProjectName", "value", _sessionConfiguration.ProjectName);
            sessionFile.SetProperty("ProfilingType", "value", _sessionConfiguration.ProfilingPreset.Name);
            sessionFile.SetProperty("DeviceName", "value", DeviceName);

            sessionFile.SetProperty("ProfilingOptions", "COMPlusEnableeventlog", true);
            sessionFile.SetProperty("ProfilingOptions", "COMPlusARMEnabled", true);
            sessionFile.SetProperty("ProfilingOptions", "ProfCollectMethod", _sessionConfiguration.ProfilingSettings.CollectMethod.ToString());
            sessionFile.SetProperty("ProfilingOptions", "ProfSamplingTimeout", _sessionConfiguration.ProfilingSettings.SamplingInterval);
            sessionFile.SetProperty("ProfilingOptions", "ProfCpu_traceTimeout", _sessionConfiguration.ProfilingSettings.CpuTraceInterval);
            sessionFile.SetProperty("ProfilingOptions", "ProfExecutionTrace", _sessionConfiguration.ProfilingSettings.TraceExecution);
            sessionFile.SetProperty("ProfilingOptions", "ProfCpuTrace", _sessionConfiguration.ProfilingSettings.TraceCpu);
            sessionFile.SetProperty("ProfilingOptions", "ProfCpuTraceProc", _sessionConfiguration.ProfilingSettings.TraceProcessCpu);
            sessionFile.SetProperty("ProfilingOptions", "ProfCpuTraceThread", _sessionConfiguration.ProfilingSettings.TraceThreadCpu);
            sessionFile.SetProperty("ProfilingOptions", "ProfMemoryTrace", _sessionConfiguration.ProfilingSettings.TraceMemoryAllocation);
            sessionFile.SetProperty("ProfilingOptions", "ProfLineTrace", _sessionConfiguration.ProfilingSettings.TraceSourceLines);
            sessionFile.SetProperty("ProfilingOptions", "ProfHighGran", _sessionConfiguration.ProfilingSettings.HighGranularitySampling);
            sessionFile.SetProperty("ProfilingOptions", "ProfStackTrack", _sessionConfiguration.ProfilingSettings.StackTrack);
            sessionFile.SetProperty("ProfilingOptions", "ProfDelayedStart", _sessionConfiguration.ProfilingSettings.DelayedStart);
            sessionFile.SetProperty("ProfilingOptions", "ProfTraceGarbageCollection", _sessionConfiguration.ProfilingSettings.TraceGarbageCollection);

            string hostId;
            try
            {
                IPAddress ip = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];
                hostId = Regex.Replace(ip.ToString(), "[:%]", "");
            }
            catch (Exception ex)
            {
                WriteToOutput($"Cannot resolve host name. {ex.Message}");
                hostId = "unknown";
            }
            sessionFile.SetProperty("ProfilingOptions", "CperfHostCode", hostId);
            sessionFile.SetProperty("ProfilingOptions", "Sleep", _sessionConfiguration.SleepTime);
            sessionFile.Save();
        }

        private void NotifySystemInfo(SysInfoItem item)
        {
            foreach (var listener in GetListeners())
            {
                listener.OnSysInfoRead?.Invoke(item);
            }
        }

        private void NotifyProfilerEvent(Event @event)
        {
            foreach (var listener in GetListeners())
            {
                listener.OnProfilerEvent?.Invoke(@event);
            }
        }

        protected override void NotifyStatusChange()
        {
            foreach (var listener in GetListeners())
            {
                listener.OnStateChanged?.Invoke(State);
            }
        }

        private void NotifyDebugStateChanged(bool isBreakState)
        {
            foreach (var listener in GetListeners())
            {
                listener.OnDebugStateChanged?.Invoke(isBreakState);
            }
        }
    }
}
