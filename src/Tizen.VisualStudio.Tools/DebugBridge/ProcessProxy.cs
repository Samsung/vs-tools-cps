/*
 *
 * Copyright 2018 (c) Samsung Electronics Co., Ltd  All Rights Reserved.
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
using System.Text.RegularExpressions;
using System.Text;

namespace Tizen.VisualStudio.Tools.DebugBridge
{
    public class ProcessProxy : Process
    {
        public bool SecureMode { get; private set; }
        public string PushPullPrefix { get; private set; }
        public string FileSync { get; private set; }
        public bool IsSupported { get; private set; }
        public bool RootSupported { get; private set; }
        public string Error { get; private set; }
        private Dictionary<string, string> capDic = new Dictionary<string, string>();

        private static Regex regex = new Regex("(\\S*\"[^\"]*\"(\"[^\"]*\")*\\S*)|[^\\s\"]+");

        class SdbCommand
        {
            private ProcessProxy Parent;
            public string Command { get; set; }
            public SdbCommand(ProcessProxy parent)
            {
                Parent = parent;
                Command = string.Empty;
            }

            public bool IsValid(List<string> parameters)
            {
                string[] param = parameters.ToArray();
                switch (Command)
                {
                    case "push":
                        if (param.Length < 2) return true; // Show sdb error for parameters
                        if (!Parent.FileSync.Contains("push")) return false;
                        return param[param[param.Length - 1].Equals("--with-utf8") ? param.Length - 2 : param.Length - 1].IndexOf(Parent.PushPullPrefix) == 0;
                    case "pull":
                        if (param.Length == 0) return true; // Show sdb error for parameters
                        if (!Parent.FileSync.Contains("pull")) return false;
                        return (Parent.SecureMode ? param[0].IndexOf(Parent.PushPullPrefix) == 0 : true);
                    case "shell":
                        if (param.Length < 1) return false; // Shell not allowed for secure mode
                        param[0] = param[0].Replace("'", "").Replace("\"", "");
                        /* TESTS
                        if (!Parent.SecureMode)
                        {
                            return param[0].IndexOf("rm -f ") < 0;
                        }
                        //*/
                        return (Parent.SecureMode ? param[0].IndexOf("0") == 0 : true);
                    case "root":
                        return Parent.RootSupported;
                    case "dlog":
                        return !Parent.SecureMode; // Not allowed for secure mode
                    case "install":
                        // return false; // TESTS 
                    case "uninstall":
                    case "forward":
                    case "capability":
                    case "devices":
                    case "start-server":
                    case "kill-server":
                    case "get-state":
                    case "get-serialno":
                        break;
                }
                return true;
            }
            /* Add only commands for checking */
            static public readonly string[] Commands =
            {
            "push",
            "pull",
            "shell",
            // /* TESTS */ "install",       /* Always allowed */
            // "uninstall",     /* Always allowed */
            // "forward",       /* Always allowed */
            // "capability",    /* Always allowed */
            // "devices",       /* Always allowed */
            // "start-server",  /* Always allowed */
            // "kill-server",   /* Always allowed */
            // "get-state",     /* Always allowed */
            // "get-serialno",  /* Always allowed */
            "encryption",       /* Not checked */
            "dlog",
            "root"
            };
        }

        public ProcessProxy()
            : base()
        {
            PushPullPrefix = "/home/owner/share/tmp/sdk_tools/";
            SecureMode = false;
            FileSync = "pushpull";
            RootSupported = true;
        }

        private void ReplaceStartInfo(int withError = 0)
        {
            ProcessStartInfo newStartInfo = new ProcessStartInfo("cmd.exe", $@"/q /c echo /Q/A|set /p='{Error}' 1>&2 && exit {withError}");
            newStartInfo.ErrorDialog = StartInfo.ErrorDialog;
            newStartInfo.StandardErrorEncoding = StartInfo.StandardErrorEncoding;
            newStartInfo.StandardOutputEncoding = StartInfo.StandardOutputEncoding;
            newStartInfo.UseShellExecute = StartInfo.UseShellExecute;
            newStartInfo.CreateNoWindow = StartInfo.CreateNoWindow;
            newStartInfo.WorkingDirectory = StartInfo.WorkingDirectory;
            newStartInfo.RedirectStandardError = StartInfo.RedirectStandardError;
            newStartInfo.RedirectStandardInput = StartInfo.RedirectStandardInput;
            newStartInfo.RedirectStandardOutput = StartInfo.RedirectStandardOutput;
            
            StartInfo = newStartInfo;
            Debug.WriteLine($@"{DateTime.Now} {this.ToString()} : Call to sdb has been replaced with error '{Error}'");
        }

        public string GetValueByKey(string key)
        {
            string result;
            return capDic.TryGetValue(key, out result) ? result : null;
        }

        private void GenCapDic(string capString)
        {
            string[] capList = capString.Split('\n');

            foreach (string capItem in capList)
            {
                if (capItem.Contains(":"))
                {
                    string[] capItemSet = capItem.Split(':');

                    if (!capDic.ContainsKey(capItemSet[0]))
                    {
                        capDic.Add(capItemSet[0], capItemSet[1]);
                    }
                }
            }
            PushPullPrefix = GetValueByKey("sdk_toolpath");
            SecureMode = "enabled".Equals(GetValueByKey("secure_protocol"));
            FileSync = GetValueByKey("filesync_support");
            RootSupported = "enabled".Equals(GetValueByKey("rootonoff_support"));
        }

        private void setError(string err)
        {
            if (!string.IsNullOrEmpty(Error)) Error += "\n";
            Error += err;
        }

        private void capability(string param)
        {
            Process p = new Process();
            p.StartInfo.FileName = StartInfo.FileName;
            string returnValue;

            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.Arguments = param + " capability";
            p.Start();

            returnValue = p.StandardOutput.ReadToEnd().Replace("\r", string.Empty);
            p.WaitForExit();

            IsSupported = !string.IsNullOrEmpty(returnValue);
            if (IsSupported)
            {
                GenCapDic(returnValue);
            }
            else
            {
                setError("Can't get capability");
            }
            Debug.WriteLine($@"Secure mode {SecureMode}, PushPullPrefix: {PushPullPrefix}");
        }

        public new bool Start()
        {
            string app = StartInfo.FileName;
            int index = app.LastIndexOf("\\");
            if (index > 0)
            {
                app = app.Remove(0, index + 1);
            }
            if (!app.Equals("sdb.exe")) return base.Start();

            int skip = 0;
            Match match = regex.Match(StartInfo.Arguments);
            SdbCommand cmd = new SdbCommand(this);
            string device_str = "";
            List<string> parameters = new List<string>();

            while (match.Success)
            {
                if (skip > 0)
                {
                    device_str = device_str + " " + match.ToString();
                    skip--; match = match.NextMatch(); continue;
                }
                switch (match.ToString())
                {
                    case "-s":
                    case "--serial":
                        device_str = device_str + " " + match.ToString();
                        skip = 1; match = match.NextMatch();
                        continue;
                    case "-d":
                    case "--device":
                    case "-e":
                    case "--emulator":
                        device_str = device_str + " " + match.ToString();
                        continue;
                }

                if (cmd.Command.Equals(string.Empty))
                {
                    cmd.Command = match.ToString();

                    if (Array.Find(SdbCommand.Commands, cmd_l => cmd_l.Equals(cmd.Command)) == null)
                    {
                        Debug.WriteLine($@"{DateTime.Now} {this.ToString()} (base): {StartInfo.FileName} {StartInfo.Arguments}");
                        return base.Start();
                    }
                    Debug.Write($@"{DateTime.Now} {this.ToString()} : Found command: {cmd.Command} ");
                }
                else
                {
                    parameters.Add(match.ToString().Replace("\"", ""));
                }
                match = match.NextMatch();
            }
            parameters.ForEach(str_l => Debug.Write(str_l + " "));
            Debug.WriteLine("");
            capability(device_str);

            if (!cmd.IsValid(parameters))
            {
                setError($@"Secure mode is {SecureMode}. Parameters are not valid: {StartInfo.FileName} {device_str} {cmd.Command} " + string.Join(" ", parameters.ToArray()));
                Debug.WriteLine($@"{DateTime.Now} {this.ToString()} : {Error}");
                ReplaceStartInfo(1);
            }

            return base.Start();
        }

    }
}
