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
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Tizen.VisualStudio.Tools.Data;
using System.Collections.Generic;

namespace Tizen.VisualStudio.OptionPages
{
    [System.ComponentModel.DesignerCategory("")]
    [Guid("D34CA4BC-2E0B-459D-9F38-E0E5436D5F99")]
    public class Certificate : DialogPage
    {
        static private Package package;

        static private Certificate page;

        public enum CertificateType
        {
            Default = 0,
            Profile,
            Manual
        };

        public CertificateInfo info = null; // for register & signing

        public string optionProfileSelected = "";

        public List<string> optionProfileList;

       // public ToolsInfo toolsInfo = ToolsInfo.Instance();

        private CertificateControl control;

        private CertificateType optionSelectedCertificateType = CertificateType.Default;
        private CertificateInfo infoProfile;
        private CertificateInfo infoManual;

        public Certificate() : base()
        {
            infoManual = new CertificateInfo();
            infoProfile = new CertificateInfo();

            LoadInfoProfile();
        }

        #region Attributes to store to registry

        public string OptionAuthorCertiFile
        {
            get { return infoManual.AuthorCertificateFile; }
            set { infoManual.AuthorCertificateFile = value; }
        }

        public string OptionAuthorCertiPass
        {
            get { return infoManual.AuthorPassword; }
            set { infoManual.AuthorPassword = value; }
        }

        public string OptionDistributorCertiFile
        {
            get { return infoManual.DistributorCertificateFile; }
            set { infoManual.DistributorCertificateFile = value; }
        }

        public string OptionDistributorCertiPass
        {
            get { return infoManual.DistributorPassword; }
            set { infoManual.DistributorPassword = value; }
        }

        public CertificateType OptionSelectedCertificateType
        {
            get { return optionSelectedCertificateType; }
            set
            {
                optionSelectedCertificateType = value;
                SetCertificateType(optionSelectedCertificateType);
            }
        }
        #endregion

        protected override IWin32Window Window
        {
            get
            {
                control = new CertificateControl();
                control.Location = new System.Drawing.Point(0, 0);
                control.page = this;
                return control;
            }
        }

        protected override void OnActivate(CancelEventArgs e)
        {
            // setting default values when OptionFirstOpen is true in this function
            // does not work correctly. some values are written to the registry
            // with empty value.
            // so set the default values in the constructor as above.
            LoadInfoProfile();
            control.UpdateData(false);
            base.OnActivate(e);
        }

        protected override void OnApply(DialogPage.PageApplyEventArgs e)
        {
            control.UpdateData(true);
            base.OnApply(e);
        }

        public void SetCertificateType(CertificateType type)
        {
            switch (type)
            {
                case CertificateType.Default:
                    info = null;
                    break;
                case CertificateType.Manual:
                    info = infoManual;
                    break;
                case CertificateType.Profile:
                    info = infoProfile;
                    break;
            }
        }

        public void LoadInfoProfile()
        {
            if (File.Exists(ToolsPathInfo.DefaultCertPath))
            {
                CertificateProfilesManager.RegisterProfileFile(ToolsPathInfo.DefaultCertPath);

                CertificateProfilesManager.ProfilesChanged += delegate(object sender, CertificateProfileChangedEventArgs e)
                {
                    UpdateInfoProfile();
                    control.UpdateData(false);
                };
            }

            UpdateInfoProfile();
        }

        public void UpdateInfoProfile()
        {
            this.optionProfileList = CertificateProfilesManager.GetProfileNameList();
            this.optionProfileSelected = CertificateProfilesManager.GetActiveProfileName();

            CertificateProfileInfo cpinfo = CertificateProfilesManager.GetProfileInfo(this.optionProfileSelected);
            if (cpinfo != null)
            { // Get Selected Profile info

                this.infoProfile.SetCertificateInfo(
                        cpinfo.profileItemDic["0"].keyPath,
                        cpinfo.profileItemDic["0"].keyPassword.DecryptDes().EncryptAes(),
                        cpinfo.profileItemDic["1"].keyPath,
                        cpinfo.profileItemDic["1"].keyPassword.DecryptDes().EncryptAes());
            }
            else
            {
                this.infoProfile.SetCertificateInfo("", "", "", "");
            }
        }

        public static void Initialize(Package package)
        {
            Certificate.package = package;
            page = (Certificate)package.GetDialogPage(typeof(Certificate));
        }

        public static CertificateInfo CheckValidCertificate()
        {
            CertificateInfo info = page?.info;
            if (info == null ||
                !File.Exists(info.AuthorCertificateFile)
                || !File.Exists(info.DistributorCertificateFile)
                || info.AuthorPassword.Length == 0
                || info.DistributorPassword.Length == 0)
            {

                return null;
            }

            return info;
        }
    }
}
