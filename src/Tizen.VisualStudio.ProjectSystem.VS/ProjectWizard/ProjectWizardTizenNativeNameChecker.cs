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

using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace Tizen.VisualStudio.ProjectWizard
{
    internal class ProjectWizardTizenNativeNameChecker : IWizard
    {
        private static readonly Dictionary<string, string> regexMap =
            new Dictionary<string, string>()
        {
            { "web", "a-zA-Z0-9" },
            { "native", "a-zA-Z0-9_-" },
            { "default", "a-zA-Z0-9_-" }
        };
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
            // check if "Place solution and project in same directory" check is selected

            if (string.Compare(replacementsDictionary["$destinationdirectory$"], replacementsDictionary["$solutiondirectory$"]) == 0)
            {
                MessageBox.Show("Can't create Tizen Web/Native project with \"Place solution and project in same directory\" CheckBox, Please uncheck the CheckBox and try again"
                    , "Tizen Project", MessageBoxButton.OK, MessageBoxImage.Error);
                Directory.Delete(replacementsDictionary["$solutiondirectory$"], true);
                throw new WizardBackoutException();
            }
            VsProjectHelper.Initialize();
            string tizenProjectType =
                replacementsDictionary.TryGetValue("$tizenprojecttype$", out tizenProjectType)
                ? tizenProjectType : "default";
            string prjName = replacementsDictionary["$projectname$"];
            if (string.IsNullOrWhiteSpace(prjName))
            {
                MessageBox.Show("Project name cannot be empty.");
                Directory.Delete(replacementsDictionary["$solutiondirectory$"], true);
                throw new WizardBackoutException("Project name cannot be empty.");
            }

            // Check if Project starts with something other than alphabet
            if (!char.IsLetter(prjName[0]))
            {
                MessageBox.Show("Project name must start with alphabet.");
                Directory.Delete(replacementsDictionary["$solutiondirectory$"], true);
                throw new WizardBackoutException("Project name must start with alphabet.");
            }

            // Check if Project name contains a-zA-Z0-9_-/a-zA-Z0-9 only
            // Handles unicode control char and surrogate char check
            string regexCompare = regexMap.ContainsKey(tizenProjectType) ?
                regexMap[tizenProjectType] : regexMap["default"];
            Regex objAlphaPattern = new Regex($@"^[{regexCompare}]*$");
            bool sts = objAlphaPattern.IsMatch(prjName);
            if (!sts)
            {
                MessageBox.Show($"Project name can only have [{regexCompare}]");
                Directory.Delete(replacementsDictionary["$solutiondirectory$"], true);
                throw new WizardBackoutException($"Project name can only have [{regexCompare}]");
            }

            // Check if Project name contain less than 3 chars
            if (prjName.Length < 3 || prjName.Length > 50)
            {
                MessageBox.Show("Project name length must be 3-50 chars.");
                Directory.Delete(replacementsDictionary["$solutiondirectory$"], true);
                throw new WizardBackoutException("Project name length must be 3-50 chars.");
            }

        }

        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }
    }
}
