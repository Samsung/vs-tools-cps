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

namespace Tizen.VisualStudio.Tools.DebugBridge.SDBCommand
{
    public class SDBAppCmd
    {
        public int ExitCode { get; }
        public string RetrunString { get; }
        public bool IsTargetFound { get; }
        public List<string> ConsoleOutput { get; }

        public SDBAppCmd(SDBDeviceInfo device, params string[] args)
        {
            List<string> rawItemList = SDBLib.RequestToTargetSync(device.Serial, SDBProtocol.appcmd, CombineArgs(args));
            ConsoleOutput = new List<string>();

            IsTargetFound = (rawItemList != null);
            RetrunString = string.Empty;
            ExitCode = SDBReqExitCode.EXIT_DEFAULT_FAILURE;

            if (IsTargetFound)
            {
                foreach (string item in rawItemList)
                {
                    if (HasPrefix(item, SDBProtocol.appcmd_returnstr))
                    {
                        RetrunString = GetPureValue(item);
                    }
                    else if (HasPrefix(item, SDBProtocol.appcmd_exitcode))
                    {
                        ExitCode = ParseInt(item);
                    }
                    else if (!string.IsNullOrWhiteSpace(item))
                    {
                        ConsoleOutput.Add(item.Replace("\r\n\0", string.Empty));
                    }
                }
            }
        }

        private string CombineArgs(params string[] args)
        {
            string combinedArgs = string.Empty;

            foreach (string arg in args)
            {
                combinedArgs += (arg + SDBProtocol.delemeter);
            }

            return combinedArgs;
        }

        private bool HasPrefix(string item, string pref)
        {
            return item.StartsWith(SDBProtocol.newline + pref + SDBProtocol.delemeter);
        }

        private string GetPureValue(string item)
        {
            int indexOf1stDelemeter = item.IndexOf(SDBProtocol.delemeter) + SDBProtocol.delemeter.ToString().Length;
            string splittedVal = item.Remove(0, indexOf1stDelemeter);
            return splittedVal.Replace(SDBProtocol.newline, string.Empty).Replace(SDBProtocol.terminator, string.Empty);
        }

        private int ParseInt(string item)
        {
            int ret = -1;
            string pureVal = GetPureValue(item);

            try
            {
                int.TryParse(pureVal, out ret);
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to parse exit code of SDB req : " + e.Message);
            }

            return ret;
        }
    }
}
