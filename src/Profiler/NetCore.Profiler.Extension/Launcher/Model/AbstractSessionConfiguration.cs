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

using System.IO;
using EnvDTE;
using NetCore.Profiler.Extension.Common;
using NetCore.Profiler.Extension.VSPackage;

namespace NetCore.Profiler.Extension.Launcher.Model
{
    public class AbstractSessionConfiguration : NotifyPropertyChanged
    {
        protected readonly Project Project;

        /// <summary>
        /// Project path on the host
        /// </summary>
        public string ProjectHostPath { get; private set; }

        /// <summary>
        /// Project bin path
        /// </summary>
        public string ProjectHostBinPath { get; protected set; }

        public string ProjectPackageVersion { get; protected set; }

        public string ProjectPackageName { get; protected set; }

        public string ProjectOutputPath { get; private set; }

        public string AppId => VsProjectHelper.Instance.GetManifestApplicationId(Project);

        public AbstractSessionConfiguration(Project project)
        {
            Project = project;
            ProjectHostPath = Path.GetDirectoryName(Project.FullName);

            VsProjectHelper prjHelper = VsProjectHelper.Instance;
            ProjectPackageVersion = prjHelper.GetManifestVersion(project);
            ProjectPackageName = prjHelper.GetManifestPackage(project);
        }

        protected void SetOutputPath(Configuration config)
        {
            Properties props = config.Properties;
            Property prop = props.Item("OutputPath");
            if (prop != null)
            {
                ProjectOutputPath = prop.Value.ToString();
            }
        }

        public string GetTpkPath()
        {
            return Path.Combine(ProjectHostBinPath, ProjectPackageName + "-" + ProjectPackageVersion + ".tpk");
        }
    }
}
