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
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel;
using System.Drawing;

namespace Tizen.VisualStudio.ManifestEditor
{
    /// <summary>
    /// Interaction logic for IconChooserWizard.xaml
    /// </summary>

    public partial class IconChooserWizard : Window
    {
        private string resPath;
        private string projectPath;
        public string selectImageValue;
        public List<string> fList = new List<string>();
        private CollectionView view;
        private EnvDTE.DTE dte;

        public IconChooserWizard(EnvDTE.DTE dte)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;
            InitializeComponent();
            this.projectPath = System.IO.Path.GetDirectoryName(dte.ActiveDocument.Path);
            this.resPath = this.projectPath + "\\shared\\res\\";
            this.dte = dte;
            Initialize();
        }

        public void Initialize()
        {
            DirectoryInfo dInfo = new DirectoryInfo(resPath);
            if (!dInfo.Exists)
            {
                dInfo.Create();
            }

            FileInfo[] Files = dInfo.GetFiles("*.png");

            foreach (FileInfo file in Files)
            {
                fList.Add(file.Name);
            }

            this.listView_iconList.ItemsSource = fList;
            this.view = (CollectionView)CollectionViewSource.GetDefaultView(listView_iconList.ItemsSource);
            this.view.Filter = UserFilter;
            ButtonEnableCheck();
        }

        private bool UserFilter(object item)
        {
            if (string.IsNullOrEmpty(textBox_search.Text))
            {
                return true;
            }
            else
            {
                return ((item as string).IndexOf(textBox_search.Text, StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }

        private void textBox_search_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(listView_iconList.ItemsSource).Refresh();
            ButtonEnableCheck();
        }

        private void button_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void ButtonEnableCheck()
        {
            this.view.Refresh();
            if (listView_iconList.Items.Count != 0)
            {
                listView_iconList.SelectedIndex = 0;
                button_ok.IsEnabled = true;
            }
            else
            {
                button_ok.IsEnabled = false;
            }
        }

        private void button_ok_Click(object sender, RoutedEventArgs e)
        {
            selectImageValue = this.listView_iconList.SelectedItem.ToString();
            this.DialogResult = true;
            this.Close();
        }

        private void button_newIcon_Click(object sender, RoutedEventArgs e)
        {
            NewIconWizard nWizard = new NewIconWizard(this.resPath, fList);
            if (nWizard.ShowDialog() == true)
            {
                if (File.Exists(nWizard.destFilePath) == false)
                {
                    File.Copy(nWizard.filePath, nWizard.destFilePath);
                    fList.Add(new FileInfo(nWizard.filePath).Name);
                    Bitmap toResize = new Bitmap(nWizard.destFilePath);
                    Bitmap resultImage = new Bitmap(toResize, new System.Drawing.Size(nWizard.imgSize, nWizard.imgSize));
                    toResize.Dispose();
                    resultImage.Save(nWizard.destFilePath + ".temp");
                    File.SetAttributes(nWizard.destFilePath, FileAttributes.Normal);
                    File.Delete(nWizard.destFilePath);
                    File.Move(nWizard.destFilePath + ".temp", nWizard.destFilePath);
                    ButtonEnableCheck();
                    textBox_search.Clear();
                    listView_iconList.SelectedIndex = listView_iconList.Items.Count - 1;
                    resultImage.Dispose();
                }
                else
                {
                    MessageBox.Show("Same file name exists!");
                }
            }
        }
    }
}
