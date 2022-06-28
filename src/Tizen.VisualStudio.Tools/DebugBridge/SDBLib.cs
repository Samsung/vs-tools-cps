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
using System.Threading;
using Tizen.VisualStudio.Tools.DebugBridge.SDBCommand;
using Tizen.VisualStudio.Tools.Data;
using Tizen.VisualStudio.Utilities;

namespace Tizen.VisualStudio.Tools.DebugBridge
{
    public static class SDBLib
    {
        //private static ToolsInfo toolsInfo = ToolsInfo.Instance();
        private static string sdbfilepath = string.Empty;

        public delegate void MessageHandler(string msg);
        public delegate void LastWorkExecutor();

        private static string SdbFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(ToolsPathInfo.SDBPath) == false)
                {
                    return ToolsPathInfo.SDBPath;
                }
                else
                {
                    return null;
                }
            }

            set
            {
                sdbfilepath = value;
            }
        }

        public static ProcessProxy CreateSdbProcess(bool createNoWindow = true, bool isEnableRasingEvent = false)
        {
            string sdbPath = SdbFilePath;

            if (string.IsNullOrEmpty(sdbPath) || File.Exists(sdbPath) == false)
            {
                return null;
            }

            var sdbProcess = new ProcessProxy();
            sdbProcess.StartInfo.UseShellExecute = false;
            sdbProcess.StartInfo.FileName = sdbPath;
            sdbProcess.StartInfo.Verb = "runas";
            sdbProcess.StartInfo.RedirectStandardInput = true;
            sdbProcess.StartInfo.RedirectStandardOutput = true;
            sdbProcess.StartInfo.RedirectStandardError = true;
            sdbProcess.StartInfo.CreateNoWindow = createNoWindow;
            sdbProcess.EnableRaisingEvents = isEnableRasingEvent;

            return sdbProcess;
        }

        public static string GetSdbFilePath()
        {
            if (string.IsNullOrEmpty(SdbFilePath) == false)
            {
                if (File.Exists(SdbFilePath))
                {
                    FileInfo fileInfo = new FileInfo(SdbFilePath);
                    if (fileInfo.IsReadOnly)
                    {
                        fileInfo.IsReadOnly = false;
                    }

                    return SdbFilePath;
                }
            }

            return SdbFilePath;
        }

        /// <summary>
        /// Send sdb command and get string result of the sdb command.
        /// </summary>
        /// <param name="serialNumber">Serial number to specify target device. Refer to the 1st column of "$sdb devices".</param>
        /// <param name="protocol">Command type of sdb. ex) appcmd, etc</param>
        /// <param name="cmd">Command to be executed. ex)install, appinfo etc.</param>
        /// <param name="args">Command to be executed. ex)App ID, package ID etc.</param>
        /// <returns>Instant which stores returned output and exit value of the sdb command.</returns>
        public static List<string> RequestToTargetSync(string serialNumber, string protocol, string cmd, params string[] args)
        {
            SDBConnection sdbConnection = EstablishConnection(serialNumber);

            if (sdbConnection == null || !SendMsg(sdbConnection, protocol, cmd, args))
            {
                return null;
            }

            List<string> responsedMsg = new List<string>();

            while (true)
            {
                string responsedLine = string.Empty;
                string ch = string.Empty;

                while ((ch) != SDBProtocol.terminator)
                {
                    ch = sdbConnection.ReadData(new byte[1]);
                    responsedLine += ch;
                }

                if (responsedLine == SDBProtocol.terminator)
                {
                    break;
                }

                responsedMsg.Add(responsedLine);
            }

            sdbConnection.Close();
            return responsedMsg;
        }

        /// <summary>
        /// Send sdb command and get string result of the sdb command.
        /// </summary>
        /// <param name="serialNumber">Serial number to specify target device. Refer to the 1st column of "$sdb devices".</param>
        /// <param name="msgCharHandler">Method to be called when each character is returned by the sdb command. String parameter and void return type is required</param>
        /// <param name="lastWorker">Method to be called when the sdb command response is over. Empty parameter and void return type is required. You can leave it as null for permanent works such as a dlogutil.</param>
        /// <param name="protocol">Command type of sdb. ex) appcmd, etc</param>
        /// <param name="cmd">Command to be executed. ex)install, appinfo etc.</param>
        /// <param name="args">Command to be executed. ex)App ID, package ID etc.</param>
        /// <returns>Thread instance running in background</returns>
        public static Thread RequestToTargetAsync(string serialNumber, MessageHandler msgCharHandler, LastWorkExecutor lastWorker,
            string protocol, string cmd, string[] args)
        {
            SDBConnection sdbConnection = EstablishConnection(serialNumber);

            if (msgCharHandler == null || sdbConnection == null || !SendMsg(sdbConnection, protocol, cmd, args))
            {
                return null ;
            }

            Thread mgsConsumerThread = new Thread(() => ConsumeMsgInBG(sdbConnection, msgCharHandler, lastWorker));
            mgsConsumerThread.Start();

            return mgsConsumerThread;
        }

        public static bool SwitchToRoot(SDBDeviceInfo device, bool on)
        {
            return RunSdbCommandAndCheckExitCode(device, $"root {(on ? "on" : "off")}");
        }

        public static bool RemountRootFileSystem(SDBDeviceInfo device, bool readWrite, out string errorMessage)
        {
            return RunSdbShellCommandAndCheckExitStatus(device, $"mount / -o remount,{(readWrite ? "rw" : "ro")}", null,
                out errorMessage, null, true);
        }

        private static SDBConnection EstablishConnection(string serialNumber)
        {
            SDBConnection sdbConnection = SDBConnection.Create();

            if (sdbConnection == null)
            {
                Debug.WriteLine("Failed to establish SDB connection.");
                return null;
            }

            string msgForConnection = "host:transport:" + serialNumber;
            SDBRequest request = SDBConnection.MakeRequest(msgForConnection);

            if (request == null)
            {
                Debug.WriteLine("request is NULL");
                return null;
            }
            SDBResponse response = sdbConnection.Send(request);

            if (response.IOSuccess && response.Okay)
            {
                return sdbConnection;
            }
            else
            {
                Debug.WriteLine($"Failed to get SDB response. {response.Message}");
                sdbConnection.Close();
                return null;
            }
        }

        private static bool SendMsg(SDBConnection sdbConnection, string protocol, string cmd, string[] args)
        {
            string msg = protocol + SDBProtocol.delemeter + cmd;

            foreach (string arg in args)
            {
                msg += (SDBProtocol.delemeter + arg);
            }

            SDBRequest request = SDBConnection.MakeRequest(msg);
            if (request == null)
            {
                Debug.WriteLine("request is NULL");
                return false;
            }
            SDBResponse response = sdbConnection.Send(request);

            if (!response.IOSuccess || !response.Okay)
            {
                Debug.WriteLine($"Failed to get SDB response. {response.Message}");
                sdbConnection.Close();
                return false;
            }

            return true;
        }

        private static void ConsumeMsgInBG(SDBConnection sdbConnection, MessageHandler msgCharHandler, LastWorkExecutor lastWorker)
        {
            string responsedMsg;
            try
            {
                while ((responsedMsg = sdbConnection.ReadData(new byte[1])) != string.Empty && responsedMsg != "\0")
                {
                    msgCharHandler(responsedMsg);
                }

                lastWorker?.Invoke();

                sdbConnection.Close();
            }
            catch (ThreadAbortException abortException)
            {
                Debug.WriteLine($"Message consumer aborted. {abortException.Message}");
                sdbConnection.Close();
            }
        }

        public static bool RunSdbCommandAndGetFirstNonEmptyLine(SDBDeviceInfo device, string сommand,
            out string firstOutputLine, out string errorMessage, TimeSpan? timeout = null)
        {
            firstOutputLine = "";
            string nonEmptyLine = "";
            int exitCode;
            SdbRunResult sdbResult = RunSdbCommand(device, сommand,
                (bool isStdOut, string line) =>
                {
                    if (line != "")
                    {
                        nonEmptyLine = line;
                        return true;
                    }
                    return false;
                },
                out exitCode, timeout);

            if (sdbResult == SdbRunResult.Success)
            {
                firstOutputLine = nonEmptyLine;
                errorMessage = "";
                return true;
            }

            errorMessage = "Cannot run command. " + FormatSdbRunResult(sdbResult, exitCode);
            return false;
        }

        public static bool RunSdbCommandAndGetLastNonEmptyLine(SDBDeviceInfo device, string сommand,
            out string lastOutputLine, out string errorMessage, TimeSpan? timeout = null)
        {
            lastOutputLine = "";
            string nonEmptyLine = "";
            int exitCode;
            SdbRunResult sdbResult = RunSdbCommand(device, сommand,
                (bool isStdOut, string line) =>
                {
                    if (line != "")
                    {
                        nonEmptyLine = line;
                    }
                    return false;
                },
                out exitCode, timeout);

            if (sdbResult == SdbRunResult.Success)
            {
                lastOutputLine = nonEmptyLine;
                errorMessage = "";
                return true;
            }

            errorMessage = "Cannot run command. " + FormatSdbRunResult(sdbResult, exitCode);
            return false;
        }

        public static bool RunSdbCommandAndGetError(SDBDeviceInfo device, string shellCommand,
            OutputDataProcessor outputDataProcessor, out string errorString, TimeSpan? timeout = null)
        {
            int exitResult = 0;
            string nonEmptyLines = "";
            errorString = "";

            SDBLib.SdbRunResult sdbResult = RunSdbCommand(device,
                shellCommand,
                (bool isStdOut, string line) =>
                {
                    if (line != "")
                    {
                        nonEmptyLines += line;
                    }
                    return false;
                },
                out exitResult,
                timeout);
            if (exitResult != 0)
            {
                sdbResult = SdbRunResult.OtherError;
                errorString = nonEmptyLines;
            }
            return (sdbResult == SdbRunResult.Success);
        }

        public static bool RunSdbShellCommand(SDBDeviceInfo device, string shellCommand,
            OutputDataProcessor outputDataProcessor, out string errorString, TimeSpan? timeout = null)
        {
            return RunSdbCommandAndGetError(device, $"shell \"{shellCommand}\"", outputDataProcessor, out errorString, timeout);
        }

        public static bool RunSdbShellSecureCommand(SDBDeviceInfo device, string secureCommand,
            OutputDataProcessor outputDataProcessor, out string errorString, TimeSpan? timeout = null)
        {
            return RunSdbCommandAndGetError(device, $"shell 0 {secureCommand}", outputDataProcessor, out errorString, timeout);
        }

        private const string ShellSuccess = "Success_FA8135A1356D";

        private const string ShellFailure = "Failure_5F87362E14AD";

        public static bool RunSdbShellCommandAndCheckExitStatus(SDBDeviceInfo device, string shellCommand,
            OutputDataProcessor outputDataProcessor, out string errorMessage, TimeSpan? timeout = null, bool isRoot = false)
        {
            int exitCode;
            bool success = false;
            SdbRunResult sdbResult = RunSdbCommand(device,
                $"shell \"{shellCommand} && echo {ShellSuccess} || echo {ShellFailure}\"",
                (bool isStdOut, string line) =>
                {
                    if (line.Contains(ShellSuccess))
                    {
                        success = true;
                        return true;
                    }
                    if (line.Contains(ShellFailure))
                    {
                        return true;
                    }
                    if (outputDataProcessor != null)
                    {
                        if (outputDataProcessor(isStdOut, line))
                        {
                            success = true;
                            return true;
                        }
                    }
                    return false;
                },
                out exitCode, timeout, isRoot);

            if (sdbResult == SdbRunResult.Success)
            {
                if (!success)
                {
                    errorMessage = "Command failed";
                    return false;
                }
                errorMessage = "";
                return true;
            }

            errorMessage = "Cannot run command. " + FormatSdbRunResult(sdbResult, exitCode);
            return false;
        }

        public static bool CheckIsRoot(SDBDeviceInfo device, out bool isRoot, out string errorMessage, TimeSpan? timeout = null)
        {
            string checkIsRootScript = $"if [ $UID == 0 ]; then echo {ShellSuccess}; else echo {ShellFailure}; fi";
            isRoot = false;
            bool result = false;
            bool success = false;
            int exitCode;
            SdbRunResult sdbResult = RunSdbCommand(device, $"shell \"{checkIsRootScript}\"",
                (bool isStdOut, string line) =>
                {
                    if (line.Contains(ShellSuccess))
                    {
                        result = true;
                        success = true;
                        return true;
                    }
                    if (line.Contains(ShellFailure))
                    {
                        success = true;
                        return true;
                    }
                    return false;
                },
                out exitCode, timeout, false);

            if (sdbResult == SdbRunResult.Success)
            {
                if (!success)
                {
                    errorMessage = "Command failed";
                    return false;
                }
                isRoot = result;
                errorMessage = "";
                return true;
            }

            errorMessage = "Cannot run command. " + FormatSdbRunResult(sdbResult, exitCode);
            return false;
        }

        public static bool RunSdbCommandAndCheckExitCode(SDBDeviceInfo device, string command)
        {
            int exitCode;
            return (RunSdbCommand(device, command, out exitCode, TimeSpan.MaxValue) == SdbRunResult.Success) &&
                (exitCode == 0);
        }

        public static SdbRunResult RunSdbCommand(SDBDeviceInfo device, string command,
            out int exitCode, TimeSpan? timeout = null)
        {
            return RunSdbCommand(device, command, null, out exitCode, timeout);
        }

        public enum SdbRunResult
        {
            Success,
            Timeout,
            CreateProcessError,
            RunProcessError,
            OtherError
        }

        public delegate bool OutputDataProcessor(bool isStdOut, string line);

        public static SdbRunResult RunSdbCommand(SDBDeviceInfo device, string command,
            OutputDataProcessor outputDataProcessor, TimeSpan? timeout = null)
        {
            int exitCode;
            return RunSdbCommand(device, command, outputDataProcessor, out exitCode, timeout);
        }

        /// <summary>
        /// Run the specified command using SDB and process its output using outputDataProcessor (if its not null).
        /// The outputDataProcessor delegate returns true to indicate it got some expected result so the function
        /// may exit (the command continues to work), and false if the result has not been obtained yet so the delegate
        /// "wants" to continue receiving the command output.
        /// </summary>
        /// <param name="command">the command to run (e.g. 'shell \"ls /usr\"')</param>
        /// <param name="outputDataProcessor">the delegate which processes the output of the command and returns
        /// true to stop (in this case the function exits but the command continues to work) or false to continue</param>
        /// <param name="outputLog">only if outputDataProcessor == null: copy process output to Tizen pane</param>
        /// <param name="exitCode">if SDB process has finished before the function returns then the SDB process exit code
        /// is copied to this out parameter else it's equal to -1</param>
        /// <param name="timeout">the timeout: if it elapses before outputDataProcessor returned true or the
        /// command finished then the function returns Timeout (the command continues to work);
        /// the default value is 30 seconds</param>
        /// <param name="isRoot">whether to execute command from root</param>
        /// <returns>
        /// if outputDataProcessor is not null:
        ///   Success if outputDataProcessor returned true, or another SdbCommandResult value otherwise (if an error
        ///   occurred or the command finished or the timeout elapsed);
        /// if outputDataProcessor is null:
        ///   Success if the command finished before the timeout elapsed, or another SdbCommandResult value otherwise.
        /// </returns>
        public static SdbRunResult RunSdbCommand(SDBDeviceInfo device, string command,
            OutputDataProcessor outputDataProcessor, out int exitCode, TimeSpan? timeout, bool isRoot)
        {
            if (isRoot)
            {
                if (!SwitchToRoot(device, true))
                {
                    exitCode = -1;
                    return SdbRunResult.OtherError;
                }
            }
            try
            {
                return RunSdbCommand(device, command, outputDataProcessor, out exitCode, timeout);
            }
            finally
            {
                if (isRoot)
                {
                    SwitchToRoot(device, false);
                }
            }
        }

        public static SdbRunResult RunSdbCommand(SDBDeviceInfo device, string command,
            OutputDataProcessor outputDataProcessor, out int exitCode, TimeSpan? timeout = null)
        {
            Debug.Assert((timeout == null) || (timeout >= TimeSpan.Zero));

            exitCode = -1;
            TimeSpan effectiveTimeout = timeout ?? TimeSpan.FromSeconds(30); // the default timeout is 30 seconds
            using (ProcessProxy process = CreateSdbProcess(true, false))
            {
                if (process == null)
                {
                    return SdbRunResult.CreateProcessError;
                }
                process.StartInfo.Arguments = DeviceManager.AdjustSdbArgument(device, command);
                Debug.WriteLine("{0} RunSdbCommand command '{1}'", DateTime.Now, process.StartInfo.Arguments);
                if (outputDataProcessor != null)
                {
                    object eventsGuard = new object();
                    var gotOutputEvent = new ManualResetEvent(false);
                    try
                    {
                        bool stopped = false; // should be volatile actually but it's not allowed for locals
                        process.OutputDataReceived += (sender, args) =>
                        {
                            if (!stopped && (args.Data != null))
                            {
                                lock (eventsGuard)
                                {
                                    if (outputDataProcessor == null)
                                    {
                                        return;
                                    }
                                    if (outputDataProcessor(true, args.Data))
                                    {
                                        gotOutputEvent.Set();
                                        stopped = true;
                                    }
                                }
                            }
                        };
                        process.ErrorDataReceived += (sender, args) =>
                        {
                            if (!stopped && (args.Data != null))
                            {
                                lock (eventsGuard)
                                {
                                    if (outputDataProcessor == null)
                                    {
                                        return;
                                    }
                                    if (outputDataProcessor(false, args.Data))
                                    {
                                        gotOutputEvent.Set();
                                        stopped = true;
                                    }
                                }
                            }
                        };
                        if (!RunProcess(process))
                        {
                            return SdbRunResult.RunProcessError;
                        }
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        Stopwatch watch = Stopwatch.StartNew();
                        do
                        {
                            try
                            {
                                if (process.WaitForExit(0))
                                {
                                    process.WaitForExit(); // wait until redirected stdin/stdout streams are processed
                                    exitCode = process.ExitCode;
                                    return SdbRunResult.Success;
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex.Message);
                                return SdbRunResult.OtherError;
                            }
                        }
                        while (watch.Elapsed < effectiveTimeout);
                        if (!process.HasExited)
                        {
                            process.Kill();
                        }
                    }
                    finally
                    {
                        lock (eventsGuard)
                        {
                            outputDataProcessor = null;
                        }
                        gotOutputEvent.Dispose();
                    }
                }
                else // outputDataProcessor == null
                {
                    if (!RunProcess(process))
                    {
                        return SdbRunResult.RunProcessError;
                    }
                    try
                    {
                        double timeoutMilliseconds = effectiveTimeout.TotalMilliseconds;
                        if (process.WaitForExit((timeoutMilliseconds <= int.MaxValue) ? (int)timeoutMilliseconds : int.MaxValue))
                        {
                            exitCode = process.ExitCode;
                            return SdbRunResult.Success;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        return SdbRunResult.OtherError;
                    }
                }
            }
            return SdbRunResult.Timeout;
        }

        private static bool RunProcess(ProcessProxy process)
        {
            try
            {
                if (!process.Start()) // process reuse is not expected
                {
                    Debug.WriteLine($"Cannot start {process.StartInfo.FileName}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
            return true;
        }

        public static string FormatSdbRunResult(SdbRunResult commandResult, int? exitCode = null)
        {
            string msg;
            switch (commandResult)
            {
                case SdbRunResult.Success:
                    if (exitCode.HasValue && (exitCode != 0) && (exitCode != -1))
                    {
                        msg = $"SDB exit code is {exitCode}";
                    }
                    else
                    {
                        msg = "";
                    }
                    break;
                case SdbRunResult.CreateProcessError:
                    msg = "Failed to get sdb.exe program";
                    break;
                case SdbRunResult.RunProcessError:
                    msg = "SDB run error";
                    break;
                case SdbRunResult.Timeout:
                    msg = "SDB timeout";
                    break;
                default:
                    msg = "SDB error";
                    break;
            }
            return msg;
        }

        public static bool RemoveForwardTcpPort(SDBDeviceInfo device, int localPort, out string errorMessage)
        {
            string lastLine;
            bool success = SDBLib.RunSdbCommandAndGetLastNonEmptyLine(device,
                $"forward --remove tcp:{localPort}", out lastLine, out errorMessage);
            if (success && lastLine.StartsWith("error:"))
            {
                errorMessage = lastLine;
                success = false;
            }
            return success;
        }

        public static bool ForwardTcpPort(SDBDeviceInfo device, int localPort, int remotePort, out string errorMessage)
        {
            // TODO!! do need to remove port forwarding first?
            RemoveForwardTcpPort(device, localPort, out errorMessage); // remove forward error is a valid case

            string lastLine;
            bool success = SDBLib.RunSdbCommandAndGetLastNonEmptyLine(device,
                $"forward tcp:{localPort} tcp:{remotePort}", out lastLine, out errorMessage);
            if (success && lastLine.StartsWith("error:"))
            {
                errorMessage = lastLine;
                success = false;
            }
            return success;
        }
    }
}
