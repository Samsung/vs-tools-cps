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
using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.VisualStudio.TemplateWizard;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Tizen.VisualStudio;

namespace Tizen.VisualStudio.ProjectWizard
{
    internal class ProjectWizardTizenCommon : IWizard
    {
        private Project projectTizen = null;
        private TizenManifestData tData = new TizenManifestData();

        public void ProjectFinishedGenerating(Project project)
        {
            this.projectTizen = project;
        }

        public void RunFinished()
        {
            VsProjectHelper prjHelperInstance = VsProjectHelper.GetInstance;
            Project prj = null;
            DTE dte = this.projectTizen.DTE;
            string projectName =
                Path.GetFileNameWithoutExtension(this.projectTizen.UniqueName);
            Property property = dte.Solution.Properties.Item("StartupProject");

            if (property != null)
            {
                property.Value = projectName;
            }

            if (!(String.IsNullOrEmpty(tData.Selected_project_name)) && tData.Selected_project_name != "Empty")
            {
                prj = prjHelperInstance.GetCurrentProjectFromName(tData.Selected_project_name);
                if (prj.Kind != prjHelperInstance.SharedProject)
                {
                    (projectTizen.Object as VSLangProj.VSProject)?.References.AddProject(prj);
                }
                else
                {
                    List<string> importPath_list = prjHelperInstance.GetImportList(prj);
                    foreach (string importPath in importPath_list)
                    {
                        string rel_importPath = prjHelperInstance.ChangeToRelativeFromFull(importPath, projectTizen.FullName);
                        prjHelperInstance.SetImportPath(rel_importPath, projectTizen);
                    }
                }
            }
        }

        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
        {
            if (replacementsDictionary["$ext_select_common$"] == "false")
            {
                try
                {
                    new DirectoryInfo(replacementsDictionary["$destinationdirectory$"]).Delete();
                }
                catch
                {
                }

                throw new WizardCancelledException();
            }

            if (replacementsDictionary["$ext_hasSharedLib$"] == "true")
            {
                replacementsDictionary.Add("$txui_safeprojectname$", ProjectWizardPortableUI.UIDictionary["$txui_safeprojectname$"]);
                replacementsDictionary.Add("$txui_guid1$", ProjectWizardPortableUI.UIDictionary["$txui_guid1$"]);
                replacementsDictionary["$comment_code$"] = "";
                replacementsDictionary["$loadapplication_code$"] = "LoadApplication(new App());";
            }
            else
            {
                tData.Selected_project_name = replacementsDictionary["$ext_hasSharedLib$"];
                replacementsDictionary["$comment_code$"] = "// Call 'LoadApplication(Application application)' here to load your application.";
                replacementsDictionary["$loadapplication_code$"] = "// e.g. LoadApplication(new App());";
            }

            if (tData.Selected_project_name == "Empty")
            {
                replacementsDictionary["$ext_hasSharedLib$"] = "false";
            }
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
