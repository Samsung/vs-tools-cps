/*
 * Copyright 2022 (c) Samsung Electronics Co., Ltd  All Rights Reserved.
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
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Text;

namespace Tizen.VisualStudio.Utilities
{
    public static class Ps1CmdExec
    {
        public static string Execute(string workDir, string argStr)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = $"-NoProfile -ExecutionPolicy unrestricted {argStr}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.WorkingDirectory = string.IsNullOrEmpty(workDir)
                        ? Path.GetDirectoryName(
                            Assembly.GetExecutingAssembly().Location)
                        : workDir;

                var stdOutput = new StringBuilder();
                process.OutputDataReceived += (sender, args) => stdOutput.AppendLine(args.Data);
                string stdError = null;
                try
                {
                    process.Start();
                    process.BeginOutputReadLine();
                    stdError = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                }
                catch (Exception e)
                {
                    throw new Exception("Ps1CmdExec: OS error while executing : " + e.Message, e);
                }

                var message = new StringBuilder();

                if (!string.IsNullOrEmpty(stdError))
                {
                    message.AppendLine(stdError);
                }

                if (stdOutput.Length != 0)
                {
                    message.AppendLine(stdOutput.ToString());
                }

                return message.ToString();
            }
        }
    }
}
