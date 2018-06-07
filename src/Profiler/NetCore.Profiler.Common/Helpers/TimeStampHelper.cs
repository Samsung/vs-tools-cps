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

namespace NetCore.Profiler.Common.Helpers
{
    public static class TimeStampHelper
    {
        public static string TimeStampToString(this ulong time, ulong offset, ulong freq)
        {
            ulong nano = 0;
            ulong micro = 0;
            ulong mili = 0;

            ulong tmp = offset + time;
            switch (freq)
            {
                case 1000000000://nano
                    nano = tmp % 1000;
                    micro = tmp % 1000000 / 1000;
                    mili = tmp % 1000000000 / 1000000;
                    DateTime epochTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    DateTime result = epochTime.AddTicks((long)(tmp / 100));
                    return string.Format("{0}.{1:D3} {2:D3} {3:D3}", result.ToString("HH:mm:ss"), mili, micro, nano);
            }

            return "Error";
        }

        public static string TimeStampToShortString(this ulong time, ulong offset, ulong freq)
        {
            ulong mili = 0;

            ulong tmp = offset + time;
            switch (freq)
            {
                case 1000000000://nano
                    mili = tmp % 1000000000 / 1000000;
                    DateTime epochTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    DateTime result = epochTime.AddTicks((long)(tmp / 100));
                    return string.Format("{0}.{1:D3}", result.ToString("HH:mm:ss"), mili);
            }

            return "Error";
        }

        public static string TimeToString(this ulong time, ulong freq)
        {
            ulong nano = 0;
            ulong micro = 0;
            ulong mili = 0;

            ulong tmp = time;
            switch (freq)
            {
                case 1000000000://nano
                    nano = tmp % 1000;
                    micro = tmp % 1000000 / 1000;
                    mili = tmp % 1000000000 / 1000000;
                    DateTime epochTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    DateTime result = epochTime.AddTicks((long)(tmp / 100));
                    return string.Format("{0}.{1:D3} {2:D3} {3:D3}", result.ToString("ss"), mili, micro, nano);
            }

            return "Error";
        }

        public static string MilliSecondsToString(this ulong ms)
        {
            return new DateTime(0).AddMilliseconds(ms).ToString("mm:ss.fff");
        }
    }
}
