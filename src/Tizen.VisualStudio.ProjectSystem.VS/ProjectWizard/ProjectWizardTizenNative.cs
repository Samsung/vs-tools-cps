/*
 * Copyright 2018 (c) Samsung Electronics Co., Ltd  All Rights Reserved.
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
using Microsoft.VisualStudio.TemplateWizard;
using System.Collections.Generic;
using System.IO;
using Tizen.VisualStudio.ProjectWizard.View;
using Tizen.VisualStudio.Tools.Data;
using Microsoft.Win32;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using Process = System.Diagnostics.Process;
using System.Text.RegularExpressions;
using System.Windows;

namespace Tizen.VisualStudio.ProjectWizard
{
    public class TizenNativeTemplate
    {
        public string profile;
        public string version;
        public string name;
    }

    internal class ProjectWizardTizenNative : IWizard
    {
        private const string kname
            = @"Software\Microsoft\VisualStudio\15.0\ApplicationPrivateSettings\Tizen\VisualStudio\ToolsOption\TizenOptionPageViewModel";

        private static readonly Regex templates = new Regex(@"(.*)-(\d+.\d+) +(.+)$");
        private static readonly Regex keywords = new Regex(@"(.+) = (.+)$");

        private Project projectTizen = null;
        List<TizenNativeTemplate> nativeTemplates;
        private string tpath;
        private string destinationDirectory;

        public void ProjectFinishedGenerating(Project project)
        {
            this.projectTizen = project;
        }

        public void RunFinished()
        {
            using (StreamWriter sw = new StreamWriter(Path.Combine(destinationDirectory, ".sdktools.props")))
            {
                sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"no\"?>");
                sw.WriteLine("<Project ToolsVersion=\"14.0\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">");
                sw.WriteLine("<PropertyGroup>");
                sw.WriteLine("    <MSBuildAllProjects>$(MSBuildAllProjects);$(MSBuildThisFileFullPath)</MSBuildAllProjects>");
                sw.WriteLine($"    <TizenSDKDir>{tpath}</TizenSDKDir>");
                sw.WriteLine("</PropertyGroup>");
                sw.WriteLine("</Project>");
            }

            // Remove wrong Build directory
            Directory.Delete(Path.Combine(destinationDirectory, "Build"), true);

            DTE dte = this.projectTizen.DTE;
            string configuration = dte.Solution.SolutionBuild.ActiveConfiguration.Name;
            string splatform = "";
            foreach (Project p in dte.Solution.Projects)
            {
                string platform = p.ConfigurationManager.ActiveConfiguration.PlatformName;
                switch (platform)
                {
                    case "ARM":
                    case "x86":
                    case "ARM64":  
                        splatform = platform;
                        break;
                    default:
                        continue;
                }
                break;
            }
            if (splatform == "")
                splatform = "x86";

            SolutionConfigurations scs = dte.Solution.SolutionBuild.SolutionConfigurations;
            foreach (SolutionConfiguration sc in scs)
            {
                foreach (SolutionContext c in sc.SolutionContexts)
                {
                    string pn = c.PlatformName;
                    string cn = c.ConfigurationName;
                    if (cn == configuration && pn == splatform)
                    {
                        sc.Activate();
                    }
                }
            }
        }

        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
        {
            // Get SDK path
            tpath = ToolsPathInfo.ToolsRootPath;
            if (string.IsNullOrEmpty(tpath))
            {
                RegistryKey kpath = Registry.CurrentUser.OpenSubKey(kname);
                tpath = kpath?.GetValue("ToolsPath") as string;
                if (!string.IsNullOrEmpty(tpath))
                    tpath = tpath.Substring(16); // Remove System.String prefix
            }

            if (string.IsNullOrEmpty(tpath)) {
                MessageBox.Show("Tizen SDK path is undefined");
                throw new WizardCancelledException();
            }

            PrepareListOfTemplates();

            if (nativeTemplates.Count <= 0)
            {
                MessageBox.Show("Tizen SDK template list is empty. Check toolchains.");
                throw new WizardCancelledException();
            }

            VsProjectHelper.Initialize();
            VsProjectHelper prjHelperInstance = VsProjectHelper.GetInstance;

            string solutionDirectory = replacementsDictionary["$solutiondirectory$"];
            destinationDirectory = replacementsDictionary["$destinationdirectory$"];

            ProjectWizardViewTizenNative nWizard = new ProjectWizardViewTizenNative(replacementsDictionary["$projectname$"],
                solutionDirectory, nativeTemplates);

            if (nWizard.ShowDialog() == false)
            {
                EnvDTE80.DTE2 dte2 = VsProjectHelper.GetInstance.GetDTE2();
                if (replacementsDictionary["$exclusiveproject$"] == "True")
                {
                    Directory.Delete(solutionDirectory, true);
                    dte2.ExecuteCommand("File.NewProject");
                }
                else
                {
                    Directory.Delete(destinationDirectory, true);
                    dte2.ExecuteCommand("File.AddNewProject");
                }
                throw new WizardCancelledException();
            }

            // Fix values
            replacementsDictionary["$tizen_profile$"] = nWizard.data.profile;
            replacementsDictionary["$tizen_toolset$"] = nWizard.data.toolset;
            replacementsDictionary["$tizen_api$"] = nWizard.data.tizenApi;
            replacementsDictionary["$tizen_project_type$"] = nWizard.data.projectType;
            replacementsDictionary["$tizen_template_name$"] = nWizard.data.projectType;
            string safename = replacementsDictionary["$safeprojectname$"];
            replacementsDictionary["$tizen_name$"] = safename.ToLower();
            replacementsDictionary["$tizen_output$"] = "lib" + replacementsDictionary["$tizen_name$"] + ".so";

            // At this moment project directory exists but empty, remove and regenerate it
            Directory.Delete(replacementsDictionary["$destinationdirectory$"]);
            Process process = getTizenBatProcess();
            process.StartInfo.Arguments = $"create native-project -n {safename} -p {nWizard.data.profile}-{nWizard.data.tizenApi} -t {nWizard.data.projectType}";
            process.StartInfo.WorkingDirectory = solutionDirectory;

            //debug
            //process.StartInfo.Arguments = " /A/K " + process.StartInfo.FileName + " " + process.StartInfo.Arguments;
            //process.StartInfo.FileName = "cmd.exe";
            //process.StartInfo.CreateNoWindow = false;
            //process.StartInfo.RedirectStandardInput= false;
            //process.StartInfo.RedirectStandardError = false;
            //process.StartInfo.RedirectStandardOutput = false;
            process.Start();
            process.WaitForExit();
            int code = process.ExitCode;
            process.Close();
            if (code != 0)
            {
                MessageBox.Show("Template generation fail for {safename}");
                throw new WizardCancelledException();
            }

            // Parse files
            using (StreamReader input = new StreamReader(Path.Combine(destinationDirectory, "project_def.prop")))
            {
                string line;
                while ((line = input.ReadLine()) != null)
                {
                    var match = keywords.Match(line);
                    if (match.Success)
                    {
                        string key = match.Groups[1].Value;
                        string value = match.Groups[2].Value;
                        switch (key)
                        {
                            case "APPNAME":
                                replacementsDictionary["$tizen_name$"] = value;
                                break;
                            case "type":
                                fixType(replacementsDictionary, value);
                                break;
                        }
                    }
                }
            }
        }

        private void fixType(Dictionary<string, string> replacementsDictionary, string value)
        {
            string name = replacementsDictionary["$tizen_name$"];
            replacementsDictionary["$tizen_project_type$"] = value;
            switch (value)
            {
                case "staticLib":
                    replacementsDictionary["$tizen_output$"] = $"lib{name}.a";
                    break;
                case "sharedLib":
                    replacementsDictionary["$tizen_output$"] = $"lib{name}.so";
                    break;
                case "app":
                    replacementsDictionary["$tizen_output$"] = name;
                    break;
            }
        }

        private Process getTizenBatProcess()
        {
            Process process = new Process();
            process.StartInfo.FileName = Path.Combine(tpath, @"tools\ide\bin\tizen.bat");
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            return process;
        }

        private void PrepareListOfTemplates()
        {
            // Start process to read info
            Process process = getTizenBatProcess();
            process.StartInfo.Arguments = "list native-project";
            process.Start();
            var input = process.StandardOutput;

            nativeTemplates = new List<TizenNativeTemplate>();

            string line;
            while ((line = input.ReadLine()) != null) {
                var match = templates.Match(line);
                if (match.Success)
                {
                    nativeTemplates.Add(new TizenNativeTemplate() {
                        profile = match.Groups[1].Value,
                        version = match.Groups[2].Value,
                        name = match.Groups[3].Value,
                    });
                }
            }
            process.Close();
        }

        #region for Item Template

        public void BeforeOpeningFile(ProjectItem projectItem)
        {

        }

        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }

        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
        }

        #endregion
    }
}
