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
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using Tizen.VisualStudio.Tools.Data;

namespace Tizen.VisualStudio.OptionPages
{

    /// <summary>
    /// User control Class part of CertificateControl
    /// </summary>
    public partial class CertificateControl : UserControl
    {
        internal Certificate page;

        private Certificate.CertificateType selectedCertType;

        public delegate void DgtUpdateData(bool save);

        private const string STR_noprofile = "<No Active Profile>";

        public CertificateControl()
        {
            InitializeComponent();

            this.comboBoxCertType.Items.Insert(0, "Use profile of Tizen Certificate Manager");
            this.comboBoxCertType.Items.Insert(1, "Direct registration");
            this.comboBoxCertType.SelectedIndex = 0;

            this.toolTipInfo.ToolTipTitle = "Info";
            this.toolTipInfo.SetToolTip(this.pbInfoSign,
                "Sign the package using the given values when the .TPK is created" +
                " otherwise signs with default certificate.");

            this.toolTipError.ToolTipTitle = "Error";
            this.toolTipError.SetToolTip(this.pbInvalidProfilePath, "Invalid Profile Path");
            this.toolTipError.SetToolTip(this.pbInvalidProfile, "Invalid Profile");
            this.toolTipError.SetToolTip(this.pbInvalidAuthorPath, "Invalid  Author Path");
            this.toolTipError.SetToolTip(this.pbInvalidDistPath, "Invalid Distributior Path");
        }

        public void UpdateData(bool save)
        {
            if (comboBoxProfile.InvokeRequired)
            {
                DgtUpdateData scpc = new DgtUpdateData(UpdateData);
                this.Invoke(scpc, new object[] { save });
            }
            else
            {
                if (save)
                {
                    this.page.OptionAuthorCertiFile = textAuthorPath.Text;
                    this.page.OptionAuthorCertiPass = textAuthorPass.Text.EncryptAes();
                    this.page.OptionDistributorCertiFile = textDistributorPath.Text;
                    this.page.OptionDistributorCertiPass = textDistributorPass.Text.EncryptAes();
                    this.page.OptionSelectedCertificateType = selectedCertType;
                }
                else // Load Data from certificate Dialog Page
                {
                    textAuthorPath.Text = this.page.OptionAuthorCertiFile;
                    textAuthorPass.Text = this.page.OptionAuthorCertiPass.DecryptAes();
                    textDistributorPath.Text = this.page.OptionDistributorCertiFile;
                    textDistributorPass.Text = this.page.OptionDistributorCertiPass.DecryptAes();

                    textProfilePath.Text = ToolsPathInfo.DefaultCertPath;

                    if (this.page.optionProfileList != null
                        && this.page.optionProfileList.Count > 0
                        && !string.IsNullOrEmpty(this.page.optionProfileSelected))
                    {
                        int index = this.page.optionProfileList.IndexOf(this.page.optionProfileSelected);
                        if (index != -1)
                        {
                            this.page.optionProfileList[index] = this.page.optionProfileSelected + " <Active>";
                        }

                        comboBoxProfile.Items.Clear();
                        comboBoxProfile.Items.AddRange(this.page.optionProfileList.ToArray());
                        comboBoxProfile.SelectedItem = this.page.optionProfileSelected + " <Active>";
                    }
                    else
                    {
                        comboBoxProfile.Items.Clear();
                        comboBoxProfile.Items.Insert(0, STR_noprofile);
                        comboBoxProfile.SelectedIndex = 0;
                    }

                    selectedCertType = this.page.OptionSelectedCertificateType;

                    if (selectedCertType == Certificate.CertificateType.Default)
                    {
                        this.checkBoxUserCert.Checked = false;
                    }
                    else if (selectedCertType == Certificate.CertificateType.Profile)
                    {
                        this.checkBoxUserCert.Checked = true;
                        this.comboBoxCertType.SelectedIndex = 0;
                    }
                    else if (selectedCertType == Certificate.CertificateType.Manual)
                    {
                        this.checkBoxUserCert.Checked = true;
                        this.comboBoxCertType.SelectedIndex = 1;
                    }
                    else
                    {
                        selectedCertType = Certificate.CertificateType.Default;
                        this.checkBoxUserCert.Checked = false;
                    }

                    EnableAvailableCertificateOption();
                    OptionValidate();
                }
            }
        }

        private void EnableAvailableCertificateOption()
        {
            if (this.checkBoxUserCert.Checked == true)
            {
                if (this.comboBoxCertType.SelectedIndex == 0) // Use Profile of Tizen Certificate Manager
                {
                    this.selectedCertType = Certificate.CertificateType.Profile;
                    this.comboBoxCertType.Enabled = true;
                    this.groupBoxProfile.Enabled = true;
                    this.groupBoxCertificate.Enabled = false;
                }
                else if (this.comboBoxCertType.SelectedIndex == 1) // Direct Register
                {
                    this.selectedCertType = Certificate.CertificateType.Manual;
                    this.comboBoxCertType.Enabled = true;
                    this.groupBoxProfile.Enabled = false;
                    this.groupBoxCertificate.Enabled = true;
                }
            }
            else
            {
                this.selectedCertType = Certificate.CertificateType.Default;
                // Disable All Option Group
                this.comboBoxCertType.Enabled = false;
                this.groupBoxProfile.Enabled = false;
                this.groupBoxCertificate.Enabled = false;
            }

            if (this.checkBoxShowAuthorpw.Checked == true)
            {
                this.textAuthorPass.UseSystemPasswordChar = false;
            }
            else
            {
                this.textAuthorPass.UseSystemPasswordChar = true;
            }

            if (this.checkBoxShowDistpw.Checked == true)
            {
                this.textDistributorPass.UseSystemPasswordChar = false;
            }
            else
            {
                this.textDistributorPass.UseSystemPasswordChar = true;
            }
        }

        private void OptionValidate()
        {
            if (this.groupBoxCertificate.Enabled == true)
            {
                if (File.Exists(this.textAuthorPath.Text))
                {
                    this.pbInvalidAuthorPath.Visible = false;
                }
                else
                {
                    this.pbInvalidAuthorPath.Visible = true;
                }

                if (File.Exists(this.textDistributorPath.Text))
                {
                    this.pbInvalidDistPath.Visible = false;
                }
                else
                {
                    this.pbInvalidDistPath.Visible = true;
                }
            }
            else
            {
                this.pbInvalidAuthorPath.Visible = false;
                this.pbInvalidDistPath.Visible = false;
            }
        }

        private string GetCertificateDialog(string filename)
        {
            openFileDialog.FileName = filename;
            openFileDialog.Filter = "Certificate Files (*.p12)|*.p12";
            openFileDialog.FilterIndex = 1;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                return openFileDialog.FileName;
            }

            return string.Empty;
        }

        private string GetCertificateProfileDialog(string filename)
        {
            if (filename == string.Empty)
            {
                openFileDialog.InitialDirectory = Path.GetDirectoryName(ToolsPathInfo.DefaultCertPath);
            }
            else
            {
                openFileDialog.InitialDirectory = Path.GetDirectoryName(filename);
                openFileDialog.FileName = Path.GetFileNameWithoutExtension(filename);
            }

            openFileDialog.Filter = "Certificate Profile Files (*.xml)|*.xml";
            openFileDialog.FilterIndex = 1;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                return openFileDialog.FileName;
            }

            return string.Empty;
        }

        #region Control Event Handling

        private void BtnAuthorPath_Click(object sender, EventArgs e)
        {
            string filename = GetCertificateDialog(textAuthorPath.Text);
            if (filename != string.Empty)
            {
                textAuthorPath.Text = openFileDialog.FileName;
                textAuthorPath.Select();
            }
        }

        private void BtnDistributorPath_Click(object sender, EventArgs e)
        {
            string filename = GetCertificateDialog(textDistributorPath.Text);
            if (filename != string.Empty)
            {
                textDistributorPath.Text = openFileDialog.FileName;
                textDistributorPath.Select();
            }
        }

        private void BtnProfilePath_Click(object sender, EventArgs e)
        {
            string filename = GetCertificateProfileDialog(textProfilePath.Text);
            if (filename != string.Empty)
            {
                textProfilePath.Text = openFileDialog.FileName;
                textProfilePath.Select();
            }
        }

        private void ComboBoxCertType_SelectedIndexChanged(object sender, EventArgs e)
        {
            EnableAvailableCertificateOption();
            OptionValidate();
        }

        private void CheckBoxUserCert_CheckedChanged(object sender, EventArgs e)
        {
            EnableAvailableCertificateOption();
            OptionValidate();
        }

        private void CheckBoxShowAuthorpw_CheckedChanged(object sender, EventArgs e)
        {
            EnableAvailableCertificateOption();
            OptionValidate();
        }

        private void CheckBoxShowDistpw_CheckedChanged(object sender, EventArgs e)
        {
            EnableAvailableCertificateOption();
            OptionValidate();
        }

        private void TextAuthorPath_TextChanged(object sender, EventArgs e)
        {
            OptionValidate();
        }

        private void TextAuthorPass_TextChanged(object sender, EventArgs e)
        {
            OptionValidate();
        }

        private void TextDistributorPath_TextChanged(object sender, EventArgs e)
        {
            OptionValidate();
        }

        private void TextDistributorPass_TextChanged(object sender, EventArgs e)
        {
            OptionValidate();
        }

        private void ComboBoxProfile_SelectedIndexChanged(object sender, EventArgs e)
        {
            /// Sign .TPK using Active Profile only.
            this.comboBoxProfile.SelectedItem = this.page.optionProfileSelected + " <Active>";
        }
        #endregion
    }
}
