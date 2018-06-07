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


namespace Tizen.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides methods for querying and testing the current project's capabilities.
    /// </summary>
    internal interface IProjectCapabilitiesService
    {   // This interface introduced just so that we can mock checks for capabilities, 
        // to avoid static state and call context data that we cannot influence

        /// <summary>
        ///     Returns a value indicating whether the current project has the specified capability
        /// </summary>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="capability"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     <paramref name="capability"/> is an empty string ("").
        /// </exception>
        bool Contains(string capability);
    }
}
