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
using Tizen.VisualStudio.Tools.Data;
using Tizen.VisualStudio.Tools.ExternalTool;

namespace Tizen.VisualStudio.Tools.ExternalTool
{
    public sealed class EmulatorManagerLauncher : ExternalToolLauncher
    {
        private const string emulatorMgrDesc = "Emulator Manager";
        //private ToolsInfo toolInfo = ToolsInfo.Instance();

        private static ProcessStartInfo pInfo = new ProcessStartInfo();

        public EmulatorManagerLauncher() : base(emulatorMgrDesc, pInfo, false)
        {
            pInfo.FileName = ToolsPathInfo.EmulatorMgrPath;
            pInfo.UseShellExecute = true;
            pInfo.Verb = "runas";
        }
    }
}
