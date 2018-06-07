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
using System.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace NetCore.Profiler.Extension.VSPackage
{
    public class SolutionListener : IVsSolutionEvents3, IVsSolutionEvents4, IDisposable
    {
        public IServiceProvider ServiceProvider { get; private set; }

        public IVsSolution Solution { get; private set; }

        private uint _eventsCookie;
        private bool _isDisposed;
        private static volatile object _mutex = new object();

        public SolutionListener(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;

            Solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            Debug.Assert(Solution != null, "Could not get the IVsSolution object");
            if (Solution == null)
            {
                throw new InvalidOperationException();
            }
        }

        public Func<object,int> AfterCloseSolution { get; set; }

        public Func<IVsHierarchy,int> AfterClosingChildren { get; set; }

        public Func<IVsHierarchy, IVsHierarchy, int> AfterLoadProject { get; set; }

        public Func<object, int> AfterMergeSolution {get; set; }

        public Func<IVsHierarchy, int, int> AfterOpenProject { get; set; }

        public Func<object, int, int> AfterOpenSolution { get; set; }

        public Func<IVsHierarchy, int> AfterOpeningChildren { get; set; }

        public Func<IVsHierarchy, int, int> BeforeCloseProject { get; set; }

        public Func<object, int> BeforeCloseSolution { get; set; }

        public Func<IVsHierarchy, int> BeforeClosingChildren { get; set; }

        public Func<IVsHierarchy, int> BeforeOpeningChildren { get; set; }

        public Func<IVsHierarchy, IVsHierarchy, int> BeforeUnloadProject { get; set; }


        //============================
        public int OnAfterCloseSolution(object reserved)
        {
            return AfterCloseSolution?.Invoke(reserved) ?? VSConstants.S_OK;
        }

        public int OnAfterClosingChildren(IVsHierarchy hierarchy)
        {
            return AfterClosingChildren?.Invoke(hierarchy) ?? VSConstants.S_OK;

        }

        public int OnAfterLoadProject(IVsHierarchy stubHierarchy, IVsHierarchy realHierarchy)
        {
            return AfterLoadProject?.Invoke(stubHierarchy, realHierarchy) ?? VSConstants.S_OK;

        }

        public int OnAfterMergeSolution(object pUnkReserved)
        {
            return AfterMergeSolution?.Invoke(pUnkReserved) ?? VSConstants.S_OK;

        }

        public int OnAfterOpenProject(IVsHierarchy hierarchy, int added)
        {
            return AfterOpenProject?.Invoke(hierarchy, added) ?? VSConstants.S_OK;

        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return AfterOpenSolution?.Invoke(pUnkReserved,fNewSolution) ?? VSConstants.S_OK;

        }

        public int OnAfterOpeningChildren(IVsHierarchy hierarchy)
        {
            return AfterOpeningChildren?.Invoke(hierarchy) ?? VSConstants.S_OK;

        }

        public int OnBeforeCloseProject(IVsHierarchy hierarchy, int removed)
        {
            return BeforeCloseProject?.Invoke(hierarchy, removed) ?? VSConstants.S_OK;

        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return BeforeCloseSolution?.Invoke(pUnkReserved) ?? VSConstants.S_OK;

        }

        public int OnBeforeClosingChildren(IVsHierarchy hierarchy)
        {
            return BeforeClosingChildren?.Invoke(hierarchy) ?? VSConstants.S_OK;

        }

        public int OnBeforeOpeningChildren(IVsHierarchy hierarchy)
        {
            return BeforeOpeningChildren?.Invoke(hierarchy) ?? VSConstants.S_OK;

        }

        public int OnBeforeUnloadProject(IVsHierarchy realHierarchy, IVsHierarchy rtubHierarchy)
        {
            return BeforeUnloadProject?.Invoke(realHierarchy, rtubHierarchy) ?? VSConstants.S_OK;

        }

        public int OnQueryCloseProject(IVsHierarchy hierarchy, int removing, ref int cancel)
        {
            return VSConstants.S_OK;

        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int cancel)
        {
            return VSConstants.S_OK;

        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int cancel)
        {
            return VSConstants.S_OK;

        }

        public int OnAfterAsynchOpenProject(IVsHierarchy hierarchy, int added)
        {
            return VSConstants.S_OK;

        }

        public int OnAfterChangeProjectParent(IVsHierarchy hierarchy)
        {
            return VSConstants.S_OK;

        }

        public int OnAfterRenameProject(IVsHierarchy hierarchy)
        {
            return VSConstants.S_OK;

        }

        public int OnQueryChangeProjectParent(IVsHierarchy hierarchy, IVsHierarchy newParentHier, ref int cancel)
        {
            return VSConstants.S_OK;

        }

        public virtual void Initialize()
        {
            if (Solution != null && _eventsCookie == 0)
            {
                ErrorHandler.ThrowOnFailure(Solution.AdviseSolutionEvents(this, out _eventsCookie));
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                lock (_mutex)
                {
                    if (disposing && Solution != null && _eventsCookie != 0)
                    {
                        ErrorHandler.ThrowOnFailure(Solution.UnadviseSolutionEvents(_eventsCookie));
                        _eventsCookie = 0;
                    }

                    _isDisposed = true;
                }
            }
        }
    }
}
