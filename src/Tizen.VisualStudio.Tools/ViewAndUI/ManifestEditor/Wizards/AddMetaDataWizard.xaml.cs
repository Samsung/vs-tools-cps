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

namespace Tizen.VisualStudio.ManifestEditor
{
    /// <summary>
    /// Interaction logic for AddMetaDataWizard.xaml
    /// </summary>
    public partial class AddMetaDataWizard : Window
    {
        private List<metadata> MetadataList = new List<metadata>();
        private List<string> MetadataKeyList = new List<string>();
        public AddMetaDataWizard(string key = null, string value = null, List<metadata> ExistList = null)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;
            InitializeComponent();
            MetadataList = ExistList;
            if (MetadataList != null)
            {
                MakeMetadataKeyList();
                if (key != null)
                {
                    MetadataKeyList.Remove(key);
                }
            }

            this.OkBtn.IsEnabled = false;
            this.keyTextBox.Text = key;
            this.valueTextBox.Text = value;
        }

        private void MakeMetadataKeyList()
        {
            foreach (metadata input in MetadataList)
            {
                MetadataKeyList.Add(input.key);
            }
        }

        private void keyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(this.keyTextBox.Text.Trim()) || MetadataKeyList.Contains(this.keyTextBox.Text.Trim()))
            {
                OkBtn.IsEnabled = false;
            }
            else
            {
                OkBtn.IsEnabled = true;
            }
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
