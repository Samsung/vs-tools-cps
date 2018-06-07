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
using System.Text.RegularExpressions;

namespace NetCore.Profiler.Common.Helpers
{
    public static class StringHelper
    {
        public static int HexToInt32(this Group group)
        {
            return HexToInt32(group.Value);
        }

        public static uint HexToUInt32(this Group group)
        {
            return HexToUInt32(group.Value);
        }

        public static ulong HexToUInt64(this Group group)
        {
            return HexToUInt64(group.Value);
        }

        public static ulong ToUInt64(this Group group)
        {
            return Convert.ToUInt64(group.Value);
        }

        public static uint ToUInt32(this Group group)
        {
            return Convert.ToUInt32(group.Value);
        }

        public static int ToInt32(this Group group)
        {
            return Convert.ToInt32(group.Value);
        }

        public static int HexToInt32(this string line)
        {
            return Convert.ToInt32(line.Substring(2), 16);
        }

        public static uint HexToUInt32(this string line)
        {
            return Convert.ToUInt32(line.Substring(2), 16);
        }

        public static ulong HexToUInt64(this string line)
        {
            return Convert.ToUInt64(line.Substring(2), 16);
        }

        public static ulong ToUInt64(this string line)
        {
            return Convert.ToUInt64(line);
        }

        public static int ToInt32(this string line)
        {
            return Convert.ToInt32(line);
        }

    }
}
