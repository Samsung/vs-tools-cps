/*
 * Copyright 2021(c) Samsung Electronics Co., Ltd  All Rights Reserved.
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
using System.Windows.Forms;

namespace Tizen.VisualStudio.OptionPages
{
    public partial class TidlControl : UserControl
    {
        internal Tidl page;

        public bool ViewProxy { get; set; }
        public bool ViewStub { get; set; }
        public bool ViewRpclib { get; set; }
        public string ViewLanguageOption { get; set; }
        public TidlControl()
        {
            InitializeComponent();
        }
        public void SavePage()
        {
            //Store page data
            page.Rpclib = ViewRpclib;
            page.Proxy = ViewProxy;
            page.Stub = ViewStub;
            page.LanguageOption = ViewLanguageOption;
            page.SaveSettingsToStorage();
        }

        public void LoadPage()
        {
            //Load page data
            rpcCheck.Checked = page.Rpclib;
            stubCheck.Checked = page.Stub;
            proxyCheck.Checked = page.Proxy;

            //reset checked states
            cRadio.Checked = false;
            cppRadio.Checked = false;
            csharpRadio.Checked = false;

            if (string.IsNullOrEmpty(page.LanguageOption))
            {
                csharpRadio.Checked = true;
            }
            else
            {
                switch (page.LanguageOption)
                {
                    case "C#":
                        csharpRadio.Checked = true;
                        break;
                    case "C":
                        cRadio.Checked = true;
                        break;
                    case "C++":
                        cppRadio.Checked = true;
                        break;
                    default:
                        break;
                }
            }
        }

        private void CheckChanged(object sender, EventArgs e)
        {
            CheckBox ch = (CheckBox)sender;
            switch (ch?.Name)
            {
                case "stubCheck":
                    ViewStub = ch.Checked;
                    break;
                case "proxyCheck":
                    ViewProxy = ch.Checked;
                    break;
                case "rpcCheck":
                    ViewRpclib = ch.Checked;
                    break;
                default:
                    break;
            }
        }

        private void RadioCheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = (RadioButton)sender;

            if (rb?.Checked == true)
            {
                ViewLanguageOption = rb.Text;
            }
        }
    }
}
