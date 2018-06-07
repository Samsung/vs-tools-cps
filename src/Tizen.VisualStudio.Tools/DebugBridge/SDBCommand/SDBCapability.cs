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
using System.Text;

namespace Tizen.VisualStudio.Tools.DebugBridge.SDBCommand
{
    public class SDBCapability
    {
        private Dictionary<string, string> capDic = new Dictionary<string, string>();
        private bool isSupported;

        public bool IsSupported
        {
            get
            {
                return isSupported;
            }
        }

        public SDBCapability()
        {
            string[] args = { "-s", DeviceManager.SelectedDevice.Serial, SDBProtocol.capability };

            Process p = new Process();
            p.StartInfo.FileName = SDBLib.GetSdbFilePath();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.Arguments = string.Join(" ", args);
            p.Start();

            string returnValue = p.StandardOutput.ReadToEnd().Replace("\r", string.Empty);
            p.WaitForExit();
            p.Dispose();

            isSupported = !string.IsNullOrEmpty(returnValue);

            if (!isSupported)
            {
                return;
            }

            GenCapDic(returnValue);
        }

        public string GetValueByKey(string key)
        {
            if (capDic.ContainsKey(key))
            {
                return capDic[key];
            }
            else
            {
                return null;
            }
        }

        public bool GetAvailabilityByKey(string key)
        {
            return SDBProtocol.enabled.Equals(GetValueByKey(key));
        }

        public float GetNumericValueByKey(string key)
        {
            float val;

            if (float.TryParse(GetValueByKey(key), out val))
            {
                return val;
            }

            return float.MinValue;
        }

        public Version GetVersionByKey(string key)
        {
            Version val;

            if (Version.TryParse(GetValueByKey(key), out val))
            {
                return val;
            }

            return null;
        }

        private string ParseStrReturnValue(string oriStr)
        {
            byte[] oriByte = Encoding.UTF8.GetBytes(oriStr);
            byte[] outputByte = new byte[oriByte.Length];

            for (int i = 0; i < oriByte.Length - SDBProtocol.capBufferSize; i++)
            {
                outputByte[i] = oriByte[i + SDBProtocol.capBufferSize];
            }

            string strReturnValue = Encoding.Default.GetString(outputByte).Replace(SDBProtocol.terminator, string.Empty);

            return strReturnValue;
        }

        private void GenCapDic(string capString)
        {
            string[] capList = capString.Split('\n');

            foreach (string capItem in capList)
            {
                if (capItem.Contains(SDBProtocol.delemeter.ToString()))
                {
                    string[] capItemSet = capItem.Split(SDBProtocol.delemeter);

                    if (!capDic.ContainsKey(capItemSet[0]))
                    {
                        capDic.Add(capItemSet[0], capItemSet[1]);
                    }
                }
            }
        }
    }
}
