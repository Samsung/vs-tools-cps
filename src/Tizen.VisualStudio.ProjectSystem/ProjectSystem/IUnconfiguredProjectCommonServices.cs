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

using Microsoft.VisualStudio.ProjectSystem;

namespace Tizen.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides access to common project services provided by the <see cref="UnconfiguredProject"/>.
    /// </summary>
    internal interface IUnconfiguredProjectCommonServices
    {
        /// <summary>
        ///     Gets the <see cref="IProjectThreadingService"/> for the current <see cref="UnconfiguredProject"/>.
        /// </summary>
        IProjectThreadingService ThreadingService
        {
            get;
        }

        /// <summary>
        ///     Gets the current <see cref="UnconfiguredProject"/>.
        /// </summary>
        UnconfiguredProject Project
        {
            get;
        }

        /// <summary>
        ///     Gets physical project tree in Solution Explorer.
        /// </summary>
        IPhysicalProjectTree ProjectTree
        {
            get;
        }

        /// <summary>
        ///     Gets the current active <see cref="ConfiguredProject"/>.
        /// </summary>
        ConfiguredProject ActiveConfiguredProject
        {
            get;
        }

        /// <summary>
        ///     Gets the <see cref="ProjectProperties"/> of the currently active configured project.
        /// </summary>
        ProjectProperties ActiveConfiguredProjectProperties
        {
            get;
        }

        /// <summary>
        ///     Gets the project lock service     
        /// </summary>
        IProjectLockService ProjectLockService
        {
            get;
        }
    }
}
