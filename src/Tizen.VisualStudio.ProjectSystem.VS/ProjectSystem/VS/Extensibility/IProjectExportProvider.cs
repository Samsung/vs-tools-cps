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



namespace Tizen.VisualStudio.ProjectSystem.VS.Extensibility
{
    /// <summary>
    /// Interface definition for global scope VS MEF component, which helps to get MEF exports from a
    /// project level scope given IVsHierarchy or project file path.
    /// </summary>
    public interface IProjectExportProvider
    {
        /// <summary>
        /// Returns the export for the given project without having to go to the 
        /// UI thread. This is the preferred method for getting access to project specific
        /// exports
        /// </summary>
        T GetExport<T>(string projectFilePath) where T : class;
    }
}
