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

namespace Tizen.VisualStudio.ProjectSystem
{
    [Export(typeof(IPhysicalProjectTree))]
    internal class PhysicalProjectTree : IPhysicalProjectTree
    {
        private readonly Lazy<IProjectTreeService> _treeService;
        private readonly Lazy<IProjectTreeProvider> _treeProvider;
        private readonly Lazy<IPhysicalProjectTreeStorage> _treeStorage;

        [ImportingConstructor]
        public PhysicalProjectTree([Import(ExportContractNames.ProjectTreeProviders.PhysicalProjectTreeService)]Lazy<IProjectTreeService> treeService,
                                   [Import(ExportContractNames.ProjectTreeProviders.PhysicalViewTree)]Lazy<IProjectTreeProvider> treeProvider,
                                   Lazy<IPhysicalProjectTreeStorage> treeStorage)
        {
            Requires.NotNull(treeService, nameof(treeService));
            Requires.NotNull(treeProvider, nameof(treeProvider));
            Requires.NotNull(treeStorage, nameof(treeStorage));

            _treeService = treeService;
            _treeProvider = treeProvider;
            _treeStorage = treeStorage;
        }

        public IProjectTree CurrentTree
        {
            get { return _treeService.Value.CurrentTree?.Tree; }
        }

        public IProjectTreeService TreeService
        {
            get { return _treeService.Value; }
        }

        public IProjectTreeProvider TreeProvider
        {
            get { return _treeProvider.Value; }
        }

        public IPhysicalProjectTreeStorage TreeStorage
        {
            get { return _treeStorage.Value; }
        }
    }
}
