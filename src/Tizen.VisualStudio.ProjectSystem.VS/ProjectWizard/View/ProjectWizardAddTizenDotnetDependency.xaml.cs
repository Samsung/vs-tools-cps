/*
 * Copyright 2021 (c) Samsung Electronics Co., Ltd  All Rights Reserved.
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

using System.Windows;
using System.Windows.Controls;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using EnvDTE80;
using System.Collections.ObjectModel;
using System;
using Tizen.VisualStudio.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Tizen.VisualStudio.ProjectWizard.View
{
    public partial class ProjectWizardAddTizenDotnetDependency : System.Windows.Window
    {
        private readonly string workspacePath;
        private string selectedProj;
        private readonly List<string> CheckedProjList;
        private Project actProj;
        private readonly CheckCycle checker;
        private static readonly string[] templateNames = { ".Common", ".Mobile", ".Wearable", ".TV" };
        public ObservableCollection<BoolStringClass> UIAppList { get; set; }
        public ObservableCollection<BoolStringClass> NativeAppList { get; set; }
        public ObservableCollection<BoolStringClass> ServiceAppList { get; set; }
        public ObservableCollection<BoolStringClass> NonAppList { get; set; }
        public ProjectWizardAddTizenDotnetDependency(string dir)
        {
            InitializeComponent();
            workspacePath = dir;
            button_ok.IsEnabled = false;
            CheckedProjList = new List<string>();
            checker = new CheckCycle();
            ParseYaml();
            PopulateList();
        }

        public class BoolStringClass
        {
            public string ItemText { get; set; }
            public int ItemValue { get; set; }
            public bool Enabled { get; set; }
        }
        public bool IsDependencyAllowed(string actProjType, string appType)
        {
            //no limitation for test projects
            if (actProjType == "test_runner")
            {
                return true;
            }
            return true;

        }

        public void PopulateList()
        {
            UIAppList = new ObservableCollection<BoolStringClass>();
            ServiceAppList = new ObservableCollection<BoolStringClass>();
            NonAppList = new ObservableCollection<BoolStringClass>();
            NativeAppList = new ObservableCollection<BoolStringClass>();
            int i = 0, j = 0, k = 0, l = 0;
            VsProjectHelper projHelper = VsProjectHelper.GetInstance;

            DTE2 dte2 = Package.GetGlobalService(typeof(DTE)) as DTE2;
            Projects ListOfProjectsInSolution = dte2.Solution.Projects;
           
            foreach (Project proj in ListOfProjectsInSolution)
            {
                actProj = (dte2.ActiveSolutionProjects as Array).GetValue(0) as Project;
                if (proj != null && proj.Name != actProj.Name)
                {
                    bool existPath = checker.checkPath(actProj.Name, proj.Name);//checking for existing dependency
                    existPath = existPath || checker.checkCycle(proj.Name, actProj.Name);// checking for cycle
                    VsProjectHelper projHelp = VsProjectHelper.GetInstance;
                    
                    string actProjType = projHelp.GetTizenAppType(actProj);
                    string appType = projHelp.GetTizenAppType(proj);
                    string projectFolder = Path.GetDirectoryName(proj.FullName);
                    bool toEnable = !existPath && IsDependencyAllowed(actProjType, appType);
                    if (File.Exists(Path.Combine(projectFolder, "tizen_native_project.yaml"))
                        && (appType != "shared_lib" && appType != "static_lib"))
                    {
                        NativeAppList.Add(new BoolStringClass { ItemText = proj.Name, ItemValue = l++, Enabled = toEnable });
                        this.DataContext = this;
                        continue;
                    }
                    if (appType == "service-application")
                    {
                        ServiceAppList.Add(new BoolStringClass { ItemText = proj.Name, ItemValue = j++, Enabled = toEnable });
                        this.DataContext = this;
                        continue;
                    }
                    else if (appType == "ui-application")
                    {
                        UIAppList.Add(new BoolStringClass { ItemText = proj.Name, ItemValue = i++, Enabled = toEnable });
                        this.DataContext = this;
                        continue;
                    }
                    else if (appType == "others")
                    {
                        NonAppList.Add(new BoolStringClass { ItemText = proj.Name, ItemValue = k++, Enabled = toEnable });
                        this.DataContext = this;
                        continue;
                    }
                    else
                    {
                        Console.WriteLine("Invalid Category");
                    }
                    this.DataContext = this;
                }
            }
            if (NativeAppList.Count == 0)
            {
                label_native.Visibility = Visibility.Collapsed;
                nativeList.Visibility = Visibility.Collapsed;
            }
        }

        void ButtonOkClick(object sender, RoutedEventArgs e)
        {
            ProjectWizardAddTizenDotnetDependencyXaml.Close();

            var executor = new TzCmdExec();
            string message = null;
            string cmd = null;
            string prjName = null;
            bool isMultiTemplatePrj = templateNames.Any(actProj.Name.Contains);

            if (isMultiTemplatePrj)
                prjName = $@"{actProj.Name.Split('.')[0]}/{actProj.Name}";

            if (CheckedProjList != null)
            {
                foreach(string CheckedProj in CheckedProjList)
                {
                    if (isMultiTemplatePrj)
                    {
                        cmd = string.Format("/c tz add-deps \"{0}\" -d \"{1}\" -w \"{2}\"", prjName, CheckedProj, workspacePath);
                    }
                    else
                        cmd = string.Format("/c tz add-deps \"{0}\" -d \"{1}\" -w \"{2}\"", actProj.Name, CheckedProj, workspacePath);

                    message = executor.RunTzCmnd(cmd);
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        System.Windows.MessageBox.Show(message);
                        return;
                    }
                }
            }
        }

        private void ButtonCancelClick(object sender, RoutedEventArgs e) => this.Close();

        private void ItemChecked(object sender, RoutedEventArgs e)
        {
            CheckBox chkSelecttedItem = (CheckBox)sender;
            CheckedProjList.Add(chkSelecttedItem.Content.ToString());
            button_ok.IsEnabled = true;
        }

        private void ItemUnChecked(object sender, RoutedEventArgs e)
        {
            CheckBox chkSelecttedItem = (CheckBox)sender;
            CheckedProjList.Remove(chkSelecttedItem.Content.ToString());
            if (String.IsNullOrEmpty(selectedProj) && (CheckedProjList == null || CheckedProjList.Count == 0))
                button_ok.IsEnabled = false;
        }

        private void ParseYaml()
        {
            string yamlPath = Path.Combine(workspacePath, "tizen_workspace.yaml");
            if (File.Exists(yamlPath))
            {
                string[] arr = File.ReadAllLines(workspacePath + "\\tizen_workspace.yaml");
                int i;
                for (i = 0; i < arr.Length; i++)
                {
                    if (arr[i].Contains("projects:"))
                    {
                        i++;
                        break;
                    }
                }
                //Making adjanceny list
                int j;
                while (i < arr.Length)
                {
                    j = i + 1;
                    if (j >= arr.Length)
                        break;
                    while(arr[j].Contains("- ")) {
                        checker.addEdge(arr[i].Substring(2, arr[i].Length - 3), arr[j].Substring(4, arr[j].Length - 4));
                        j++;
                        if (j >= arr.Length)
                            break;
                    }
                    i = j;
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Yaml not found");
            }
        }
    }
}
