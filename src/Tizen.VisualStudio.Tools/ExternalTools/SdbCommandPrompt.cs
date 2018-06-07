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
using System.Threading.Tasks;
using System.Diagnostics;
using Tizen.VisualStudio.Tools.Data;
using System.IO;

namespace Tizen.VisualStudio.Tools.ExternalTool
{
    public class SdbCommandPrompt
    {

        private string sdbpath;
        //private ToolsInfo toolInfo = ToolsInfo.Instance();

        public SdbCommandPrompt()
        {
            sdbpath = ToolsPathInfo.SDBPath;
        }


        public void StartSdbCommandPrompt()
        {
            Process proc = new Process();
            proc.StartInfo.FileName = "cmd.exe";
            proc.StartInfo.WorkingDirectory = Path.GetDirectoryName(sdbpath);
            proc.Start();
        }
    }
}
