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

using System;
using System.IO;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.ComponentModelHost;
using EnvDTE;
using EnvDTE80;
using System.Xml;
using VSLangProj;
using NuGet.VisualStudio;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Microsoft.Build.Construction;
using System.Xml.Linq;
using Tizen.VisualStudio.Tools.Data;
using Tizen.VisualStudio.TizenYamlParser;
using Newtonsoft.Json;
using Tizen.VisualStudio.Utilities;
using System.Threading;
using Tizen.VisualStudio.OptionPages;

namespace Tizen.VisualStudio
{
    public class VsProjectHelper
    {
        private static VsProjectHelper instance = new VsProjectHelper();
        private static readonly string[] projectsArr = { "dotnet", "native", "web" };
        private DTE2 dte2 = null;
        private Solution2 sol2 = null;
        private ServiceProvider serviceProvider = null;
        private IVsSolution solutionService = null;
        public Dictionary<string, Dictionary<string, List<string>>> TemplateListDictionary { get; private set; }

        //public readonly string PCLProjGuid = "786C830F-07A1-408B-BD7F-6EE04809D6DB";
        public readonly string NETStandardProject = "9A19103F-16F7-4668-BE54-9A1E7A4F7556";
        public readonly string SharedProject = "{D954291E-2A0B-460D-934E-DC6B0785DB48}";
        public readonly string TizenProjGuid = "2F98DAC9-6F16-457B-AED7-D43CAC379341";
        public readonly string TizenWebProjGuid = "{8E00536E-BD0D-4447-B307-F2C80A762AD0}";
        public readonly string CPPBasedProject = "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}";
        public readonly string TizenProjCfgName = "ProjectCommonFlavorCfg";

        private readonly List<string> appNodeNameCandidates = new List<string>() { "tizen:application", "ui-application", "service-application", "widget-application", "watch-application", "component-based-application" };

        static public VsProjectHelper GetInstance
        {
            get
            {
                VsProjectHelper.instance.sol2 =
                    (Solution2)VsProjectHelper.instance.dte2.Solution;
                return VsProjectHelper.instance;
            }
        }

        public System.Threading.Tasks.Task TemplateTask { get; internal set; }
        public CancellationTokenSource TemplateTaskSource { get; internal set; }

        static public void Initialize()
        {
            if (VsProjectHelper.instance == null)
            {
                VsProjectHelper.instance = new VsProjectHelper();
            }
        }

        private VsProjectHelper()
        {
            this.dte2 = Package.GetGlobalService(typeof(SDTE)) as DTE2;
            this.sol2 = (Solution2)this.dte2.Solution;
            this.serviceProvider = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)this.dte2);
            this.solutionService = (IVsSolution)serviceProvider.GetService(typeof(IVsSolution));
        }

        internal IVsSolution GetSolutionService()
        {
            return this.solutionService;
        }

        internal IVsHierarchy GetHierachyFromProjectUniqueName(string uniqueName)
        {
            IVsHierarchy hierarchy;
            solutionService.GetProjectOfUniqueName(uniqueName, out hierarchy);
            return hierarchy;
        }

        private IVsHierarchy GetIVsHierarchyObject(Project project)
        {
            string uniqueName = project?.UniqueName;
            int ret = solutionService.GetProjectOfUniqueName(uniqueName, out IVsHierarchy hierarchyProject);
            if (ret != VSConstants.S_OK) return null;

            return hierarchyProject;
        }


        public string GetProjProperty(Project project, string name,uint storage)
        {
            IVsHierarchy hierarchyProject = GetIVsHierarchyObject(project);
            if (hierarchyProject is IVsBuildPropertyStorage buildPropertyStorage)
            {
                var result = buildPropertyStorage.GetPropertyValue(name, "", storage, out string value);
                if (result == VSConstants.S_OK)
                {
                    return value;
                }
            }

            return null;
        }

        public bool RemoveProjProperty(Project project, string name, uint storage)
        {
            IVsHierarchy hierarchyProject = GetIVsHierarchyObject(project);
            if (hierarchyProject is IVsBuildPropertyStorage buildPropertyStorage)
            {
                var result = buildPropertyStorage.RemoveProperty(name, "", storage);
                if (result == VSConstants.S_OK)
                {
                    return true;
                }
            }

            return false;
        }


        internal bool IsCapabilityMatch(string uniqueName, string capability)
        {
            IVsHierarchy hierarchy = GetHierachyFromProjectUniqueName(uniqueName);
            bool match = hierarchy.IsCapabilityMatch(capability);
            return match;
        }

        public bool ExistChildNode(XmlNode node, string childNodeName)
        {
            bool result = false;
            if (node == null)
            {
                return result;
            }

            XmlNodeList childNodeLists = node.ChildNodes;
            foreach (XmlNode child in childNodeLists)
            {
                if (child.Name == childNodeName)
                {
                    return true;
                }
            }

            return result;
        }

        public bool GetExcludeXamarinFormsProperty(Project project, string solutionConfig)
        {
            // From Tizen .NET M1 version, xamarin.forms dll should be included by default
            bool excludeXamarinFormsProperty = true; // PropertyUtil.GetBoolProperty(project, TizenProjGuid, TizenProjCfgName, "excludeXamarinForms");

            return excludeXamarinFormsProperty;
        }

        public bool GetSCDProperty(Project project, string solutionConfig, out string architecture)
        {
            bool scdProperty = PropertyUtil.GetBoolProperty(project, TizenProjGuid, TizenProjCfgName, "chkSCDProperty");
            architecture = PropertyUtil.GetStringProperty(project, TizenProjGuid, TizenProjCfgName, "scdArchitecture");

            return scdProperty;
        }

        public string GetExtraArguments(Project project)
        {

            if (IsUseExtraArguments(project))
            {
                string ExtraArguments = " " + PropertyUtil.GetStringProperty(project, TizenProjGuid, TizenProjCfgName, "txtExtraArgs");
                return ExtraArguments;
            }
            else
            {
                return string.Empty;
            }

        }

        private bool IsUseExtraArguments(Project project)
        {
            bool IsUseExtraArgs = PropertyUtil.GetBoolProperty(project, TizenProjGuid, TizenProjCfgName, "IsUseExtraArguments");

            return IsUseExtraArgs;
        }

        public Project GetCurrentProjectFromUniqueName(string projectUniqueName)
        {
            //Projects projects = this.GetProjects();
            List<Project> projects = GetSolutionFolderProjects();
            foreach (Project project in projects)
            {
                if (String.Equals(project.UniqueName, projectUniqueName))
                {
                    return project;
                }
            }

            return null;
        }

        public Project GetCurrentProjectFromName(string projectName)
        {
            //Projects projects = this.GetProjects();
            List<Project> projects = GetSolutionFolderProjects();
            foreach (Project project in projects)
            {
                if (String.Equals(project.Name, projectName))
                {
                    return project;
                }
            }

            return null;
        }

        public List<Project> GetSolutionFolderProjects()
        {
            List<Project> projects = new List<Project>();
            Projects solutionProjects = this.GetProjects();
            foreach (Project project in solutionProjects)
            {
                if (project.Kind == /*ProjectKinds.vsProjectKindSolutionFolder*/"{66A26720-8FB5-11D2-AA7E-00C04F688DDE}")
                {
                    GetInnerProjectFromSolutionFolder(project, ref projects);
                }
                else
                {
                    projects.Add(project);
                }
            }

            return projects;
        }

        public void GetInnerProjectFromSolutionFolder(Project project, ref List<Project> projects)
        {
            int cntInnerItem = project.ProjectItems.Count;
            for (int cnt = 1; cnt <= cntInnerItem; cnt++)
            {
                var innerItem = project.ProjectItems.Item(cnt).SubProject;

                if (innerItem == null)
                {
                    continue;
                }

                if (innerItem.Kind == /*ProjectKinds.vsProjectKindSolutionFolder*/"{66A26720-8FB5-11D2-AA7E-00C04F688DDE}")
                {
                    GetInnerProjectFromSolutionFolder(innerItem, ref projects);
                }
                else
                {
                    Project innerProject = innerItem as Project;
                    if (innerProject != null)
                    {
                        projects.Add(innerProject);
                    }
                }
            }
        }

        public Project GetStartupProject()
        {
            string startPrjName = string.Empty;
            Property property = dte2.Solution.Properties.Item("StartupProject");
            Projects prjs = GetProjects();
            Project startPrj = null;

            if (property != null)
            {
                startPrjName = (string)property.Value;
            }

            foreach (Project prj in prjs)
            {
                if (prj.Name == startPrjName)
                {
                    startPrj = prj;
                }
            }

            return startPrj;
        }

        public string GetValidRootPath()
        {
            if (this.sol2 == null || !this.sol2.IsOpen)
            {
                return String.Empty;
            }

            string solutionfile = this.sol2.FullName;
            string solutionpath = String.Empty;
            if (String.IsNullOrEmpty(solutionfile))
            {
                Array projects = this.dte2.ActiveSolutionProjects as Array;
                if (projects != null && projects.Length > 0)
                {
                    Project activeProject = projects.GetValue(0) as Project;
                    if (activeProject != null)
                    {
                        solutionfile = activeProject.FullName;
                        solutionpath = Path.GetDirectoryName(solutionfile);
                    }
                }
            }
            else
            {
                solutionpath = Path.GetDirectoryName(solutionfile);
            }

            return solutionpath;
        }

        public bool CheckProjectLockJson(Project prj)
        {
            string projectPath = Path.GetDirectoryName(prj.FullName);
            string filterProjectJson = "*project.json";
            string[] files = Directory.GetFiles(projectPath,
                                                filterProjectJson,
                                                SearchOption.TopDirectoryOnly);
            if (files.Length > 0)
            {
                string filterProjectLockJson = "*project.lock.json";
                files = Directory.GetFiles(projectPath,
                                           filterProjectLockJson,
                                           SearchOption.TopDirectoryOnly);
                if (files.Length > 0)
                {
                    return true;
                }

            }

            return false;
        }

        public bool RestoreNugetProject(Project prj)
        {
            string restoreCommand;

            if (this.CheckProjectLockJson(prj))
            {
                return false;
            }

            restoreCommand = "ProjectandSolutionContextMenus.Solution.RestoreNugetPackages";

            try
            {
                this.dte2.ExecuteCommand(restoreCommand);
            }
            catch (Exception e)
            {
                Console.WriteLine("Tizen " + e.Message);
            }

            return true;
        }

        private uint GetItemId(object pVar)
        {
            if (pVar == null)
            {
                return VSConstants.VSITEMID_NIL;
            }

            if (pVar is int)
            {
                return (uint)(int)pVar;
            }

            if (pVar is uint)
            {
                return (uint)pVar;
            }

            if (pVar is short)
            {
                return (uint)(short)pVar;
            }

            if (pVar is ushort)
            {
                return (uint)(ushort)pVar;
            }

            if (pVar is long)
            {
                return (uint)(long)pVar;
            }

            return VSConstants.VSITEMID_NIL;
        }

        //private void SelectRefreshReferences(IVsSolution solution,
        //                                     IVsUIHierarchyWindow hierWindow,
        //                                     IVsUIHierarchy hierProject)
        //{
        //    int hr;
        //    uint itemId, childId;
        //    object pVar;
        //    string itemName;

        //    // get the project name, for debugging
        //    //itemId = (uint)VSConstants.VSITEMID.Root;
        //    //hr = hierProject.GetProperty(itemId,
        //    //        (int)__VSHPROPID.VSHPROPID_Name, out pVar);
        //    //itemName = (string)pVar;

        //    // get the first child item of the project
        //    itemId = (uint)VSConstants.VSITEMID.Root;
        //    hr = hierProject.GetProperty(itemId,
        //                                 (int)__VSHPROPID.VSHPROPID_FirstChild,
        //                                 out pVar);

        //    childId = GetItemId(pVar);
        //    while (childId != VSConstants.VSITEMID_NIL)
        //    {
        //        itemId = childId;
        //        hr = hierProject.GetProperty(itemId,
        //                                    (int)__VSHPROPID.VSHPROPID_Name,
        //                                    out pVar);
        //        itemName = (string)pVar;
        //        if (itemName == "References")
        //        {
        //            // select and expand "References" node
        //            hierWindow.ExpandItem(hierProject,
        //                                  itemId,
        //                                  EXPANDFLAGS.EXPF_SelectItem);
        //            hierWindow.ExpandItem(hierProject,
        //                                  itemId,
        //                                  EXPANDFLAGS.EXPF_ExpandFolder);
        //            break;
        //        }

        //        hr = hierProject.GetProperty(childId,
        //                                     (int)__VSHPROPID.VSHPROPID_NextSibling,
        //                                     out pVar);
        //        childId = GetItemId(pVar);
        //    }
        //}

        //public void RefreshAllReferences()
        //{
        //    /*
        //     * Using soliution explorer window, this method selects all
        //     * real projects and refershes them one by one to reflect
        //     * nuget package restore results.
        //     * So current implementation is something like refresh All Projects,
        //     * not "refresh all References node".
        //     */
        //    IVsSolution solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
        //    if (solution == null)
        //    {
        //        return;
        //    }

        //    Guid guid = Guid.Empty;
        //    IEnumHierarchies enumerator = null;
        //    solution.GetProjectEnum(
        //        (uint)__VSENUMPROJFLAGS.EPF_ALLINSOLUTION,
        //        ref guid, out enumerator);

        //    IVsUIHierarchyWindow hierWindow =
        //        VsShellUtilities.GetUIHierarchyWindow(
        //            serviceProvider,
        //            VSConstants.StandardToolWindows.SolutionExplorer);

        //    string commandRefresh = "SolutionExplorer.Refresh";
        //    IVsHierarchy[] hierarchy = new IVsHierarchy[1] { null };
        //    uint fetched = 0;
        //    for (enumerator.Reset();
        //         enumerator.Next(1, hierarchy, out fetched) == VSConstants.S_OK
        //         && fetched == 1; /*nothing*/)
        //    {
        //        IVsUIHierarchy uihierarchy;
        //        uihierarchy = hierarchy[0] as IVsUIHierarchy;
        //        hierWindow.ExpandItem(uihierarchy,
        //                              VSConstants.VSITEMID_ROOT,
        //                              EXPANDFLAGS.EXPF_SelectItem);

        //        hierWindow.ExpandItem(uihierarchy,
        //                              VSConstants.VSITEMID_ROOT,
        //                              EXPANDFLAGS.EXPF_ExpandFolder);

        //        SelectRefreshReferences(solution, hierWindow, uihierarchy);

        //        // select project again for project "Refresh"
        //        hierWindow.ExpandItem(uihierarchy,
        //                              VSConstants.VSITEMID_ROOT,
        //                              EXPANDFLAGS.EXPF_SelectItem);

        //        // TODO: Change if there is a interface.method way to refresh
        //        try
        //        {
        //            dte2.ExecuteCommand(commandRefresh);
        //        }
        //        catch (Exception e)
        //        {
        //            Console.WriteLine("Tizen " + e.Message);
        //        }
        //    }
        //}

        public Reference GetReference(Project prj, string refName)
        {
            var vsproject = prj.Object as VSProject;
            return vsproject.References.Find(refName);
        }

        public IVsPackageMetadata GetNugetMetadata(Project prj, string pkgName)
        {
            var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
            IVsPackageInstallerServices installerServices = componentModel.GetService<IVsPackageInstallerServices>();

            var installedPackages = installerServices.GetInstalledPackages(prj);

            foreach (IVsPackageMetadata meta in installedPackages)
            {
                if (meta.Id == pkgName)
                {
                    return meta;
                }
            }

            return null;
        }

        public string GetNugetVersion(Project prj, string pkgName)
        {
            IVsPackageMetadata m_data = this.GetNugetMetadata(prj, pkgName);
            if (m_data != null)
            {
                return m_data.VersionString;
            }

            return string.Empty;
        }

        private void OutputDebugLaunchMessage(string rawMsg)
        {
            if (string.IsNullOrEmpty(rawMsg))
            {
                return;
            }

            string message = String.Format($"{DateTime.Now} : {rawMsg}\n");

            VsPackage.outputPaneTizen?.Activate();

            VsPackage.outputPaneTizen?.OutputStringThreadSafe(message);
        }

        public void UpdateYaml(string workspacePath, string tag, string value)
        {
            if (!File.Exists(workspacePath + "\\tizen_workspace.yaml"))
            {
                OutputDebugLaunchMessage($"Failed to set {tag}, tizen_workspace.yaml not found.");
                return;
            }

            string[] arr = File.ReadAllLines(workspacePath + "\\tizen_workspace.yaml");

            using (var writer = new StreamWriter(workspacePath + "\\tizen_workspace.yaml"))
            {
                for (int i = 0; i < arr.Length; i++)
                {
                    string line = arr[i];

                    if (line.Contains(tag))
                    {
                        line = tag + " " + value;
                    }
                    writer.WriteLine(line);
                }
            }
        }

        public string getTag(string workspacePath, string tag, char delimiter=':')
        {
            if (!File.Exists(workspacePath + "\\tizen_workspace.yaml"))
            {
                OutputDebugLaunchMessage($"Failed to get {tag}, tizen_workspace.yaml not found.");
                return "";
            }

            string[] arr = File.ReadAllLines(workspacePath + "\\tizen_workspace.yaml");


            for (int i = 0; i < arr.Length; i++)
            {
                string line = arr[i];
                if (line.StartsWith(tag))
                {
                    return line.Substring(tag.Length).Trim().Split(delimiter)[1].Trim();
                }
            }
            return "";
        }

        public string getPrjTag(string projectFolder, string tag)
        {
            string yamlPath;
            if (getProjectType(projectFolder) == "native")
            {
                yamlPath = Path.Combine(projectFolder, "tizen_native_project.yaml");
                if (!File.Exists(yamlPath))
                {
                    OutputDebugLaunchMessage($"Failed to set {tag}, tizen_native_project.yaml not found.");
                    return "";
                }
            } else if (getProjectType(projectFolder) == "web")
            {
                yamlPath = Path.Combine(projectFolder, "tizen_web_project.yaml");
                if (!File.Exists(yamlPath))
                {
                    OutputDebugLaunchMessage($"Failed to set {tag}, tizen_web_project.yaml not found.");
                    return "";
                }
            } else if (getProjectType(projectFolder) == "dotnet")
            {
                yamlPath = Path.Combine(projectFolder, "tizen_dotnet_project.yaml");
                if (!File.Exists(yamlPath))
                {
                    OutputDebugLaunchMessage($"Failed to set {tag}, tizen_web_project.yaml not found.");
                    return "";
                }
            } else
            {
                OutputDebugLaunchMessage($"INVALID project type");
                return "";
            }
            
            string[] arr = File.ReadAllLines(yamlPath);

            for (int i = 0; i < arr.Length; i++)
            {
                string line = arr[i];
                if (line.StartsWith(tag))
                {
                    return line.Substring(tag.Length).Trim().Split(':')[1].Trim();
                }
            }
            return "";
        }

        public string getTPKFolder(string workspacePath, string buildFolderName = null)
        {
            string build_type = getTag(workspacePath, "build_type");
            string profile = getTag(workspacePath, "profile");
            string api_version = getTag(workspacePath, "api_version");
            api_version = api_version.Replace('"', ' ').Trim();
            string profileName = profile + "-" + api_version;
            string arch = getTag(workspacePath, "arch");

            if (buildFolderName != null)
                return Path.Combine(workspacePath, build_type, profileName, buildFolderName, arch, "tpk");
            else
                return Path.Combine(workspacePath, build_type,profileName, arch, "tpk");
        }

        public IEnumerable<string> GetIncludeDirectories(string rootstrapPath)
        {
            IEnumerable<string> pathDir = Directory.GetDirectories(rootstrapPath, "*", SearchOption.AllDirectories);
            pathDir = pathDir.Append(rootstrapPath);
            return pathDir;
        }

        public string GetAdditionalIncludeDirectories(string rootIncludePath)
        {
            string includePath = "";
            List<string> qlDir = GetIncludeDirectories(rootIncludePath).ToList();
            foreach (string incDir in qlDir)
            {
                if (string.IsNullOrEmpty(includePath))
                {
                    includePath = incDir;
                }
                else
                {
                    includePath += ";" + incDir;
                }
            }

            return includePath;
        }

        public string GetRootstrapPath()
        {
            string workspacePath = getSolutionFolderPath();
            string tizenVersion = getTag(workspacePath, "api_version").Replace('"', ' ').Trim(); ;
            string profile = getTag(workspacePath, "profile");
            string arch = getTag(workspacePath, "arch");
            string rootstraptype = getTag(workspacePath, "rootstrap");
            string rootstrapsPath = arch == "x86"
                ? Path.Combine(ToolsPathInfo.ToolsRootPath, "platforms",
                    "tizen-" + tizenVersion, profile, "rootstraps",
                    profile + "-" + tizenVersion + "-emulator.core", "usr", "include")
                : (rootstraptype == "private" && (arch == "arm" || arch == "arch64"))
                ? Path.Combine(ToolsPathInfo.ToolsRootPath, "platforms",
                    "tizen-" + tizenVersion, profile, "rootstraps",
                    profile + "-" + tizenVersion + "-device.core.private", "usr", "include")
                : (arch == "arm" || arch == "arch64")
                ? Path.Combine(ToolsPathInfo.ToolsRootPath, "platforms",
                    "tizen-" + tizenVersion, profile, "rootstraps",
                    profile + "-" + tizenVersion + "-device.core", "usr", "include")
                : "";

            return rootstrapsPath;
        }

        public void updateAdditionalIncludeDirectoriesOfSolution()
        {
            if (IsTizenNativeProject())
            {
                Projects ListOfProjectsInSolution = GetProjects();
                if (ListOfProjectsInSolution != null)
                {
                    foreach (Project project in ListOfProjectsInSolution)
                    {
                        ShowAdditionalIncludeDirectories(project);
                    }
                }
            }
        }

        public void ShowAdditionalIncludeDirectories(Project prj)
        {
            string rootstrapsPath = GetRootstrapPath();

            if (!!string.IsNullOrEmpty(rootstrapsPath))
            {
                return;
            }

            Microsoft.VisualStudio.VCProjectEngine.VCProject proj;
            Microsoft.VisualStudio.VCProjectEngine.VCCLCompilerTool compilerTool;
            Microsoft.VisualStudio.VCProjectEngine.IVCCollection toolsCollection;
            Microsoft.VisualStudio.VCProjectEngine.IVCCollection configurationsCollection;

            proj = (Microsoft.VisualStudio.VCProjectEngine.VCProject)prj.Object;

            configurationsCollection = (Microsoft.VisualStudio.VCProjectEngine.IVCCollection)proj.Configurations;

            foreach (Microsoft.VisualStudio.VCProjectEngine.VCConfiguration configuration in configurationsCollection)
            {
                toolsCollection = (Microsoft.VisualStudio.VCProjectEngine.IVCCollection)configuration.Tools;

                foreach (object toolObject in toolsCollection)
                {
                    if (toolObject is Microsoft.VisualStudio.VCProjectEngine.VCCLCompilerTool tool)
                    {
                        compilerTool = tool;

                        string addIncludeDir = compilerTool.AdditionalIncludeDirectories;

                        // convert addIncludeDir string to list based on ';' (token)
                        List<string> dirList = addIncludeDir.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries).ToList();

                        // create new list from the current which does not contain rootstraps folder
                        List<string> addInclDirList = dirList.FindAll(e => !e.Contains("\\rootstraps\\"));

                        //construct delimited string based on current yaml profile and arch
                        //TODO: Check how to support user defined rootstraps too 
                        string includePath = GetAdditionalIncludeDirectories(rootstrapsPath);

                        // conver remaining items to string
                        foreach (string inclDirElem in addInclDirList)
                        {
                            includePath += ";" + inclDirElem;
                        }
                        // appen result string to includePath
                        compilerTool.AdditionalIncludeDirectories = includePath;
                        break;
                    }
                }
            }
        }

        private bool CreateTemplateList(bool isAsync = false)
        {
            TzCmdExec executor = new TzCmdExec();

            foreach (string type in projectsArr)
            {
                TemplateListDictionary[type] = new Dictionary<string, List<string>>();

                string message = executor.RunTzCmnd(string.Format("/c tz list templates -t {0}", type), isAsync);
                if (!!string.IsNullOrWhiteSpace(message))
                {
                    return false;
                }
                else if (message.StartsWith("[null]:"))
                {
                    int index = message.LastIndexOf(":");
                    OutputDebugLaunchMessage(message.Substring(index + 1));
                    return false;
                }

                //remove leading and trailing spaces and \r\n
                string trimmedStr = message.Trim().Trim('\r', '\n');
                trimmedStr = trimmedStr.Replace(" ", "");
                string[] templatesWithVersion = trimmedStr.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                string version = null;
                List<string> templateList = null;
                foreach (string template in templatesWithVersion)
                {
                    if (template.Contains(":"))
                    {
                        //assign previous templateList
                        if (templateList != null)
                        {
                            TemplateListDictionary[type][version] = templateList;
                        }

                        templateList = new List<string>();
                        version = template.Split(':')[0];
                        if (!!string.IsNullOrEmpty(version))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        string typeString = "[";
                        if (template.Contains(typeString))
                        {
                            string templateListElem = template.Split(new[] { typeString }, StringSplitOptions.RemoveEmptyEntries)[0];
                            if (!!string.IsNullOrEmpty(templateListElem))
                            {
                                return false;
                            }

                            if (templateList != null)
                            {
                                templateList.Add(templateListElem);
                            }
                        }
                    }
                }
                //assign last templateList
                if (templateList != null)
                {
                    TemplateListDictionary[type][version] = templateList;
                }
            }
            return true;
        }

        public void LoadTemplates()
        {
            //load templates synchronously
            string jsonFilePath = Path.Combine(Path.GetDirectoryName
                (System.Reflection.Assembly.GetExecutingAssembly().Location),
                "templates.json");
            JsonSerializerSettings settings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };

            if (!CreateTemplateList())
            {
                //Remove all entries
                TemplateListDictionary.Clear();
                return;
            }
            string jsonString = JsonConvert.SerializeObject(TemplateListDictionary, Newtonsoft.Json.Formatting.Indented, settings);
            File.WriteAllText(jsonFilePath, jsonString);
        }

        public System.Threading.Tasks.Task LoadTemplatesAsync()
        {
            //load templates asynchronously
            if (TemplateListDictionary != null)
            {
                return System.Threading.Tasks.Task.CompletedTask;
            }

            string jsonFilePath = Path.Combine(Path.GetDirectoryName
                (System.Reflection.Assembly.GetExecutingAssembly().Location),
                "templates.json");
            JsonSerializerSettings settings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };

            //Instantiate templateListDictionary
            TemplateListDictionary = new Dictionary<string, Dictionary<string, List<string>>>();

            if (!CreateTemplateList(true))
            {
                TemplateListDictionary.Clear();
                return System.Threading.Tasks.Task.CompletedTask;
            }
            string jsonString = JsonConvert.SerializeObject(TemplateListDictionary, Newtonsoft.Json.Formatting.Indented, settings);
            File.WriteAllText(jsonFilePath, jsonString);

            return System.Threading.Tasks.Task.CompletedTask;
        }

        public List<string> GetProjectTypeList()
        {
            return TemplateListDictionary.Keys.ToList();
        }

        public List<string> GetProfileList(string type)
        {
            if (!TemplateListDictionary.ContainsKey(type))
            {
                return null;
            }

            return TemplateListDictionary[type].Keys.ToList();
        }

        public List<string> GetTemplateList(string type, string profile)
        {
            if (!TemplateListDictionary.ContainsKey(type) || !TemplateListDictionary[type].ContainsKey(profile))
            {
                return null;
            }

            return TemplateListDictionary[type][profile];
        }

        #region CommonUtils
        public object GetServiceObject(object serviceProvider, Type type)
        {
            return GetServiceObject(serviceProvider, type.GUID);
        }

        public object GetServiceObject(object serviceProviderObject, Guid guid)
        {
            object service = null;
            Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider = null;
            IntPtr serviceIntPtr;
            int hr = 0;
            Guid SIDGuid;
            Guid IIDGuid;
            SIDGuid = guid;
            IIDGuid = SIDGuid;
            serviceProvider = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)serviceProviderObject;
            hr = serviceProvider.QueryService(SIDGuid, IIDGuid, out serviceIntPtr);
            if (hr != 0)
            {
                System.Runtime.InteropServices.Marshal.ThrowExceptionForHR(hr);
            }
            else if (!serviceIntPtr.Equals(IntPtr.Zero))
            {
                service = System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(serviceIntPtr);
                System.Runtime.InteropServices.Marshal.Release(serviceIntPtr);
            }

            return service;
        }

        public Projects GetProjects()
        {
            return this.dte2.Solution.Projects;
        }

        public bool IsHaveTizenManifest(Project prj)
        {
            bool result = false;
            try
            {
                string projectFolder = Path.GetDirectoryName(prj.FullName);
                if (File.Exists(Path.Combine(projectFolder, "tizen-manifest.xml")))
                {
                    result = true;
                }
            }
            catch
            {
            }

            return result;

        }

        public bool IsHaveTizenDotnetYaml(Project prj)
        {
            bool result = false;
            try
            {
                string projectFolder = Path.GetDirectoryName(prj.FullName);
                if (File.Exists(Path.Combine(projectFolder, "tizen_dotnet_project.yaml")))
                {
                    result = true;
                }
            }
            catch
            {
            }

            return result;

        }
        public bool IsHaveTizenNativeYaml(Project prj)
        {
            bool result = false;
            try
            {
                string projectFolder = Path.GetDirectoryName(prj.FullName);
                if (File.Exists(Path.Combine(projectFolder, "tizen_native_project.yaml")))
                {
                    result = true;
                }
            }
            catch
            {
            }

            return result;

        }

        // Adding New helper function to launch web app
        public String getSolutionFolderPath()
        {
            String result = null;
            Solution solution = dte2.Application.Solution;
            if (solution == null)
                return result;

            if(!String.IsNullOrWhiteSpace(solution.FullName))
                result = Path.GetDirectoryName(solution.FullName);

            return result;
        }
        public String getConfigXML()
        {
            String emptyPath = null;
            Solution solution = dte2.Application.Solution;
            if (solution == null)
                return emptyPath;
            Projects ListOfProjectsInSolution = solution.Projects;

            foreach (Project project in ListOfProjectsInSolution)
            {
                try
                {
                    String projectFolder = Path.GetDirectoryName(project.FullName);
                    String path = Path.Combine(projectFolder, "config.xml");
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Tizen: " + e.Message);
                }
            }
            return emptyPath;
        }


        public bool IsHaveTizenConfig()
        {
            bool result = false;
            Solution solution = dte2.Application.Solution;
            if (solution == null)
                return result;
            //Check for config xml
            Projects ListOfProjectsInSolution = solution.Projects;

            foreach (Project project in ListOfProjectsInSolution)
            {
                try
                {
                    string projectFolder = Path.GetDirectoryName(project.FullName);
                    if (File.Exists(Path.Combine(projectFolder, "config.xml")))
                    {
                        return true;
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("Tizen: " + e.Message);
                }
            }
            return result;
        }

        public string GetSolutionBuildConfig()
        {
            Solution solution = dte2.Application.Solution;

            return solution.SolutionBuild.ActiveConfiguration.Name;
        }

        public bool IsHaveTizenNativeYaml()
        {
            Solution solution = dte2.Application.Solution;
            if (solution == null)
                return false;
            //Check for config xml
            Projects ListOfProjectsInSolution = solution.Projects;

            foreach (Project project in ListOfProjectsInSolution)
            {
                try
                {
                    string projectFolder = Path.GetDirectoryName(project.FullName);
                    if (File.Exists(Path.Combine(projectFolder, "tizen_native_project.yaml")))
                    {
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Tizen: " + e.Message);
                    return false;
                }
            }
            return false;
        }

        public bool IsTizenWebProject()
        {
            if (this.sol2 == null || !this.sol2.IsOpen)
                return false;

            //Check for Tizen Web Project GUID
            Projects ListOfProjectsInSolution = this.GetProjects();

            foreach (Project project in ListOfProjectsInSolution)
            {
                try
                {
                    string projectFolder = Path.GetDirectoryName(project.FullName);
                    if (File.Exists(Path.Combine(projectFolder, "config.xml")))
                    {
                        if (project.Kind == "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}")
                        {
                            XDocument xmldoc = XDocument.Load(project.FullName);
                            XDocument xd = xmldoc.Document;

                            foreach (XElement element in xd.Descendants("ProjectTypeGuids"))
                            {
                                var val = element.Value;

                                if (val != null && val.ToUpper().Contains(TizenWebProjGuid))
                                {
                                    return true;
                                }
                            }
                        }
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("Tizen: " + e.Message);
                }
            }

            return false;
        }
        public string GetDotnetProjType(Project prj)
        {
            try
            {
                string projectFolder = Path.GetDirectoryName(prj.FullName);
                if (File.Exists(Path.Combine(projectFolder, "tizen-manifest.xml")))
                {
                    try
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(Path.Combine(projectFolder, "tizen-manifest.xml"));
                        XmlNodeList uiNode = doc.GetElementsByTagName("ui-application");
                        if (uiNode != null && uiNode.Count != 0)
                            return "ui-application";
                        XmlNodeList serviceNode = doc.GetElementsByTagName("service-application");
                        if (serviceNode != null && serviceNode.Count != 0)
                            return "service-application";
                        return "others";
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Tizen: " + e.Message);
                        return "others";
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Tizen: " + e.Message);
            }
            return "others";
        }
        public bool IsTizenWebWgtProject(Project prj)
        {
            try
            {
                string projectFolder = Path.GetDirectoryName(prj.FullName);
                if (File.Exists(Path.Combine(projectFolder, "config.xml")))
                {
                    try
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(Path.Combine(projectFolder, "config.xml"));
                        XmlNodeList wgtNode = doc.GetElementsByTagName("tizen:app-widget");
                        if (wgtNode != null && wgtNode.Count != 0)
                            return true;
                        return false;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Tizen: " + e.Message);
                        return false;
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Tizen: " + e.Message);
            }
            return false;
        }
        public string GetTizenAppType(Project prj)
        {
            string projType = "";
            try
            {
                string projectFolder = Path.GetDirectoryName(prj.FullName);
                if (getProjectType(projectFolder) == "native")
                {
                    projType = getPrjTag(projectFolder, "project_type");
                    if(projType == "native_app")
                    {
                        projType = GetAppType(prj);
                    }                        
                    
                } else if (getProjectType(projectFolder) == "web")
                {
                    if (IsTizenWebWgtProject(prj))
                    {
                        projType = "web_widget";
                    }
                    else
                    {
                        projType = getPrjTag(projectFolder, "project_type");
                    }                    
                } else if (getProjectType(projectFolder) == "dotnet")
                {
                    return GetDotnetProjType(prj);
                }
                else
                {
                    Console.WriteLine("Invalid project type");
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Tizen: " + e.Message);
            }
            return projType;
        }
        public void RemoveActiveDebuggerEntry(Project project)
        {
            VsProjectHelper vsProjectHelper = VsProjectHelper.GetInstance;
            string activeDebugProfile = vsProjectHelper.GetProjProperty(project, "ActiveDebugProfile", (uint)_PersistStorageType.PST_USER_FILE);
            if (activeDebugProfile != null)
            {
                vsProjectHelper.RemoveProjProperty(project, "ActiveDebugProfile", (uint)_PersistStorageType.PST_USER_FILE);
                project.Save();
            }
        }

        public void RemoveActiveDebuggerEntry()
        {
            if (this.sol2 == null || !this.sol2.IsOpen) return;
            Projects ListOfProjectsInSolution = this.GetProjects();
            foreach (Project project in ListOfProjectsInSolution)
            {
                RemoveActiveDebuggerEntry(project);
            }
        }

        public bool IsTizenNativeProject()
        {
            if (this.sol2 == null || !this.sol2.IsOpen)
                return false;
            //Check for Tizen Web Project GUID
            Projects ListOfProjectsInSolution = this.GetProjects();
            foreach (Project project in ListOfProjectsInSolution)
            {
                try
                {
                    if (IsHaveTizenManifest(project) || IsHaveTizenNativeYaml(project))
                    {
                        if (project.Kind == CPPBasedProject)
                        {
                            return true;
                        }
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("Tizen: " + e.Message);
                }
            }

            return false;
        }

        public bool IsTizenDotnetProject()
        {
            if (this.sol2 == null || !this.sol2.IsOpen)
                return false;
            Projects ListOfProjectsInSolution = this.GetProjects();
            foreach (Project project in ListOfProjectsInSolution)
            {
                try
                {
                    if (IsHaveTizenDotnetYaml(project))
                    {
                        return true;
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("Tizen: " + e.Message);
                }
            }

            return false;
        }

        public void UpdateBuildForProjects(string startUpProject)
        {
            SolutionConfigurations solutionConfigurations;
            solutionConfigurations = dte2.Solution.SolutionBuild.SolutionConfigurations;

            foreach (Project project in dte2.Solution.Projects)
            {
                string projType = getProjectType(Path.GetDirectoryName(project.FullName));
                if (!projType.Equals("dotnet") && project.UniqueName != startUpProject)
                {
                    UpdateBuildForProject(project.UniqueName, false);
                }
                else
                {
                    UpdateBuildForProject(project.UniqueName, true);
                }
            }
        }

        public void UpdateBuildForProject(string projectName, bool value)
        {
            SolutionConfigurations solutionConfigurations;
            solutionConfigurations = dte2.Solution.SolutionBuild.SolutionConfigurations;

            foreach (Project project in dte2.Solution.Projects)
            {
                foreach (SolutionConfiguration2 solutionConfiguration2 in solutionConfigurations)
                {
                    foreach (SolutionContext solutionContext in solutionConfiguration2.SolutionContexts)
                    {
                        if (solutionContext.ProjectName == project.UniqueName)
                        {
                            if (project.UniqueName == projectName)
                            {
                                solutionContext.ShouldBuild = value;
                            }
                        }
                    }
                }
            }
        }

        //public bool IsTizenProject(Project prj)
        //{
        //    string prjTypeList = null;
        //    prjTypeList = GetProjectTypeList(prj);
        //    if (prjTypeList.Contains(TizenProjGuid))
        //    {
        //        return true;
        //    }

        //    return false;
        //}

        //public bool IsKindOfProject(Project prj, string prjGuid)
        //{
        //    string prjTypeList = null;
        //    prjTypeList = GetProjectTypeList(prj);
        //    if (prjTypeList.Contains(prjGuid))
        //    {
        //        return true;
        //    }

        //    return false;
        //}

        public Project GetCurProjectFromHierarchy(IVsHierarchy hierarchy)
        {
            object objProj;
            hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out objProj);

            return objProj as Project;
        }

        //public IList<Project> GetTizenProjects()
        //{
        //    var tprjs = new List<Project>();
        //    Projects prjs = this.GetProjects();

        //    foreach (Project prj in prjs)
        //    {
        //        if (IsTizenProject(prj))
        //        {
        //            tprjs.Add(prj);
        //        }
        //    }
        //    return tprjs;
        //}

        public IList<string> GetProjectName()
        {
            var prjName = new List<string>();
            Projects prjs = this.GetProjects();

            foreach (Project prj in prjs)
            {
                prjName.Add(prj.Name);
            }

            return prjName;
        }

        public String getProjectType(string projPath)
        {
            String prjType = String.Empty;

            if (File.Exists(Path.Combine(projPath, "tizen_web_project.yaml")))
            {
                prjType = "web";
            }
            else if (File.Exists(Path.Combine(projPath, "tizen_native_project.yaml")))
            {
                prjType = "native";
            }
            else if (File.Exists(Path.Combine(projPath, "tizen_dotnet_project.yaml")))
            {
                prjType = "dotnet";
            }

            return prjType;
        }

        public void HandleFileRemovalFromProject(ProjectItem projectItem)
        {
            String projectPath = projectItem.ContainingProject.FullName;
            String folderPath = Path.GetDirectoryName(projectPath);
            if (folderPath == null)
            {
                return;
            }
            String projectType = getProjectType(folderPath);
            Uri folderUri = new Uri(folderPath);

            if (projectType.Equals("web"))
            {
                ModifyWebYaml(projectItem, folderPath, folderUri, true);
            }
            else if (projectType.Equals("native"))
            {
                ModifyNativeYaml(projectItem, folderPath, folderUri, true);
            }
        }

            
        public void HandleFileAddingToProject(ProjectItem projectItem)
        {
            String projectPath = projectItem.ContainingProject.FullName;
            String folderPath = Path.GetDirectoryName(projectPath);
            if (folderPath == null)
            {
                return;
            }
            String projectType = getProjectType(folderPath);
            Uri folderUri = new Uri(folderPath);

            if (projectType.Equals("web"))
            {
                ModifyWebYaml(projectItem, folderPath, folderUri, false);
            }
            else if (projectType.Equals("native"))
            {
                ModifyNativeYaml(projectItem, folderPath, folderUri, false);
            }
        }

        public void HandleFilRenameInProject(ProjectItem projectItem, string oldName)
        {
            String projectPath = projectItem.ContainingProject.FullName;
            String folderPath = Path.GetDirectoryName(projectPath);
            if (folderPath == null)
            {
                return;
            }
            String projectType = getProjectType(folderPath);
            Uri folderUri = new Uri(folderPath);

            if (projectType.Equals("web"))
            {
                String yaml = File.ReadAllText(folderPath + "\\tizen_web_project.yaml");
                ParseWebYaml WebYaml = ParseWebYaml.FromYaml(yaml);
                Uri fileUri = new Uri(oldName);
                string sourcePath = fileUri.ToString().Replace(folderUri.ToString() + "/", "");
                WebYaml.Files.Remove(sourcePath);
                string text = ParseWebYaml.ToYaml(WebYaml);
                File.WriteAllText(folderPath + "\\tizen_web_project.yaml", text);

                ModifyWebYaml(projectItem, folderPath, folderUri, false);
            }
            else if (projectType.Equals("native"))
            {
                
                if (isSourceFile(oldName))
                {
                    String yaml = File.ReadAllText(folderPath + "\\tizen_native_project.yaml");
                    ParseNativeYaml NativeYaml = ParseNativeYaml.FromYaml(yaml);
                    string oldNamePath = Path.GetDirectoryName(projectItem.FileNames[0]);
                    Uri fileUri = new Uri(oldNamePath + "/" + oldName);
                    string sourcePath = fileUri.ToString().Replace(folderUri.ToString() + "/", "");
                    NativeYaml.Sources.Remove(sourcePath);
                    string text = ParseNativeYaml.ToYaml(NativeYaml);
                    File.WriteAllText(folderPath + "\\tizen_native_project.yaml", text);
                }
                ModifyNativeYaml(projectItem, folderPath, folderUri, false);
            }
        }

        public String getProfile(String filePath)
        {
            ParseWorkspaceYaml WorkspaceYaml = getWorkspaceYamlParser(filePath);
            return WorkspaceYaml.Profile;
        }

        public void setActiveCertificate(String certificateType)
        {
            if (this.sol2 == null || !this.sol2.IsOpen)
                return;

            string workspaceFolder = Path.GetDirectoryName(sol2.FullName);
            if (!File.Exists(workspaceFolder + "\\tizen_workspace.yaml"))
            {
                return;
            }
            ParseWorkspaceYaml WorkspaceYaml = getWorkspaceYamlParser(workspaceFolder + "\\tizen_workspace.yaml");
            WorkspaceYaml.SigningProfile = certificateType;
            string text = ParseWorkspaceYaml.ToYaml(WorkspaceYaml);
            File.WriteAllText(workspaceFolder + "\\tizen_workspace.yaml", text);
        }

        public String getCertificateType()
        {
            CertificateInfo info = Certificate.CheckValidCertificate();

            if (info == null ||
                String.IsNullOrEmpty(info.AuthorCertificateFile) ||
                String.IsNullOrEmpty(info.AuthorPassword) ||
                String.IsNullOrEmpty(info.DistributorCertificateFile) ||
                String.IsNullOrEmpty(info.DistributorPassword) ||
                !File.Exists(info.AuthorCertificateFile) ||
                !File.Exists(info.DistributorCertificateFile))
            {
                return ".";
            }
            return String.Empty;
        }

        private ParseWorkspaceYaml getWorkspaceYamlParser(string filePath)
        {
            String yaml = File.ReadAllText(filePath);
            ParseWorkspaceYaml WorkspaceYaml = ParseWorkspaceYaml.FromYaml(yaml);
            return WorkspaceYaml;
        }

        public String getPlatform(String filePath)
        {
            ParseWorkspaceYaml WorkspaceYaml = getWorkspaceYamlParser(filePath);
            return WorkspaceYaml.ApiVersion;
        }

        private void ModifyNativeYaml(ProjectItem projectItem, string folderPath, Uri folderUri, Boolean isDelete)
        {
            String yaml = File.ReadAllText(folderPath + "\\tizen_native_project.yaml");

            ParseNativeYaml NativeYaml = ParseNativeYaml.FromYaml(yaml);
            for (int i = 0; i < projectItem.FileCount; i++)
            {
                String fullName = projectItem.FileNames[(short)i];
                if (fullName == null)
                {
                    continue;
                }

                if (isSourceFile(fullName))
                {
                    Uri fileUri = new Uri(fullName);
                    string sourcePath = fileUri.ToString().Replace(folderUri.ToString() + "/", "");
                    if (!isDelete)
                        NativeYaml.Sources.Add(sourcePath);
                    else
                        NativeYaml.Sources.Remove(sourcePath);
                }
            }
            string text = ParseNativeYaml.ToYaml(NativeYaml);
            File.WriteAllText(folderPath + "\\tizen_native_project.yaml", text);
        }

        private bool isSourceFile(string fullName)
        {
            String ext = System.IO.Path.GetExtension(fullName);
            return !Directory.Exists(fullName) && (".c".Equals(ext) || ".cpp".Equals(ext)
                    || ".h".Equals(ext) || ".cc".Equals(ext) || ".cxx".Equals(ext) || ".hh".Equals(ext));
        }

        private void ModifyWebYaml(ProjectItem projectItem, string folderPath, Uri folderUri, Boolean isDelete)
        {
            String yaml = File.ReadAllText(folderPath + "\\tizen_web_project.yaml");

            ParseWebYaml WebYaml = ParseWebYaml.FromYaml(yaml);
            for (int i = 0; i < projectItem.FileCount; i++)
            {
                String fullName = projectItem.FileNames[(short)i];
                if (fullName == null)
                {
                    continue;
                }
                Uri fileUri = new Uri(fullName);
                string sourcePath = fileUri.ToString().Replace(folderUri.ToString() + "/", "");
                if (!isDelete)
                    WebYaml.Files.Add(sourcePath);
                else
                    WebYaml.Files.Remove(sourcePath);
            }
            string text = ParseWebYaml.ToYaml(WebYaml);
            File.WriteAllText(folderPath + "\\tizen_web_project.yaml", text);
        }

        public void UpdatePlatformToolsetVersion(string projPath, string latestVer)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(projPath);
            XmlNodeList nodes = doc.GetElementsByTagName("PlatformToolset");

            foreach (XmlNode node in nodes)
            {
                if (!node.InnerText.Equals(latestVer))
                {
                    node.InnerText = latestVer;
                }
            }
            doc.Save(projPath);
	}

        public void createBuildFile(string solDir, string prjName)
        {
            string buildTemplateFilePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "BuildTemplate");

            // Check if file is available.
            if (File.Exists(buildTemplateFilePath))
            {
                // Open the build template file to read from.
                string[] buildFileContent = File.ReadAllLines(buildTemplateFilePath);
                string projBuildFilePath = Path.Combine(solDir, prjName, "Directory.Build.targets");

                for (int i = 0; i < buildFileContent.Length; i++)
                {
                    string line = buildFileContent[i];

                    if (buildFileContent[i].Contains("Exec"))
                    {
                        // Modify the TZ path in <Exec> tag Command
                        buildFileContent[i] = $@"{"\t\t"}<Exec Command=""{ToolsPathInfo.ToolsRootPath}\tools\tizen-core\tz.exe pack  -S $(ProjectDir) $(WorkspaceFolder)""> </Exec>";
                    }
                }
                File.WriteAllLines(projBuildFilePath, buildFileContent);
            }
        }

        #endregion

        #region Deprecated Method
        public Project IsSpecificProject(string prjGuid)
        {
            string prjTypeList = null;
            Projects prjs = GetProjects();
            foreach (Project prj in prjs)
            {
                prjTypeList = GetProjectTypeList(prj);
                if (prjTypeList.Contains(prjGuid))
                {
                    return prj;
                }
            }

            return null;
        }

        public bool IsContainsProject(string prjGuid)
        {
            string prjTypeList = null;
            Projects prjs = GetProjects();
            foreach (Project prj in prjs)
            {
                prjTypeList = GetProjectTypeList(prj);
                if (prjTypeList.Contains(prjGuid))
                {
                    return true;
                }
            }

            return false;
        }

        public string GetProjectTypeList(Project prj)
        {
            string projectTypeGuids = "";
            object service = null;
            IVsSolution solution = null;
            IVsHierarchy hierarchy = null;
            IVsAggregatableProject aggregatableProject = null;
            int? result = 0;
            service = GetServiceObject(prj.DTE, typeof(IVsSolution));
            solution = (IVsSolution)service;
            result = solution?.GetProjectOfUniqueName(prj.UniqueName, out hierarchy);
            if (result == 0)
            {
                try
                {
                    aggregatableProject = (IVsAggregatableProject)hierarchy;
                    result = aggregatableProject.GetAggregateProjectTypeGuids(out projectTypeGuids);
                }
                catch (Exception)
                {
                }
            }

            return projectTypeGuids;
        }

        #endregion


        #region EnvDTEExtension
        /// <summary>
        /// Gets the %Tizen package path.
        /// </summary>
        /// <param name="prj">project</param>
        /// <returns>Path of tpk for project</returns>
        public string GetTizenPackagePath(Project prj)
        {
            string projectFolder = Path.GetDirectoryName(prj.FullName);
            //string targetName = Path.GetFileNameWithoutExtension(prj.FullName);
            string projectOutput = Path.Combine(projectFolder, GetProjectOutputPath(prj));
            //string tpkFilepath = Path.Combine(projectOutput, targetName + ".tpk");
            string packageId = GetPackageId(prj);
            string packageVersion = GetPackageVersion(prj);
            string tpkFileName = packageId + "-" + packageVersion + ".tpk";
            string tpkFilePath = Path.Combine(projectOutput, tpkFileName);
            return tpkFilePath;
        }

        /// <summary>
        /// Gets the project output path.
        /// </summary>
        /// <param name="prj">project</param>
        /// <returns>Project build output path</returns>
        public string GetProjectOutputPath(Project prj)
        {
            Properties props = prj?.ConfigurationManager?.ActiveConfiguration?.Properties;
            return this.GetPropertyString(props, "OutputPath");
        }

        public bool IsLibraryProject(Properties props)
        {
            string type = GetPropertyString(props, "OutputType");
            return type.Equals("2", StringComparison.OrdinalIgnoreCase);
        }

        public string GetPropertyString(Properties props, string name)
        {
            Property prop = props.Item(name);
            if (prop != null)
            {
                return prop.Value.ToString();
            }

            return String.Empty;
        }

        public string GetProjectBuildProperty(Project prj, string name)
        {
            string output = null;
            object service = null;
            IVsSolution solution = null;
            IVsHierarchy hierarchy = null;
            int? result = 0;

            service = GetServiceObject(prj.DTE, typeof(IVsSolution));
            solution = (IVsSolution)service;
            result = solution?.GetProjectOfUniqueName(prj.UniqueName, out hierarchy);
            IVsBuildPropertyStorage buildPropStorage = (IVsBuildPropertyStorage)hierarchy;
            buildPropStorage?.GetPropertyValue(name, String.Empty, (uint)_PersistStorageType.PST_PROJECT_FILE, out output);

            return output;
        }

        public List<string> GetImportList(Project prj)
        {
            List<string> output = new List<string>();
            object service = null;
            IVsSolution solution = null;
            IVsHierarchy hierarchy = null;
            int? result = 0;

            service = GetServiceObject(prj.DTE, typeof(IVsSolution));
            solution = (IVsSolution)service;
            result = solution?.GetProjectOfUniqueName(prj.UniqueName, out hierarchy);
            IVsProject project = (IVsProject)hierarchy;
            Microsoft.Build.Evaluation.Project msbuildProject = ForceLoadMSBuildProjectFromIVsProject(project);
            string prj_path = Path.GetDirectoryName(prj.FullName);

            foreach (ProjectImportElement import in msbuildProject.Xml.Imports)
            {
                if (import.Label == "Shared")
                {
                    output.Add(Path.Combine(prj_path, import.Project));
                }
            }
            UnloadMSbuildProject(msbuildProject);
            return output;
        }

        public string ChangeToRelativeFromFull(string full, string current)
        {
            Uri fileUri = new Uri(full);
            Uri relativeUri = new Uri(current);
            return relativeUri.MakeRelativeUri(fileUri).ToString();
        }

        public void SetImportPath(string importPath, Project prj)
        {
            object service = null;
            IVsSolution solution = null;
            IVsHierarchy hierarchy = null;
            ProjectImportElement importProject;
            int? result = 0;

            service = GetServiceObject(prj.DTE, typeof(IVsSolution));
            solution = (IVsSolution)service;
            result = solution?.GetProjectOfUniqueName(prj.UniqueName, out hierarchy);
            IVsProject project = (IVsProject)hierarchy;
            Microsoft.Build.Evaluation.Project msbuildProject = MSBuildProjectFromIVsProject(project);
            if (msbuildProject == null)
            {
                msbuildProject = ForceLoadMSBuildProjectFromIVsProject(project);
            }
            importProject = msbuildProject.Xml.AddImport(importPath);
            importProject.Label = "Shared";
            msbuildProject.Save();
            UnloadMSbuildProject(msbuildProject);
        }

        private Microsoft.Build.Evaluation.Project MSBuildProjectFromIVsProject(IVsProject project)
        {
            return Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.GetLoadedProjects(GetProjectFilePath(project)).FirstOrDefault();
        }

        private Microsoft.Build.Evaluation.Project ForceLoadMSBuildProjectFromIVsProject(IVsProject project)
        {
            return new Microsoft.Build.Evaluation.Project(GetProjectFilePath(project));
        }

        private void UnloadMSbuildProject(Microsoft.Build.Evaluation.Project project)
        {
            Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.UnloadProject(project);
        }

        static public string GetProjectFilePath(IVsProject project)
        {
            string path = string.Empty;
            int hr = project.GetMkDocument((uint)VSConstants.VSITEMID.Root, out path);
            System.Diagnostics.Debug.Assert(hr == VSConstants.S_OK || hr == VSConstants.E_NOTIMPL, "GetMkDocument failed for project.");

            return path;
        }

        /// <summary>
        /// Gather dll files in the Output/packaging lib folder
        /// </summary>
        /// <param name="prj">Project</param>
        /// <returns>comma separated file names with extension</returns>
        public string GetPackageLibraryFiles(Project prj)
        {
            string projectFullName = prj.FullName;
            string projectOutputPath = GetProjectOutputPath(prj);
            string projectFolder = Path.GetDirectoryName(projectFullName);
            string projectOutput = Path.Combine(projectFolder, projectOutputPath);
            string outputDir = Path.GetDirectoryName(projectOutput);
            string pathPackage = Path.Combine(outputDir, "packaging");
            string pathPackageLib = Path.Combine(pathPackage, "lib");
            string[] files = Directory.GetFiles(pathPackageLib, "*.dll");
            string result = string.Empty;

            foreach (string file in files)
            {
                if (!String.IsNullOrEmpty(result))
                {
                    result += ",";
                }

                result += Path.GetFileName(file);
            }

            return result;
        }

        public DTE2 GetDTE2()
        {
            return dte2;
        }
        #endregion

        #region multi-packaged tizen-manifest handling
        /// <summary>
        /// Parameter "tpkPath" means getting information from *.tpk file which has single or multiple app info. Multiple app info are integrated in build time.
        /// Parameter "project" means getting information from tizen-manifest.xml in the selected project directory which has single app info.
        /// </summary>
        private const string packageKey = "package";
        private const string versionKey = "version";
        private const string apiversionKey = "api-version";
        private const string appIdKey = "appid";
        private const string webAppIdKey = "id";
        private const string execKey = "exec";

        private string ManifestPath(string tpkPath) => Path.Combine(Path.GetDirectoryName(tpkPath), "tpkroot", "tizen-manifest.xml");
        private string ManifestPath(Project project)
        {
            string projPath = Path.GetDirectoryName(project.FullName);
            var manifestFiles = Directory.GetFiles(projPath, "tizen-manifest.xml", SearchOption.AllDirectories);
            if (manifestFiles.Length > 0)
                return manifestFiles[0];
            else
                return null;
        }
        private string WebManifestPath(Project project) => Path.Combine(Path.GetDirectoryName(project.FullName), "config.xml");

        public string GetPackageAttribute(string manifestPath, string attrName)
        {
            XmlDocument doc = new XmlDocument();

            try
            {
                doc.Load(manifestPath);
                return doc.GetElementsByTagName("manifest")[0].Attributes.GetNamedItem(attrName).Value;
            }
            catch (Exception)
            {
            }

            return null;
        }

        public string GetPackageIdByManifestPath(string manifestPath) => GetPackageAttribute(manifestPath, packageKey);
        public string GetPackageId(Project project) => GetPackageIdByManifestPath(ManifestPath(project));
        public string GetPackageId(string tpkPath) => GetPackageIdByManifestPath(ManifestPath(tpkPath));

        public string GetPackageVersionByManifestPath(string manifestPath) => GetPackageAttribute(manifestPath, versionKey);

        public string GetPackageAPIVersionByManifestPath(string manifestPath) => GetPackageAttribute(manifestPath, apiversionKey);
        public string GetPackageVersion(Project project) => GetPackageVersionByManifestPath(ManifestPath(project));
        public string GetPackageVersion(string tpkPath) => GetPackageVersionByManifestPath(ManifestPath(tpkPath));
        public string GetPackageAPIVersion(string tpkPath) => GetPackageAPIVersionByManifestPath(ManifestPath(tpkPath));

        public IEnumerable<XmlNode> GetAppNodes(string manifestPath)
        {
            XmlDocument doc = new XmlDocument();

            try
            {
                doc.Load(manifestPath);
                var appNodes = doc.GetElementsByTagName("manifest")[0].ChildNodes.Cast<XmlNode>().Where(_ => appNodeNameCandidates.Contains(_.Name.ToLower()));//new List<XmlNode>();

                return appNodes;
            }
            catch (Exception)
            {
            }

            return null;
        }

        //
        // Summary:
        //     Get all the child tags/nodes inside the "widget" tag and returns the Nodes as a IEnumerable List
        //
        // Parameters:
        //   manifestPath:
        //     The manifest/xml file Path to be parsed to get the nodes list.
        //
        // Returns:
        //     The child Nodes as a IEnumerable List.
        //
        public IEnumerable<XmlNode> GetWebAppNodes(string manifestPath)
        {
            XmlDocument doc = new XmlDocument();

            try
            {
                doc.Load(manifestPath);
                var appNodes = doc.GetElementsByTagName("widget")[0].ChildNodes.Cast<XmlNode>().Where(_ => appNodeNameCandidates.Contains(_.Name.ToLower()));//new List<XmlNode>();

                return appNodes;
            }
            catch (Exception)
            {
            }

            return null;
        }

        public List<string> GetAppIds(string tpkPath)
        {
            try
            {
                var appIds = from node in GetAppNodes(ManifestPath(tpkPath))
                             let value = node.Attributes.GetNamedItem(appIdKey).Value
                             select value;

                return appIds.ToList();
            }
            catch (Exception)
            {
            }

            return null;
        }

        public string GetCustomAttrValue(Project project, string tagName, string attrName)
        {
            var nodes = GetAppNodes(ManifestPath(project)).ElementAt(0);
            string res = string.Empty;

            foreach (XmlNode n in nodes)
            {
                if (tagName.Equals(n.Name))
                    res = n.Attributes?.GetNamedItem(attrName)?.Value;
            }

            return res;

        }

        public string GetAppType(Project project) => GetAppNodes(ManifestPath(project))?.First(_ => true)?.Name.ToLower();
        public string GetAttrValue(Project project, string attrName) => GetAppNodes(ManifestPath(project))?.First(_ => true)?.Attributes?.GetNamedItem(attrName)?.Value;
        public string GetWebAttrValue(Project project, string attrName) => GetWebAppNodes(WebManifestPath(project))?.First(_ => true)?.Attributes?.GetNamedItem(attrName)?.Value;
        public string GetAppId(Project project) => GetAttrValue(project, appIdKey);
        public string GetAppCategory(Project project) => GetCustomAttrValue(project, "category", "name");
        public string GetWebAppId(Project project) => GetWebAttrValue(project, webAppIdKey);
        public string GetAppExec(Project project) => GetAttrValue(project, execKey);
        #endregion
    }
}
