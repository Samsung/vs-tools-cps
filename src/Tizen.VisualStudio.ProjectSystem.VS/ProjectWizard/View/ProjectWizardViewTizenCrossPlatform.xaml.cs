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

using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Collections.Generic;
using System.Windows.Media;

namespace Tizen.VisualStudio.ProjectWizard.View
{
    /// <summary>
    /// Interaction logic for ProjectWizardViewTizenCrossPlatform.xaml
    /// </summary>
    public partial class ProjectWizardViewTizenCrossPlatform : Window
    {
        internal TizenManifestData manifestData = new TizenManifestData();
        private Checker checker;

        public ProjectWizardViewTizenCrossPlatform(string project_name, string project_path, bool common_project, 
            bool mobile_project, bool tv_project, bool wearable_project, bool shared_library, List<string> project_list)
        {
            InitializeComponent();
            checker = new Checker();
            foreach (string project in project_list)
            {
                checker.ProjectList.Add(project);
            }
            
            /* Default select value */
            if (checker.ProjectList.Count <= 1)
            {
                radio_combobox.SelectedIndex = -1;
                radio_panel.IsEnabled = false;
                radio_include.Foreground = Brushes.Gray;
                radio_none.Foreground = Brushes.Gray;
            }
            else
            {
                radio_combobox.SelectedIndex = 0;
            }

            this.DataContext = checker;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;
            label_get_projectname.Content = project_name;
            label_get_projectlocation.Content = project_path;
            label_get_packagename.Content = "org.tizen.example." + project_name + ".Tizen";
            radio_combobox.IsEnabled = false;
            PreviewKeyDown += new KeyEventHandler(KeyPressEvent);
        }

        private void KeyPressEvent(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Escape))
            {
                DialogResult = false;
            }
            else if (e.Key.Equals(Key.Enter))
            {
                ValidationChecker();
            }
        }

        private void Button_cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void Button_ok_Click(object sender, RoutedEventArgs e) => ValidationChecker();

        private void ValidationChecker()
        {
            if (!checker.Common && !checker.Mobile && !checker.Tv && !checker.Wearable)
            {
                MessageBox.Show("Select at least one profile.");
            }
            else
            {
                manifestData.Select_common = checker.Common;
                manifestData.Select_mobile = checker.Mobile;
                manifestData.Select_tv = checker.Tv;
                manifestData.Select_wearable = checker.Wearable;

                if (radio_include.IsChecked == true)
                {
                    manifestData.Shared_library = true;
                }

                else
                {
                    manifestData.Shared_library = false;
                }

                DialogResult = true;
            }
        }

        private void Radio_none_Checked(object sender, RoutedEventArgs e)
        {
            radio_combobox.IsEnabled = true;
            radio_combobox.Visibility = Visibility.Visible;
        }

        private void Radio_none_Unchecked(object sender, RoutedEventArgs e)
        {
            radio_combobox.IsEnabled = false;
            radio_combobox.Visibility = Visibility.Hidden;
        }

        private void Radio_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e) => manifestData.Selected_project_name = radio_combobox.SelectedValue?.ToString();
    }

    public class Checker : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private List<string> projectList = new List<string>();

        private bool _common = true;
        public bool Common
        {
            get => _common;
            set
            {
                _common = value;
                OnPropertyChanged();
                if (_common == true)
                {
                    this.Mobile = false;
                    this.Tv = false;
                    this.Wearable = false;
                }
            }
        }

        private bool _mobile;
        public bool Mobile
        {
            get => _mobile;
            set
            {
                _mobile = value;
                OnPropertyChanged();
                if (_mobile == true)
                {
                    this.Common = false;
                }
            }
        }

        private bool _tv;
        public bool Tv
        {
            get => _tv;
            set
            {
                _tv = value;
                OnPropertyChanged();
                if (_tv == true)
                {
                    this.Common = false;
                }
            }
        }

        private bool _wearable;
        public bool Wearable
        {
            get => _wearable;
            set
            {
                _wearable = value;
                OnPropertyChanged();
                if (_wearable == true)
                {
                    this.Common = false;
                }
            }
        }

        public List<string> ProjectList { get => projectList; set => projectList = value; }

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
