/*
 * Copyright 2018 (c) Samsung Electronics Co., Ltd  All Rights Reserved.
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
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System;
using System.IO;

namespace Tizen.VisualStudio.ProjectWizard.View
{
    public class ViewModel
    {
        public ObservableCollection<string> platformVersionList { get; private set; }

        public ViewModel(List<string> versionList)
        {
            platformVersionList = new ObservableCollection<string>(versionList);
        }
    }

    /// <summary>
    /// Interaction logic for ProjectWizardViewTizenNative.xaml
    /// </summary>
    public partial class ProjectWizardViewTizenPlatformVersion : Window
    {

        internal TizenNativeSelector data = new TizenNativeSelector();
        public string platformVersion;

        public ProjectWizardViewTizenPlatformVersion(List<string> VersionList)
        {

            InitializeComponent();
            //platformVersionList = VersionList;
            DataContext = new ViewModel(VersionList);
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;
        }

        private void FindOrAdd(List<string> list, string value)
        {
            foreach(string s in list)
            {
                if (s == value)
                    return;
            }
            list.Add(value);
        }
        private void Button_cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void Button_ok_Click(object sender, RoutedEventArgs e) => ValidationChecker();


        private void ValidationChecker()
        {
            if (comboBox_PlatformVersion.SelectedValue != null && comboBox_PlatformVersion.SelectedValue.ToString() != "")
            {
                platformVersion = comboBox_PlatformVersion.SelectedValue.ToString();
                DialogResult = true;
                return;
            }

            MessageBox.Show("Pleae select platform version for which you are trying to create application");
        }
    }
}
