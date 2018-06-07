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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using EnvDTE;
using NetCore.Profiler.Extension.Common;
using NetCore.Profiler.Extension.Options;
using Newtonsoft.Json.Linq;
using NetCore.Profiler.Extension.VSPackage;

namespace NetCore.Profiler.Extension.Launcher.Model
{
    public class ProfileSessionConfiguration : NotifyPropertyChanged
    {

        public int SleepTime { get; private set; }

        public string ProjectConfigurationName { get; private set; }

        public string ProjectPlatformName { get; private set; }

        public string ProjectOutputPath { get; private set; }

        public string TargetDll { get; private set; }

        public string AppId { get; private set; }

        public ProfilingSettings ProfilingSettings
        {
            get { return profilingSettings; }
            set { SetProperty(ref profilingSettings, value, false); }
        }

        private ProfilingSettings profilingSettings;

        public List<ProfilingPreset> ProfilingPresets { get; } = ProfilingPreset.PredefinedPresets;

        public ProfilingPreset ProfilingPreset
        {
            get { return profilingPreset; }
            set
            {
                SetProperty(ref profilingPreset, value, false);
                ProfilingSettings = profilingPreset.ProfilingSettings.Copy();
            }
        }

        private ProfilingPreset profilingPreset;

        /// <summary>
        /// Project name
        /// </summary>
        public string ProjectName => Project.Name;

        /// <summary>
        /// Project bin path
        /// </summary>
        public string ProjectHostBinPath { get; private set; }

        /// <summary>
        /// Project path on the host
        /// </summary>
        public string ProjectHostPath { get; private set; }

        /// <summary>
        /// Project destination on the target
        /// </summary>
        public string ProjectTargetPath { get; private set; }

        public string ProjectPackageVersion { get; private set; }

        public string ProjectPackageName { get; private set; }
        /// <summary>
        /// Application arguments
        /// </summary>
        public string Arguments { get; set; }


        private readonly Project Project;

        public ProfileSessionConfiguration(Project project, GeneralOptions go)
        {
            Project = project;
            SleepTime = go.SleepTime;
            Arguments = "";

            profilingSettings = new ProfilingSettings();
            profilingPreset = ProfilingPresets.FirstOrDefault();
            if (profilingPreset != null)
            {
                profilingSettings = profilingPreset.ProfilingSettings.Copy();
            }

            ReadProjectConfiguration();
            ProjectHostPath = Path.GetDirectoryName(Project.FullName);
            ProjectHostBinPath = Path.Combine(ProjectHostPath, ProjectOutputPath);
            ProjectTargetPath = "/opt/usr/home/owner";
            VsProjectHelper prjHelper = VsProjectHelper.Instance;
            ProjectPackageVersion = prjHelper.GetManifestVersion(project);
            ProjectPackageName = prjHelper.GetManifestPackage(project);
            TargetDll = ProjectTargetPath + "/apps_rw/" + prjHelper.GetManifestPackage(project) +
                "/bin/" + prjHelper.GetManifestAppExec(project);

            AppId = prjHelper.GetManifestApplicationId(project);

            ParseLaunchSettings();

        }

        private void ReadProjectConfiguration()
        {
            if (Project.ConfigurationManager != null)
            {
                Configuration config = Project.ConfigurationManager.ActiveConfiguration;
                ProjectConfigurationName = config.ConfigurationName;
                ProjectPlatformName = config.PlatformName;
                Properties props = config.Properties;
                Property prop = props.Item("OutputPath");
                if (prop != null)
                {
                    ProjectOutputPath = prop.Value.ToString();
                }
            }
        }

        private void ParseLaunchSettings()
        {
            string activeDebugProfile = "";

            string path = Path.GetFullPath(Path.Combine(ProjectHostPath, ProjectName + ".xproj.user"));
            if (!File.Exists(path))
            {
                return;
            }

            string props = File.ReadAllText(path);
            using (XmlReader reader = XmlReader.Create(new StringReader(props)))
            {
                reader.ReadToFollowing("ActiveDebugProfile");
                if (reader.NodeType != XmlNodeType.None)
                {
                    activeDebugProfile = reader.ReadElementContentAsString();
                }
            }

            path = Path.Combine(ProjectHostPath, "Properties", "launchSettings.json");
            if (!File.Exists(path))
            {
                return;
            }

            props = File.ReadAllText(path);
            JObject o = JObject.Parse(props);
            string args = o["profiles"][activeDebugProfile]["commandLineArgs"].ToString();

            Arguments = args;
        }

    }
}
