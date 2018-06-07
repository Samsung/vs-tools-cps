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

using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem;

namespace Tizen.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides file and folder operations that operate on the physical <see cref="IProjectTree"/>.
    /// </summary>
    /// <remarks>
    ///     This interface provides a simple facade over the <see cref="IFileSystem"/>, <see cref="IFolderManager"/>, 
    ///     <see cref="IProjectTreeService"/> and <see cref="IProjectItemProvider"/> interfaces.
    /// </remarks>
    internal interface IPhysicalProjectTreeStorage
    {
        /// <summary>
        ///     Creates a folder on disk, adding it add to the physical project tree.
        /// </summary>
        /// <param name="path">
        ///     The path of the folder to create, can be relative to the project directory.
        /// </param>
        /// <returns>
        ///     The created <see cref="IProjectTree"/>.
        /// </returns>
        /// <remarks>
        ///     This method will automatically publish the resulting tree to <see cref="IProjectTreeService.CurrentTree"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="path"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="path"/> is an empty string (""), contains only white space, or contains one or more invalid characters.
        ///     
        ///     <paramref name="path"/> is prefixed with, or contains, only a colon character (:).
        /// </exception>
        /// <exception cref="IOException">
        ///     The directory specified by <paramref name="path"/> is a file.
        ///     <para>
        ///         -or-
        ///     </para>
        ///     The network name is not known.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        ///     The caller does not have the required permission.
        /// </exception>
        /// <exception cref="PathTooLongException"> 
        ///     The specified path, file name, or both exceed the system-defined maximum length.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        ///     The specified path is invalid (for example, it is on an unmapped drive).
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///     <paramref name="path"/> contains a colon character (:) that is not part of a drive label ("C:\").
        /// </exception>
        Task<IProjectTree> CreateFolderAsync(string path);
    }
}