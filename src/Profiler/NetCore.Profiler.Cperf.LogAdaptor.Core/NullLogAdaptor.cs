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
using System.Diagnostics;
using System.IO;

namespace NetCore.Profiler.Cperf.LogAdaptor.Core
{
    /// <summary>
    /// A trivial %Core %Profiler log adapter (for debugging, unit testing, etc.).
    /// </summary>
    public class NullLogAdaptor : ILogAdaptor
    {
        public StreamWriter Output { get; set; }
        public string PdbDirectory { get; set; }

        public void Process(Func<string> readFunc)
        {
            try
            {
                string inputString;
                while ((inputString = readFunc()) != null)
                {
                    Output.WriteLine(inputString);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
    }
}
