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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NetCore.Profiler.Common.Helpers;
using NetCore.Profiler.Extension.VSPackage;
using Tizen.VisualStudio.Tools.Data;
using Tizen.VisualStudio.Tools.DebugBridge;
using Tizen.VisualStudio.Tools.DebugBridge.SDBCommand;
using Tizen.VisualStudio.Tools.Utilities;
using Tizen.VisualStudio.Utilities;

namespace NetCore.Profiler.Extension.Launcher.Model
{
    /// <summary>
    /// A base generic class for classes representing a running %Core %Profiler (<see cref="ProfileSession"/>) or
    /// memory profiling (<see cref="HeaptrackSession"/>) session.
    /// </summary>
    public abstract class AbstractSession<TSessionListener, TSessionState, TSessionConfiguration>
        where TSessionConfiguration : AbstractSessionConfiguration
    {
        public readonly TimeSpan SocketConnectTimeout = TimeSpan.FromSeconds(10);

        public string ProjectDirectory;
        public string DeviceName;
        public string SessionDirectory { get; protected set; }
        public TSessionState State { get; private set; }

        protected readonly object _logFileLock = new object();

        protected readonly TSessionConfiguration _sessionConfiguration;

        protected readonly List<TSessionListener> _sessionListeners = new List<TSessionListener>();

        protected string _tizenVersion;

        protected string _sdkToolPath;

        protected bool _isSecureProtocol;

        private static Regex _profilerNameFromLogPattern = new Regex(@"\/profctl_(\w+)\.log$");

        protected SDBDeviceInfo _selectedDevice;

        protected bool AsyncError { get { return _asyncErrorFlag; } }
        private volatile bool _asyncErrorFlag;

        private EventWaitHandle _asyncErrorEvent = new ManualResetEvent(false);

        private Task _asyncErrorTask;

        private readonly EventWaitHandle _launchAppStartedEvent = new ManualResetEvent(false);

        private bool _errorShown;

        protected AbstractSession(SDBDeviceInfo device, TSessionConfiguration sessionConfiguration)
        {
            _selectedDevice = device;
            var cap = new SDBCapability(_selectedDevice);
            _tizenVersion = cap.GetValueByKey("platform_version");
            if (!ProfilerPlugin.IsTizenVersionSupported(_tizenVersion, false))
            {
                throw new Exception($"Target platform version {_tizenVersion} is not supported");
            }
            _sdkToolPath = cap.GetValueByKey("sdk_toolpath");
            _isSecureProtocol = cap.GetAvailabilityByKey("secure_protocol");
            _sessionConfiguration = sessionConfiguration;
            ProjectDirectory = _sessionConfiguration.ProjectHostPath;
            DeviceName = _selectedDevice.Name;
            _asyncErrorTask = Task.Run(() =>
            {
                try
                {
                    _asyncErrorEvent.WaitOne();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                DisposeHelper.SafeDispose(ref _asyncErrorEvent);
            });
        }

        public void Destroy()
        {
            try
            {
                _asyncErrorEvent.Set();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        protected void SetAsyncError()
        {
            _asyncErrorFlag = true;
            _asyncErrorEvent.Set();
        }

        protected string GetOnDemandFolderPath()
        {
            return ToolsPathInfo.GetOnDemandFolderPath(_tizenVersion);
        }

        private string _profctlPath;

        private readonly object _profctlPathLock = new object();

        protected string GetProfctlPath()
        {
            if (_profctlPath == null)
            {
                lock (_profctlPathLock)
                {
                    if (_profctlPath == null) // may be changed from another thread
                    {
                        _profctlPath = FindProfctl();
                    }
                }
                if (_profctlPath == null)
                {
                    throw new Exception("Required utility 'profctl' was not found on the target");
                }
            }
            return _profctlPath;
        }

        private string FindProfctl() // temporary: support both profctl installed from rpm and from tar.gz
        {
            string result = $"{_sdkToolPath}/profctl/profctl";
            string errorMessage;
            if (!DeployHelper.FileExists(_selectedDevice, result, out errorMessage))
            {
                result = "/usr/bin/profctl";
                if (!DeployHelper.FileExists(_selectedDevice, result, out errorMessage))
                {
                    return null;
                }
            }
            DebugWriteToOutput($"Found 'profctl' utility: {result}");
            return result;
        }

        protected string GetSessionDirName(string prefix)
        {
            return Path.Combine(ProjectDirectory, ProfilerPlugin.TizenProfilerDirectory,
                prefix + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        }

        protected virtual bool CheckConfiguration()
        {
            string toolsPath = GetOnDemandFolderPath();
            if (String.IsNullOrEmpty(toolsPath))
            {
                DisplaySessionError("Tizen Tools are not configured (tools path not set)");
                return false;
            }

            if (!Directory.Exists(toolsPath))
            {
                DisplaySessionError($"Folder \"{toolsPath}\" not found");
                return false;
            }

            return true;
        }

        protected virtual string GetSdbLaunchArguments()
        {
            return "";
        }

        private string GetSdbLaunchCommand(string sdkCode)
        {
            string result = $"-s {_selectedDevice.Serial} ";
            string args = GetSdbLaunchArguments();
            string space = (args != "") ? " " : "";
            if (_isSecureProtocol)
            {
                result += $"shell 0 vs_sdklaunch {sdkCode} {_sessionConfiguration.AppId}{space}{args}";
            }
            else
            {
                result += $"shell sh -c \"launch_app {_sessionConfiguration.AppId} __AUL_SDK__ {sdkCode}{space}{args}\"";
            }
            return result;
        }

        protected bool StartRemoteApplication(string sdkCode)
        {
            string appId = _sessionConfiguration.AppId;
            DebugWriteToOutput($"Starting launch_app({appId}; SDK={sdkCode})");
            ProcessProxy launchAppProcess = SDBLib.CreateSdbProcess(true, true);
            if (launchAppProcess == null)
            {
                WriteToOutput(SDBLib.FormatSdbRunResult(SDBLib.SdbRunResult.CreateProcessError));
                return false;
            }
            launchAppProcess.StartInfo.Arguments = GetSdbLaunchCommand(sdkCode);
            string firstOutputLine = null;
            _launchAppStartedEvent.Reset();
            launchAppProcess.OutputDataReceived += ((sender, e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    firstOutputLine = e.Data;
                    _launchAppStartedEvent.Set();
                    DebugWriteToOutput($"{appId} : {e.Data}");
                }
            });
            launchAppProcess.ErrorDataReceived += ((sender, e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    DebugWriteToOutput($"{appId} [StdErr] {e.Data}");
                }
            });
            launchAppProcess.Exited += (object sender, EventArgs e) =>
            {
                DebugWriteToOutput($"launch_app({appId}) finished");
                launchAppProcess.Dispose();
            };
            Debug.WriteLine("{0} {1} StartRemoteApplication command '{2}'", DateTime.Now, this.ToString(), launchAppProcess.StartInfo.Arguments);
            launchAppProcess.Start();
            DebugWriteProcessToOutput(launchAppProcess);
            try
            {
                launchAppProcess.BeginOutputReadLine();
                launchAppProcess.BeginErrorReadLine();
                if (_launchAppStartedEvent.WaitOne(30000))
                {
                    if (firstOutputLine.EndsWith("launch failed"))
                    {
                        WriteToOutput($"launch_app({appId}) failed");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return true;
        }

        protected bool SendCommandToControlProcess(NetworkStream stream, string command)
        {
            if (stream != null)
            {
                try
                {
                    Debug.WriteLine("{0} {1} SendCommandToControlProcess command '{2}'", DateTime.Now, this.ToString(), command);
                    byte[] data = Encoding.ASCII.GetBytes(command + '\n');
                    stream.Write(data, 0, data.Length);
                    stream.Flush();
                    DebugWriteToOutput($"Command sent to Control Process: \"{command}\"");
                    return true;
                }
                catch (Exception ex)
                {
                    WriteToOutput($"Cannot send \"{command}\" command to Control Process. {ex.Message}");
                }
            }
            return false;
        }

        // read line from 'reader' until success or 'timeout' elapses or 'AsyncError' set
        protected string ReadLineIfNotAsyncError(StreamReader reader, TimeSpan timeout)
        {
            Task<string> readLineTask = reader.ReadLineAsync();
            var tasks = new Task[] { readLineTask, _asyncErrorTask };
            return (Task.WaitAny(tasks, timeout) == 0) ? readLineTask.Result : null;
        }

        protected string ReadLineIfNotAsyncError(StreamReader reader)
        {
            Task<string> readLineTask = reader.ReadLineAsync();
            return (Task.WaitAny(readLineTask, _asyncErrorTask) == 0) ? readLineTask.Result : null;
        }

        // connect to a port and read "ready" from it, return StreamReader on success (a caller must dispose it
        // after finished using the stream reader!)
        protected StreamReader TryConnectToPort(int port, int streamReaderBufferSize, TimeSpan timeout,
            out string errorMessage)
        {
            NetworkStream networkStream; // will be disposed when the returned StreamReader disposes
            StreamReader result;
            Debug.WriteLine("{0} {1} call StreamReader TryConnectToPort", DateTime.Now, this.ToString());
            TryConnectToPort(port, streamReaderBufferSize, false, timeout, out networkStream,
                out result, out errorMessage);
            return result;
        }

        protected bool TryConnectToPort(int port, int streamReaderBufferSize, TimeSpan timeout,
            out NetworkStream networkStream, out StreamReader streamReader, out string errorMessage)
        {
            Debug.WriteLine("{0} {1} call bool TryConnectToPort", DateTime.Now, this.ToString());
            return TryConnectToPort(port, streamReaderBufferSize, true, timeout, out networkStream,
                out streamReader, out errorMessage);
        }

        // connect to a port and read "ready" from it
        private bool TryConnectToPort(int port, int streamReaderBufferSize, bool needNetworkStream, TimeSpan timeout,
            out NetworkStream networkStream, out StreamReader streamReader, out string errorMessage)
        {
            var endPoint = new IPEndPoint(IPAddress.Loopback, port);
            DebugWriteToOutput($"Connecting to {endPoint}...");
            networkStream = null;
            streamReader = null;
            errorMessage = null;
            int attempt = 0;
            var timer = Stopwatch.StartNew();
            while (!AsyncError)
            {
                string line;
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    socket.Connect(endPoint);
                    Debug.WriteLine("{0} {1}({2}) connect to port {3}", DateTime.Now, this.ToString(), ((IPEndPoint)socket.LocalEndPoint).Port, ((IPEndPoint)socket.RemoteEndPoint).Port);
                    ++attempt;
                    networkStream = new NetworkStream(socket, ownsSocket: true);
                    streamReader = new StreamReader(networkStream, Encoding.UTF8, false, streamReaderBufferSize,
                        leaveOpen: needNetworkStream);
                    TimeSpan remaining = timeout - timer.Elapsed;
                    if (remaining < TimeSpan.Zero)
                    {
                        remaining = TimeSpan.FromMilliseconds(100);
                    }
                    line = ReadLineIfNotAsyncError(streamReader, remaining);
                    if (AsyncError)
                    {
                        break;
                    }
                    if ((line != null) && line.StartsWith("ready"))
                    {
                        Debug.WriteLine("{0} {1}({2}) connected to port {3}", DateTime.Now, this.ToString(), ((IPEndPoint)socket.LocalEndPoint).Port, ((IPEndPoint)socket.RemoteEndPoint).Port);
                        DebugWriteToOutput($"Connected to {endPoint}, got \"{line}\" reply (attempt #{attempt})");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    if (streamReader != null)
                    {
                        DisposeHelper.SafeDispose(ref streamReader);
                        if (needNetworkStream) // i.e. leaveOpen was true for streamReader
                        {
                            DisposeHelper.SafeDispose(ref networkStream); // will dispose socket as well
                        }
                    }
                    else if (networkStream != null)
                    {
                        DisposeHelper.SafeDispose(ref networkStream); // will dispose socket as well
                    }
                    else
                    {
                        Debug.WriteLine("{0} {1}({2}) safe dispose", DateTime.Now, this.ToString(), ((IPEndPoint)socket.LocalEndPoint).Port);
                        DisposeHelper.SafeDispose(ref socket);
                    }
                    throw;
                }
                if (line != null) // received something different from "ready"
                {
                    try
                    {
                        Debug.WriteLine("{0} {1}({2}) shutdown", DateTime.Now, this.ToString(), ((IPEndPoint)socket.LocalEndPoint).Port);
                        socket.Shutdown(SocketShutdown.Both);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                    errorMessage = $"Unexpected line received: \"{line}\"";
                }
                if (errorMessage != null)
                {
                    break;
                }
                // if received null line then reconnect while not timeout, exit otherwise
                if (timer.Elapsed >= timeout)
                {
                    break;
                }
                DisposeHelper.SafeDispose(ref streamReader);
                if (needNetworkStream)
                {
                    DisposeHelper.SafeDispose(ref networkStream);
                }
                Thread.Sleep(50); // reduce CPU load
            }
            DisposeHelper.SafeDispose(ref streamReader);
            if (needNetworkStream)
            {
                DisposeHelper.SafeDispose(ref networkStream);
            }
            errorMessage = AsyncError ? "Connect canceled" : "Timeout trying to connect";
            return false;
        }

        protected bool InstallTpk()
        {
            const string AppDir = "/opt/usr/globalapps/";

            string tpkPath = _sessionConfiguration.GetTpkPath();

            DebugWriteToOutput($"Checking Tizen package {tpkPath}...");
            string signatureFileName = "signature1.xml";
            string prefix = AppDir + _sessionConfiguration.AppId + '/';

            string tmpFile = Path.GetTempFileName();
            try
            {
                if (!DownloadFile(prefix + signatureFileName, tmpFile))
                {
                    signatureFileName = "author-signature.xml";
                    if (!DownloadFile(prefix + signatureFileName, tmpFile))
                    {
                        signatureFileName = null;
                    }
                }

                if (signatureFileName != null)
                {
                    string signatureFilePath = Path.Combine(Path.GetDirectoryName(tpkPath), "tpkroot", signatureFileName);
                    if (DeployHelper.AreFilesEqual(tmpFile, signatureFilePath))
                    {
                        DebugWriteToOutput($"Tizen package {tpkPath} is already installed");
                        return true;
                    }
                }
            }
            finally
            {
                try { File.Delete(tmpFile); } catch { }
            }

            // Install Tizen Package with binary
            WriteToOutput($"Installing Tizen package \"{tpkPath}\"...");
            bool success = false;
            string errorMessage;
            // TODO!! show wait dialog (see InstallTizenPackage in Tizen.VisualStudio.ProjectSystem.VS\Debug\Launcher.cs)
            success = DeployHelper.InstallTpk(_selectedDevice, tpkPath,
                (bool isStdOut, string line) =>
                {
                    if (!isStdOut) // error output
                        {
                        WriteToOutput(line);
                    }

                    return false;
                },
                out errorMessage,
                TimeSpan.FromMinutes(5));
            if (!success)
            {
                WriteToOutput($"[InstallTpk] {errorMessage}");
            }
            return success;
        }

        protected bool DownloadFile(string source, string destination)
        {
            string errorString;
            if (!SDBLib.RunSdbCommandAndGetError(_selectedDevice, $"pull {source} \"{destination}\"", null, out errorString))
            {
                WriteToOutput(Tizen.VisualStudio.Utilities.StringHelper.CombineMessages(
                    "Cannot execute command.", errorString));
                return false;
            }
            return true;
        }

        // download a log file from a Tizen device, change LF to CR LF, and remove the file from the target
        protected void DownloadAndDeleteOnTargetLogFile(string sourceLogPathName, string destinationLogFileName)
        {
            if (!String.IsNullOrEmpty(SessionDirectory))
            {
                string tempFileName = Path.GetTempFileName();
                try
                {
                    if (DownloadFile(sourceLogPathName, tempFileName))
                    {
                        string errorString;
                        FileHelper.CopyToWindowsText(tempFileName, Path.Combine(SessionDirectory, destinationLogFileName));
                        bool successResult;

                        if (_isSecureProtocol)
                        {
                            string profilerName = _profilerNameFromLogPattern.Match(sourceLogPathName).Groups[1].Value;
                            successResult = SDBLib.RunSdbShellSecureCommand(_selectedDevice, $"vs_profiler_log_remove {profilerName}", null, out errorString);
                        }
                        else
                        {
                            successResult = SDBLib.RunSdbShellCommand(_selectedDevice, $"rm -f {sourceLogPathName}", null, out errorString);
                        }
                        if (!successResult)
                        {
                            WriteToOutput(Tizen.VisualStudio.Utilities.StringHelper.CombineMessages(
                                "Cannot execute command.", errorString));
                        }
                    }
                }
                finally
                {
                    try { File.Delete(tempFileName); } catch { }
                }
            }
        }

        protected bool InstallProfiler(params string[] packageNames)
        {
            var installer = new OnDemandInstaller(device: _selectedDevice, supportRpms: false, supportTarGz: true,
                onMessage: (msg) => ProfilerPlugin.Instance.WriteToOutput(msg));
            if (!installer.Install(packageNames))
            {
                DisplaySessionError(Tizen.VisualStudio.Utilities.StringHelper.CombineMessages(
                    "Cannot check/install the required packages.\n", installer.ErrorMessage));
                return false;
            }
            return true;
        }

        protected void DisplaySessionError(string message)
        {
            WriteToOutput(message);
            if (!_errorShown)
            {
                _errorShown = true;
                Task.Run(() => ProfilerPlugin.Instance.ShowError(message));
            }
        }

        [Conditional("DEBUG")]
        protected void DebugWriteProcessToOutput(Process process)
        {
            DebugWriteToOutput($"{Path.GetFileNameWithoutExtension(process.StartInfo.FileName)} {process.StartInfo.Arguments}");
        }

        [Conditional("DEBUG")]
        protected void DebugWriteToOutput(string message)
        {
            WriteToOutput($"[DBG] {{{Thread.CurrentThread.ManagedThreadId}}} {message}", true);
        }

        protected static void WriteToOutput(string message, bool showDateTime = true)
        {
            message = message.Replace('\n', ' ');
            if (showDateTime)
            {
                message = $"{DateTime.Now.ToDebugString()} {message}";
            }
            ProfilerPlugin.Instance.WriteToOutput(message);
        }

        public void AddListener(TSessionListener listener)
        {
            lock (_sessionListeners)
            {
                if (!_sessionListeners.Contains(listener))
                {
                    _sessionListeners.Add(listener);
                }
            }
        }

        public void RemoveListener(TSessionListener listener)
        {
            lock (_sessionListeners)
            {
                _sessionListeners.Remove(listener);
            }
        }

        protected IEnumerable<TSessionListener> GetListeners()
        {
            lock (_sessionListeners)
            {
                return new List<TSessionListener>(_sessionListeners);
            }
        }

        protected void SetState(TSessionState state, bool notify = false)
        {
            State = state;
            if (notify)
            {
                NotifyStatusChange();
            }
        }

        protected abstract void NotifyStatusChange();
    }
}
