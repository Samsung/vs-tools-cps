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


using Microsoft.VisualStudio;

namespace Tizen.VisualStudio.ProjectSystem.VS.Extensibility
{
    /// <summary>
    ///     Represents an ITEMID in an IVsHierarchy.
    /// </summary>
    internal struct HierarchyId
    {
        private readonly uint _id;

        /// <summary>
        ///     Represents the root of a project hierarchy and is used to identify the entire hierarchy, as opposed
        ///     to a single item.
        /// </summary>
        public static readonly HierarchyId Root = new HierarchyId(VSConstants.VSITEMID_ROOT);

        /// <summary>
        ///     Represents the currently selected items, which can include the root of the hierarchy.
        /// </summary>
        public static readonly HierarchyId Selection = new HierarchyId(VSConstants.VSITEMID_SELECTION);

        /// <summary>
        ///     Represents the absence of a project item. This value is used when there is no current selection.
        /// </summary>
        public static readonly HierarchyId Nil = new HierarchyId(VSConstants.VSITEMID_NIL);

        /// <summary>
        ///     Represent an empty item.
        /// </summary>
        public static readonly HierarchyId Empty = new HierarchyId(0);

        public HierarchyId(uint id)
        {
            _id = id;
        }

        /// <summary>
        ///     Returns the underlying ITEMID.
        /// </summary>
        public uint Id
        {
            get { return _id; }
        }

        /// <summary>
        ///     Returns a value indicating if the <see cref="HierarchyId"/> represents the root of a project hierarchy
        ///     and is used to identify the entire hierarchy, as opposed to a single item.
        /// </summary>
        public bool IsRoot
        {
            get { return _id == Root.Id; }
        }

        /// <summary>
        ///     Returns a value indicating if the <see cref="HierarchyId"/> represents the currently selected items,
        ///     which can include the root of the hierarchy.
        /// </summary>
        public bool IsSelection
        {
            get { return _id == Selection.Id; }
        }

        /// <summary>
        ///    Returns a value indicating if the <see cref="HierarchyId"/> is empty.
        /// </summary>
        public bool IsEmpty
        {
            get { return _id == Empty.Id; }
        }

        /// <summary>
        ///    Returns a value indicating if the <see cref="HierarchyId"/> is <see cref="IsNil"/> or
        ///    <see cref="IsEmpty"/>.
        /// </summary>
        public bool IsNilOrEmpty
        {
            get { return IsNil || IsEmpty; }
        }

        /// <summary>
        ///    Returns a value indicating if the <see cref="HierarchyId"/> represents the absence of a project item.
        ///    This value is used when there is no current selection.
        /// </summary>
        public bool IsNil
        {
            get { return _id == Nil.Id; }
        }

        public static implicit operator uint(HierarchyId id)
        {
            return id.Id;
        }

        public static implicit operator HierarchyId(uint id)
        {
            return new HierarchyId(id);
        }
    }
}
