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

using Microsoft;
using Microsoft.VisualStudio.ProjectSystem;
using System;
using System.ComponentModel.Composition;

namespace Tizen.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides a default implementation of <see cref="IUnconfiguredProjectCommonServices"/>.
    /// </summary>
    [Export(typeof(IUnconfiguredProjectCommonServices))]
    internal class UnconfiguredProjectCommonServices : IUnconfiguredProjectCommonServices
    {
        private readonly UnconfiguredProject _project;
        private readonly Lazy<IPhysicalProjectTree> _projectTree;
        private readonly Lazy<IProjectThreadingService> _threadingService;
        private readonly Lazy<IProjectLockService> _projectLockService;
        private readonly ActiveConfiguredProject<ConfiguredProject> _activeConfiguredProject;
        private readonly ActiveConfiguredProject<ProjectProperties> _activeConfiguredProjectProperties;

        [ImportingConstructor]
        public UnconfiguredProjectCommonServices(UnconfiguredProject project, Lazy<IPhysicalProjectTree> projectTree, Lazy<IProjectThreadingService> threadingService,
                                                 ActiveConfiguredProject<ConfiguredProject> activeConfiguredProject, ActiveConfiguredProject<ProjectProperties> activeConfiguredProjectProperties,
                                                 Lazy<IProjectLockService> projectLockService)
        {
            Requires.NotNull(project, nameof(project));
            Requires.NotNull(projectTree, nameof(projectTree));
            Requires.NotNull(threadingService, nameof(threadingService));
            Requires.NotNull(activeConfiguredProject, nameof(activeConfiguredProject));
            Requires.NotNull(activeConfiguredProjectProperties, nameof(activeConfiguredProjectProperties));
            Requires.NotNull(projectLockService, nameof(projectLockService));

            _project = project;
            _projectTree = projectTree;
            _threadingService = threadingService;
            _activeConfiguredProject = activeConfiguredProject;
            _activeConfiguredProjectProperties = activeConfiguredProjectProperties;
            _projectLockService = projectLockService;
        }

        public IProjectThreadingService ThreadingService
        {
            get { return _threadingService.Value; }
        }

        public UnconfiguredProject Project
        {
            get { return _project; }
        }

        public IPhysicalProjectTree ProjectTree
        {
            get { return _projectTree.Value; }
        }

        public ConfiguredProject ActiveConfiguredProject
        {
            get { return _activeConfiguredProject.Value; }
        }

        public ProjectProperties ActiveConfiguredProjectProperties
        {
            get { return _activeConfiguredProjectProperties.Value; }
        }

        public IProjectLockService ProjectLockService
        {
            get { return _projectLockService.Value; }
        }
    }
}
