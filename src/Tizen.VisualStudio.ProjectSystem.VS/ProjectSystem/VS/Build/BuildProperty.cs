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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tizen.VisualStudio.ProjectSystem.VS.Build
{
    /// <summary>
    ///     Contains common MSBuild build properties.
    /// </summary>
    internal static class BuildProperty
    {
        /// <summary>
        ///     Indicates Author certificate file path.
        /// </summary>
        public static string AuthorPath = nameof(AuthorPath);

        /// <summary>
        ///     Indicates Author certificate file password.
        /// </summary>
        public static string AuthorPass = nameof(AuthorPass);

        /// <summary>
        ///     Indicates Distributor certificate file path.
        /// </summary>
        public static string DistributorPath = nameof(DistributorPath);

        /// <summary>
        ///     Indicates Distributor certificate file password.
        /// </summary>
        public static string DistributorPass = nameof(DistributorPass);
    }

}
