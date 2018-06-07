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

using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.ProjectSystem.VS.Properties;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Tizen.VisualStudio.ProjectSystem.VS.PropertyPages
{
    [Export(typeof(IPageMetadata))]
    internal partial class TizenPropertyPageMetaData : IPageMetadata
    {
        string IPageMetadata.Name { get { return TizenPropertyPage.PageName; } }

        Guid IPageMetadata.PageGuid { get { return typeof(TizenPropertyPage).GUID; } }

        int IPageMetadata.PageOrder { get { return 30; } }

        bool IPageMetadata.HasConfigurationCondition { get { return false; } }
    }
    
    [Guid("4A5DAC64-29D2-4E90-9EE0-08CBA3ED6187")]
    [ExcludeFromCodeCoverage]
    internal partial class TizenPropertyPage : WpfBasedPropertyPage
    {
        internal static readonly string PageName = "Tizen";
       
        protected override string PropertyPageName
        {
            get
            {
                return PageName;
            }
        }
        protected override PropertyPageControl CreatePropertyPageControl()
        {
            return new TizenPropertyPageControl();
        }

        protected override PropertyPageViewModel CreatePropertyPageViewModel()
        {
            return new TizenPropertyPageViewModel();
        }
    }
}
