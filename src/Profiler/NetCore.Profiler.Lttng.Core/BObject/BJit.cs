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
using NetCore.Profiler.Common.Helpers;
using NetCore.Profiler.Lttng.Core.CTFObject;

namespace NetCore.Profiler.Lttng.Core.BObject
{
    public class BJit : IBJob
    {
        public BThread BThread { get; set; }
        public CTFERecord MethodJittingStarted { get; set; }
        public CTFERecord MethodLoad { get; set; }
        public bool IsFull => (BThread != null &&
                               MethodJittingStarted != null &&
                               MethodLoad != null);

        #region IBJob
        public ulong JobStartAt => BThread.GlobalOffset + MethodJittingStarted.Time;

        public ulong JobDuration => JobEndAt - JobStartAt;

        public ulong JobEndAt
        {
            get
            {
                if (MethodLoad == null)
                {
                    return 0;
                }
                else
                {
                    return BThread.GlobalOffset + MethodLoad.Time;
                }
            }
        }
        #endregion IBJob

        public ulong Duration
        {
            get
            {
                if (MethodLoad == null)
                {
                    return 0;
                }
                else
                {
                    return MethodLoad.Time - MethodJittingStarted.Time;
                }
            }
        }

        public string StartAtStr => MethodJittingStarted.Time.TimeStampToString(BThread.GlobalOffset, BThread.GlobalFreq);

        public string LoadAtStr
        {
            get
            {
                if (MethodLoad == null)
                {
                    return Double.PositiveInfinity.ToString();
                }
                else
                {
                    return MethodLoad.Time.TimeStampToString(BThread.GlobalOffset, BThread.GlobalFreq);
                }
            }
        }

        public string DurationStr
        {
            get
            {
                if (MethodLoad == null)
                {
                    return Double.PositiveInfinity.ToString();
                }
                else
                {
                    return (MethodLoad.Time - MethodJittingStarted.Time).TimeToString(BThread.GlobalFreq);
                }
            }
        }

        public string FuncName => MethodJittingStarted.Er.GetValue("_MethodNamespace").ToString() + ":" + MethodJittingStarted.Er.GetValue("_MethodName").ToString();
    }
}
