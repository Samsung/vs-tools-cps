/*
 * Copyright 2021 (c) Samsung Electronics Co., Ltd  All Rights Reserved.
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

using System.Windows;
using System.IO;
using Tizen.VisualStudio.Utilities;
using System;
using System.Windows.Input;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System.Text.RegularExpressions;

namespace Tizen.VisualStudio.ProjectWizard.View
{
    public partial class ProjectWizardAddProjectName : System.Windows.Window
    {
        private readonly string tempName;
        private readonly string workspacePath;
        public ProjectWizardAddProjectName(string tname,string dir)
        {
            InitializeComponent();
            tempName = tname;
            workspacePath = dir;
        }

        private void ButtonCancelClick(object sender, RoutedEventArgs e) => this.Close();

        private void ButtonOkClick(object sender, RoutedEventArgs e)
        {
            string projName = Textbox_Name.Text;

            if (!ValidateTextboxText(projName))
                return;

            ProjectWizardAddProjectNameXaml.Close();

            var waitPopup = new WaitDialogUtil();
            waitPopup.ShowPopup("Adding Tizen Project",
                    "Please wait while the new project is being loaded...",
                    "Preparing...", "Tizen project load in progress...");

            var executor = new TzCmdExec();
            var dte = Package.GetGlobalService(typeof(_DTE)) as DTE2;

            string message = executor.RunTzCmnd(string.Format("/c tz new -t \"{0}\" -w \"{1}\" -p \"{2}\"", tempName, workspacePath, projName));
            if (!string.IsNullOrWhiteSpace(message))
            {
                System.Windows.MessageBox.Show(message);
                waitPopup.ClosePopup();
                return;
            }

            VsProjectHelper projHelp = VsProjectHelper.GetInstance;

            if (projHelp.IsTizenNativeProject())
            {
                dte.Solution.AddFromFile(workspacePath + "\\" + projName + "\\" + projName + ".vcxproj");
                projHelp.UpdateBuildForProject(projName + "\\" + projName + ".vcxproj", false);
            }
            else
            {
                dte.Solution.AddFromFile(workspacePath + "\\" + projName + "\\" + projName + ".csproj");
                projHelp.UpdateBuildForProject(projName + "\\" + projName + ".csproj", false);
            }

            waitPopup.ClosePopup();
        }

        private bool ValidateTextboxText(string prjName)
        {
            if (string.IsNullOrWhiteSpace(prjName))
            {
                System.Windows.MessageBox.Show("Project name must not be empty");
                return false;
            }

            // Check if Project starts with Digit
            if (!char.IsLetter(prjName[0]))
            {
                System.Windows.MessageBox.Show("Project name must start with alphabet.");
                return false;
            }

            // Check if Project name contains a-zA-Z0-9_ only
            Regex objAlphaPattern = new Regex(@"^[a-zA-Z0-9_]*$");
            bool sts = objAlphaPattern.IsMatch(prjName);
            if (!sts)
            {
                System.Windows.MessageBox.Show("Project name can only have [a-zA-Z0-9_]");
                return false;
            }

            // Check if Project name contain less than 3 chars
            if (prjName.Length < 3 || prjName.Length > 50)
            {
                System.Windows.MessageBox.Show("Project name length must be 3-50 chars.");
                return false;
            }

            if (workspacePath != string.Empty)
            {
                if (Directory.Exists(Path.Combine(workspacePath, prjName)))
                {
                    System.Windows.MessageBox.Show("Project name already exists in the selected Path!");
                    return false;
                }
            }

            return true;
        }
        private void WindowMouseDown(object sender, MouseButtonEventArgs e)
        {
            // To get LostFocus event for TextBox set focus on Grid 
            projGrid.Focus();
        }
    }
}

