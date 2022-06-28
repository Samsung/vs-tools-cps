
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
using System.Windows;

namespace Tizen.VisualStudio.Command
{
    internal sealed class TizenSettingsCommand
    {
        public static readonly Guid guidTizenSettings =
            new Guid("f97a3f9b-66cc-44ab-8870-905a47c34417");

        public const int CmdIdMenuItemTizenSettingsCmdSet = 0x3004;

        private readonly VsPackage package;
        private static TizenSettingsCommand instance;

        public static void Initialize(VsPackage package)
        {
            instance = new TizenSettingsCommand(package);
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

                cmdId = new CommandID(guidTizenSettings, CmdIdMenuItemTizenSettingsCmdSet);
                mItem = new OleMenuCommand(HandleMenuItemTizenSettings, cmdId);

                // Add an event handler to BeforeQueryStatus if one was passed in
                mItem.BeforeQueryStatus += BeforeQueryStatus;
                commandService.AddCommand(mItem);
            }
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand command = (OleMenuCommand)sender;

            VsProjectHelper projHelp = VsProjectHelper.GetInstance;
            bool isWebPrj = projHelp.IsHaveTizenNativeYaml();
            if (isWebPrj)
            {
                command.Visible = true;
            }
            else
            {
                command.Visible = false;
            }
        }

        private void HandleMenuItemTizenSettings(object sender, EventArgs e)
        {
            OutputWSLaunchMessage("<<< Project Settings Window Open >>>");
            
            VsProjectHelper projHelp = VsProjectHelper.GetInstance;
            String workspacePath = projHelp.getSolutionFolderPath();
            if (workspacePath == null)
            {
                OutputWSLaunchMessage("<<< unable to get workspace path >>>");
                return;
            }

            var propertiesWindow = new ProjectWizardTizenSettings(workspacePath);
            propertiesWindow.Show();
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

        private TizenSettingsCommand(VsPackage package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
        }
    }
}