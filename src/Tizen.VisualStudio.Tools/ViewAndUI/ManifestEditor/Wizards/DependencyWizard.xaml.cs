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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;

namespace Tizen.VisualStudio.ManifestEditor
{
    /// <summary>
    /// Interaction logic for DependencyWizard.xaml
    /// </summary>
    public partial class DependencyWizard : Window
    {
        List<string> dependencyTypes = new List<string>() { "requires", "wants" };

        public DependencyWizard(string wizardTitle,
                                  string dependencyType = null,
                                  string packageIDValue = null, string requiredVersion = null)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;
            InitializeComponent();
            this.Title = wizardTitle;
            AddComboBoxChild();
            // Modify Case
            if (dependencyType != null)
            {
                if (this.TypeComboBox.Items.Contains(dependencyType))
                {
                    this.TypeComboBox.SelectedItem = dependencyType;
                }
            }
            // Modify Case
            if (packageIDValue != null)
            {
                this.packageIDTextBox.Text = packageIDValue;
            }
            // Modify Case
            if (requiredVersion != null)
            {
                this.requiredVersionTextBox.Text = requiredVersion;
            }

            this.Loaded += DependencyWizard_Loaded;
        }

        private void DependencyWizard_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.packageIDTextBox.Text == string.Empty)
            {
                this.OkBtn.IsEnabled = false;
            }
        }

        private void AddComboBoxChild()
        {
            foreach (var item in dependencyTypes)
            {
                this.TypeComboBox.Items.Add(item);
            }
        }
        
        private void EnableCheckOKbtn()
        {
            if (this.packageIDTextBox.Text != string.Empty && this.TypeComboBox.SelectedItem != null)
            {
                this.OkBtn.IsEnabled = true;
            }
            else
            {
                this.OkBtn.IsEnabled = false;
            }
        }

        private void packageIDTextBox_TextChanged(object sender,
                                                    TextChangedEventArgs e)
        {
            EnableCheckOKbtn();
        }

        private void TypeComboBox_SelectionChanged(object sender,
                                                   SelectionChangedEventArgs e)
        {
            EnableCheckOKbtn();
        }

        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
