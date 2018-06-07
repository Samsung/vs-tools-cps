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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Tizen.VisualStudio.LogViewer
{
    public class Log
    {
        public Log(string[] arr, string _msg)
        {
            this.Time = arr[0] + " " + arr[1];
            this.Pid = arr[4].Substring(0, arr[4].Length - 1);
            this.Tid = arr[6];
            this.Level = Resource.GetLevelName(arr[2][0]);
            this.Tag = arr[2].Substring(2, arr[2].Length - 2);
            this.Msg = _msg;
        }

        public string Time { get; set; }
        public string Level { get; set; }
        public string Pid { get; set; }
        public string Tid { get; set; }
        public string Tag { get; set; }
        public string Msg { get; set; }


        public static Regex MobileLongRegex = new Regex("^\\[\\s(\\d\\d-\\d\\d\\s\\d\\d:\\d\\d:\\d\\d\\.\\d+)\\s+" //time
                                              + "(\\d*):\\s*" //pid
                                              + "(\\d+)\\s" //tid
                                              + "([VDIWEF])/(.+\\s+)\\]$"); //Level //Tag
        public static Regex tvLongReg1 = new Regex("^\\[DLOG\\].*?" + "([+-]?\\d*\\.\\d+)(?![-+0-9\\.])"    // Float Time
                    + ".*?" + "(\\d+)"  // int Pid
                    + ".*?" + "(\\d+)"  // int Tid
                    + ".*?" + "((?:[a-z][a-z0-9_]*))" // Level
                    + ".*?" + "((?:[a-z][a-z0-9_]*))"  // Tag,
                    , RegexOptions.IgnoreCase | RegexOptions.Singleline);
        public static Regex tvLongReg2 = new Regex("(.*?)" + "\\[DLOG\\]"
                                    , RegexOptions.IgnoreCase | RegexOptions.Singleline);
    }
}
