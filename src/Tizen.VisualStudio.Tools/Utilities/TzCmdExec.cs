/*
 * Copyright 2021 (c) Samsung Electronics Co., Ltd  All Rights Reserved.
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
using System.IO;
using System.Text;
using System.Windows;
using Tizen.VisualStudio.Tools.Data;

namespace Tizen.VisualStudio.Utilities
{
    public class TzCmdExec
    {
        public string RunTzCmnd(string arg, bool isAsync = false)
        {
            var process = new System.Diagnostics.Process();

            process.StartInfo.FileName = @"C:\Windows\System32\cmd.exe";
            process.StartInfo.Arguments = arg;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;
            if (string.IsNullOrEmpty(ToolsPathInfo.TizenCorePath) || !Directory.Exists(ToolsPathInfo.TizenCorePath))
            {
                string msg = "Tizen Core path is not set";

                if (isAsync)
                {
                    return "[null]:" + msg;
                }
                _ = MessageBox.Show(msg);
                return null;
            }
            process.StartInfo.WorkingDirectory = ToolsPathInfo.TizenCorePath;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
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
                throw new Exception("OS error while executing : " + e.Message, e);
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
