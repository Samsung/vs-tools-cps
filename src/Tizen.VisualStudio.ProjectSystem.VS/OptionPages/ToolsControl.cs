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
using System.Windows.Forms;
using Tizen.VisualStudio.Tools.Data;

namespace Tizen.VisualStudio.OptionPages
{
    /// <summary>
    /// Class ToolsControl
    /// </summary>
    public partial class ToolsControl : UserControl
    {
        internal Tools page;

        public ToolsControl()
        {
            InitializeComponent();

            this.Load += ToolsControl_Load;
        }

        private void ToolsControl_Load(object sender, EventArgs e)
        {
            if (page != null)
            {
                this.textToolsFolderPath.Text = page.BaseToolsFolderPath;
                RefreshDialogToolPath();
            }
        }

        private void BtnToolsFolderPath_Click(object sender, EventArgs e)
        {
            string folderPath = GetToolsFolderDialog(textToolsFolderPath.Text);
            if (folderPath != string.Empty)
            {
                textToolsFolderPath.Text = folderBrowserDialog.SelectedPath;
                this.page.info.ToolsFolderPath = textToolsFolderPath.Text;
                RefreshDialogToolPath();
            }
        }

        private void RefreshDialogToolPath()
        {
            //this.page.info.RefreshToolsPath();
            textEmulatorManagerPath.Text = this.page.info.EmulatorMgrPath;
            if (File.Exists(textEmulatorManagerPath.Text))
            {
                pictureEmulatorManagerPath.Image = Tizen.VisualStudio.Resources.StatusOK_16x;
            }
            else
            {
                pictureEmulatorManagerPath.Image = Tizen.VisualStudio.Resources.StatusInvalid_16x1;
            }

            textCertificateManagerPath.Text = this.page.info.CertificateMgrPath;
            if (File.Exists(textCertificateManagerPath.Text))
            {
                pictureCertificateManagerPath.Image = Tizen.VisualStudio.Resources.StatusOK_16x;
            }
            else
            {
                pictureCertificateManagerPath.Image = Tizen.VisualStudio.Resources.StatusInvalid_16x1;
            }

            textDeviceManagerPath.Text = this.page.info.DeviceMgrPath;
            if (File.Exists(textDeviceManagerPath.Text))
            {
                pictureDeviceManagerPath.Image = Tizen.VisualStudio.Resources.StatusOK_16x;
            }
            else
            {
                pictureDeviceManagerPath.Image = Tizen.VisualStudio.Resources.StatusInvalid_16x1;
            }

            textSDBCommandPromptPath.Text = this.page.info.SDBPath;
            if (File.Exists(textSDBCommandPromptPath.Text))
            {
                pictureSDBCommandPromptPath.Image = Tizen.VisualStudio.Resources.StatusOK_16x;
            }
            else
            {
                pictureSDBCommandPromptPath.Image = Tizen.VisualStudio.Resources.StatusInvalid_16x1;
            }
        }

        private string GetToolsFolderDialog(string toolsPath)
        {
            folderBrowserDialog.SelectedPath = toolsPath;
            folderBrowserDialog.Description = "Tizen Tools Path";
            folderBrowserDialog.ShowNewFolderButton = false;

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                return folderBrowserDialog.SelectedPath;
            }

            return string.Empty;
        }

        public void UpdateData(bool save)
        {
            if (save)
            {
                this.page.info.ToolsFolderPath = textToolsFolderPath.Text;
            }
            else
            {
                textToolsFolderPath.Text = this.page.info.ToolsFolderPath;
            }
        }

        private void Button_reset_Click(object sender, EventArgs e)
        {
            this.page.info.ToolsFolderPath = textToolsFolderPath.Text = string.Empty;
            RefreshDialogToolPath();
        }
    }
}
