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
using System.Collections.Generic;

namespace Tizen.VisualStudio.ManifestEditor
{
    public interface IViewModelTizen
    {
        ItemsChoiceType AppType { get; set; }
        #region overview
        string ApplicationID { get; set; }
        string Package { get; set; }
        string Version { get; set; }
        string Label { get; set; }
        string Exec { get; set; }
        List<string> ApiVersionList { get; }
        string ApiVersion { get; set; }
        string Icon { get; set; }
        string Author { get; set; }
        string Email { get; set; }
        string Website { get; set; }
        string Description { get; set; }
        profile Profile { get; set; }
        #endregion

        #region privilege_feature
        List<feature> FeatureField { get; set; }
        privileges PrivilegeList { get; set; }
        List<appdefprivilege> AppdefprivilegeList { get; set; }
        List<string> DefaultprivilegeList { get; set; }
        List<appdefprivilege> ConsumerappdefprivilegeList { get; set; }
        List<string> IntegratedprivilegeList { get; }
        #endregion

        #region Locallization
        List<label> LocalizationLabels { get; set; }
        List<icon> LocalizationIcons { get; set; }
        List<description> LocalizationDescriptions { get; set; }
        #endregion

        #region Advanced
        List<metadata> AdvanceMetaList { get; set; }
        List<datacontrol> AdvanceDataControlList { get; set; }
        ManageTaskType TaskManage { get; set; }
        NoDisplayType NoDisplay { get; set; }
        NewHWaccelerationType HWAcceleration { get; set; }
        LaunchType LaunchMode { get; set; }
        AmbientType AmbientSupport { get; set; }
        NewDisplaySplashType NewDisplaySplash { get; set; }
        string UpdatePeriod { get; set; }

        AutorestartType Autorestart { get; set; }
        OnbootType Onboot { get; set; }
        List<appcontrol> AdvanceAppControlList { get; set; }
        List<shortcut> ShortcutList { get; set; }
        List<background> BackgroundCategoryList { get; set; }
        List<account> AccountField { get; set; }
        List<splashscreen> SplashscreenList { get; set; }
        List<packages> AdvancePkgList { get; set; }
        #endregion

        bool DesignerDirty { get; set; }
        event EventHandler ViewModelChanged;
        void DoIdle();
        void Close();
    }
}
