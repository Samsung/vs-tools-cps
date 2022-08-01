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
using System.Windows.Forms;

namespace Tizen.VisualStudio.ProjectWizard
{
    internal class ProjectWizardTizenProjectNameCheck : IWizard
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
            VsProjectHelper.Initialize();
            VsProjectHelper prjHelperInstance = VsProjectHelper.GetInstance;
            string prjName = replacementsDictionary["$projectname$"];

            if (prjName.Contains(" "))
            {
                MessageBox.Show("Please enter project name without space", "Wrong Project Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                EnvDTE80.DTE2 dte2 = VsProjectHelper.GetInstance.GetDTE2();
                if (replacementsDictionary["$exclusiveproject$"] == "True")
                {
                    Directory.Delete(replacementsDictionary["$solutiondirectory$"], true);
                }
                else
                {
                    Directory.Delete(replacementsDictionary["$destinationdirectory$"], true);
                }
                throw new WizardBackoutException("Please enter project name without space");
            }

        }

        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }
    }
}
