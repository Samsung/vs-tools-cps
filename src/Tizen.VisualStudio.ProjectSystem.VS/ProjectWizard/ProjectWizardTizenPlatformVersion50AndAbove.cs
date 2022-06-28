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
    internal class ProjectWizardTizenPlatformVersion50AndAbove : IWizard
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
            Dictionary<string, string> prjlist = new Dictionary<string, string>();

            prjlist.Add("5.0", "tizen50");
            prjlist.Add("5.5", "tizen60");
            prjlist.Add("6.0", "tizen80");
            prjlist.Add("6.5", "tizen90");
            prjlist.Add("7.0", "tizen10.0");

            Dictionary<string, string> PlatformVersion = new Dictionary<string, string>();
            PlatformVersion.Add("5.0", "5");
            PlatformVersion.Add("5.5", "5.5");
            PlatformVersion.Add("6.0", "6");
            PlatformVersion.Add("6.5", "6.5");
            PlatformVersion.Add("7.0", "7.0");

            IEnumerator prjEnum = prjHelperInstance.GetProjects().GetEnumerator();

            List<string> platformVersionList = new List<string>() { "5.0","5.5", "6.0", "6.5", "7.0" };

            ProjectWizardViewTizenPlatformVersion nWizard = new ProjectWizardViewTizenPlatformVersion(platformVersionList);
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

            replacementsDictionary.Add("$platformversion$", PlatformVersion[nWizard.platformVersion]);
            replacementsDictionary.Add("$targetframwork$", prjlist[nWizard.platformVersion]);
            if(nWizard.platformVersion == "7.0")
                replacementsDictionary.Add("$apiversion$", "api-version=\"10\"");
            else if(nWizard.platformVersion == "6.5")
                replacementsDictionary.Add("$apiversion$", "api-version=\"9\"");
            else if(nWizard.platformVersion == "5.5" || nWizard.platformVersion == "6.0")
                replacementsDictionary.Add("$apiversion$", "api-version=\"6\"");
            else
                replacementsDictionary.Add("$apiversion$", "");
            // Project Name change

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
