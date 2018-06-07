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
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace Tizen.VisualStudio.ManifestEditor
{
    /// <summary>
    /// Interaction logic for NewIconWizard.xaml
    /// </summary>
    public partial class NewIconWizard : Window
    {
        public string resPath;
        public string filePath;
        public string fileName;
        public string destFilePath = null;
        public int imgSize;
        enum Density : int
        {
            MainXhigh = 117,
            MainHigh = 78,
            AccountXhigh = 72,
            AccountHigh = 48,
            AccountSmallXhigh = 45,
            AccountSmallHigh = 30
        }

        public NewIconWizard(string resPath, List<String> fList)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;
            InitializeComponent();
            this.resPath = resPath;
            this.label_size.Content = "";
            this.radioButton_mainMenu.IsChecked = true;
            this.comboBox_density.SelectedIndex = 0;
            this.button_ok.IsEnabled = false;
            this.label_path_WARNING.Content = "Click browse button to set icon path";
            this.label_path_WARNING.Foreground = new SolidColorBrush(Colors.Red);
            this.label_path.Visibility = Visibility.Hidden;
        }

        private void button_browseIcon_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fDialog = new OpenFileDialog();
            fDialog.DefaultExt = ".png";
            fDialog.Filter = "PNG FIles (*.png)|*.png";
            fDialog.InitialDirectory = this.resPath;

            if (fDialog.ShowDialog() == true)
            {
                try
                {
                    this.filePath = fDialog.FileName;
                    this.fileName = new FileInfo(filePath).Name;
                    this.destFilePath = this.resPath + this.fileName;
                    this.button_ok.IsEnabled = true;
                    this.label_path_WARNING.Visibility = Visibility.Hidden;
                    this.label_path.Content = fileName;
                    this.label_path.Visibility = Visibility.Visible;

                    BitmapImage previewImage = new BitmapImage();
                    previewImage.BeginInit();
                    previewImage.UriSource = new Uri(filePath);
                    previewImage.EndInit();
                    this.image_preview.Source = previewImage;
                }
                catch (Exception LoadBitmapException)
                {
                    MessageBox.Show(LoadBitmapException + "");
                    this.DialogResult = false;
                }
            }
        }

        private string StringSplit(string density)
        {
            return density.Split('x')[0];
        }

        private void button_ok_Click(object sender, RoutedEventArgs e)
        {
            if (destFilePath != null)
            {
                this.DialogResult = true;
            }
        }

        private void button_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void radioButton_mainMenu_Checked(object sender, RoutedEventArgs e)
        {
            comboBox_density.Items.Clear();
            comboBox_density.Items.Add("Xhigh");
            comboBox_density.Items.Add("High");
            comboBox_density.SelectedIndex = 0;
        }

        private void radioButton_account_Checked(object sender, RoutedEventArgs e)
        {
            comboBox_density.Items.Clear();
            comboBox_density.Items.Add("Xhigh");
            comboBox_density.Items.Add("High");
            comboBox_density.SelectedIndex = 0;
        }

        private void radioButton_accountSmall_Checked(object sender, RoutedEventArgs e)
        {
            comboBox_density.Items.Clear();
            comboBox_density.Items.Add("Xhigh");
            comboBox_density.Items.Add("High");
            comboBox_density.SelectedIndex = 0;
        }

        private void radioButton_etc_Checked(object sender, RoutedEventArgs e)
        {
            comboBox_density.Items.Clear();
            comboBox_density.Items.Add("None");
            comboBox_density.SelectedIndex = 0;
            this.SetImageSize(0);
        }

        private void SetImageSize(int imgSize)
        {
            this.imgSize = imgSize;
            this.label_size.Content = imgSize + "x" + imgSize;
            if (imgSize == 0)
            {
                this.label_size.Content = "Original size";
            }
        }

        private void comboBox_density_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (radioButton_mainMenu.IsChecked == true)
            {
                if (comboBox_density.SelectedIndex == 0)
                {
                    this.SetImageSize((int)Density.MainXhigh);
                }
                else
                {
                    this.SetImageSize((int)Density.MainHigh);
                }
            }

            else if (radioButton_account.IsChecked == true)
            {
                if (comboBox_density.SelectedIndex == 0)
                {
                    this.SetImageSize((int)Density.AccountXhigh);
                }
                else
                {
                    this.SetImageSize((int)Density.AccountHigh);
                }
            }

            else if (radioButton_accountSmall.IsChecked == true)
            {
                if (comboBox_density.SelectedIndex == 0)
                {
                    this.SetImageSize((int)Density.AccountSmallXhigh);
                }
                else
                {
                    this.SetImageSize((int)Density.AccountSmallHigh);
                }
            }
            else
            {
            }
        }
    }
}
