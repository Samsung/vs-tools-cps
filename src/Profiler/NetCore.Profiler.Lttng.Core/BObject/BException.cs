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

using System.Collections.Generic;
using NetCore.Profiler.Common.Helpers;
using NetCore.Profiler.Lttng.Core.CTFObject;

namespace NetCore.Profiler.Lttng.Core.BObject
{
    public class BException
    {
        public BThread BThread { get; set; }
        public ulong StartAtTS { get; set; }
        //public CTFERecord ExcFilterStart { get; set; }
        //public CTFERecord ExcCatchStart { get; set; }
        public CTFERecord ExcFilterStop { get; set; }
        public List<CTFERecord> ExcFilterStart { get; set; } = new List<CTFERecord>();
        public List<CTFERecord> ExcFinallyStart { get; set; } = new List<CTFERecord>();
        public List<CTFERecord> ExcCatchStart { get; set; } = new List<CTFERecord>();

        public string StartAt => StartAtTS.TimeStampToString(BThread.GlobalOffset, BThread.GlobalFreq);

        public string CatchedAtFuncs
        {
            get
            {
                string res = "";

                for (int i = 0; i < ExcCatchStart.Count; i++)
                {
                    res += ExcCatchStart[i].Er.GetValue("_MethodName").ToString();

                    if (i != ExcCatchStart.Count - 1)
                    {
                        res += "\n";
                    }
                }

                return res;
            }
        }

        public string FilterededAtFuncs
        {
            get
            {
                string res = "";

                for (int i = 0; i < ExcFilterStart.Count; i++)
                {
                    res += ExcFilterStart[i].Er.GetValue("_MethodName").ToString();

                    if (i != ExcFilterStart.Count - 1)
                    {
                        res += "\n";
                    }
                }

                return res;
            }
        }

        public string FinallyedAtFuncs
        {
            get
            {
                string res = "";

                for (int i = 0; i < ExcFinallyStart.Count; i++)
                {
                    res += ExcFinallyStart[i].Er.GetValue("_MethodName").ToString();

                    if (i != ExcFinallyStart.Count - 1)
                    {
                        res += "\n";
                    }
                }

                return res;
            }
        }
    }
}
