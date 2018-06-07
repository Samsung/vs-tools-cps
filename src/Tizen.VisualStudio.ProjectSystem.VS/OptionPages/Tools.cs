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

using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO;
using Tizen.VisualStudio.Tools.Data;

namespace Tizen.VisualStudio.OptionPages
{
    [System.ComponentModel.DesignerCategory("")]
    [Guid("3AB0D0BF-2C58-425F-A893-94C9801BAD12")]
    public class Tools : DialogPage
    {
        public ToolsInfo info;
        private ToolsControl control;

        public Tools() : base()
        {
            info = ToolsInfo.Instance();
        }

        #region Attributes to store to registry
        public string BaseToolsFolderPath
        {
            get { return info.ToolsFolderPath; }
            set { info.ToolsFolderPath = value; }
        }
        #endregion

        protected override IWin32Window Window
        {
            get
            {
                control = new ToolsControl();
                control.page = this;
                return control;
            }
        }

        protected override void OnApply(DialogPage.PageApplyEventArgs e)
        {
            control.UpdateData(true);
            base.OnApply(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }

        public static void Initialize(Package package)
        {
            var page = (Tools)package.GetDialogPage(typeof(Tools));

            ToolsInfo.Instance().OnToolsDirChanged += (toolsDirPath) =>
            {
                page.BaseToolsFolderPath = toolsDirPath;
                page.SaveSettingsToStorage();
            };
        }
    }
}
