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
 * 
 * Contributors:
 * - SRIB
*/

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using Tizen.VisualStudio.Tools.Data;

namespace Tizen.VisualStudio.ProjectWizard.View
{
    /// <summary>
    /// Interaction logic for ProjectWizardViewNativeProject.xaml
    /// </summary>
    public class ProjectWizardProjectViewModel
    {
        public ObservableCollection<string> ProfileList { get; set; }

        public string ProjectName { get; set; }

        public string SolnDir { get; set; }
        public string SelectedProfile { get; set; }
        public string SelectedPlatform { get; set; }

        public ProjectWizardProjectViewModel(List<string> profileList, string prjName, string solnDir)
        {
            ProfileList = new ObservableCollection<string>(profileList);
            ProjectName = prjName;
            SolnDir = solnDir;
        }
    }
    public partial class ProjectWizardViewProject : Window
    {
        private readonly string projectType;
        private readonly string projectName;
        private readonly string solnDir;

        public List<string> PlatformList
        {
            get; private set;
        }
        public string ProfileValue
        {
            get; private set;
        }
        public string MultiTemplateValue
        {
            get; private set;
        }

        public string TemplateValue
        {
            get; private set;
        }

        public ProjectWizardViewProject(string type, string prjName, string solDir, 
            List<string> profileList, List<string> platformList)
        {
            InitializeComponent();
            projectType = type;
            projectName = prjName;
            PlatformList = platformList;
            solnDir = solDir;
            DataContext = new ProjectWizardProjectViewModel(profileList, projectName, solnDir);

            /* Set the default value for Profile and Platform
             * incase of adding project to an exiting solution.
             */
            if (profileList.Count == 1 && platformList.Count == 1)
            {
                comboBox_Profile.ItemsSource = profileList;
                comboBox_Profile.SelectedIndex = 0;
                comboBox_PlatformVersion.ItemsSource = platformList;
                comboBox_PlatformVersion.SelectedIndex = 0;
            }
            else
            {
                comboBox_PlatformVersion.ItemsSource = new List<string>();
                comboBox_PlatformVersion.SelectedItem = null;
                comboBox_PlatformVersion.IsEnabled = false;
                comboBox_Template.ItemsSource = new List<string>();
                comboBox_Template.SelectedItem = null;
                comboBox_Template.IsEnabled = false;
            }

        }

        private void OnCommonChecked(object sender, RoutedEventArgs e)
        {
            mobile.IsChecked = false;
            tv.IsChecked = false;
            wearable.IsChecked = false;
            MultiTemplateValue = "common";
        }

        private void OnCommonUnchecked(object sender, RoutedEventArgs e)
        {
            MultiTemplateValue = null;
        }

        private void OnMobileChecked(object sender, RoutedEventArgs e)
        {
            common.IsChecked = false;
            MultiTemplateValue = "mobile";

            if (tv.IsChecked == true)
                MultiTemplateValue = MultiTemplateValue + "," + "tv";
            if (wearable.IsChecked == true)
                MultiTemplateValue = MultiTemplateValue + "," + "wearable";

        }

        private void OnMobileUnchecked(object sender, RoutedEventArgs e)
        {
            MultiTemplateValue = null;

            if (tv.IsChecked == true)
                MultiTemplateValue = "tv";
            if (wearable.IsChecked == true)
                MultiTemplateValue = MultiTemplateValue + "," + "wearable";
        }
        private void OnTvChecked(object sender, RoutedEventArgs e)
        {
            common.IsChecked = false;
            MultiTemplateValue = "tv";

            if (mobile.IsChecked == true)
                MultiTemplateValue = MultiTemplateValue + "," + "mobile";
            if (wearable.IsChecked == true)
                MultiTemplateValue = MultiTemplateValue + "," + "wearable";
        }

        private void OnTvUnchecked(object sender, RoutedEventArgs e)
        {
            MultiTemplateValue = null;

            if (mobile.IsChecked == true)
                MultiTemplateValue = "mobile";
            if (wearable.IsChecked == true)
                MultiTemplateValue = MultiTemplateValue + "," + "wearable";
        }
        private void OnWearChecked(object sender, RoutedEventArgs e)
        {
            common.IsChecked = false;
            MultiTemplateValue = "wearable";

            if (mobile.IsChecked == true)
                MultiTemplateValue = MultiTemplateValue + "," + "mobile";
            if (tv.IsChecked == true)
                MultiTemplateValue = MultiTemplateValue + "," + "tv";
        }

        private void OnWearUnchecked(object sender, RoutedEventArgs e)
        {
            MultiTemplateValue = null;

            if (mobile.IsChecked == true)
                MultiTemplateValue = "mobile";
            if (tv.IsChecked == true)
                MultiTemplateValue = MultiTemplateValue + "," + "tv";
        }

        private void OnProfileSelectionChanged(object sender, RoutedEventArgs e)
        {
            comboBox_PlatformVersion.SelectedItem = null;
            comboBox_PlatformVersion.IsEnabled = false;
            comboBox_Template.SelectedItem = null;
            comboBox_Template.IsEnabled = false;

            if (comboBox_Profile.SelectedValue == null
                || comboBox_Profile.SelectedValue.ToString() == string.Empty)
            {
                return;
            }

            
            if (string.IsNullOrEmpty(ToolsPathInfo.ToolsRootPath))
            {
                _ = Dispatcher.BeginInvoke(
                    new ThreadStart(
                        () => MessageBox.Show($"Tizen tools path not set.",
                         "Error",
                        (MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                        (MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Exclamation))
                    );
                DialogResult = false;
                return;
            }

            string profileVal = comboBox_Profile.SelectedValue.ToString();
            comboBox_PlatformVersion.IsEnabled = true;

            if (PlatformList.Count > 0)
            {
                comboBox_PlatformVersion.ItemsSource = PlatformList;
            }
            else
            {
                List<string> profileList = VsProjectHelper.GetInstance.GetProfileList(projectType);
                List<string> tempList =
                    profileList.FindAll(v => v.Contains(profileVal));
                List<string> platformList = new List<string>();
                tempList.ForEach(x =>
                {
                    int hyphenPos = x.LastIndexOf("-");
                    platformList.Add(x.Substring(hyphenPos + 1));
                });

                comboBox_PlatformVersion.ItemsSource = platformList;
            }
        }
        private void OnPlatformSelectionChanged(object sender, RoutedEventArgs e)
        {
            comboBox_Template.SelectedItem = null;
            comboBox_Template.IsEnabled = false;

            if (comboBox_PlatformVersion.SelectedValue == null
                || comboBox_PlatformVersion.SelectedValue.ToString() == string.Empty 
                || comboBox_Profile.SelectedValue == null
                || comboBox_Profile.SelectedValue.ToString() == string.Empty)
            {
                return;
            }

            string platformVal = comboBox_PlatformVersion.SelectedValue.ToString();
            string profileVal = comboBox_Profile.SelectedValue.ToString();
            string val = profileVal + "-" + platformVal;

            string rootStrapPath = Path.Combine(ToolsPathInfo.ToolsRootPath, "platforms", "tizen-" + platformVal, profileVal, "rootstraps", "info");

            List<string> templateList = VsProjectHelper.GetInstance.GetTemplateList(projectType, val);

            if (projectType == "native" && (!Directory.Exists(rootStrapPath) || Directory.GetFiles(rootStrapPath, $"{val}-*.core.dev.xml").Length == 0)) //sdk not installed for native case
            {
                comboBox_Template.IsEnabled = false;
                _ = Dispatcher.BeginInvoke(
                    new ThreadStart(
                        () => MessageBox.Show($"Sdk profile {val} not found.",
                        "Error",
                        (MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                        (MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Exclamation)
                    )
                );
            }
            else
            {
                comboBox_Template.IsEnabled = true;
                comboBox_Template.ItemsSource = templateList;
            }
        }

        private void ResetMultiTemplatePanel()
        {
            multiTemplatePanel.Visibility = Visibility.Hidden;
            common.IsChecked = false;
            mobile.IsChecked = false;
            tv.IsChecked = false;
            wearable.IsChecked = false;
            MultiTemplateValue = null;
        }

        private void OnTemplateChanged(object sender, RoutedEventArgs e)
        {
            button_ok.IsEnabled =
                comboBox_PlatformVersion.SelectedValue != null
                && comboBox_PlatformVersion.SelectedValue.ToString() != string.Empty
                && comboBox_Profile.SelectedValue != null
                && comboBox_Profile.SelectedValue.ToString() != string.Empty
                && comboBox_Template.SelectedValue != null
                && comboBox_Template.SelectedValue.ToString() != string.Empty;

            /* handle dynamic change of Wizard UI for providing option for Multi template
             * for TizenXamlApp and  TizenCrossNET dotnet app.
             */
            if (projectType.Equals("dotnet"))
            {
                if(comboBox_Template.SelectedValue == null)
                {
                    ResetMultiTemplatePanel();
                    return;
                }

                var templateName = comboBox_Template.SelectedValue.ToString();
                if(templateName.Equals("TizenXamlApp") || templateName.Equals("TizenCrossNET"))
                {
                    multiTemplatePanel.Visibility = Visibility.Visible;
                    common.IsChecked = true;
                    MultiTemplateValue = "common";
                }
                else
                {
                    ResetMultiTemplatePanel();
                }
            }
        }

        private void Button_cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Button_ok_Click(object sender, RoutedEventArgs e)
        {
            if (comboBox_Profile.SelectedValue == null
                || comboBox_Profile.SelectedValue.ToString() == string.Empty)
            {
                _ = MessageBox.Show("Please select a profile.", "Failed",
                    (MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                    (MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Exclamation);
                return;
            }
            if (comboBox_PlatformVersion.SelectedValue == null 
                || comboBox_PlatformVersion.SelectedValue.ToString() == string.Empty)
            {
                _ = MessageBox.Show("Please select a platform version.", "Failed",
                    (MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                    (MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Exclamation);
                return;
            }
            if (comboBox_Template.SelectedValue == null
                || comboBox_Template.SelectedValue.ToString() == string.Empty)
            {
                _ = MessageBox.Show("Please select a template.", "Failed",
                    (MessageBoxButton)System.Windows.Forms.MessageBoxButtons.OK,
                    (MessageBoxImage)System.Windows.Forms.MessageBoxIcon.Exclamation);
                return;
            }

            string platformVal = comboBox_PlatformVersion.SelectedValue.ToString();
            string profileVal = comboBox_Profile.SelectedValue.ToString();
            string templateVal = comboBox_Template.SelectedValue.ToString();
            ProfileValue = profileVal + "-" + platformVal;
            TemplateValue = templateVal;

            DialogResult = true;
        }
    }
}
