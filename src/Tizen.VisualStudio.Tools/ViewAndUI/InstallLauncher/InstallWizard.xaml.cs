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

using System.Windows;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Tizen.VisualStudio.Tools.Data;
using System.Windows.Input;
using Microsoft.Win32;
using System.Text;
using System;
using System.Windows.Controls;
using Tizen.VisualStudio.ViewAndUI.InstallLauncher;
using Tizen.VisualStudio.ExternalTools;
using System.Net;
using System.Collections.Generic;
using Microsoft.WindowsAPICodePack;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Tizen.VisualStudio.InstallLauncher
{
    /// <summary>
    /// InstallWizard.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class InstallWizard : Window
    {
        public Info info;
        public string InstallerPath { get; set; }

        private SdkInstaller installer = null;

        public delegate void OnToolsDirChangedEventHandler(string toolsDirPath);
        public static event OnToolsDirChangedEventHandler OnToolsDirChanged;

        public InstallWizard()
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;
            InitializeComponent();
            info = new Info();
            this.DataContext = info;
            PreviewKeyDown += new KeyEventHandler(KeyPressEvent);
            SecondScreen_New.Visibility = ThirdScreen_New.Visibility = FourthScreen_New.Visibility = SecondScreen_Exist.Visibility = Visibility.Hidden;
            Check_JRE();
            Button_ok.Visibility = Visibility.Hidden;
        }

        private void Check_JRE()
        {
            RegistryKey rk = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            RegistryKey subJREKey = rk?.OpenSubKey("SOFTWARE\\JavaSoft\\Java Runtime Environment");
            RegistryKey subJDKKey = rk?.OpenSubKey("SOFTWARE\\JavaSoft\\Java Development Kit");
            string currentJREVersion = subJREKey?.GetValue("CurrentVersion").ToString();
            string currentJDKVersion = subJDKKey?.GetValue("CurrentVersion").ToString();

            if (currentJREVersion != null || currentJDKVersion != null)
            {
                Label_JREWARN.Visibility = Visibility.Hidden;
            }
            else
            {
                Label_JREWARN.Visibility = Visibility.Visible;
            }
        }

        private void KeyPressEvent(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Escape))
            {
                DialogResult = false;
            }

            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key.Equals(Key.F11))
            {
                System.Windows.Forms.Form textdialog = new System.Windows.Forms.Form()
                {
                    Width = 300,
                    Height = 150,
                    StartPosition = System.Windows.Forms.FormStartPosition.CenterParent,
                    FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog,
                    Text = "Input"
                };
                System.Windows.Forms.Label label = new System.Windows.Forms.Label()
                {
                    Text = "Input installer url",
                    Left = 20,
                    Top = 10
                };
                System.Windows.Forms.TextBox url = new System.Windows.Forms.TextBox()
                {
                    Left = 20,
                    Top = 40,
                    Width = 250
                };
                System.Windows.Forms.Button okButton = new System.Windows.Forms.Button()
                {
                    Text = "OK",
                    Left = 100,
                    Top = 80,
                    Width = 100,
                    DialogResult = System.Windows.Forms.DialogResult.OK
                };
                textdialog.Controls.Add(label);
                textdialog.Controls.Add(url);
                textdialog.Controls.Add(okButton);

                if (textdialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    InstallerPath = url.Text;
                }
                else
                {
                    InstallerPath = string.Empty;
                }
            }
        }

        private void Button_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;

            installer?.CancelDownload();
        }

        private void Button_New_Click(object sender, RoutedEventArgs e)
        {
            info.NewPath = true;
            info.ExistPath = false;
            SecondScreen_New.Visibility = Visibility.Visible;

            Button_ok.Visibility = Visibility.Visible;
        }

        private void Button_Exist_Click(object sender, RoutedEventArgs e)
        {
            info.NewPath = false;
            info.ExistPath = true;
            SecondScreen_Exist.Visibility = Visibility.Visible;
            Button_ok.Visibility = Visibility.Visible;
        }


        private void Button_ok_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            string content = button.Content as string;
            if (content == InstallWizardString.Button_Next)
            {
                FourthScreen_New.Visibility = Visibility.Visible;
                LaunchOrDownloadInstaller();

                Button_ok.Visibility = Visibility.Visible;
            }

            else if (content == InstallWizardString.Button_Agree)
            {
                ThirdScreen_New.Visibility = Visibility.Visible;
            }

            else if (content == InstallWizardString.Button_Install ||
                content == InstallWizardString.Button_OK)
            {
                ToolsPathInfo.ToolsRootPath = info.Path;
                ToolsPathInfo.IsDirty = true;
                OnToolsDirChanged?.Invoke(info.Path);
                this.DialogResult = true;
            }
        }

        private void LaunchOrDownloadInstaller()
        {
            ToolsPathInfo.ToolsRootPath = info.Path;
            ToolsPathInfo.IsDirty = true;
            OnToolsDirChanged?.Invoke(info.Path);
            installer = new SdkInstaller();

            if (installer.IsDownloadNeeded())
            {
                installer.StartDownload(OnUpdateDownloadProgress, OnDownloadComplete);
            }
            else
            {
                OnDownloadComplete(null, null);
            }
        }

        private void OnUpdateDownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            info.ProgressDown = e.ProgressPercentage;
        }

        private void OnDownloadComplete(object sender, AsyncCompletedEventArgs e)
        {
            if (DialogResult != false)
            {
                DialogResult = true;
                installer?.LaunchInstaller();
            }
        }

        private void Button_back_Click(object sender, RoutedEventArgs e)
        {
            if (ThirdScreen_New.Visibility == Visibility.Visible)
            {
                SecondScreen_New.Visibility = Visibility.Visible;
            }
            else
            {
                FirstScreen.Visibility = Visibility.Visible;
                Button_ok.Visibility = Visibility.Hidden;
            }
        }

        private void Path_set_Button_Click(object sender, RoutedEventArgs e)
        {
            if (info.NewPath == true)
            {
                CommonOpenFileDialog FolderDialog = new CommonOpenFileDialog
                {
                    IsFolderPicker = true,
                    Title = InstallWizardString.Label_ThirdScreen
                };
                if (FolderDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    info.Path = FolderDialog.FileName;
                }

                FolderDialog.Dispose();
            }
            else if (info.ExistPath == true)
            {
                CommonOpenFileDialog FolderDialog = new CommonOpenFileDialog
                {
                    IsFolderPicker = true,
                    Title = InstallWizardString.Label_SecondScreen_Exist
                };
                if (FolderDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    info.Path = FolderDialog.FileName;
                }

                FolderDialog.Dispose();
            }
        }

        private void Path_exist_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (info.ExistPath == true)
            {

                Version ReferenceVer = BaselineSDKInfo.GetBaselineSDKMinVersion();
                Version InstalledVer = new Version("0.0.0");

                try
                {
                    if (File.Exists(System.IO.Path.Combine(info.Path, "baselinesdk.version")))
                    {
                        string[] ParsedString = File.ReadAllText(System.IO.Path.Combine(info.Path, "baselinesdk.version")).Split('=');
                        if (ParsedString[0].Equals("BASELINE_SDK_VERSION"))
                        {
                            Version.TryParse(ParsedString[1], out InstalledVer);
                        }

                        if (ReferenceVer > InstalledVer)
                        {
                            Label_VERWARN.Content = "Tizen SDK version is low, Please update it using package manager";
                            Label_VERWARN.Visibility = Visibility.Visible;
                            Button_ok.IsEnabled = true;
                        }

                        else
                        {
                            Label_VERWARN.Content = "";
                            Button_ok.IsEnabled = true;
                            Label_VERWARN.Visibility = Visibility.Hidden;
                        }
                    }

                    else
                    {
                        Label_VERWARN.Content = "Tizen Baseline SDK is not installed. Please install Baseline SDK packages.";
                        Label_VERWARN.Visibility = Visibility.Visible;
                        Button_ok.IsEnabled = true;
                    }
                }

                catch
                {
                    Label_VERWARN.Content = "Tizen Baseline SDK is not installed. Please install Baseline SDK packages.";
                    Label_VERWARN.Visibility = Visibility.Visible;
                    Button_ok.IsEnabled = true;
                }
            }
        }

        private void Screen_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (FirstScreen.Visibility == Visibility.Visible && ((Grid)sender) == FirstScreen)
            {
                SecondScreen_New.Visibility = ThirdScreen_New.Visibility = SecondScreen_Exist.Visibility = FourthScreen_New.Visibility = Visibility.Hidden;
                Button_back.Visibility = Visibility.Hidden;
                Button_ok.Content = InstallWizardString.Button_Next;
                Button_ok.IsEnabled = true;
            }
            else if (SecondScreen_New.Visibility == Visibility.Visible && ((Grid)sender) == SecondScreen_New)
            {
                FirstScreen.Visibility = ThirdScreen_New.Visibility = SecondScreen_Exist.Visibility = FourthScreen_New.Visibility = Visibility.Hidden;
                Button_back.Visibility = Visibility.Visible;
                Button_back.Content = InstallWizardString.Button_Back;
                Button_ok.Content = InstallWizardString.Button_Agree;
                Button_ok.IsEnabled = true;
            }
            else if (ThirdScreen_New.Visibility == Visibility.Visible && ((Grid)sender) == ThirdScreen_New)
            {
                FirstScreen.Visibility = SecondScreen_New.Visibility = SecondScreen_Exist.Visibility = FourthScreen_New.Visibility = Visibility.Hidden;
                Button_back.Visibility = Visibility.Visible;
                Button_back.Content = InstallWizardString.Button_Back;
                Button_ok.Content = InstallWizardString.Button_Next;
                Path_new.Text = string.Empty;
                Button_ok.IsEnabled = false;
                Label_WARN_EmptyDir.Visibility = Visibility.Hidden;
            }
            else if (SecondScreen_Exist.Visibility == Visibility.Visible && ((Grid)sender) == SecondScreen_Exist)
            {
                FirstScreen.Visibility = SecondScreen_New.Visibility = ThirdScreen_New.Visibility = FourthScreen_New.Visibility = Visibility.Hidden;
                Button_back.Visibility = Visibility.Visible;
                Button_back.Content = InstallWizardString.Button_Back;
                Button_ok.Content = InstallWizardString.Button_OK;
                Path_exist.Text = string.Empty;
                Button_ok.IsEnabled = false;
                Label_VERWARN.Visibility = Visibility.Hidden;
            }
            else if (FourthScreen_New.Visibility == Visibility.Visible && ((Grid)sender) == FourthScreen_New)
            {
                FirstScreen.Visibility = SecondScreen_New.Visibility = ThirdScreen_New.Visibility = SecondScreen_Exist.Visibility = Visibility.Hidden;
                Button_back.Visibility = Visibility.Hidden;
                Button_ok.IsEnabled = false;
                Button_ok.Content = InstallWizardString.Button_Finish;
            }
        }

        private void Path_new_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (info.NewPath == true)
            {
                if (!string.IsNullOrEmpty(Path_new.Text))
                {
                    if (Directory.Exists(Path_new.Text))
                    {
                        Label_WARN_EmptyDir.Visibility = Visibility.Visible;
                        Label_WARN_EmptyDir.Content = InstallWizardString.Label_DirEmptyWARN;
                        info.Path = Path_new.Text;
                        if (!Directory.EnumerateFileSystemEntries(Path_new.Text).Any())
                        {
                            Label_WARN_EmptyDir.Visibility = Visibility.Hidden;
                            Button_ok.IsEnabled = true;
                        }
                        else
                        {
                            Label_WARN_EmptyDir.Visibility = Visibility.Visible;
                            Button_ok.IsEnabled = false;
                        }
                    }
                    else
                    {
                        Label_WARN_EmptyDir.Content = InstallWizardString.Label_DirExistWARN;
                        Label_WARN_EmptyDir.Visibility = Visibility.Visible;
                        Button_ok.IsEnabled = false;
                    }
                }
                else
                {
                    Label_WARN_EmptyDir.Content = "Please select or input an empty directory";
                    Label_WARN_EmptyDir.Visibility = Visibility.Visible;
                    Button_ok.IsEnabled = false;
                }
            }
        }

    }
}
