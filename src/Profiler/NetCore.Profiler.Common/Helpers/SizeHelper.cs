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

namespace NetCore.Profiler.Common.Helpers
{
    public static class SizeHelper
    {
        private static readonly string[] Templates = { "B", "KB", "MB", "GB" };

        public static string SizeBytesToString(this ulong size)
        {
            var formatIndex = 0;
            double dtmp = size;
            while (true)
            {
                if ((uint)dtmp / 1024 > 0)
                {
                    formatIndex++;
                }
                else
                {
                    break;
                }

                dtmp /= 1024;
            }

            return (formatIndex > 0)
                ? $"{dtmp:N2} {Templates[formatIndex]}"
                : $"{(int)dtmp:D} {Templates[formatIndex]}";
        }

    }
}
