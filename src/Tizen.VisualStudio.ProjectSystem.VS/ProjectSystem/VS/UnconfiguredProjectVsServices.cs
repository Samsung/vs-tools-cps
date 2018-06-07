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
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft;
using Microsoft.VisualStudio.ProjectSystem;
using Tizen.VisualStudio.ProjectSystem;
using Tizen.VisualStudio.ProjectSystem.VS;

namespace Tizen.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides an implementation of <see cref="IUnconfiguredProjectVsServices"/> that delegates onto
    ///     it's <see cref="IUnconfiguredProjectServices.HostObject"/> and underlying <see cref="IUnconfiguredProjectCommonServices"/>.
    /// </summary>
    [Export(typeof(IUnconfiguredProjectVsServices))]
    internal class UnconfiguredProjectVsServices : IUnconfiguredProjectVsServices
    {
        private readonly IUnconfiguredProjectCommonServices _commonServices;

        [ImportingConstructor]
        public UnconfiguredProjectVsServices(IUnconfiguredProjectCommonServices commonServices)
        {
            Requires.NotNull(commonServices, nameof(commonServices));

            _commonServices = commonServices;
        }

        public IVsHierarchy VsHierarchy
        {
            get { return (IVsHierarchy)_commonServices.Project.Services.HostObject; }
        }

        public IVsProject4 VsProject
        {
            get { return (IVsProject4)_commonServices.Project.Services.HostObject; }
        }

        public IProjectThreadingService ThreadingService
        {
            get { return _commonServices.ThreadingService; }
        }

        public UnconfiguredProject Project
        {
            get { return _commonServices.Project; }
        }

        public IPhysicalProjectTree ProjectTree
        {
            get { return _commonServices.ProjectTree; }
        }

        public ConfiguredProject ActiveConfiguredProject
        {
            get { return _commonServices.ActiveConfiguredProject; }
        }

        public ProjectProperties ActiveConfiguredProjectProperties
        {
            get { return _commonServices.ActiveConfiguredProjectProperties; }
        }

        public IProjectLockService ProjectLockService
        {
            get { return _commonServices.ProjectLockService; }
        }
    }
}
