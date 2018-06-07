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


using EnvDTE;

namespace Tizen.VisualStudio.ProjectWizard
{
    internal class TizenManifestData
    {
        public string ProjectName { get; set; }
        public string ProjectPath { get; set; }
        public string PackageName { get; set; }
        public string ApiVersionName { get; set; }
        public string ProfileName { get; set; }

        public bool Select_common;
        public bool Select_mobile;
        public bool Select_wearable;
        public bool Select_tv;
        public bool Shared_library;

        public string Selected_project_name;

        public TizenManifestData()
        {
            this.PackageName = null;
            this.ProfileName = null;
            this.ApiVersionName = null;
            this.ProfileName = null;
            this.Selected_project_name = null;
        }

        public string PlatformName
        {
            get
            {
                return ProfileName + "-" + ApiVersionName;
            }
        }
    }
}

