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
using EnvDTE80;
using Microsoft.VisualStudio.TemplateWizard;
using Tizen.VisualStudio.ProjectWizard.View;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using System;

namespace Tizen.VisualStudio.ProjectWizard
{
    internal class ProjectWizardTizenCrossPlatform : IWizard
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
            //Startup.StartupProject();
        }

        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
        {
            VsProjectHelper.Initialize();
            VsProjectHelper prjHelperInstance = VsProjectHelper.GetInstance;
            List<string> prjlist = new List<string>();
            Project prj = null;
            prjlist.Add("Empty");

            IEnumerator prjEnum = prjHelperInstance.GetProjects().GetEnumerator();
            while (prjEnum.MoveNext())
            {
                prj = (Project)prjEnum.Current;
                prjlist.Add(prj.Name);
            }

            ProjectWizardViewTizenCrossPlatform nWizard = new ProjectWizardViewTizenCrossPlatform(replacementsDictionary["$projectname$"], replacementsDictionary["$solutiondirectory$"],
                Trans_boolean(replacementsDictionary["$hasCommon$"]), Trans_boolean(replacementsDictionary["$hasMobile$"]), Trans_boolean(replacementsDictionary["$hasTV$"]),
                Trans_boolean(replacementsDictionary["$hasWearable$"]), Trans_boolean(replacementsDictionary["$hasSharedLib$"]), prjlist);
            if (nWizard.ShowDialog() == false)
            {
                EnvDTE80.DTE2 dte2 = VsProjectHelper.GetInstance.GetDTE2();
                if (replacementsDictionary["$exclusiveproject$"] == "True")
                {
                    Directory.Delete(replacementsDictionary["$solutiondirectory$"], true);
                    dte2.ExecuteCommand("File.NewProject");
                }
                else
                {
                    Directory.Delete(replacementsDictionary["$destinationdirectory$"], true);
                    dte2.ExecuteCommand("File.AddNewProject");
                }
                throw new WizardCancelledException();
            }
            else
            {
                replacementsDictionary["$select_common$"] = nWizard.manifestData.Select_common.ToString().ToLower();
                replacementsDictionary["$select_mobile$"] = nWizard.manifestData.Select_mobile.ToString().ToLower();
                replacementsDictionary["$select_tv$"] = nWizard.manifestData.Select_tv.ToString().ToLower();
                replacementsDictionary["$select_wearable$"] = nWizard.manifestData.Select_wearable.ToString().ToLower();
                if (nWizard.manifestData.Shared_library)
                {
                    replacementsDictionary["$hasSharedLib$"] = "true";
                }
                else
                {
                    replacementsDictionary["$hasSharedLib$"] = nWizard.manifestData.Selected_project_name;
                }
            }

            // Project Name change
            int idx = replacementsDictionary["$safeprojectname$"].IndexOf("Tizen");
            string prjName = replacementsDictionary["$safeprojectname$"];
            replacementsDictionary["$namespace$"] = prjName;
            replacementsDictionary["$lib_prjName$"] = prjName;
            if (idx != -1)// && Trans_boolean(replacementsDictionary["$hasSharedLib$"]))
            {
                if(replacementsDictionary["$hasSharedLib$"] != "true")
                    replacementsDictionary["$common_prjName$"] = prjName;
                else
                    replacementsDictionary["$common_prjName$"] = prjName + ".Tizen";
                replacementsDictionary["$mobile_prjName$"] = prjName + ".Mobile";
                replacementsDictionary["$tv_prjName$"] = prjName + ".TV";
                replacementsDictionary["$wearable_prjName$"] = prjName + ".Wearable";
            }
            else
            {
                replacementsDictionary["$common_prjName$"] = prjName + ".Tizen";
                replacementsDictionary["$mobile_prjName$"] = prjName + ".Tizen.Mobile";
                replacementsDictionary["$tv_prjName$"] = prjName + ".Tizen.TV";
                replacementsDictionary["$wearable_prjName$"] = prjName + ".Tizen.Wearable";
            }
        }

        private bool Trans_boolean(string input)
        {
            if (input == "true")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }
    }
}
