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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using Tizen.VisualStudio.Tools.Data;

namespace Tizen.VisualStudio.ToolsOption
{
    /// <summary>
    /// TizenOptionPage.xaml에 대한 상호 작용 논리
    /// </summary>
    internal partial class TizenOptionPage : System.Windows.Controls.UserControl
    {
        public TizenOptionPageViewModel _TizenOptionPageViewModel = null;

        public TizenOptionPage(TizenOptionPageViewModel tizenOptionPageViewModel)
        {
            InitializeComponent();
            _TizenOptionPageViewModel = tizenOptionPageViewModel;
            this.DataContext = _TizenOptionPageViewModel;
            Refresh_Textbox();
            Tool_Checker();
        }

        private void Textbox_Path_TextChanged(object sender, TextChangedEventArgs e)
        {
            _TizenOptionPageViewModel.ToolsPath = Textbox_Path.Text;
            Refresh_Textbox();
            Tool_Checker();
        }

        private void Refresh_Textbox()
        {
            Textbox_Path.Text = _TizenOptionPageViewModel.ToolsPath;
            EmulatorMgr.Text = ToolsPathInfo.EmulatorMgrPath;
            DeviceMgr.Text = ToolsPathInfo.DeviceMgrPath;
            Sdb.Text = ToolsPathInfo.SDBPath;
            CertificateMgr.Text = ToolsPathInfo.CertificateMgrPath;
            if (!string.IsNullOrEmpty(Textbox_Path.Text))
            {
                Version ReferenceVer = BaselineSDKInfo.GetBaselineSDKMinVersion();
                Version InstalledVer = new Version("0.0.0");

                try
                {
                    if (File.Exists(System.IO.Path.Combine(Textbox_Path.Text, "baselinesdk.version")))
                    {
                        string[] ParsedString = File.ReadAllText(System.IO.Path.Combine(Textbox_Path.Text, "baselinesdk.version")).Split('=');
                        if (ParsedString[0].Equals("BASELINE_SDK_VERSION"))
                        {
                            Version.TryParse(ParsedString[1], out InstalledVer);
                        }

                        if (ReferenceVer > InstalledVer)
                        {
                            _TizenOptionPageViewModel.Notice = "Tizen SDK version is low, Please update it using package manager";
                        }

                        else
                        {
                            _TizenOptionPageViewModel.Notice = "";
                        }
                    }

                    else
                    {
                        _TizenOptionPageViewModel.Notice = "Tizen Baseline SDK is not installed. Please install Baseline SDK packages.";
                    }
                }

                catch
                {
                    _TizenOptionPageViewModel.Notice = "";
                }

            }
            else
            {
                _TizenOptionPageViewModel.Notice = "";
            }
        }

        private void Button_Browse_Click(object sender, RoutedEventArgs e)
        {
            string folderPath = GetToolsFolderDialog(Textbox_Path.Text);
            if (folderPath != string.Empty)
            {
                Textbox_Path.Text = folderPath;
            }
        }

        private string GetToolsFolderDialog(string toolsPath)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
            {
                SelectedPath = toolsPath,
                Description = "Tizen Tools Path",
                ShowNewFolderButton = false
            })
            {
                return (folderBrowserDialog.ShowDialog() == DialogResult.OK) ?
                    folderBrowserDialog.SelectedPath : string.Empty;
            }
        }

        private void Button_Reset_Click(object sender, RoutedEventArgs e)
        {
            Textbox_Path.Text = string.Empty;
        }

        private void Tool_Checker()
        {
            var StatusInvalid = new Uri("StatusInvalid_16x.png", UriKind.Relative);
            var StatusOK = new Uri("StatusOK_16x.png", UriKind.Relative);
            if (File.Exists(ToolsPathInfo.EmulatorMgrPath))
            {
                EmulatorMgr_image.Source = new BitmapImage(StatusOK);
            }
            else
            {
                EmulatorMgr_image.Source = new BitmapImage(StatusInvalid);
            }

            if (File.Exists(ToolsPathInfo.DeviceMgrPath))
            {
                DeviceMgr_image.Source = new BitmapImage(StatusOK);
            }
            else
            {
                DeviceMgr_image.Source = new BitmapImage(StatusInvalid);
            }

            if (File.Exists(ToolsPathInfo.SDBPath))
            {
                Sdb_image.Source = new BitmapImage(StatusOK);
            }
            else
            {
                Sdb_image.Source = new BitmapImage(StatusInvalid);
            }

            if (File.Exists(ToolsPathInfo.CertificateMgrPath))
            {
                CertificateMgr_image.Source = new BitmapImage(StatusOK);
            }
            else
            {
                CertificateMgr_image.Source = new BitmapImage(StatusInvalid);
            }

        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Refresh_Textbox();
            Tool_Checker();
        }
    }
}
