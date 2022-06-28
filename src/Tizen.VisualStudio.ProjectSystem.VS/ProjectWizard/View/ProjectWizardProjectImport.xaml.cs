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

using EnvDTE;
using EnvDTE80;
using System.Windows;
using System.Windows.Forms;
using System.IO;
using Microsoft.VisualStudio.Shell;
using Tizen.VisualStudio.Utilities;
using System;
using System.Linq;

namespace Tizen.VisualStudio.ProjectWizard.View
{
    public partial class ProjectWizardProjectImport : System.Windows.Window
    {
        private string[] project_list = { "Select an option", "native", "web" };
        private string[] profile_list = { "Select an option", "mobile", "wearable", "tv-samsung" };
        private string[] platform_list = { "Select an option", "7.0", "6.5", "6.0", "5.5" };
        private const string TizenDotnetProject = "dotnet";
        private const string TizenNativeProject = "native";
        private const string TizenWebProject = "web";

        public ProjectWizardProjectImport()
        {
            InitializeComponent();
            PopulateList();
            button_ok.IsEnabled = false;
        }
        private static void PrintLogs(string msg)
        {
            VsPackage.outputPaneTizen?.Activate();
            VsPackage.outputPaneTizen?.OutputStringThreadSafe(msg);
        }

        public void PopulateList()
        {
            profile_type.ItemsSource = profile_list;
            profile_type.SelectedIndex = 0;
            platform_ver.ItemsSource = platform_list;
            platform_ver.SelectedIndex = 0;
        }

        void OnDropDownClosed(object sender, EventArgs e)
        {
            if (profile_type.SelectedIndex != 0
                    && platform_ver.SelectedIndex != 0
                    && !string.IsNullOrWhiteSpace(Textbox_ProjPath.Text)
                    && !string.IsNullOrWhiteSpace(Textbox_Path.Text))
            {
                button_ok.IsEnabled = true;
            }
            else
            {
                button_ok.IsEnabled = false;
            }
        }

        private void ButtonCancelClick(object sender, RoutedEventArgs e) => this.Close();

        private bool isMultiProject(string projPath)
        {
            var projFiles = Directory.GetFiles(projPath, "*.csproj", SearchOption.AllDirectories);
            if (projFiles.Length > 1)
                return true;
            projFiles = Directory.GetFiles(projPath, "*.vcxproj", SearchOption.AllDirectories);
            if (projFiles.Length > 1)
                return true;

            return false;
        }

        private String getProjectType(string wrkspace)
        {
            String prjType = String.Empty;

            if (Directory.GetFiles(wrkspace, "tizen_native_project.yaml", SearchOption.AllDirectories).FirstOrDefault() != null)
                prjType = TizenNativeProject;
            else if (Directory.GetFiles(wrkspace, "tizen_web_project.yaml", SearchOption.AllDirectories).FirstOrDefault() != null)
                prjType = TizenWebProject;
            else if (Directory.GetFiles(wrkspace, "tizen_dotnet_project.yaml", SearchOption.AllDirectories).FirstOrDefault() != null)
                prjType = TizenDotnetProject;

            return prjType;
        }

        private void DeleteExtraFilesFromImport(string folder, string src)
        {
            // Delete the .project and .tproject files copied from imported TS Project
            File.Delete(Path.Combine(folder, ".project"));
            File.Delete(Path.Combine(folder, ".tproject"));

            if (src.Equals(TizenNativeProject))
            {
                // Delete the .cproject and .exportMap files copied from imported TS Native Project
                File.Delete(Path.Combine(folder, ".cproject"));
                File.Delete(Path.Combine(folder, ".exportMap"));
            }
        }

        private void ImportMultiProject(string projPath, string wrkspace, string projName, string prof)
        {
            string srcPrjType = string.Empty;
            VsProjectHelper.Initialize();
            VsProjectHelper vsProjectHelper = VsProjectHelper.GetInstance;
            var executor = new TzCmdExec();
            bool importInExsitingProject = true;

            var waitPopup = new WaitDialogUtil();
            waitPopup.ShowPopup("Creating Tizen Project",
                    "Please wait while the new project is being loaded...",
                    "Preparing...", "Tizen project load in progress...");

            if (!File.Exists(Path.Combine(wrkspace, "tizen_workspace.yaml")))
            {
                importInExsitingProject = false;
                //creating tizen workspace in path
                wrkspace  = Path.Combine(wrkspace, projName);
                if (!Directory.Exists(wrkspace))
                {
                    Directory.CreateDirectory(wrkspace);
                }
            }

            //copy proj in workspace
            if (!Directory.Exists(Path.Combine(wrkspace, projName)))
            {
                Directory.CreateDirectory(Path.Combine(wrkspace, projName));
            }

            string message = executor.RunTzCmnd(string.Format("/c Xcopy /E /I \"{0}\" \"{1}\"", projPath, Path.Combine(wrkspace, projName)));
            if (!string.IsNullOrWhiteSpace(message))
            {
                PrintLogs(message);
            }

            if (!importInExsitingProject)
                message = executor.RunTzCmnd(string.Format("/c tz import -u -w \"{0}\" -p {1}", wrkspace, prof));
            else
                message = executor.RunTzCmnd(string.Format("/c tz import -w \"{0}\" -p {1}", wrkspace, prof));

            if (!string.IsNullOrWhiteSpace(message))
            {
                PrintLogs(message);
                // Add error handling in case of command fail
            }

            srcPrjType = getProjectType(wrkspace);
            if (srcPrjType.Equals(TizenDotnetProject))
            {
                // Create and Add Build file in the imported Dotnet Project
                vsProjectHelper.createBuildFile(wrkspace, projName);
            }

            // Delete the extra files copied from imported TS Project
            // Get the list of the sub-project directories of the worksapce directory.
            var foundPrjctFiles = Directory.EnumerateFiles(wrkspace, "*.vcxproj", SearchOption.AllDirectories);

            foreach (var file in foundPrjctFiles)
            {
                DeleteExtraFilesFromImport(System.IO.Path.GetDirectoryName(file), srcPrjType);
                /*
                 * Update the "PlatformToolset" Tag of the .vcxproj file
                 * From "v142" to "v143" for VS2022
                 * Currently TZ creates the .vcxroj with "v142" PlatformToolset version.
                 */
                if (srcPrjType.Equals(TizenNativeProject))
                    vsProjectHelper.UpdatePlatformToolsetVersion(file, "v143");
            }

            string[] slns = Directory.GetFiles(wrkspace, "*.sln", SearchOption.AllDirectories);
            if (slns.Length == 0)
            {
                waitPopup.ClosePopup();
                System.Windows.MessageBox.Show("Unable to find solution file");
                return;
            }

            var dte = Package.GetGlobalService(typeof(_DTE)) as DTE2;
            dte.Solution.Open(slns[0]);

            // update additional directories
            if (vsProjectHelper.IsTizenNativeProject())
            {
                Projects ListOfProjectsInSolution = dte.Solution.Projects;
                if (ListOfProjectsInSolution != null)
                {
                    foreach (Project project in ListOfProjectsInSolution)
                    {
                        vsProjectHelper.ShowAdditionalIncludeDirectories(project);
                    }
                }
            }

            //add new proj to sln
            if (importInExsitingProject)
            {
                Projects ProjectsInSolution = dte?.Solution?.Projects;
                try
                {
                    string projectFolder = Path.Combine(wrkspace, projName);

                    var projFiles = Directory.EnumerateFiles(projectFolder, "*.csproj", SearchOption.AllDirectories)
                        .Union(Directory.EnumerateFiles(projectFolder, "*.vcxproj", SearchOption.AllDirectories));

                    foreach (EnvDTE.Project project in ProjectsInSolution)
                    {
                        foreach (string currentFile in projFiles)
                        {
                            var prjPath = currentFile.Replace("/", "\\");
                            if (!prjPath.Equals(project.FullName))
                                dte.Solution.AddFromFile(prjPath);

                            //update build for proj
                            vsProjectHelper.UpdateBuildForProject(Path.Combine(project.FileName, currentFile), false);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                //apply cert
                vsProjectHelper.setActiveCertificate(vsProjectHelper.getCertificateType());
            }

            waitPopup.ClosePopup();
        }

        private void ImportProject(string projPath, string wrkspace, string projName, string wrkspaceName, string prof)
        {
            string message;
            string srcPrjType = string.Empty;
            bool importInExsitingProject = true;
            var executor = new TzCmdExec();

            var waitPopup = new WaitDialogUtil();
            waitPopup.ShowPopup("Creating Tizen Project",
                    "Please wait while the new project is being loaded...",
                    "Preparing...", "Tizen project load in progress...");

            VsProjectHelper prjHelper = VsProjectHelper.GetInstance;

            if (!File.Exists(Path.Combine(wrkspace, "tizen_workspace.yaml")))
            {
                importInExsitingProject = false;
                //creating tizen workspace in path
                wrkspace = Path.Combine(wrkspace, projName);
                wrkspaceName = projName;
                if (!Directory.Exists(wrkspace))
                {
                    Directory.CreateDirectory(wrkspace);
                }
            }
            else // Importing into an Existing TZ workspace.
            {
                // Check if the Source TS project's profile and platform version being imported is matching with the target TZ Workspace.
                string src_platform_type = (string)profile_type.SelectedItem + "-" + (string)platform_ver.SelectedItem;
                string wrkspace_platform_type = prjHelper.getTag(wrkspace, "profile") + "-" + prjHelper.getTag(wrkspace, "api_version");

                if (!src_platform_type.Equals(wrkspace_platform_type))
                {
                    waitPopup.ClosePopup();
                    System.Windows.MessageBox.Show("Incompatible Project type being imported into the Workspace!");
                    return;
                }
            }

            //copy proj in workspace
            if (!Directory.Exists(Path.Combine(wrkspace, projName)))
            {
                Directory.CreateDirectory(Path.Combine(wrkspace, projName));
            }

            message = executor.RunTzCmnd(string.Format("/c Xcopy /E /I \"{0}\" \"{1}\"", projPath, Path.Combine(wrkspace, projName)));
            if (!string.IsNullOrWhiteSpace(message))
            {
                PrintLogs(message);
            }

            if (!importInExsitingProject)
                message = executor.RunTzCmnd(string.Format("/c tz import -u -w \"{0}\" -p {1}", wrkspace, prof));
            else
                message = executor.RunTzCmnd(string.Format("/c tz import -w \"{0}\" -p {1}", wrkspace, prof));

            if (!string.IsNullOrWhiteSpace(message))
            {
                PrintLogs(message);
                // Add error handling in case of command fail
            }

            srcPrjType = getProjectType(wrkspace);

            if (srcPrjType.Equals(TizenDotnetProject))
            {
                // Create and Add Build file in the imported Dotnet Project
                prjHelper.createBuildFile(wrkspace, projName);
            }

            DeleteExtraFilesFromImport(Path.Combine(wrkspace, projName), srcPrjType);

            if (!File.Exists(string.Format("{0}\\{1}.sln", wrkspace, wrkspaceName)))
            {
                waitPopup.ClosePopup();
                System.Windows.MessageBox.Show("Unable to find solution file");
                return;
            }

            /*
             * Update the "PlatformToolset" Tag of the .vcxproj file
             * From "v142" to "v143" for VS2022
             * Currently TZ creates the .vcxroj with "v142" PlatformToolset version.
             */
            if (srcPrjType.Equals(TizenNativeProject))
                prjHelper.UpdatePlatformToolsetVersion(wrkspace + "\\" + projName + "\\" + projName + ".vcxproj", "v143");

            var dte = Package.GetGlobalService(typeof(_DTE)) as DTE2;
            dte.Solution.Open(string.Format("{0}\\{1}.sln", wrkspace, wrkspaceName));

            // Expand the Opened Solution in VS Solution Explorer (as by default Solution Explorer shows collapsed view)
            ExpandInSolutionExplorer(projName, importInExsitingProject, dte);

            if (importInExsitingProject)
            {
                if (srcPrjType.Equals(TizenNativeProject))
                {
                    string[] projFiles = Directory.GetFiles(wrkspace + "\\" + projName, "*.vcxproj", SearchOption.AllDirectories);
                    dte.Solution.AddFromFile(projFiles[0]);
                    prjHelper.UpdateBuildForProject(Path.Combine(projName, projName + ".vcxproj"), false);
                    prjHelper.setActiveCertificate(prjHelper.getCertificateType());
                }
                else
                {
                    string[] projFiles = Directory.GetFiles(wrkspace + "\\" + projName, "*.csproj", SearchOption.AllDirectories);
                    dte.Solution.AddFromFile(projFiles[0]);
                    string prjFilePath = projFiles[0].Substring(wrkspace.Length+1);
                    prjHelper.UpdateBuildForProject(prjFilePath, false);
                    prjHelper.setActiveCertificate(prjHelper.getCertificateType());
                }
            }

            if (File.Exists(string.Format("{0}\\{1}\\config.xml", wrkspace, projName)))
            {
                dte.ItemOperations.OpenFile(wrkspace + "\\" + projName + "\\config.xml");
            }

            if (prjHelper.IsTizenNativeProject())
            {
                string[] projFiles = Directory.GetFiles(wrkspace + "\\" + projName, "*.vcxproj", SearchOption.AllDirectories);
                string targetProjectName = projFiles[0];
                Projects ListOfProjectsInSolution = dte.Solution.Projects;
                if (ListOfProjectsInSolution != null)
                {
                    foreach (Project project in ListOfProjectsInSolution)
                    {
                        if (project.FullName.Equals(targetProjectName))
                            prjHelper.ShowAdditionalIncludeDirectories(project);
                    }
                }
            }

            waitPopup.ClosePopup();
        }

        private void ButtonOkClick(object sender, RoutedEventArgs e)
        {
            string projPath = Textbox_ProjPath.Text;
            string wrkspace = Textbox_Path.Text;
            string projName = new DirectoryInfo(projPath).Name;
            string wrkspaceName = new DirectoryInfo(wrkspace).Name;
            string profile = profile_type.SelectedItem.ToString() + '-' +
                platform_ver.SelectedItem.ToString();

            DTE2 dte2 = Package.GetGlobalService(typeof(DTE)) as DTE2;
            if (!dte2.Solution.Saved)
            {
                dte2.Solution.SaveAs(dte2.Solution.FullName);
            }

            if (isMultiProject(projPath))
                ImportMultiProject(projPath, wrkspace, projName, profile);
            else
                ImportProject(projPath, wrkspace, projName, wrkspaceName, profile);

            ProjectWizardProjectImportXaml.Close();

        }

        private void ExpandInSolutionExplorer(string projName, bool importInExsitingProject, DTE2 dte)
        {
            if (!importInExsitingProject)
            {
                string solutionName = System.IO.Path.GetFileNameWithoutExtension(dte.Solution.FullName);
                dte.Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer).Activate();
                UIHierarchyItem root = ((DTE2)dte).ToolWindows.SolutionExplorer.GetItem(solutionName + @"\" + projName);
                if (root != null)
                {
                    root.Select(vsUISelectionType.vsUISelectionTypeSelect);
                    root.UIHierarchyItems.Expanded = true;
                }
            }
        }

        private void ProjPathBrowse(object sender, RoutedEventArgs e)
        {
            string folderPath = GetToolsFolderDialog(false);
            if (folderPath != string.Empty)
            {
                Textbox_ProjPath.Text = folderPath;

                if (!Directory.Exists(Path.Combine(Textbox_ProjPath.Text)))
                {
                    button_ok.IsEnabled = false;
                    System.Windows.MessageBox.Show("Project doesn't exist in path");
                    return;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(Textbox_Path.Text)
                        && (profile_type.SelectedIndex != 0)
                        && (platform_ver.SelectedIndex != 0))
                    {
                        button_ok.IsEnabled = true;
                    }
                }
            }
        }

        private void WrkspacePathBrowse(object sender, RoutedEventArgs e)
        {
            string folderPath = GetToolsFolderDialog(true);
            if (folderPath != string.Empty)
            {
                Textbox_Path.Text = folderPath;

                if (!Directory.Exists(Path.Combine(Textbox_Path.Text)))
                {
                    button_ok.IsEnabled = false;
                    System.Windows.MessageBox.Show("Tizen workspace path is not valid");
                    return;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(Textbox_ProjPath.Text)
                        && (profile_type.SelectedIndex != 0)
                        && (platform_ver.SelectedIndex != 0))
                    {
                        button_ok.IsEnabled = true;
                    }
                }
            }
        }

        private string GetToolsFolderDialog(Boolean newFolderOption)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
            {
                SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                                                            + "\\source\\repos",
                Description = "Select Path",
                ShowNewFolderButton = newFolderOption
            })
            {
                return (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) ?
                    folderBrowserDialog.SelectedPath : string.Empty;
            }
        }

        private void ProjPathReset(object sender, RoutedEventArgs e)
        {
            Textbox_ProjPath.Text = string.Empty;
            button_ok.IsEnabled = false;
        }

        private void WrkspacePathReset(object sender, RoutedEventArgs e)
        {
            Textbox_Path.Text = string.Empty;
            button_ok.IsEnabled = false;
        }
    }
}
