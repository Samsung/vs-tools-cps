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

using EnvDTE;
using EnvDTE80;
using System.Windows;
using System.Windows.Forms;
using System.IO;
using Microsoft.VisualStudio.Shell;
using Tizen.VisualStudio.Utilities;
using Tizen.VisualStudio.Tools.Data;
using System;
using System.Windows.Input;

namespace Tizen.VisualStudio.ProjectWizard.View
{
    public partial class ProjectWizardProject : System.Windows.Window
    {
        private string temp_name;
        private string prjtype;
        private string profile_name;
        private string oldText;

        public ProjectWizardProject(string tname, string type, string profile)
        {
            InitializeComponent();
            button_ok.IsEnabled = false;
            temp_name = tname;
            prjtype = type;
            profile_name = profile;
        }

        private void Button_cancel_click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void Button_ok_click(object sender, RoutedEventArgs e)
        {
            string proj_name = Textbox_Name.Text;
            string wrkspace;
            if (!string.IsNullOrEmpty(Textbox_Path.Text))//add validity checks
            {
                wrkspace = Path.Combine(Textbox_Path.Text, proj_name);
            }
            else
            {
                wrkspace = Path.Combine(ToolsPathInfo.TizenCorePath, proj_name);
            }

            ProjectWizardProjectXaml.Close();

            var waitPopup = new WaitDialogUtil();
            waitPopup.ShowPopup("Creating Tizen Project",
                    "Please wait while the new project is being loaded...",
                    "Preparing...", "Tizen project load in progress...");

            if (!Directory.Exists(wrkspace))
            {
                Directory.CreateDirectory(wrkspace);
            }
            var executor = new TzCmdExec();
            string message;
            if (prjtype == "web")
            {
                // Added \" escape sequence to include Workspace Path in quotes ("") to avoid error if Path has Spaces.
                message = executor.RunTzCmnd(string.Format("/c tz init -t web -p {0} -w \"{1}\"", profile_name, wrkspace));

                if (!string.IsNullOrWhiteSpace(message))
                {
                    System.Windows.MessageBox.Show(message);
                    waitPopup.ClosePopup();
                    return;
                }
            }
            else
            {
                // Added \" escape sequence to include Workspace Path in quotes ("") to avoid error if Path has Spaces.
                message = executor.RunTzCmnd(string.Format("/c tz init -t dotnet -w \"{0}\"", wrkspace));

                if (!string.IsNullOrWhiteSpace(message))
                {
                    System.Windows.MessageBox.Show(message);
                    waitPopup.ClosePopup();
                    return;
                }
            }

            // Added \" escape sequence to include Workspace Path in quotes ("") to avoid error if Path has Spaces.
            message = executor.RunTzCmnd(string.Format("/c tz new -t \"{0}\" -w \"{1}\" -p \"{2}\"", temp_name, wrkspace, proj_name));

            if (!string.IsNullOrWhiteSpace(message))
            {
                System.Windows.MessageBox.Show(message);
                waitPopup.ClosePopup();
                return;
            }

            var dte = Package.GetGlobalService(typeof(_DTE)) as DTE2;
            if (prjtype == "dotnet")
            {
                if (!File.Exists(string.Format("{0}\\{1}\\{2}.sln", wrkspace, proj_name, proj_name.ToLower())))
                {
                    System.Windows.MessageBox.Show("Unable to find solution file");
                    waitPopup.ClosePopup();
                    return;
                }
                dte.Solution.Open(string.Format("{0}\\{1}\\{2}.sln", wrkspace, proj_name, proj_name.ToLower()));

            }
            else
            {
                if (!File.Exists(string.Format("{0}\\{1}.sln", wrkspace, proj_name)))
                {
                    System.Windows.MessageBox.Show("Unable to find solution file");
                    waitPopup.ClosePopup();
                    return;
                }
                dte.Solution.Open(string.Format("{0}\\{1}.sln", wrkspace, proj_name));

                if (File.Exists(string.Format("{0}\\{1}\\config.xml", wrkspace, proj_name)))
                {
                    dte.ItemOperations.OpenFile(wrkspace + "\\" + proj_name + "\\config.xml");
                }

                string solutionName = System.IO.Path.GetFileNameWithoutExtension(dte.Solution.FullName);
                dte.Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer).Activate();
                UIHierarchyItem root = ((DTE2)dte).ToolWindows.SolutionExplorer.GetItem(solutionName + @"\" + proj_name);
                if (root != null)
                {
                    root.Select(vsUISelectionType.vsUISelectionTypeSelect);
                    root.UIHierarchyItems.Expanded = true;
                }
            }

            waitPopup.ClosePopup();
        }

        private bool validateTextbox_Text()
        {
            if (string.IsNullOrWhiteSpace(Textbox_Name.Text))
                return false;

            // Check if Project starts with Digit
            if (Char.IsDigit(Textbox_Name.Text[0]))
            {
                button_ok.IsEnabled = false;
                System.Windows.MessageBox.Show("Project name must start with alphabet.");
                return false;
            }

            // Check if Project name contains Space
            if (Textbox_Name.Text.Contains(" "))
            {
                button_ok.IsEnabled = false;
                System.Windows.MessageBox.Show("Project name can only have [a-zA-Z0-9_]");
                return false;
            }

            // Check if Project name contain less than 3 chars
            if (Textbox_Name.Text.Length < 3 || Textbox_Name.Text.Length > 50)
            {
                button_ok.IsEnabled = false;
                System.Windows.MessageBox.Show("Project name length must be 3-50 chars.");
                return false;
            }

            return true;
        }

        private void window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // To get LostFocus event for TextBox set focus on Grid 
            projGrid.Focus();
        }

        private void Textbox_Name_Lostfocus(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(oldText) && oldText.Equals(Textbox_Name.Text))
                return;

            if (string.IsNullOrWhiteSpace(Textbox_Name.Text))
                return;

            if (!validateTextbox_Text())
                return;

            if (Textbox_Path.Text != string.Empty)
            {
                if (Directory.Exists(Path.Combine(Textbox_Path.Text, Textbox_Name.Text)))
                {
                    button_ok.IsEnabled = false;
                    System.Windows.MessageBox.Show("Project name already exists in the selected Path!");
                    return;
                }
                else
                {
                    button_ok.IsEnabled = true;
                }
            }
        }

        private void Textbox_Name_Gotfocus(object sender, RoutedEventArgs e)
        {
            oldText = Textbox_Name.Text;
        }

        private void Button_Browse_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = GetToolsFolderDialog();
            if (folderPath != string.Empty)
            {
                Textbox_Path.Text = folderPath;

                if (string.IsNullOrWhiteSpace(Textbox_Name.Text))
                {
                    button_ok.IsEnabled = false;
                    return;
                }

                if (Directory.Exists(Path.Combine(Textbox_Path.Text, Textbox_Name.Text)))
                {
                    button_ok.IsEnabled = false;
                    System.Windows.MessageBox.Show("Project name already exists in the selected Path!");
                    return;
                }

                if (validateTextbox_Text())
                    button_ok.IsEnabled = true;
                else
                    button_ok.IsEnabled = false;
            }
        }

        private string GetToolsFolderDialog()
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
            {
                SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                                                            + "\\source\\repos",
                Description = "Tizen Workspace Path",
                ShowNewFolderButton = true
            })
            {
                return (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) ?
                    folderBrowserDialog.SelectedPath : string.Empty;
            }
        }

        private void Button_Reset_Click(object sender, RoutedEventArgs e)
        {
            Textbox_Path.Text = string.Empty;
            button_ok.IsEnabled = false;
        }

    }
}
