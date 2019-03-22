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
    /// <summary>
    /// Helpers for working with timestamps.
    /// </summary>
    public static class TimeStampHelper
    {
        /// <summary>
        /// Unix epoch time (POSIX time) start. By definition Unix time is the number of seconds that have elapsed since
        /// 00:00:00 Coordinated Universal Time (UTC), Thursday, 1 January 1970.
        /// </summary>
        public static readonly DateTime UnixEpochTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// An extension method to convert floating point (double) Unix time to DateTime.
        /// </summary>
        /// <param name="timeSeconds">Unix time</param>
        /// <returns></returns>
        public static DateTime UnixTimeToDateTime(this double timeSeconds)
        {
            return UnixEpochTime.AddSeconds(timeSeconds);
        }

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
                    DateTime result = UnixEpochTime.AddTicks((long)(tmp / 100));
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
                    DateTime result = UnixEpochTime.AddTicks((long)(tmp / 100));
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
                    DateTime result = UnixEpochTime.AddTicks((long)(tmp / 100));
                    return string.Format("{0}.{1:D3} {2:D3} {3:D3}", result.ToString("ss"), mili, micro, nano);
            }

            return "Error";
        }

        public static string MillisecondsToString(this ulong ms)
        {
            return new DateTime(0).AddMilliseconds(ms).ToString("mm:ss.fff");
        }

        public static string ToDebugString(this DateTime dateTime)
        {
            string result = dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
            if (dateTime.Kind == DateTimeKind.Utc)
            {
                result += " Z";
            }
            return result;
        }
    }
}
