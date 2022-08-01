
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
using Tizen.VisualStudio.Utilities;

namespace Tizen.VisualStudio.Command
{
    internal sealed class WebSimulatorCommand
    {
        public static readonly Guid guidWebSimulatorCommand =
            new Guid("c99da730-5fc8-48e8-a00d-a9de0d354655");

        public const int CmdIdMenuItemWebSimulatorCmdSet = 0x3001;

        private readonly VsPackage package;
        private static WebSimulatorCommand instance;

        public static void Initialize(VsPackage package)
        {
            instance = new WebSimulatorCommand(package);
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

                cmdId = new CommandID(guidWebSimulatorCommand, CmdIdMenuItemWebSimulatorCmdSet);
                mItem = new OleMenuCommand(HandleMenuItemWebSimulatorCompile, cmdId);

                // Add an event handler to BeforeQueryStatus if one was passed in
                mItem.BeforeQueryStatus += BeforeQueryStatus;
                commandService.AddCommand(mItem);
            }
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand command = (OleMenuCommand)sender;

            VsProjectHelper projHelp = VsProjectHelper.GetInstance;
            bool isWebPrj = projHelp.IsTizenWebProject();

            if (isWebPrj)
            {
                command.Visible = true;
            }
            else
            {
                command.Visible = false;
            }
        }

        private void HandleMenuItemWebSimulatorCompile(object sender, EventArgs e)
        {
            OutputWSLaunchMessage("<<< web app simulator launch>>>");

            VsProjectHelper projHelp = VsProjectHelper.GetInstance;
            bool isWebPrj = projHelp.IsTizenWebProject();

            String workspacePath = projHelp.getSolutionFolderPath();
            if (workspacePath == null)
            {
                OutputWSLaunchMessage("<<< unable to get workspace path >>>");
                return;
            }

            var waitPopup = new WaitDialogUtil();
            waitPopup.ShowPopup("Launching Web Simulator",
                    "Please wait while the simulator is being launched...",
                    "Preparing...", "Launching Web Simulator in progress...");


            // Launch the Tizen web app in web simulator
            var executor = new TzCmdExec();
            string command = string.Format("/c tz run -r -w \"{0}\"", workspacePath);

            //TODO: TZ need to handle Web Simualtor Launch in next release and VS need to remove below Code block
            //Temporary change in workspace Yaml for workspace_folder to launch the Web Simualtor
            string working_folder = string.Empty;
            {
                //working_folder = projHelp.getWorkingFolder(workspacePath);
                working_folder = projHelp.getTag(workspacePath, "working_folder", ' ');
                if(!working_folder.EndsWith("config.xml"))
                {
                    projHelp.UpdateYaml(workspacePath, "working_folder:", working_folder + "\\config.xml");
                }
            }

            string message = executor.RunTzCmnd(command);
            waitPopup.ClosePopup();

            if (message.Contains("error:"))
            {
                OutputWSLaunchMessage("<<<  Failed to launch Web package.  >>>");
            }
            else
            {
                OutputWSLaunchMessage("<<< web package launhed ! >>>");
            }

            //TODO: TZ need to handle Web Simualtor Launch in next release and  VS need to remove below Code block
            //Revert back to original workspace_folder after launch of Web Simualtor
            {
                projHelp.UpdateYaml(workspacePath, "working_folder:", working_folder);
            }
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

        private WebSimulatorCommand(VsPackage package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
        }
    }
}