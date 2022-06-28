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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Shell;
using Tizen.VisualStudio.Tools.Data;
using Tizen.VisualStudio.InstallLauncher;
using Tizen.VisualStudio.Utilities;
using System.Windows.Forms;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;

namespace Tizen.VisualStudio.ToolsOption
{
    public class TizenOptionPageViewModel : UIElementDialogPage, INotifyPropertyChanged
    {
        public string Notice;
        private static WritableSettingsStore userSettingsStore = null;
        private const string SettingsCollectionPath = "TizenOptions";

        protected override System.Windows.UIElement Child
        {
            get { return new TizenOptionPage(this); }
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);
            if (!string.IsNullOrEmpty(Notice))
            {
                System.Windows.Forms.MessageBox.Show(Notice);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (ToolsPathInfo.IsDirty)
            {
                ToolsPathInfo.IsDirty = false;
                SaveSettingsToStorage();
            }
            base.OnClosed(e);
        }

        public static void Initialize(Package package)
        {
            SettingsManager settingsManager = new ShellSettingsManager(package);
            userSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
            if (userSettingsStore != null && !userSettingsStore.CollectionExists(SettingsCollectionPath))
            {
                userSettingsStore.CreateCollection(SettingsCollectionPath);
            }

            if (userSettingsStore != null && !userSettingsStore.PropertyExists(SettingsCollectionPath, "UseAnalytics"))
                userSettingsStore.SetBoolean(SettingsCollectionPath, "UseAnalytics", true);

            var page = (TizenOptionPageViewModel)package.GetDialogPage(typeof(TizenOptionPageViewModel));
            if (page != null)
            {
                if (userSettingsStore != null)
                    page.UseAnalytics = userSettingsStore.GetBoolean(SettingsCollectionPath, "UseAnalytics");
                ToolsPathInfo.ToolsRootPath = page.ToolsPath;
                DebuggerInfo.UseLiveProfiler = page.UseLiveProfiler;
                AnalyticsInfo.UseAnalytics = page.UseAnalytics;
                InstallWizard.OnToolsDirChanged += page.OnUpdateByInstallWizard;
                HotReloadInfo.UseHotReload = page.UseHotReload;
                ToolsPathInfo.ChromePath = page.ChromePath;
            }
        }

        private void OnUpdateByInstallWizard(string updatedPath)
        {
            ToolsPath = updatedPath;
            SaveSettingsToStorage();
        }

        public string ToolsPath
        {
            get => ToolsPathInfo.ToolsRootPath;
            set
            {
                if (ToolsPathInfo.ToolsRootPath != value)
                {
                    ToolsPathInfo.ToolsRootPath = value;
                    HandlePropertyChanged();
                }
            }
        }

        public string ChromePath
        {
            get => ToolsPathInfo.ChromePath;
            set
            {
                if (ToolsPathInfo.ChromePath != value)
                {
                    ToolsPathInfo.ChromePath = value;
                    HandlePropertyChanged();
                }
            }
        }

        public bool UseLiveProfiler
        {
            get => DebuggerInfo.UseLiveProfiler;
            set
            {
                if (DebuggerInfo.UseLiveProfiler != value)
                {
                    DebuggerInfo.UseLiveProfiler = value;
                    HandlePropertyChanged();
                }
            }
        }
        public bool UseAnalytics
        {
            get => AnalyticsInfo.UseAnalytics;
            set
            {
                if (userSettingsStore != null)
                    userSettingsStore.SetBoolean(SettingsCollectionPath, "UseAnalytics", value);
                if (AnalyticsInfo.UseAnalytics != value)
                {
                    AnalyticsInfo.UseAnalytics = value;
                    if(AnalyticsInfo.UseAnalytics == false)
                    {
                        string msg = string.Format("Do you want to delete the logged info?");
                        string title = "Analytics";
                        MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                        DialogResult result = MessageBox.Show(msg, title, buttons);
                        if (result == DialogResult.Yes)
                        {
                            RemoteLogger.deleteAnalytics();
                        }
                    }
                    HandlePropertyChanged();
                }
            }
        }

        public bool UseHotReload
        {
            get => HotReloadInfo.UseHotReload;
            set
            {
                if (HotReloadInfo.UseHotReload != value)
                {
                    HotReloadInfo.UseHotReload = value;
                    HandlePropertyChanged();
                }
            }
        }

        public void HandlePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
