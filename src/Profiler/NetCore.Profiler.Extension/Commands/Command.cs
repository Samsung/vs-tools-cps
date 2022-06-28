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
using System.ComponentModel.Design;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NetCore.Profiler.Extension.VSPackage;

namespace NetCore.Profiler.Extension.Commands
{
    internal abstract class Command
    {

        private readonly OleMenuCommand _windowMenuCommand;

        public bool Enabled
        {
            set => _windowMenuCommand.Enabled = value;
        }

        protected Command(IServiceProvider serviceProvider, int commandId, EventHandler handler, bool enabled = false)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            OleMenuCommandService commandService = serviceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            commandService?.AddCommand(_windowMenuCommand = new OleMenuCommand(handler, new CommandID(GeneralProperties.CommandSet, commandId))
            {
                Enabled = enabled
            });

            // use BeforeQueryStatus event callBack function to Dynamically Enable/Disable the menu
            _windowMenuCommand.BeforeQueryStatus += (sender, evt) =>
            {
                OleMenuCommand item = (OleMenuCommand)sender;
                DTE2 dte2 = Package.GetGlobalService(typeof(SDTE)) as DTE2;

                if (dte2.Solution.IsOpen)
                {
                    VsProjectHelper projHelp = VsProjectHelper.Instance;
                    bool isWebPrj = projHelp.IsTizenWebProject();
                    bool isNativePrj = projHelp.IsTizenNativeProject();
                    if (isWebPrj || isNativePrj)
                        item.Enabled = false;
                    else
                        item.Enabled = true;
                }else
                {
                    item.Enabled = false;
                }
            };
        }

    }
}
