
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
using EnvDTE80;
using EnvDTE;
using System.IO;

namespace Tizen.VisualStudio.Command
{
    internal sealed class AddTizenDependencyCommand
    {
        public static readonly Guid guidAddTizenDependency =
            new Guid("ba720d87-a961-4477-976e-e9e4a34f0dad");

        public const int CmdIdMenuItemAddTizenDependencyCmdSet = 0x3003;

        private readonly VsPackage package;
        private static AddTizenDependencyCommand instance;

        public static void Initialize(VsPackage package)
        {
            instance = new AddTizenDependencyCommand(package);
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

                cmdId = new CommandID(guidAddTizenDependency , CmdIdMenuItemAddTizenDependencyCmdSet);
                mItem = new OleMenuCommand(HandleMenuItemAddTizenDependency, cmdId);

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
            bool isNativePrj = projHelp.IsTizenNativeProject();
            bool isDotnetPrj = projHelp.IsTizenDotnetProject();

            if (isWebPrj || isNativePrj || isDotnetPrj)
            {
                command.Visible = true;
            }
            else
            {
                command.Visible = false;
            }
        }

        private void HandleMenuItemAddTizenDependency(object sender, EventArgs e)
        {
            OutputWSLaunchMessage("<<< Project Dependency Window Open >>>");
            
            VsProjectHelper projHelp = VsProjectHelper.GetInstance;
            bool isWebPrj = projHelp.IsTizenWebProject();
            bool isNativePrj = projHelp.IsTizenNativeProject();
            bool isDotnetPrj = projHelp.IsTizenDotnetProject();
            String workspacePath = projHelp.getSolutionFolderPath();
            if (workspacePath == null)
            {
                OutputWSLaunchMessage("<<< unable to get workspace path >>>");
                return;
            }

            if((isWebPrj && isNativePrj) || (isWebPrj && isDotnetPrj) || (isDotnetPrj && isNativePrj))
            {
                DTE2 dte2 = Package.GetGlobalService(typeof(DTE)) as DTE2;
                Projects ListOfProjectsInSolution = dte2.Solution.Projects;

                Project actProj = (dte2.ActiveSolutionProjects as Array).GetValue(0) as Project;
                string projectFolder = Path.GetDirectoryName(actProj.FullName);
                if (File.Exists(Path.Combine(projectFolder, "tizen_native_project.yaml")))
                {
                    var projectListWindow = new ProjectWizardAddTizenNativeDependency(workspacePath);
                    projectListWindow.Owner = Application.Current.MainWindow;
                    projectListWindow.ShowDialog();
                } else if (File.Exists(Path.Combine(projectFolder, "config.xml")))
                {
                    var projectListWindow = new ProjectWizardAddTizenWebDependency(workspacePath);
                    projectListWindow.Owner = Application.Current.MainWindow;
                    projectListWindow.ShowDialog();
                }
                else if (File.Exists(Path.Combine(projectFolder, "tizen_dotnet_project.yaml")))
                {
                    var projectListWindow = new ProjectWizardAddTizenDotnetDependency(workspacePath);
                    projectListWindow.Owner = Application.Current.MainWindow;
                    projectListWindow.ShowDialog();
                }
                else
                {
                    OutputWSLaunchMessage("<<< invalid type >>>");
                    return;
                }

            } 
            else if (isWebPrj)
            {
                var projectListWindow = new ProjectWizardAddTizenWebDependency(workspacePath);
                projectListWindow.Owner = Application.Current.MainWindow;
                projectListWindow.ShowDialog();
            }
            else if (isNativePrj)
            {
                var projectListWindow = new ProjectWizardAddTizenNativeDependency(workspacePath);
                projectListWindow.Owner = Application.Current.MainWindow;
                projectListWindow.ShowDialog();
            }
            else if (isDotnetPrj)
            {
                var projectListWindow = new ProjectWizardAddTizenDotnetDependency(workspacePath);
                projectListWindow.Owner = Application.Current.MainWindow;
                projectListWindow.ShowDialog();
            }
            else
            {
                OutputWSLaunchMessage("<<< invalid type >>>");
                return;
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

        private AddTizenDependencyCommand(VsPackage package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
        }
    }
}
