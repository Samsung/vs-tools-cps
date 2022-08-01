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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Linq;
using System.Xml;
using Microsoft.Build.Evaluation;
using System.Collections;
using Tizen.VisualStudio.Utilities;
using System.IO;
using ProjectItem = EnvDTE.ProjectItem;

namespace Tizen.VisualStudio
{
    public class VsEvents
        : IVsSolutionEvents, IVsSelectionEvents
    {
        private static VsEvents instance = null;

        private IVsEventsHandler eventsHandler = null;
        private IVsSolution solution = null;

        private uint solutionEventsCookie = 0;
        private Events dteEvents = null;
        private BuildEvents buildEvents;
        private DTE dte = null;

        private uint selectionEventsCookie;

        private static long startTime = 0, endTime = 0, usage = 0;
        private ProjectItemsEvents PIEvents;

        public static void Initialize(IVsEventsHandler eventsHandler,
                                      IVsSolution solution, IVsMonitorSelection ms)
        {
            if (eventsHandler == null)
            {
                throw new ArgumentNullException("eventsHandler");
            }

            if (solution == null)
            {
                throw new ArgumentNullException("solution");
            }
            startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() / 1000;

            VsEvents.instance = new VsEvents(eventsHandler, solution, ms);

            instance.addBuildListners();

            //Commenting listners for future use
            //instance.addProjectItenEvents();
        }

        private void addProjectItenEvents()
        {
            Events2 events = this.dte.Events as Events2;
            PIEvents = events.ProjectItemsEvents;
            PIEvents.ItemAdded += ItemAddedToProject;
            PIEvents.ItemRemoved += ItemRemovedFromProject;
            PIEvents.ItemRenamed += ItemRenamedInProject;
        }

        private void ItemRenamedInProject(ProjectItem projectItem, string OldName)
        {
            //Check event for folder renaming
            VsProjectHelper.Initialize();
            VsProjectHelper vsProjectHelper = VsProjectHelper.GetInstance;
            vsProjectHelper.HandleFilRenameInProject(projectItem, OldName);
        }

        private void ItemRemovedFromProject(ProjectItem projectItem)
        {
            VsProjectHelper.Initialize();
            VsProjectHelper vsProjectHelper = VsProjectHelper.GetInstance;
            vsProjectHelper.HandleFileRemovalFromProject(projectItem);
        }

        private void ItemAddedToProject(ProjectItem projectItem)
        {
            VsProjectHelper.Initialize();
            VsProjectHelper vsProjectHelper = VsProjectHelper.GetInstance;
            vsProjectHelper.HandleFileAddingToProject(projectItem);
        }

        public static void Dispose()
        {
            if (VsEvents.instance != null)
            {
                VsEvents.instance.Release();
                VsEvents.instance = null;
            }
        }

        private VsEvents(IVsEventsHandler eventsHandler, IVsSolution solution, IVsMonitorSelection ms)
        {
            this.eventsHandler = eventsHandler;
            this.solution = solution;
            this.solution.AdviseSolutionEvents(this,
                                               out solutionEventsCookie);
            this.dte = Package.GetGlobalService(typeof(DTE)) as DTE;

            if (ms != null)
            {
                // Register for selection events
                ms.AdviseSelectionEvents(this, out selectionEventsCookie);
            }
        }

        private void Release()
        {
            if (this.solutionEventsCookie != 0)
            {
                this.solution.UnadviseSolutionEvents(solutionEventsCookie);
                this.solutionEventsCookie = 0;
            }
        }

        #region IVsSolutionEvents implements


        private void addBuildListners()
        {
            // https://msdn.microsoft.com/en-us/library/envdte.events.aspx
            this.dteEvents = this.eventsHandler.GetDTEEvents();
            this.buildEvents = this.dteEvents.BuildEvents;
            this.buildEvents.OnBuildProjConfigDone += OnBuildProjConfigDone;
            this.buildEvents.OnBuildBegin += OnBuildBegin;
        }

        // Solution ------------------------------------------------------------
        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            VsProjectHelper prjHelperInstance = VsProjectHelper.GetInstance;
            addBuildListners();
            //this.buildEvents.OnBuildBegin += OnBuildBegin;

            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            this.eventsHandler.OnVsEventSolutionBeforeClose();

            if (this.buildEvents != null)
            {
                this.buildEvents.OnBuildProjConfigDone -= OnBuildProjConfigDone;
                this.buildEvents.OnBuildBegin -= OnBuildBegin;
                this.buildEvents = null;
            }

            this.dteEvents = null;

            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy,
                                        ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy,
                                         IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy,
                                       int fRemoving,
                                       ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy,
                                        int fRemoved)
        {
            VsProjectHelper prjHelperInstance = VsProjectHelper.GetInstance;
            EnvDTE.Project prj = prjHelperInstance.GetCurProjectFromHierarchy(pHierarchy);
            bool isTizenproj = prjHelperInstance.IsHaveTizenManifest(prj);
            if (isTizenproj)
            {
                endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() / 1000;
                usage = endTime - startTime;
                RemoteLogger.logUsage("Tizen.VSWin", usage);
            }
            this.eventsHandler.OnVsEventBeforeCloseProject(prj, fRemoved);
            return VSConstants.S_OK;
        }
        #endregion


        #region BuildEvent Listener

        // https://msdn.microsoft.com/en-us/library/envdte._dispbuildevents_onbuildprojconfigdoneeventhandler.aspx
        private void OnBuildProjConfigDone(string Project,
                                           string ProjectConfig,
                                           string Platform,
                                           string SolutionConfig,
                                           bool Success)
        {
            bool scdProperty = false;
            string arch = string.Empty;
            EnvDTE.Project currentProject = VsProjectHelper.GetInstance.GetCurrentProjectFromUniqueName(Project);
            if (currentProject != null && VsProjectHelper.GetInstance.IsHaveTizenManifest(currentProject))
            {
                scdProperty = VsProjectHelper.GetInstance.GetSCDProperty(currentProject, SolutionConfig, out arch);
            }

            eventsHandler.OnVsEventBuildProjectDone(
                Project, ProjectConfig, Platform, SolutionConfig, Success, scdProperty, arch);
        }

        private void OnBuildBegin(vsBuildScope scope, vsBuildAction action)
        {
            VsProjectHelper projHelper = VsProjectHelper.GetInstance;
            bool isWebPrj = projHelper.IsTizenWebProject();
            bool isNativePrj = projHelper.IsTizenNativeProject();

            if (!isWebPrj && !isNativePrj)
                return;

            String workspacePath = projHelper.getSolutionFolderPath();
            String tag = projHelper.getTag(workspacePath, "build_type");
            string mode = projHelper.GetSolutionBuildConfig().ToLower();

            if (!tag.Equals(mode))
            {
                projHelper.UpdateYaml(workspacePath, "build_type:", mode);
            }

            //APIChecker.APICheckerCommand.Instance.RunBuildTime();
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            VsProjectHelper prjHelperInstance = VsProjectHelper.GetInstance;
            EnvDTE.Project prj = prjHelperInstance.GetCurProjectFromHierarchy(pHierarchy);
            bool isTizenproj = prjHelperInstance.IsHaveTizenManifest(prj);
            startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() / 1000;
            if (isTizenproj)
            {
                RemoteLogger.logAccess("Tizen.VSWin");
            }
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        #endregion

        public int OnSelectionChanged(IVsHierarchy pHierOld, uint itemidOld, IVsMultiItemSelect pMISOld, ISelectionContainer pSCOld, IVsHierarchy pHierNew, uint itemidNew, IVsMultiItemSelect pMISNew, ISelectionContainer pSCNew)
        {
            return VSConstants.S_OK;
        }

        public int OnElementValueChanged(uint elementid, object varValueOld, object varValueNew)
        {
            if (elementid == (uint)VSConstants.VSSELELEMID.SEID_StartupProject)
            {
                // Update working folder for web project
                VsProjectHelper projHelp = VsProjectHelper.GetInstance;
                String workspacePath = projHelp.getSolutionFolderPath();


                /* 1.) Check for Workspace path
                   2.) Check if the Solution is creation from TZ API.
                */
                if (workspacePath == null)
                    return VSConstants.S_OK;
                else if (!File.Exists(Path.Combine(workspacePath, "tizen_workspace.yaml")))
                    return VSConstants.S_OK;

                //get active project name
                var dte = Package.GetGlobalService(typeof(_DTE)) as DTE2;
                Array startupPojects = dte?.Solution?.SolutionBuild?.StartupProjects as Array;

                String projectName = startupPojects?.Length > 0 ? startupPojects.GetValue(0) as String : "";
                if ("".Equals(projectName))
                    return VSConstants.S_OK;

                Projects ListOfProjectsInSolution = dte?.Solution?.Projects;
                String projectFolder = null;

                foreach (EnvDTE.Project project in ListOfProjectsInSolution)
                {
                    try
                    {
                        if (projectName.Equals(project.UniqueName))
                            projectFolder = Path.GetDirectoryName(project.FullName);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Tizen: " + e.Message);
                        return VSConstants.S_OK;
                    }
                }

                projHelp.UpdateBuildForProjects(projectName);

                // Set/Update the workspace folder for debugging
                projHelp.UpdateYaml(workspacePath, "working_folder:", projectFolder);
            }
            return VSConstants.S_OK;
        }

        public int OnCmdUIContextChanged(uint dwCmdUICookie, int fActive)
        {
            return VSConstants.S_OK;
        }

    }
}
