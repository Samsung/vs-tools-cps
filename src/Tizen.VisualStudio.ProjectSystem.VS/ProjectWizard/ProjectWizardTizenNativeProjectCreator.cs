/*
 * Copyright (c) 2021 Samsung Electronics Co., Ltd. All rights reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * Contributors:
 * - SRIB
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using Tizen.VisualStudio.ProjectWizard.View;
using Tizen.VisualStudio.Utilities;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System.Linq;
using Tizen.VisualStudio.Tools.Data;

namespace Tizen.VisualStudio.ProjectWizard
{
    internal class ProjectWizardTizenNativeProjectCreator : IWizard
    {
        public void BeforeOpeningFile(ProjectItem projectItem)
        {
        }

        public void ProjectFinishedGenerating(Project project)
        {
        }

        public void ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
        }

        public void RunFinished()
        {
        }

        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
        {
            //Delete the folder created by VS as TZ will create the same.
            Directory.Delete(replacementsDictionary["$destinationdirectory$"], true);
            VsProjectHelper.Initialize();
            VsProjectHelper prjHelperInstance = VsProjectHelper.GetInstance;
            IEnumerator prjEnum = prjHelperInstance.GetProjects().GetEnumerator();

            if (string.IsNullOrEmpty(ToolsPathInfo.ToolsRootPath))
            {
                _ = MessageBox.Show($"Tizen tools path not set.",
                    "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                Directory.Delete(replacementsDictionary["$solutiondirectory$"], true);
                throw new WizardCancelledException();
            }
            //Cancel if the template load async is in waiting state
            if (prjHelperInstance.TemplateTask.Status == System.Threading.Tasks.TaskStatus.WaitingForActivation)
            {
                prjHelperInstance.TemplateTaskSource.Cancel();
            }

            //load templates synchronously
            if (prjHelperInstance.TemplateTask.Status == System.Threading.Tasks.TaskStatus.Canceled
                || prjHelperInstance.TemplateListDictionary.Count == 0)
            {
                //wait dialog for sync loading of templates
                WaitDialogUtil waitPopup = new WaitDialogUtil();
                waitPopup.ShowPopup("Reading template data",
                    "Please wait while the new template data is being loaded...",
                    "Preparing...", "Tizen template data load in progress...");
                prjHelperInstance.LoadTemplates();
                waitPopup.ClosePopup();
            }
            if (prjHelperInstance.TemplateListDictionary.Count == 0)
            {
                //unable to load templates
                _ = MessageBox.Show("Could not load templates",
                    "Project Creation Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Directory.Delete(replacementsDictionary["$solutiondirectory$"], true);
                throw new WizardCancelledException();
            }

            string type = replacementsDictionary.TryGetValue("$tizenprojecttype$", out type)
                ? type : "native";

            string solDir = replacementsDictionary["$solutiondirectory$"];
            string prjName = replacementsDictionary["$projectname$"];
            solDir = solDir.Replace("\\", "/");

            //profiles and platforms should have unique entries
            HashSet<string> profiles = new HashSet<string>();
            HashSet<string> platforms = new HashSet<string>();
            List<string> profileList = prjHelperInstance.GetProfileList(type);
            var workspaceYAML = Path.Combine(solDir, "tizen_workspace.yaml");
            Boolean initProj = false;

            if (File.Exists(workspaceYAML))
            {
                _ = platforms.Add(prjHelperInstance.getPlatform(solDir + "\\tizen_workspace.yaml"));
                _ = profiles.Add(prjHelperInstance.getProfile(solDir + "\\tizen_workspace.yaml"));
            }
            else
            {
                initProj = true;
                foreach (string prof in profileList ?? Enumerable.Empty<string>())
                {
                    int hyphenPos = prof.LastIndexOf("-");
                    string prf = prof.Substring(0, hyphenPos);
                    _ = profiles.Add(prf);
                }
            }

            ProjectWizardViewProject nWizard = new ProjectWizardViewProject(type, prjName, solDir, profiles.ToList(), platforms.ToList());

            if (nWizard.ShowDialog() == false)
            {
                if(initProj)
                    Directory.Delete(replacementsDictionary["$solutiondirectory$"], true);
                throw new WizardCancelledException();
            }

            string template = nWizard.TemplateValue;
            string profile = nWizard.ProfileValue;

            var executor = new TzCmdExec();
            string message = "";
            if (initProj)
            {
                message = executor.RunTzCmnd(string.Format("/c tz init -w \"{0}\" -p \"{1}\"", replacementsDictionary["$solutiondirectory$"], profile));
                if (message.Contains("error:"))
                {
                    System.Windows.MessageBox.Show(message);
                    Directory.Delete(replacementsDictionary["$solutiondirectory$"], true);
                    throw new WizardCancelledException();
                }
            }
            message = executor.RunTzCmnd(string.Format("/c tz new -T native -t \"{0}\" -w \"{1}\" -p \"{2}\"", template, solDir, prjName));
            if (message.Contains("error:"))
            {
                MessageBox.Show(message, "Project Creation Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if(initProj)
                    Directory.Delete(replacementsDictionary["$solutiondirectory$"], true);
                throw new WizardCancelledException();
            }
            var dte = Package.GetGlobalService(typeof(_DTE)) as DTE2;
            string[] slns = System.IO.Directory.GetFiles(solDir, "*.sln");
            if (slns.Length == 0)
            {
                MessageBox.Show("No sln file found", "Project Creation Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if(initProj)
                    Directory.Delete(replacementsDictionary["$solutiondirectory$"], true);
                throw new WizardCancelledException();
            }

            string projPath = Path.Combine(solDir, prjName, prjName + ".vcxproj");
            projPath = projPath.Replace("/", "\\");

            /*
             * Update the "PlatformToolset" Tag of the .vcxproj file
             * From "v142" to "v143" for VS2022
             * Currently TZ creates the .vcxroj with "v142" PlatformToolset version.
             */
            prjHelperInstance.UpdatePlatformToolsetVersion(projPath, "v143");

            if (!initProj)
            {
                dte.Solution.AddFromFile(projPath);
                prjHelperInstance.UpdateBuildForProject(Path.Combine(prjName, prjName + ".vcxproj"), false);
            }
            else
            {
                dte.Solution.Open(slns[0]);
                prjHelperInstance.setActiveCertificate(prjHelperInstance.getCertificateType());
            }


            // update additional directories
            VsProjectHelper.Initialize();
            VsProjectHelper vsProjectHelper = VsProjectHelper.GetInstance;

            Projects ListOfProjectsInSolution = dte.Solution.Projects;
            if (ListOfProjectsInSolution != null)
            {
                projPath = projPath.Replace("/", "\\");
                foreach (Project project in ListOfProjectsInSolution)
                {
                    if (project.FullName.Equals(projPath))
                        vsProjectHelper.ShowAdditionalIncludeDirectories(project);
                }
            }

            throw new WizardCancelledException();
        }

        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }
    }
}
