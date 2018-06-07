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
using Microsoft.VisualStudio.Shell;
using Tizen.VisualStudio.Tools.Data;
using Tizen.VisualStudio.InstallLauncher;

namespace Tizen.VisualStudio.ToolsOption
{
    public class TizenOptionPageViewModel : ToolsPathInfo
    {
        public string Notice;
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
            if (ToolsPathInfo.isDirty != true)
            {
                ToolsPathInfo.isDirty = false;
                base.OnClosed(e);
            }
            else
            {
                ToolsPathInfo.isDirty = false;
                this.SaveSettingsToStorage();
            }
        }

        public static void Initialize(Package package)
        {
            var page = (TizenOptionPageViewModel)package.GetDialogPage(typeof(TizenOptionPageViewModel));
            if (page != null)
            {
                ToolsPathInfo.ToolsRootPath = page.ToolsPath;
                InstallWizard.OnToolsDirChanged += page.OnUpdateByInstallWizard;
            }
        }

        private void OnUpdateByInstallWizard(string updatedPath)
        {
            ToolsPath = updatedPath;
            SaveSettingsToStorage();
        }
    }
}
