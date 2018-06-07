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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem.VS.Properties;
using Microsoft.VisualStudio.ProjectSystem;
using Tizen.VisualStudio.ProjectSystem;

namespace Tizen.VisualStudio.ProjectSystem.VS.PropertyPages
{
    [Export(typeof(IVsProjectDesignerPageProvider))]
    [AppliesTo(MyUnconfiguredProject.UniqueCapability)]
    internal class TizenProjectDesignerPageProvider : IVsProjectDesignerPageProvider
    {
        private readonly IProjectCapabilitiesService _capabilitites;

        [ImportingConstructor]
        internal TizenProjectDesignerPageProvider(IProjectCapabilitiesService capabilities)
        {
            _capabilitites = capabilities;
        }

        public Task<IReadOnlyCollection<IPageMetadata>> GetPagesAsync()
        {
            var builder = ImmutableArray.CreateBuilder<IPageMetadata>();

            builder.Add(TizenProjectDesignerPage.Tizen);

            return Task.FromResult<IReadOnlyCollection<IPageMetadata>>(builder.ToImmutable());
        }
    }
}
