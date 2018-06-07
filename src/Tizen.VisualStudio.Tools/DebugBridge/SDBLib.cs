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
using System.Threading.Tasks;
using Tizen.VisualStudio.Tools.DebugBridge.SDBCommand;
using Tizen.VisualStudio.Tools.Data;
using Tizen.VisualStudio.Tools.Utilities;

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

        public static Process CreateSdbProcess(bool createNoWindow = true, bool isEnableRasingEvent = false)
        {
            string sdbPath = SdbFilePath;

            if (string.IsNullOrEmpty(sdbPath) || File.Exists(sdbPath) == false)
            {
                return null;
            }

            Process sdbProcess = new Process();
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

        public static void RunSdbProcess(Process sdbProcess, string argument, bool outputlog = false)
        {
            if (sdbProcess == null)
            {
                return;
            }

            PrintSdbCommand(argument);

            sdbProcess.StartInfo.Arguments = argument;
            sdbProcess.Start();

            if (outputlog == true)
            {
                OutputWindow output = new OutputWindow();
                StreamReader reader = sdbProcess.StandardOutput;
                string line = string.Empty;
                output.CreatePane("Tizen");
                output.ActivatePane("Tizen");
                do
                {
                    line = reader.ReadLine();
                    output.PrintString(line + "\n");
                }
                while (line != null);
            }

            sdbProcess.WaitForExit();
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

        public static async Task<int> RunSdbProcessAsync(Process sdbProcess, string argument, bool outputlog = false, TizenAutoWaiter waiter = null)
        {
            sdbProcess.StartInfo.Arguments = argument;
            PrintSdbCommand(argument);

            return await RunProcessAsync(sdbProcess, waiter).ConfigureAwait(false);
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
        public static Thread RequestToTargetAsync(string serialNumber, MessageHandler msgCharHandler, LastWorkExecutor lastWorker, string protocol, string cmd, string[] args)
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

        private static SDBConnection EstablishConnection(string serialNumber)
        {
            SDBConnection sdbConnection = SDBConnection.Create();

            if (sdbConnection == null)
            {
                Console.WriteLine("Failed to establish SDB conenction.");
                return null;
            }

            string msgForConnection = "host:transport:" + serialNumber;
            SDBRequest request = SDBConnection.MakeRequest(msgForConnection);
            SDBResponse response = sdbConnection.Send(request);

            if (response.IOSuccess && response.Okay)
            {
                return sdbConnection;
            }
            else
            {
                Console.WriteLine("Failed to get SDB response. {0}", response.Message);
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
            SDBResponse response = sdbConnection.Send(request);

            if (!response.IOSuccess || !response.Okay)
            {
                Console.WriteLine("Failed to get SDB response. {0}", response.Message);
                sdbConnection.Close();
                return false;
            }

            return true;
        }

        private static void ConsumeMsgInBG(SDBConnection sdbConnection, MessageHandler MsgCharHandler, LastWorkExecutor LastWorker)
        {
            string responsedMsg;
            try
            {
                while ((responsedMsg = sdbConnection.ReadData(new byte[1])) != string.Empty && responsedMsg != "\0")
                {
                    MsgCharHandler(responsedMsg);
                }

                LastWorker?.Invoke();

                sdbConnection.Close();
            }
            catch (ThreadAbortException abortException)
            {
                Console.WriteLine("Message consumer aborted. {0}", abortException.Message);
                sdbConnection.Close();
            }
        }

        private static Task<int> RunProcessAsync(Process process, TizenAutoWaiter waiter = null)
        {
            var tcs = new TaskCompletionSource<int>();
            process.Exited += (s, ea) => tcs.SetResult(process.ExitCode);

            process.OutputDataReceived += (s, ea) =>
            {
                if (waiter != null && ea.Data != null)
                {
                    if (waiter.IsWaiterSet(ea.Data))
                    {
                        waiter.Waiter.Set();
                    }
                }
            };

            process.Exited += (s, ea) =>
            {
                waiter.OnExit();
                waiter.Waiter.Set();
            };

            bool started = process.Start();
            if (!started)
            {
                throw new InvalidOperationException("Could not start process: " + process);
            }

            process.BeginOutputReadLine();

            return tcs.Task;
        }

        private static void PrintSdbCommand(string sdbCmdArgs)
        {
            OutputWindow output = new OutputWindow();
            string msg = string.Format("{0} : {1} {2}\n",DateTime.Now.ToString(), GetSdbFilePath(), sdbCmdArgs);

            output.CreatePane("Tizen");
            output.ActivatePane("Tizen");
            output.PrintString(msg);
        }
    }
}
