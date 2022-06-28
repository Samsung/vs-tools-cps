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
using System.ComponentModel;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Tizen.VisualStudio.Tools.Data;

namespace Tizen.VisualStudio.OptionPages
{
    internal class Tidl : DialogPage
    {
        private TidlControl tidlControl;

        #region Attributes to store to registry
        public bool Proxy
        {
            get => TidlInfo.ProxyVal;
            set => TidlInfo.ProxyVal = value;
        }
        public bool Stub
        {
            get => TidlInfo.StubVal;
            set => TidlInfo.StubVal = value;
        }
        public bool Rpclib
        {
            get => TidlInfo.RpcVal;
            set => TidlInfo.RpcVal = value;
        }

        public string LanguageOption
        {
            get => TidlInfo.LanguageOption;
            set => TidlInfo.LanguageOption = value;
        }
        #endregion
        protected override IWin32Window Window
        {
            get
            {
                tidlControl = new TidlControl
                {
                    page = this
                };

                return tidlControl;
            }
        }

        protected override void OnActivate(CancelEventArgs e)
        {
            base.OnActivate(e);
            tidlControl.LoadPage();
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            tidlControl.SavePage();
            base.OnApply(e);
        }

        public static void Initialize(Package package)
        {
            Tidl page = (Tidl)package.GetDialogPage(typeof(Tidl));
            page.LoadSettingsFromStorage();
        }
    }
}
