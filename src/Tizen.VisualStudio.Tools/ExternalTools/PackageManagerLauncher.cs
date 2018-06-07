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
using System.Security.Permissions;
using System.Threading;
using System.Windows.Forms;
using Tizen.VisualStudio.Tools.Data;
using Tizen.VisualStudio.Tools.DebugBridge;
using Tizen.VisualStudio.Tools.ExternalTool;
using Tizen.VisualStudio.Tools.Utilities;
using Tizen.VisualStudio.Utilities;

namespace Tizen.VisualStudio.Tools.ExternalTool
{
    public sealed class PackageManagerLauncher : ExternalToolLauncher
    {
        private const string pkgMgrDesc = "Package Manager";
        private static ProcessStartInfo pInfo = new ProcessStartInfo();
        //private ToolsInfo toolInfo = ToolsInfo.Instance();

        public PackageManagerLauncher() : base(pkgMgrDesc, pInfo, false)
        {
            pInfo.FileName = ToolsPathInfo.PkgMgrPath;
            pInfo.UseShellExecute = true;
            pInfo.Verb = "runas";
        }
    }
}
