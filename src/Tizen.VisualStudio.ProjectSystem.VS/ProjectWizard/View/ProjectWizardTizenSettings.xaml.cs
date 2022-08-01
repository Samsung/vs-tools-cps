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
*/

using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using Tizen.VisualStudio.Tools.Data;
using Tizen.VisualStudio.Utilities;

namespace Tizen.VisualStudio.ProjectWizard.View
{
    public partial class ProjectWizardTizenSettings : System.Windows.Window
    {
        string workspacePath;
        VsProjectHelper prjHelperInstance;
        string profile, api_version, deviceType;
        Dictionary<string, string> archMap, toolchainMap;
        List<string> archList, toolchainList, rootstrapList;
        string archVal, tcVal, rootstrapVal;
        public ProjectWizardTizenSettings(string dir)
        {
            InitializeComponent();
            workspacePath = dir;
            archList = new List<string>();
            toolchainList = new List<string>();
            rootstrapList = new List<string>();
            prjHelperInstance = VsProjectHelper.GetInstance;
            profile = prjHelperInstance.getTag(workspacePath, "profile");
            api_version = prjHelperInstance.getTag(workspacePath, "api_version");
            archMap = new Dictionary<string, string>(){
                {"mobile-7.0", "x86,arm" },
                {"mobile-6.5", "x86,arm" },
                {"mobile-6.0", "x86,arm" },
                {"mobile-5.5", "x86,arm" },
                {"mobile-5.0", "x86,arm" },
                {"mobile-4.0", "x86,arm,x86_64,aarch64" },
                {"wearable-7.0", "x86,arm" },
                {"wearable-6.5", "x86,arm" },
                {"wearable-6.0", "x86,arm" },
                {"wearable-5.5", "x86,arm" },
                {"wearable-5.0", "x86,arm" },
                {"wearable-4.0", "x86,arm" },
                {"iot-headed-7.0", "aarch64,arm" },
                {"iot-headed-6.5", "aarch64,arm" },
                {"iot-headed-6.0", "aarch64,arm" },
                {"iot-headed-5.5", "arm" },
                {"iot-headless-7.0", "arm" },
                {"iot-headless-6.5", "arm" },
                {"iot-headless-6.0", "arm" },
                {"iot-headless-5.5", "arm" },
            };
            toolchainMap = new Dictionary<string, string>(){
                {"mobile-7.0", "LLVM-10.0 with GCC-9.2,GCC-9.2"},
                {"mobile-6.5", "LLVM-10.0 with GCC-9.2,GCC-9.2"},
                {"mobile-6.0", "LLVM-10.0 with GCC-9.2,GCC-9.2"},
                {"mobile-5.5", "LLVM-4.0 with GCC-6.2,GCC-6.2"},
                {"mobile-5.0", "LLVM-4.0 with GCC-6.2,GCC-6.2"},
                {"mobile-4.0", "LLVM-4.0 with GCC-6.2,GCC-6.2"},
                {"wearable-7.0", "LLVM-10.0 with GCC-9.2,GCC-9.2"},
                {"wearable-6.5", "LLVM-10.0 with GCC-9.2,GCC-9.2"},
                {"wearable-6.0", "LLVM-10.0 with GCC-9.2,GCC-9.2"},
                {"wearable-5.5", "LLVM-4.0 with GCC-6.2,GCC-6.2"},
                {"wearable-5.0", "LLVM-4.0 with GCC-6.2,GCC-6.2"},
                {"wearable-4.0", "LLVM-4.0 with GCC-6.2,GCC-6.2"},
                {"iot-headed-7.0", "LLVM-10.0 with GCC-9.2,GCC-9.2"},
                {"iot-headed-6.5", "LLVM-10.0 with GCC-9.2,GCC-9.2"},
                {"iot-headed-6.0", "LLVM-10.0 with GCC-9.2,GCC-9.2"},
                {"iot-headed-5.5", "LLVM-4.0 with GCC-6.2,GCC-6.2"},
                {"iot-headless-7.0", "LLVM-10.0 with GCC-9.2,GCC-9.2"},
                {"iot-headless-6.5", "LLVM-10.0 with GCC-9.2,GCC-9.2"},
                {"iot-headless-6.0", "LLVM-10.0 with GCC-9.2,GCC-9.2"},
                {"iot-headless-5.5", "LLVM-4.0 with GCC-6.2,GCC-6.2"},
            };
            string platform = profile + "-" + api_version;
            label_get_platform.Content = platform;
            char[] delims = new[] { ',' };
            string[] archstrings = archMap[platform].Split(delims, StringSplitOptions.RemoveEmptyEntries);
            foreach (string str in archstrings)
            {
                archList.Add(str);
            }
            string[] tcstrings = toolchainMap[platform].Split(delims, StringSplitOptions.RemoveEmptyEntries);
            foreach (string str in tcstrings)
            {
                toolchainList.Add(str);
            }
            rootstrapList.Add("public");
            if (IsPrivateRootstrapAvailable())
            {
                rootstrapList.Add("private");
            }
            arch_combobox.ItemsSource = new ObservableCollection<string>(archList);
            toolchain_combobox.ItemsSource = new ObservableCollection<string>(toolchainList);
            rootstrap_combobox.ItemsSource = new ObservableCollection<string>(rootstrapList);
            //setting default selected values
            arch_combobox.Text = prjHelperInstance.getTag(workspacePath, "arch");
            if(arch_combobox.Text == "arm")
            {
                deviceType = "device";
            } else if(arch_combobox.Text == "aarch64")
            {
                deviceType = "device64";
            } else
            {
                deviceType = "emulator";
            }
            if (prjHelperInstance.getTag(workspacePath, "compiler") == "llvm")
            {
                toolchain_combobox.Text = toolchainList[0];
            } else
            {
                toolchain_combobox.Text = toolchainList[1];
            }
            rootstrap_combobox.Text = prjHelperInstance.getTag(workspacePath, "rootstrap");
        }

        void ButtonOkClick(object sender, RoutedEventArgs e)
        {
            ProjectWizardTizenSettingsXaml.Close();

            prjHelperInstance.UpdateYaml(workspacePath, "arch:", archVal);

            string compilerVal;
            if (tcVal.Contains("LLVM"))
            {
                compilerVal = "llvm";
            }
            else
            {
                compilerVal = "gcc";
            }
            prjHelperInstance.UpdateYaml(workspacePath, "compiler:", compilerVal);
            string oldRootstrap = prjHelperInstance.getTag(workspacePath, "rootstrap");
            prjHelperInstance.UpdateYaml(workspacePath, "rootstrap:", rootstrapVal);
            if (!rootstrapVal.Equals(oldRootstrap))
            {
                var waitPopup = new WaitDialogUtil();
                waitPopup.ShowPopup("Tizen Settings",
                        "Please wait while project settings are updated...",
                        "updating...", "Rootstrap changes update in progress...");
                prjHelperInstance.updateAdditionalIncludeDirectoriesOfSolution();
                waitPopup.ClosePopup();
            }
               
        }
        private void ArchSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string value = arch_combobox.SelectedValue as string;
            if (!string.IsNullOrEmpty(value))
                archVal = value;
            if (value == "arm")
            {
                deviceType = "device";
            }
            else if (value == "aarch64")
            {
                deviceType = "device64";
            }
            else
            {
                deviceType = "emulator";
            }
            //resetting rootsrap value
            rootstrap_combobox.ItemsSource = null;
            rootstrapList.Clear();
            rootstrapList.Add("public");
            rootstrap_combobox.SelectedItem = rootstrapList[0];
            if (IsPrivateRootstrapAvailable())
            {
                rootstrapList.Add("private");
            }
            rootstrap_combobox.ItemsSource = rootstrapList;
        }
        private void RootstrapSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string value = rootstrap_combobox.SelectedValue as string;
            if (!string.IsNullOrEmpty(value))
                rootstrapVal = value;
        }
        private void ToolchainSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string value = toolchain_combobox.SelectedValue as string;
            if (!string.IsNullOrEmpty(value))
                tcVal = value;
        }
        private void ButtonCancelClick(object sender, RoutedEventArgs e) => this.Close();
        private bool IsPrivateRootstrapAvailable()
        {
            string platform = "tizen-" + api_version;
            string rootstrapPath = Path.Combine(ToolsPathInfo.ToolsRootPath, "platforms", platform, profile, "rootstraps");
            string[] subDirs = Directory.GetDirectories(rootstrapPath);
            foreach (string path in subDirs)
            {
                string folderName = Path.GetFileName(path);
                if(folderName.Contains(deviceType + ".") && folderName.Contains("private"))
                {
                    return true;
                }
            }
            return false;
        }

    }
}
