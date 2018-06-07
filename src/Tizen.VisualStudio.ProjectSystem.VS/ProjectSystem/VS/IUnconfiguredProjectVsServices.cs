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

using Microsoft.VisualStudio.Shell.Interop;

namespace Tizen.VisualStudio.ProjectSystem.VS
{
    /// <summary>
    ///     Provides access to common Visual Studio project services provided by the <see cref="UnconfiguredProject"/>.
    /// </summary>
    internal interface IUnconfiguredProjectVsServices : IUnconfiguredProjectCommonServices
    {
        /// <summary>
        ///     Gets <see cref="IVsHierarchy"/> provided by the <see cref="UnconfiguredProject"/>.
        /// </summary>
        IVsHierarchy VsHierarchy
        {
            get;
        }

        /// <summary>
        ///     Gets <see cref="IVsProject4"/> provided by the <see cref="UnconfiguredProject"/>.
        /// </summary>
        IVsProject4 VsProject
        {
            get;
        }
    }
}
