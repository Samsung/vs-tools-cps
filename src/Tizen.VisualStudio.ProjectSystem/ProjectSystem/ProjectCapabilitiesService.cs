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

using Microsoft;
using Microsoft.VisualStudio.ProjectSystem;
using System.ComponentModel.Composition;

namespace Tizen.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides an implementation of <see cref="IProjectCapabilitiesService"/> that simply delegates 
    ///     onto the <see cref="CapabilitiesExtensions.Contains(IProjectCapabilitiesScope, string)"/> method.
    /// </summary>
    [Export(typeof(IProjectCapabilitiesService))]
    internal class ProjectCapabilitiesService : IProjectCapabilitiesService
    {
        private readonly UnconfiguredProject _project;

        [ImportingConstructor]
        public ProjectCapabilitiesService(UnconfiguredProject project)
        {
            Requires.NotNull(project, nameof(project));

            _project = project;
        }

        public bool Contains(string capability)
        {
            Requires.NotNullOrEmpty(capability, nameof(capability));

            // Just to check capabilities, requires static state and call context that we cannot influence
            return _project.Capabilities.Contains(capability);
        }
    }
}
