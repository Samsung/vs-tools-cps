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

using EnvDTE;
using EnvDTE80;
using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NetCore.Profiler.Extension.VSPackage;
using Tizen.VisualStudio.Utilities;
using Tizen.VisualStudio.Tools.DebugBridge;
using Tizen.VisualStudio.Tools.DebugBridge.SDBCommand;

namespace NetCore.Profiler.Extension.Commands
{
    internal sealed class ProfilingProgressWindowCommand : Command
    {
        /// <summary>
        /// Command ID.
        /// </summary>

        private Package _package;

        public const int CommandId = 0x0114;

        public static ProfilingProgressWindowCommand Instance { get; private set; }

        private ProfilingProgressWindowCommand(IServiceProvider serviceProvider) :
            base(serviceProvider, CommandId, Execute, true)
        {
            _package = (Package)serviceProvider;
        }

        private static Project GetStartupProject()
        {
            try
            {
                string startPrjName = string.Empty;
                Project startPrj = null;
                DTE2 dte2 = (DTE2)Package.GetGlobalService(typeof(SDTE));
                Property property = dte2.Solution.Properties.Item("StartupProject");
                if (property != null)
                {
                    startPrjName = (string)property.Value;
                }
                foreach (Project prj in dte2.Solution.Projects)
                {
                    if (prj.Name == startPrjName)
                    {
                        startPrj = prj;
                    }
                }
                return startPrj;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private bool IsNetCoreDbgSupported()
        {
            SDBDeviceInfo device = DeviceManager.SelectedDevice;
            bool is_netcoredbg_support = false;
            if (device != null)
            {
                var cap = new SDBCapability(device);
                is_netcoredbg_support =
                    cap.GetAvailabilityByKey("netcoredbg_support");
            }
            return is_netcoredbg_support;
        }

        private bool CanStart()
        {
            Project project = null;

            project = GetStartupProject();
            if (project == null)
            {
                ShellHelper.ShowMessage(_package, MessageDialogType.Info, "", "No active project");
                return false;
            }

            if (!IsNetCoreDbgSupported())
            {
                ShellHelper.ShowMessage(_package, MessageDialogType.Info, "",
                    "Profiling is supported starting from Tizen 5.0 version only");
                return false;
            }

            return true;
        }

        private static void Execute(object sender, EventArgs e)
        {
            if (Instance.CanStart())
            {
                ProfilerPlugin.Instance.ProfilingProgressWindow.Show();
            }
        }

        public static void Initialize(IServiceProvider serviceProvider)
        {
            Instance = new ProfilingProgressWindowCommand(serviceProvider);
        }
    }
}
