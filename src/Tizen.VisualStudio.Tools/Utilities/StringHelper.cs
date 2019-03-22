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

namespace Tizen.VisualStudio.Utilities
{
    public static class StringHelper
    {
        public static string CombineMessages(string message1, string message2)
        {
            if (String.IsNullOrEmpty(message1))
            {
                return message2;
            }
            if (String.IsNullOrEmpty(message2))
            {
                return message1;
            }
            if (message1.IndexOfAny(new[] { '.', '!', ';', ':', '?', '\n' }, message1.Length - 1) < 0)
            {
                message1 += '.';
            }
            if (message2[0] != '\n')
            {
                message1 += ' ';
            }
            return message1 + message2;
        }
    }
}
