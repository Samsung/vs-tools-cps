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

using System;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.ProjectSystem;

namespace Tizen.VisualStudio.ProjectSystem.VS.Extensibility
{
    /// <summary>
    /// MEF component which has methods for consumers to get to project specific MEF exports
    /// </summary>
    [Export(typeof(IProjectExportProvider))]
    [AppliesTo(MyUnconfiguredProject.UniqueCapability)]
    internal class ProjectExportProvider : IProjectExportProvider
    {
        private IProjectServiceAccessor ProjectServiceAccesspr { get; }

        [ImportingConstructor]
        public ProjectExportProvider(IProjectServiceAccessor serviceAccessor)
        {
            ProjectServiceAccesspr = serviceAccessor;
        }

        /// <summary>
        /// Returns the export for the given project without having to go to the
        /// UI thread. This is the preferred method for getting access to project specific
        /// exports
        /// </summary>
        public T GetExport<T>(string projectFilePath) where T : class
        {
            if (string.IsNullOrEmpty(projectFilePath))
            {
                throw new ArgumentNullException(nameof(projectFilePath));
            }

            var projectService = ProjectServiceAccesspr.GetProjectService();
            if (projectService == null)
            {
                return null;
            }

            var unconfiguredProject = projectService.LoadedUnconfiguredProjects
                                                    .FirstOrDefault(x => x.FullPath.Equals(projectFilePath,
                                                                            StringComparison.OrdinalIgnoreCase));
            return unconfiguredProject?.Services.ExportProvider.GetExportedValueOrDefault<T>();
        }
    }
}
