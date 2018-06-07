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

namespace Tizen.VisualStudio
{
    public class VsEvents
        : IVsSolutionEvents
    {
        private static VsEvents instance = null;

        private IVsEventsHandler eventsHandler = null;
        private IVsSolution solution = null;

        private uint solutionEventsCookie = 0;
        private Events dteEvents = null;
        private BuildEvents buildEvents;
        private DTE dte = null;

        public static void Initialize(IVsEventsHandler eventsHandler,
                                      IVsSolution solution)
        {
            if (eventsHandler == null)
            {
                throw new ArgumentNullException("eventsHandler");
            }

            if (solution == null)
            {
                throw new ArgumentNullException("solution");
            }

            VsEvents.instance = new VsEvents(eventsHandler, solution);

            instance.addBuildListners();
        }

        public static void Dispose()
        {
            if (VsEvents.instance != null)
            {
                VsEvents.instance.Release();
                VsEvents.instance = null;
            }
        }

        private VsEvents(IVsEventsHandler eventsHandler, IVsSolution solution)
        {
            this.eventsHandler = eventsHandler;
            this.solution = solution;
            this.solution.AdviseSolutionEvents(this,
                                               out solutionEventsCookie);
            this.dte = Package.GetGlobalService(typeof(DTE)) as DTE;
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
        }

        // Solution ------------------------------------------------------------
        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
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
                //this.buildEvents.OnBuildBegin -= OnBuildBegin;
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
            APIChecker.APICheckerCommand.Instance.RunBuildTime();
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        #endregion
    }
}
