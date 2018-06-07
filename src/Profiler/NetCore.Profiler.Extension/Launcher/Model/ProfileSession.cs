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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using NetCore.Profiler.Cperf.Core.Model;
using NetCore.Profiler.Cperf.LogAdaptor.Core;
using Process = System.Diagnostics.Process;
using Thread = System.Threading.Thread;
using NetCore.Profiler.Extension.Session;
using NetCore.Profiler.Extension.VSPackage;
using NetCore.Profiler.Session.Core;
using Tizen.VisualStudio.Tools.DebugBridge;
using Tizen.VisualStudio.Tools.Data;

namespace NetCore.Profiler.Extension.Launcher.Model
{
    internal class ProfileSession : IProfileSession
    {

        private const string SessionArchiveLocalFile = "cperf.zip";

        public string ProjectDirectory;

        public string DeviceName;

        private string _hostId;

        private readonly ProfileSessionConfiguration _sessionConfiguration;

        private Process _cperfControlProcess;

        private Process _cperfStartProcess;

        private StreamWriter _procLogStreamWriter;

        private readonly List<IProfileSessionListener> _sessionListeners = new List<IProfileSessionListener>();

        public ProfileSession(ProfileSessionConfiguration sessionConfiguration)
        {

            _sessionConfiguration = sessionConfiguration;

            ProjectDirectory = _sessionConfiguration.ProjectHostPath;

            //DeviceName = $"{DeviceManager.SelectedDevice.name} ({DeviceManager.SelectedDevice.serial})";
            DeviceName = DeviceManager.SelectedDevice.Name;

            State = ProfileSessionState.Initial;

        }

        public string SessionDirectory { get; private set; }

        public ProfileSessionState State { get; private set; }

        private string CperfZipName => "cperf_" + _hostId + ".zip";


        public void AddListener(IProfileSessionListener listener)
        {
            lock (_sessionListeners)
            {
                if (!_sessionListeners.Contains(listener))
                {
                    _sessionListeners.Add(listener);
                }
            }
        }

        public void RemoveListener(IProfileSessionListener listener)
        {
            lock (_sessionListeners)
            {
                _sessionListeners.Remove(listener);
            }
        }

        public void Stop()
        {
            if (State == ProfileSessionState.Waiting || State == ProfileSessionState.Running || State == ProfileSessionState.Paused)
            {
                WriteToOutput("Stopping the application");
                SetState(ProfileSessionState.Stopping, true);
                SendCommandToCperfControl("kill");
            }
        }

        public void Pause()
        {
            if (State != ProfileSessionState.Running)
            {
                throw new InvalidOperationException();
            }

            if (SendCommandToCperfControl("stop"))
            {
                //TODO Handle error
            }
            else
            {
                SetState(ProfileSessionState.Paused, true);
            }
        }

        public void Resume()
        {
            if (State != ProfileSessionState.Waiting && State != ProfileSessionState.Paused)
            {
                throw new InvalidOperationException();
            }

            if (SendCommandToCperfControl("start"))
            {
                //TODO Handle error
            }
            else
            {
                SetState(ProfileSessionState.Running, true);
            }
        }

        internal void Start()
        {
            if (State != ProfileSessionState.Initial)
            {
                throw new InvalidOperationException();
            }

            if (!CheckConfiguration())
            {
                State = ProfileSessionState.Failed;
                NotifyStatusChange();
                return;
            }

            State = ProfileSessionState.Starting;

            new Thread(delegate()
            {
                RunProfileSession();

                StopControlServer();

                CloseOpenStreams();

                WriteToOutput("-> Done");

                NotifyStatusChange();

            }).Start();
        }

        private bool CheckConfiguration()
        {
            //var toolsInfo = ToolsInfo.Instance();
            var toolsPath = ToolsPathInfo.OndemandFolderPath;
            if (string.IsNullOrEmpty(ToolsPathInfo.OndemandFolderPath))
            {
                ProfilerPlugin.Instance.ShowError("Error", "Tizen Tools are not configured");
                return false;
            }

            var scriptsPath = Path.Combine(toolsPath, "cperf_scripts");
            foreach (var x in new[] { "cperfenv", "cperf_control", "cperf_logcontrol" })
            {
                if (!File.Exists(Path.Combine(scriptsPath, x)))
                {
                    ProfilerPlugin.Instance.ShowError("Error", "Profiler scripts are not found");
                    return false;
                }
            }

            return true;
        }

        private bool InstallProfiler()
        {
            bool retval = true;
            OnDemandInstaller installer = null;

            try
            {
                installer = new OnDemandInstaller();
                retval = installer.Install();
            }
            finally
            {
                installer = null;
            }

            if (!retval)
            {
                ProfilerPlugin.Instance.ShowError("Error", "Profiler is not found on target");
            }

            return retval;
        }

        private void RunProfileSession()
        {
            var ip = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];
            _hostId = Regex.Replace(ip.ToString(), "[:%]", "");

            var startDateTime = DateTime.Now;
            var sessionDirName = "DotNET-" + startDateTime.ToString("yyyyMMdd-HHmmss");

            SessionDirectory = Path.Combine(ProjectDirectory, sessionDirName);
            try
            {
                Directory.CreateDirectory(SessionDirectory);
                lock (_logFileLock)
                {
                    _procLogStreamWriter = new StreamWriter(Path.Combine(SessionDirectory, "proc.log"));
                }
            }
            catch (Exception e)
            {
                SetState(ProfileSessionState.Failed);
                WriteToOutput($"[ERROR] {e.Message}");
            }

            if (!InstallProfiler())
            {
                SetState(ProfileSessionState.Failed);
                return;
            }

            // Generate files and copy them to target
            SetState(ProfileSessionState.UploadFiles, true);
            if (PrepareAndCopyFilesToTarget())
            {
                SetState(ProfileSessionState.Failed);
                return;
            }

            SetState(ProfileSessionState.StartHost, true);

            if (SwitchToRoot(true))
            {
                SetState(ProfileSessionState.Failed);
                return;
            }

            if (AdjustAttributes())
            {
                SetState(ProfileSessionState.Failed);
                return;
            }

            if (StartControlServer())
            {
                SetState(ProfileSessionState.Failed);
                return;
            }

            SetState(_sessionConfiguration.ProfilingSettings.DelayedStart ? ProfileSessionState.Waiting : ProfileSessionState.Running, true);


            // Start remote application. It's blocking call
            if (StartRemoteApplication())
            {
                SetState(ProfileSessionState.Failed);
                return;
            }

            //Download Cperf zip-file
            SetState(ProfileSessionState.DownloadFiles, true);
            if (DownloadTraceFiles())
            {
                SetState(ProfileSessionState.Failed);
                return;
            }

            SwitchToRoot(false);

            SetState(ProfileSessionState.WritingSession, true);
            //Generate Session files
            WriteSessionFiles(startDateTime);

            SetState(ProfileSessionState.Finished);

        }

        private bool PrepareAndCopyFilesToTarget()
        {

            var filesToCopy = new List<Tuple<string, string>>();

            // Install Tizen Package with binary
            var tpkPath = Path.Combine(_sessionConfiguration.ProjectHostBinPath, _sessionConfiguration.ProjectPackageName + "-" + _sessionConfiguration.ProjectPackageVersion + ".tpk");
            WriteToOutput($"-> Installing Tizen Package {tpkPath}...");
            var process = SDBLib.CreateSdbProcess(true, true);
            if (process == null)
            {
                WriteToOutput("[ERROR]\n");
            }
            else
            {
                var argument = DeviceManager.AdjustSdbArgument("install \"" + tpkPath + "\"");
                SDBLib.RunSdbProcess(process, argument, true);
                var rc = process.ExitCode;
                process.Close();
                if (rc != 0)
                {
                    WriteToOutput("[ERROR]\n");
                }
            }

            var cperfStartFile = Path.GetTempFileName();
            if (WriteCperfStartFile(_sessionConfiguration.TargetDll, _sessionConfiguration.AppId, cperfStartFile))
            {
                WriteToOutput("[ERROR]\n");
                return true;
            }

            filesToCopy.Add(new Tuple<string, string>(cperfStartFile, "cperfstart_" + _hostId));

            //var toolsInfo = ToolsInfo.Instance();
            var toolsPath = ToolsPathInfo.OndemandFolderPath;
            if (string.IsNullOrEmpty(toolsPath))
            {
                WriteToOutput("[ERROR] ToolsPath is empty");
                return true;
            }

            var scriptsPath = Path.Combine(toolsPath, "cperf_scripts");

            filesToCopy.AddRange(new[] { "cperfenv", "cperf_control", "cperf_logcontrol" }.
                Select(script => new Tuple<string, string>(Path.Combine(scriptsPath, script), script)));

            foreach (var pair in filesToCopy)
            {
                WriteToOutput($"-> Uploading {pair.Item1} to {pair.Item2}...");
                if (UploadFile(pair.Item1, pair.Item2))
                {
                    WriteToOutput("[ERROR]\n");
                    return true;
                }
            }

            return false;
        }

        private bool WriteCperfStartFile(string binPath, string appId, string filePath)
        {
            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    writer.NewLine = "\n";
                    writer.WriteLine("#! /bin/bash");
                    writer.WriteLine(". {0}/cperfenv", _sessionConfiguration.ProjectTargetPath);
                    writer.WriteLine("export COMPlus_EnableEventLog=1");
                    writer.WriteLine("export COMPlus_ARMEnabled=1");
                    writer.WriteLine("export PROF_COLLECT_METHOD={0}", _sessionConfiguration.ProfilingSettings.CollectMethod);
                    if (_sessionConfiguration.ProfilingSettings.CollectMethod == ProfilingMethod.Sampling)
                    {
                        writer.WriteLine("export PROF_SAMPLING_TIMEOUT={0}", _sessionConfiguration.ProfilingSettings.SamplingInterval);
                        writer.WriteLine("export PROF_CPU_TRACE_TIMEOUT={0}", _sessionConfiguration.ProfilingSettings.CpuTraceInterval);
                    }
                    else
                    {
                        writer.WriteLine("unset PROF_SAMPLING_TIMEOUT");
                        writer.WriteLine("unset PROF_CPU_TRACE_TIMEOUT");
                    }

                    writer.WriteLine("export PROF_EXECUTION_TRACE={0}", _sessionConfiguration.ProfilingSettings.TraceExecution ? 1 : 0);
                    writer.WriteLine("export PROF_CPU_TRACE={0}", _sessionConfiguration.ProfilingSettings.TraceCpu ? 1 : 0);
                    writer.WriteLine("export PROF_CPU_TRACE_PROC={0}", _sessionConfiguration.ProfilingSettings.TraceProcessCpu ? 1 : 0);
                    writer.WriteLine("export PROF_CPU_TRACE_THREAD={0}",
                        _sessionConfiguration.ProfilingSettings.TraceThreadCpu ? 1 : 0);
                    writer.WriteLine("export PROF_MEMORY_TRACE={0}",
                        _sessionConfiguration.ProfilingSettings.TraceMemoryAllocation ? 1 : 0);
                    writer.WriteLine("export PROF_LINE_TRACE={0}", _sessionConfiguration.ProfilingSettings.TraceSourceLines ? 1 : 0);
                    writer.WriteLine("export PROF_HIGH_GRAN={0}",
                        _sessionConfiguration.ProfilingSettings.HighGranularitySampling ? 1 : 0);
                    writer.WriteLine("export PROF_STACK_TRACK={0}", _sessionConfiguration.ProfilingSettings.StackTrack ? 1 : 0);
                    writer.WriteLine("export PROF_DELAYED_START={0}", _sessionConfiguration.ProfilingSettings.DelayedStart ? 1 : 0);
                    writer.WriteLine("export PROF_GC_TRACE={0}", _sessionConfiguration.ProfilingSettings.TraceGarbageCollection ? 1 : 0);

                    writer.WriteLine("export CPERF_ZIP_NAME={0}/{1}", _sessionConfiguration.ProjectTargetPath, CperfZipName);
                    writer.WriteLine("export CPERF_HOST_CODE={0}", _hostId);
                    writer.WriteLine("export CPERF_APPID={0}", appId);
                    writer.WriteLine("cd {0}/share", _sessionConfiguration.ProjectTargetPath);
                    writer.WriteLine("cperf collect {0} zaybxcwdveuftgsh", binPath);
                    writer.WriteLine("sleep {0}", _sessionConfiguration.SleepTime);
                }
            }
            catch (Exception)
            {
                return true;
            }

            return false;
        }

        private bool SwitchToRoot(bool on)
        {
            WriteToOutput($"-> Switch to root mode: {on}");
            var process = SDBLib.CreateSdbProcess();
            SDBLib.RunSdbProcess(process, $"root {((on) ? "on" : "off")}");
            var rc = process.ExitCode;
            process.Close();
            return rc != 0;
        }

        private bool AdjustAttributes()
        {
            WriteToOutput("-> Adjust script attributes");
            var process = SDBLib.CreateSdbProcess();
            SDBLib.RunSdbProcess(process,
                DeviceManager.AdjustSdbArgument($"shell \"chmod +x {_sessionConfiguration.ProjectTargetPath}/cperfstart_{_hostId}\""));
            var rc = process.ExitCode;
            process.Close();

            process = SDBLib.CreateSdbProcess();
            SDBLib.RunSdbProcess(process,
                DeviceManager.AdjustSdbArgument($"shell \"chsmack -a '_' -e 'System::Privileged' {_sessionConfiguration.ProjectTargetPath}/cperfstart_{_hostId}\""));
            var rc1 = process.ExitCode;
            process.Close();
            return rc != 0 || rc1 != 0;
        }

        private bool StartRemoteApplication()
        {
            WriteToOutput("-> Starting remote application...");

            var outputToWindow = (_sessionConfiguration.SleepTime != 0);
            _cperfStartProcess = SDBLib.CreateSdbProcess(!outputToWindow);
            if (outputToWindow)
            {
                _cperfStartProcess.StartInfo.RedirectStandardOutput = false;
                _cperfStartProcess.StartInfo.RedirectStandardError = false;
            }

            SDBLib.RunSdbProcess(_cperfStartProcess,
                    DeviceManager.AdjustSdbArgument($"shell \"{_sessionConfiguration.ProjectTargetPath}/cperfstart_{_hostId}\""), !outputToWindow);
            var rc = _cperfStartProcess.ExitCode;
            _cperfStartProcess.Close();
            if (rc != 0)
            {
                WriteToOutput("[ERROR]\n");
                return true;
            }

            return false;
        }

        private bool StartControlServer()
        {
            WriteToOutput("-> Starting Control server...");

            System.Threading.Tasks.Task.Run(() => ProcessCperfLog(SessionDirectory));

            _cperfControlProcess = SDBLib.CreateSdbProcess();

            _cperfControlProcess.StartInfo.Arguments =
                DeviceManager.AdjustSdbArgument(
                    $"shell \"/bin/bash {_sessionConfiguration.ProjectTargetPath}/cperf_control 1 {_hostId}\"");
            _cperfControlProcess.Start();

            System.Threading.Tasks.Task.Run(() => ProcessCperfControlOutStream(_cperfControlProcess.StandardOutput));
            System.Threading.Tasks.Task.Run(() => ProcessCperfControlErrStream(_cperfControlProcess.StandardError));

            return false;
        }

        private void StopControlServer()
        {
            WriteToOutput("-> Stopping Control server...");
            if (_cperfControlProcess != null)
            {
                try
                {
                    _cperfControlProcess.StandardInput.AutoFlush = true;
                    _cperfControlProcess.StandardInput.WriteLine("exit");
                    _cperfControlProcess.StandardInput.Flush();
                    _cperfControlProcess.StandardInput.Write("\003\003");
                    _cperfControlProcess.StandardInput.Close();
                }
                catch (Exception e)
                {
                    WriteToOutput($"[ERROR] Stopping Control server {e.Message}");
                }

                _cperfControlProcess.Close();
                _cperfControlProcess = null;
            }
        }

        private bool SendCommandToCperfControl(string command)
        {
            if (_cperfControlProcess != null)
            {
                try
                {
                    _cperfControlProcess.StandardInput.WriteLine(command);
                    _cperfControlProcess.StandardInput.Flush();
                    return false;
                }
                catch (Exception e)
                {
                    WriteToOutput($"[ERROR] Sending command to Control server {e.Message}");
                }
            }

            return true;
        }

        private readonly object _logFileLock = new object();

        private void CloseOpenStreams()
        {
            WriteToOutput("-> Finishing Process Log ");
            try
            {
                lock (_logFileLock)
                {
                    if (_procLogStreamWriter != null)
                    {
                        _procLogStreamWriter.Flush();
                        _procLogStreamWriter.Close();
                        _procLogStreamWriter.Dispose();
                        _procLogStreamWriter = null;
                    }
                }
            }
            catch (Exception e)
            {
                WriteToOutput($"[ERROR] Finishing process log {e.Message}");
            }
        }

        private void ProcessCperfControlOutStream(TextReader stream)
        {
            try
            {
                string line;
                while ((line = stream.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    line = $"{line} {StateToCperfLabel()}";
                    if (State == ProfileSessionState.Waiting || State == ProfileSessionState.Running || State == ProfileSessionState.Paused)
                    {
                        try
                        {
                            lock (_logFileLock)
                            {
                                if (_procLogStreamWriter != null)
                                {
                                    _procLogStreamWriter.WriteLine(line);
                                    _procLogStreamWriter.Flush();
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            WriteToOutput($"[ERROR] Writing process log {e.Message}");
                        }

                        var si = SysInfoItem.CreateInstance(line);
                        if (si != null)
                        {
                            NotifySystemInfo(si);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                WriteToOutput($"[ERROR] Reading Cperf output {ex.Message}");
            }
        }

        private string StateToCperfLabel()
        {
            switch (State)
            {
                case ProfileSessionState.Waiting:
                case ProfileSessionState.Paused:
                    return "Waiting";
                case ProfileSessionState.Running:
                    return "Running";
                default:
                    return "Unknown";
            }
        }

        private static void ProcessCperfControlErrStream(TextReader stream)
        {
            try
            {
                while (stream.ReadLine() != null)
                {
                }
            }
            catch (Exception e)
            {
                WriteToOutput($"[ERROR] Reading Cperf Error stream {e.Message}");
            }
        }

        private void ProcessCperfLog(string traceLocation)
        {
            var proc = SDBLib.CreateSdbProcess();

            proc.StartInfo.Arguments = DeviceManager.AdjustSdbArgument(
                    $"shell \"/bin/bash {_sessionConfiguration.ProjectTargetPath}/cperf_logcontrol {_hostId} {_sessionConfiguration.ProjectTargetPath}\"");

            var outputLogPath = Path.Combine(traceLocation, _sessionConfiguration.ProjectName + ".log");
            var pdbFilesPath = _sessionConfiguration.ProjectHostBinPath;

            using (var outStreamWriter = new StreamWriter(File.Create(outputLogPath)))
            {
                try
                {
                    proc.Start();
                    new DebugDataInjectionFilter
                    {
                        Input = proc.StandardOutput,
                        Output = outStreamWriter,
                        PdbDirectory = pdbFilesPath
                    }.Process();
                }
                catch (Exception e)
                {
                    WriteToOutput($"[ERROR] Reading Cperf log {e.Message}");
                    proc.Close();
                }
            }
        }


        private bool DownloadTraceFiles()
        {
            WriteToOutput("-> Downloading zip file...");
            if (DownloadFile($"{_sessionConfiguration.ProjectTargetPath}/{CperfZipName}", Path.Combine(_sessionConfiguration.ProjectHostPath, SessionArchiveLocalFile)))
            {
                WriteToOutput("[ERROR]\n");
                return true;
            }

            WriteToOutput("-> Extracting zip file...");
            if (ExtractZipFile(Path.Combine(_sessionConfiguration.ProjectHostPath, SessionArchiveLocalFile), SessionDirectory))
            {
                WriteToOutput("[ERROR]\n");
                return true;
            }

            File.Delete(Path.Combine(_sessionConfiguration.ProjectHostPath, SessionArchiveLocalFile));
            return false;
        }

        private void WriteSessionFiles(DateTime dt)
        {
            var timestamp = (dt.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

            var sessionFile = new SessionProperties(Path.Combine(SessionDirectory, SessionConstants.SessionFileName));
            sessionFile.SetProperty("CoreClrProfilerReport", "name", _sessionConfiguration.ProjectName + ".log");
            sessionFile.SetProperty("CoreClrProfilerReport", "path", "./");
            var uid = GetMetadataUid(SessionDirectory);
            var spath = "ust/uid/" + uid;
            if (Directory.Exists(Path.Combine(SessionDirectory, spath, "32-bit")))
            {
                spath = spath + "/32-bit";
            }
            else
            {
                spath = spath + "/64-bit";
            }

            sessionFile.SetProperty("CtfReport", "name", "metadata");
            sessionFile.SetProperty("CtfReport", "path", spath);
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

            sessionFile.SetProperty("ProfilingOptions", "CperfHostCode", _hostId);
            sessionFile.SetProperty("ProfilingOptions", "Sleep", _sessionConfiguration.SleepTime);
            sessionFile.Save();
        }

        private static string GetMetadataUid(string unzipedDirName)
        {
            var upath = Path.Combine(unzipedDirName, "ust", "uid");
            try
            {
                var dirs = Directory.GetDirectories(upath);
                return Path.GetFileName(dirs[0]); // must be one
            }
            catch (Exception)
            {
                return "1001"; // default value
            }
        }

        private static bool DownloadFile(string source, string destination)
        {
            var process = SDBLib.CreateSdbProcess();
            SDBLib.RunSdbProcess(process, DeviceManager.AdjustSdbArgument($"pull {source} \"{destination}\""));
            var rc = process.ExitCode;
            process.Close();
            return rc != 0;
        }

        private static bool UploadFile(string source, string destination)
        {
            var process = SDBLib.CreateSdbProcess();
            SDBLib.RunSdbProcess(process, DeviceManager.AdjustSdbArgument($"push \"{source}\" \"/opt/usr/home/owner/{destination}\""));
            var rc = process.ExitCode;
            process.Close();
            return rc != 0;
        }

        private static bool ExtractZipFile(string archivePath, string extractPath)
        {
            try
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(archivePath, extractPath);
                return false;
            }
            catch (Exception e)
            {
                WriteToOutput($"[ERROR] {e.Message}");
                return true;
            }
        }


        private static void WriteToOutput(string message)
        {
            ProfilerPlugin.Instance.WriteToOutput(message);
        }

        private void SetState(ProfileSessionState state, bool notify = false)
        {
            State = state;
            if (notify)
            {
                NotifyStatusChange();
            }
        }

        private void NotifySystemInfo(SysInfoItem item)
        {
            foreach (var listener in GetListeners())
            {
                SafeCallListener(() => listener.SysInfoRead(item));
            }
        }

        private void NotifyStatusChange()
        {
            foreach (var listener in GetListeners())
            {
                SafeCallListener(() => listener.StateChanged(State));
            }
        }

        private IEnumerable<IProfileSessionListener> GetListeners()
        {
            lock (_sessionListeners)
            {
                return new List<IProfileSessionListener>(_sessionListeners);
            }
        }

        private void SafeCallListener(Action action)
        {
            new Thread(delegate()
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    WriteToOutput($"[ERROR] {e.Message}");
                }

            }).Start();

        }

    }
}
