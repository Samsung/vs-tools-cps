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

namespace Tizen.VisualStudio
{
    public class VsProjectHelper
    {
        private static VsProjectHelper instance = null;
        private DTE2 dte2 = null;
        private Solution2 sol2 = null;
        private ServiceProvider serviceProvider = null;
        private IVsSolution solutionService = null;

        //public readonly string PCLProjGuid = "786C830F-07A1-408B-BD7F-6EE04809D6DB";
        public readonly string NETStandardProject = "9A19103F-16F7-4668-BE54-9A1E7A4F7556";
        public readonly string SharedProject = "{D954291E-2A0B-460D-934E-DC6B0785DB48}";
        public readonly string TizenProjGuid = "2F98DAC9-6F16-457B-AED7-D43CAC379341";
        public readonly string TizenProjCfgName = "ProjectCommonFlavorCfg";

        private readonly List<string> appNodeNameCandidates = new List<string>() { "ui-application", "service-application", "widget-application", "watch-application" };

        static public VsProjectHelper GetInstance
        {
            get
            {
                VsProjectHelper.instance.sol2 =
                    (Solution2)VsProjectHelper.instance.dte2.Solution;
                return VsProjectHelper.instance;
            }
        }

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

        private const string appIdKey = "appid";
        private const string execKey = "exec";

        private string ManifestPath(string tpkPath) => Path.Combine(Path.GetDirectoryName(tpkPath), "tpkroot", "tizen-manifest.xml");
        private string ManifestPath(Project project) => Path.Combine(Path.GetDirectoryName(project.FullName), "tizen-manifest.xml");

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
        public string GetPackageVersion(Project project) => GetPackageVersionByManifestPath(ManifestPath(project));
        public string GetPackageVersion(string tpkPath) => GetPackageVersionByManifestPath(ManifestPath(tpkPath));

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

        public string GetAppType(Project project) => GetAppNodes(ManifestPath(project))?.First(_ => true)?.Name.ToLower();
        public string GetAttrValue(Project project, string attrName) => GetAppNodes(ManifestPath(project))?.First(_ => true)?.Attributes?.GetNamedItem(attrName)?.Value;
        public string GetAppId(Project project) => GetAttrValue(project, appIdKey);
        public string GetAppExec(Project project) => GetAttrValue(project, execKey);
        #endregion
    }
}
