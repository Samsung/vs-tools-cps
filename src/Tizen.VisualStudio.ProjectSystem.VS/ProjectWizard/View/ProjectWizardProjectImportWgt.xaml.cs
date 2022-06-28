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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Tizen.VisualStudio.TizenYamlParser;
using Tizen.VisualStudio.Utilities;

namespace Tizen.VisualStudio.ProjectWizard.View
{
    /// <summary>
    /// Interaction logic for ProjectWizardProjectImportWgt.xaml
    /// </summary>
    
    public class ProjectWizardProjectImportWgtModel
    {
        public ObservableCollection<string> ProfileList { get; set; }

        public ProjectWizardProjectImportWgtModel(List<string> profileList)
        {
            ProfileList = new ObservableCollection<string>(profileList);
        }
    }

    public partial class ProjectWizardProjectImportWgt : Window
    {
        private static void WriteOutputPane(string msg)
        {
            if (string.IsNullOrEmpty(msg))
            {
                return;
            }

            string message = string.Format($"{DateTime.Now} : {msg}\n");

            VsPackage.outputPaneTizen?.Activate();
            VsPackage.outputPaneTizen?.OutputStringThreadSafe(message);
        }

        public List<string> ProfileValueList { get; private set; }
        public string ProfileValue { get; private set; }
        public string WorkspacePath { get; private set;}
        public string WgtPath { get; private set; }
        
        public ProjectWizardProjectImportWgt(List<string> profileList)
        {
            InitializeComponent();
            ProfileValueList = profileList;
            HashSet<string> profiles = new HashSet<string>();
            foreach (string prof in profileList ?? Enumerable.Empty<string>())
            {
                int hyphenPos = prof.LastIndexOf("-");
                string prf = prof.Substring(0, hyphenPos);
                _ = profiles.Add(prf);
            }
            DataContext = new ProjectWizardProjectImportWgtModel(profiles.ToList());
            comboBox_PlatformVersion.SelectedItem = null;
            comboBox_PlatformVersion.IsEnabled = false;
            button_ok.IsEnabled = false;
        }

        private void ButtonCancelClick(object sender, RoutedEventArgs e) => Close();

        /*
         * tizen_workspace.yaml
         * projects:
         *   nativeProjEntry: []
         *   webProjEntry: []
         *   dotnetProjEntry: []
         */
        private bool IsMultiProject(string projPath)
        {
            string workspaceYamlPath = Path.Combine(projPath, "tizen_workspace.yaml");

            if (!File.Exists(workspaceYamlPath))
            {
                return false;
            }

            ParseWorkspaceYaml wkspaceYaml = ParseWorkspaceYaml.FromYaml(
                File.ReadAllText(workspaceYamlPath));
            
            return wkspaceYaml.Projects.Keys.ToList().Count > 0;
        }

        void ImportMultiWgt(string wkspace, string wgtPath, string profileValue)
        {
            var executor = new TzCmdExec();
            var waitPopup = new WaitDialogUtil();
            string prjName = Path.GetFileNameWithoutExtension(wgtPath);
            VsProjectHelper.Initialize();
            VsProjectHelper prjHelperInstance = VsProjectHelper.GetInstance;

            //Get the wkspace sln
            string[] slns = Directory.GetFiles(wkspace, "*.sln");
            if (slns.Length == 0)
            {
                _ = System.Windows.MessageBox.Show("No sln file found", "Wgt Import Failed",
                    (MessageBoxButton)MessageBoxButtons.OK, (MessageBoxImage)MessageBoxIcon.Warning);
                return;
            }

            waitPopup.ShowPopup("Import wgt file",
                    "Please wait while the wgt is being loaded...",
                    "Preparing...", "Wgt load in progress...");

            var dte = Package.GetGlobalService(typeof(EnvDTE._DTE)) as EnvDTE80.DTE2;

            string message = executor.RunTzCmnd(string.Format("/c tz import -w {0} -W {1} -p {2}", wkspace, wgtPath, profileValue));
            message = message.Trim().Trim('\r', '\n');

            WriteOutputPane(message);

            string projPath = Path.Combine(wkspace, prjName);
            string csProjPath = Path.Combine(projPath, prjName + ".csproj");

            if (message.Contains("error:"))
            {
                _ = System.Windows.MessageBox.Show(message, "Wgt Import Failed",
                    (MessageBoxButton)MessageBoxButtons.OK, (MessageBoxImage)MessageBoxIcon.Warning);
                Directory.Delete(projPath, true);
                return;
            }

            //open sln
            dte.Solution.Open(slns[0]);

            //add new proj to sln
            dte.Solution.AddFromFile(csProjPath);

            //update build for proj
            prjHelperInstance.UpdateBuildForProject(Path.Combine(prjName, prjName + ".csproj"), false);

            //apply cert
            prjHelperInstance.setActiveCertificate(prjHelperInstance.getCertificateType());

            waitPopup.ClosePopup();
        }

        void ImportWgt(string wkspace, string wgtPath, string profileValue)
        {
            var executor = new TzCmdExec();
            var waitPopup = new WaitDialogUtil();
            string prjName = Path.GetFileNameWithoutExtension(wgtPath);
            VsProjectHelper.Initialize();
            VsProjectHelper prjHelperInstance = VsProjectHelper.GetInstance;

            waitPopup.ShowPopup("Import wgt file",
                    "Please wait while the wgt is being loaded...",
                    "Preparing...", "Wgt load in progress...");
            
            string slnPath = Path.Combine(wkspace, prjName);
            if (!Directory.Exists(slnPath))
            {
                Directory.CreateDirectory(slnPath);
            }

            string message = executor.RunTzCmnd(string.Format("/c tz import -w {0} -W {1} -p {2}", slnPath, wgtPath, profileValue));
            message = message.Trim().Trim('\r', '\n');

            WriteOutputPane(message);

            if (message.Contains("error:"))
            {
                _ = System.Windows.MessageBox.Show(message, "Wgt Import Failed",
                    (MessageBoxButton)MessageBoxButtons.OK, (MessageBoxImage)MessageBoxIcon.Warning);
                Directory.Delete(slnPath, true);
                return;
            }

            var dte = Package.GetGlobalService(typeof(EnvDTE._DTE)) as EnvDTE80.DTE2;

            //Get newly created sln
            string[] slns = Directory.GetFiles(slnPath, "*.sln");
            if (slns.Length == 0)
            {
                _ = System.Windows.MessageBox.Show("No sln file found", "Wgt Import Failed",
                    (MessageBoxButton)MessageBoxButtons.OK, (MessageBoxImage)MessageBoxIcon.Warning);
                Directory.Delete(slnPath, true);
                return;
            }

            string projPath = Path.Combine(slnPath, prjName, prjName + ".csproj");

            //open sln
            dte.Solution.Open(slns[0]);

            //add new proj to sln
            dte.Solution.AddFromFile(projPath);

            //update build for proj
            prjHelperInstance.UpdateBuildForProject(Path.Combine(prjName, prjName + ".csproj"), false);

            //apply cert
            prjHelperInstance.setActiveCertificate(prjHelperInstance.getCertificateType());

            waitPopup.ClosePopup();
        }

        private void OnProfileSelectionChanged(object sender, RoutedEventArgs e)
        {
            comboBox_PlatformVersion.SelectedItem = null;
            comboBox_PlatformVersion.IsEnabled = false;
            if (comboBox_Profile.SelectedValue == null
                || comboBox_Profile.SelectedValue.ToString() == string.Empty)
            {
                return;
            }

            string profileVal = comboBox_Profile.SelectedValue.ToString();
            comboBox_PlatformVersion.IsEnabled = true;

            List<string> tempList =
                ProfileValueList.FindAll(v => v.Contains(profileVal));
            List<string> platformList = new List<string>();
            tempList.ForEach(x =>
            {
                int hyphenPos = x.LastIndexOf("-");
                platformList.Add(x.Substring(hyphenPos + 1));
            });

            comboBox_PlatformVersion.ItemsSource = platformList;
        }

        private bool IsOkBtnEnabled()
        {
            return comboBox_PlatformVersion.SelectedValue != null
                && comboBox_PlatformVersion.SelectedValue.ToString() != string.Empty
                && comboBox_Profile.SelectedValue != null
                && comboBox_Profile.SelectedValue.ToString() != string.Empty
                && Textbox_WgtPath.Text != string.Empty
                && Textbox_Path.Text != string.Empty;
        }

        private void OnPlatformSelectionChanged(object sender, RoutedEventArgs e)
        {
            button_ok.IsEnabled = IsOkBtnEnabled();
        }
        private void ButtonOkClick(object sender, RoutedEventArgs e)
        {
            string platformVal = comboBox_PlatformVersion.SelectedValue.ToString();
            string profileVal = comboBox_Profile.SelectedValue.ToString();

            WgtPath = Textbox_WgtPath.Text;
            WorkspacePath = Textbox_Path.Text;
            ProfileValue = profileVal + "-" + platformVal;

            bool isMultiProject = IsMultiProject(WorkspacePath);

            if (isMultiProject)
                ImportMultiWgt(WorkspacePath, WgtPath, ProfileValue);
            else
                ImportWgt(WorkspacePath, WgtPath, ProfileValue);

            //close win
            Close();
        }

        private string GetToolsFolderDialog(bool newFolderOption)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
            {
                SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                                                            + "\\source\\repos",
                Description = "Select Path",
                ShowNewFolderButton = newFolderOption
            })
            {
                return (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) ?
                    folderBrowserDialog.SelectedPath : string.Empty;
            }
        }

        private string GetWgtFileDialog()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = Path.Combine(Environment.GetFolderPath
                (Environment.SpecialFolder.UserProfile), "workspace"),
                Title = "Browse Wgt Files",
                DefaultExt = "wgt",
                Filter = "wgt files (*.wgt)|*.wgt|All files (*.*)|*.*"
            })
            {
                return openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ?
                    openFileDialog.FileName : string.Empty;
            }
        }

        private void WgtPathBrowse(object sender, RoutedEventArgs e)
        {
            string wgtFilePath = GetWgtFileDialog();
            if (wgtFilePath != string.Empty)
            {
                if (!wgtFilePath.EndsWith(".wgt"))
                {
                    button_ok.IsEnabled = false;
                    System.Windows.MessageBox.Show("Not a .wgt file");
                    return;
                }

                Textbox_WgtPath.Text = wgtFilePath;
                button_ok.IsEnabled = IsOkBtnEnabled();
            }
        }

        private void WrkspacePathBrowse(object sender, RoutedEventArgs e)
        {
            string folderPath = GetToolsFolderDialog(true);
            if (folderPath != string.Empty)
            {
                if (!Directory.Exists(Path.Combine(folderPath)))
                {
                    button_ok.IsEnabled = false;
                    System.Windows.MessageBox.Show("Tizen workspace path is not valid");
                    return;
                }

                Textbox_Path.Text = folderPath;
                button_ok.IsEnabled = IsOkBtnEnabled();
            }
        }

        private void WgtPathReset(object sender, RoutedEventArgs e)
        {
            Textbox_WgtPath.Text = string.Empty;
            button_ok.IsEnabled = false;
        }

        private void WrkspacePathReset(object sender, RoutedEventArgs e)
        {
            Textbox_Path.Text = string.Empty;
            button_ok.IsEnabled = false;
        }
    }
}
