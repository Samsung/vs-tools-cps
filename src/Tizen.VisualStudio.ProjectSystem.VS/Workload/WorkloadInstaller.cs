/*
 * Copyright 2022 (c) Samsung Electronics Co., Ltd  All Rights Reserved.
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

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using Tizen.VisualStudio.Utilities;

namespace Tizen.VisualStudio.Workload
{
    public class WorkloadInstaller
    {
        private static WorkloadInstaller _instance = null;
        private readonly string _workloadUrl = @"https://raw.githubusercontent.com/Samsung/Tizen.NET/main/workload/scripts/workload-install.ps1";
        public static WorkloadInstaller GetInstance()
        {
            if (_instance == null)
            {
                _instance = new WorkloadInstaller();
            }

            return _instance;
        }

        private void WriteOutputPane(string msg)
        {
            if (string.IsNullOrEmpty(msg))
            {
                return;
            }

            string message = String.Format($"{DateTime.Now} : {msg}\n");

            VsPackage.outputPaneTizen?.Activate();
            VsPackage.outputPaneTizen?.OutputStringThreadSafe(message);
        }

        public void InstallWorkload()
        {
            int index = _workloadUrl.LastIndexOf("/");
            string ps1File = index > -1 ? _workloadUrl.Substring(index + 1) : "workload-install.ps1";
            string workDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            IVsStatusbar statusBar = (IVsStatusbar)Package.GetGlobalService(typeof(SVsStatusbar));
            statusBar.SetText("Installing Workload...");

            //Invoke-WebRequest
            string message = Ps1CmdExec.Execute(workDir, $"Invoke-WebRequest \"{_workloadUrl}\" -OutFile \"{ps1File}\"");
            message = message.Trim().Trim('\r', '\n');
            
            WriteOutputPane(message);

            if (!File.Exists(Path.Combine(workDir, ps1File)))
            {
                WriteOutputPane("workload script download failed.");
                statusBar.SetText("Workload script download failed.");
                return;
            }

            //Run workload script
            WriteOutputPane($".\\{ps1File}");
            message = Ps1CmdExec.Execute(workDir, $"\".\\{ps1File}\"");
            message = message.Trim().Trim('\r', '\n');
            WriteOutputPane(message);
            statusBar.SetText("Finished Installing Workload.");
        }

        private WorkloadInstaller()
        {
        }
    }
}
