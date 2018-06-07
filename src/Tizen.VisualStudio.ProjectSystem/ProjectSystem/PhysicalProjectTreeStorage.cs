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

using Microsoft;
using Microsoft.VisualStudio.ProjectSystem;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Tizen.VisualStudio.IO;

namespace Tizen.VisualStudio.ProjectSystem
{
    [Export(typeof(IPhysicalProjectTreeStorage))]
    internal class PhysicalProjectTreeStorage : IPhysicalProjectTreeStorage
    {
        private readonly Lazy<IFileSystem> _fileSystem;
        private readonly ActiveConfiguredProject<IFolderManager> _folderManager;
        private readonly Lazy<IProjectTreeService> _treeService;
        private readonly Lazy<IProjectTreeProvider> _treeProvider;
        private readonly UnconfiguredProject _unconfiguredProject;

        [ImportingConstructor]
        public PhysicalProjectTreeStorage([Import(ExportContractNames.ProjectTreeProviders.PhysicalProjectTreeService)]Lazy<IProjectTreeService> treeService,
                                          [Import(ExportContractNames.ProjectTreeProviders.PhysicalViewTree)]Lazy<IProjectTreeProvider> treeProvider,
                                          Lazy<IFileSystem> fileSystem,
                                          ActiveConfiguredProject<IFolderManager> folderManager,
                                          UnconfiguredProject unconfiguredProject)
        {
            Requires.NotNull(fileSystem, nameof(fileSystem));
            Requires.NotNull(folderManager, nameof(folderManager));
            Requires.NotNull(treeService, nameof(treeService));
            Requires.NotNull(treeProvider, nameof(treeProvider));
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));

            _treeService = treeService;
            _treeProvider = treeProvider;
            _fileSystem = fileSystem;
            _folderManager = folderManager;
            _unconfiguredProject = unconfiguredProject;
        }

        public Task<IProjectTree> CreateFolderAsync(string path)
        {
            Requires.NotNullOrEmpty(path, nameof(path));

            if (_treeService.Value.CurrentTree == null)
            {
                throw new InvalidOperationException("Physical project tree has not yet been published.");
            }

            string fullPath = _unconfiguredProject.MakeRooted(path);

            return AddToProjectAsync(fullPath);
        }

        private async Task<IProjectTree> AddToProjectAsync(string fullPath)
        {
            _fileSystem.Value.CreateDirectory(fullPath);

            await _folderManager.Value.IncludeFolderInProjectAsync(fullPath, recursive: false)
                                  .ConfigureAwait(false);

            await _treeService.Value.PublishLatestTreeAsync()
                                    .ConfigureAwait(false);

            return _treeProvider.Value.FindByPath(_treeService.Value.CurrentTree.Tree, fullPath);
        }
    }
}
