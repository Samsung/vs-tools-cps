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
    ///     Represents the physical project tree in Solution Explorer.
    /// </summary>
    internal interface IPhysicalProjectTree
    {
        /// <summary>
        ///     Gets the service that provides file and folder operations that operate on the physical <see cref="IProjectTree"/>.
        /// </summary>
        IPhysicalProjectTreeStorage TreeStorage
        {
            get;
        }

        /// <summary>
        ///     Gets the most recently published tree, or <see langword="null"/> if it has not yet be published.
        /// </summary>
        IProjectTree CurrentTree
        {
            get;
        }

        /// <summary>
        ///     Gets the service that manages the tree in Solution Explorer.
        /// </summary>
        IProjectTreeService TreeService
        {
            get;
        }

        /// <summary>
        ///     Gets the project tree provider that creates the Solution Explorer tree.
        /// </summary>
        IProjectTreeProvider TreeProvider
        {
            get;
        }
    }
}
