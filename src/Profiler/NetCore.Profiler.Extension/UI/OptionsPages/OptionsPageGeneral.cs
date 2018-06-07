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

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using NetCore.Profiler.Extension.VSPackage;

namespace NetCore.Profiler.Extension.UI.OptionsPages
{
    [Guid(GuidPageGeneral)]
    class OptionsPageGeneral : UIElementDialogPage
    {
        internal const string GuidPageGeneral = "a40b4057-cc2e-4046-b358-4d88bfb3f417";

        OptionsPageGeneralControl _optionsPageGeneralControl;

        protected override UIElement Child => _optionsPageGeneralControl ?? (_optionsPageGeneralControl = new OptionsPageGeneralControl());

        protected override void OnActivate(CancelEventArgs e)
        {
            base.OnActivate(e);
            ProfilerPlugin.Instance.GeneralOptions.LoadSettings();
        }

        protected override void OnApply(PageApplyEventArgs args)
        {
            if (args.ApplyBehavior == ApplyKind.Apply)
            {
                ProfilerPlugin.Instance.GeneralOptions.SaveSettings();
            }
            else
            {
                ProfilerPlugin.Instance.GeneralOptions.LoadSettings();
            }

            base.OnApply(args);
        }
    }
}
