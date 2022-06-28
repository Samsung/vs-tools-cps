
/*
 * Copyright 2021(c) Samsung Electronics Co., Ltd  All Rights Reserved.
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
using Microsoft.VisualStudio.Shell;
using Tizen.VisualStudio.Tools.Data;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell.Interop;
using Tizen.VisualStudio.ProjectWizard.View;

namespace Tizen.VisualStudio.Command
{
    internal sealed class AddTizenProjectCommand
    {
        public static readonly Guid guidAddTizenProjectCommand =
            new Guid("207aac64-71c3-4855-b166-a17e55397a8b");

        public const int CmdIdMenuItemAddTizenProjectCmdSet= 0x3002;

        private readonly VsPackage package;
        private static AddTizenProjectCommand instance;

        public static void Initialize(VsPackage package)
        {
            instance = new AddTizenProjectCommand(package);
            instance.RegisterHandlers();
        }

        private IServiceProvider ServiceProvider
        {
            get { return this.package as IServiceProvider; }
        }

        private void RegisterHandlers()
        {
            if (this.ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                CommandID cmdId;
                OleMenuCommand mItem;

                cmdId = new CommandID(guidAddTizenProjectCommand, CmdIdMenuItemAddTizenProjectCmdSet);
                mItem = new OleMenuCommand(HandleMenuItemAddTizenProject, cmdId);

                // Add an event handler to BeforeQueryStatus if one was passed in
                mItem.BeforeQueryStatus += BeforeQueryStatus;
                commandService.AddCommand(mItem);
            }
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand command = (OleMenuCommand)sender;
            VsProjectHelper projHelp = VsProjectHelper.GetInstance;

            if (projHelp.IsTizenWebProject() || projHelp.IsTizenNativeProject())
            {
                command.Visible = true;
            }
            else
            {
                command.Visible = false;
            }
        }

        private void HandleMenuItemAddTizenProject(object sender, EventArgs e)
        {
            OutputWSLaunchMessage("<<< Project Wizard Open >>>");

            VsProjectHelper projHelp = VsProjectHelper.GetInstance;
            String workspacePath = projHelp.getSolutionFolderPath();
            if (workspacePath == null)
            {
                OutputWSLaunchMessage("<<< unable to get workspace path >>>");
                return;
            }

            var projectWizard = new ProjectWizardAddProjTempList(workspacePath);
            projectWizard.Show();
        }

        
        private void OutputWSLaunchMessage(string rawMsg)
        {
            if (string.IsNullOrEmpty(rawMsg))
            {
                return;
            }

            string message = String.Format($"{DateTime.Now} : {rawMsg}\n");

            VsPackage.outputPaneTizen?.Activate();

            VsPackage.outputPaneTizen?.OutputStringThreadSafe(message);
        }

        private AddTizenProjectCommand(VsPackage package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
        }
    }
}