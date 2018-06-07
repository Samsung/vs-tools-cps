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
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Build;
using System.Security.Cryptography;
using System.IO;
using Tizen.VisualStudio.OptionPages;
using Tizen.VisualStudio.Tools.Data;

namespace Tizen.VisualStudio.ProjectSystem.VS.Build
{
    [Export(typeof(IProjectGlobalPropertiesProvider))]
    [AppliesTo(MyUnconfiguredProject.UniqueCapability)]
    internal class CertificatePropertiesProvider : StaticGlobalPropertiesProviderBase
    {
        [ImportingConstructor]
        internal CertificatePropertiesProvider(IProjectService projectService)
            : base(projectService.Services)
        {
        }

        public override Task<IImmutableDictionary<string, string>> GetGlobalPropertiesAsync(CancellationToken cancellationToken)
        {
            CertificateInfo info = Certificate.CheckValidCertificate();

            if (info == null ||
                String.IsNullOrEmpty(info.AuthorCertificateFile) ||
                String.IsNullOrEmpty(info.AuthorPassword) ||
                String.IsNullOrEmpty(info.DistributorCertificateFile) ||
                String.IsNullOrEmpty(info.DistributorPassword) ||
                !File.Exists(info.AuthorCertificateFile) ||
                !File.Exists(info.DistributorCertificateFile))
            {
                /// TODO ::
                /// Need to show warning & error message to outputpane window
                return Task.FromResult<IImmutableDictionary<string, string>>(Empty.PropertiesMap);
            }

            IImmutableDictionary<string, string> properties = Empty.PropertiesMap
                .Add(BuildProperty.AuthorPath, info.AuthorCertificateFile)
                .Add(BuildProperty.AuthorPass, CertificateEncDec.Decrypt<AesManaged>(info.AuthorPassword))
                .Add(BuildProperty.DistributorPath, info.DistributorCertificateFile)
                .Add(BuildProperty.DistributorPass, CertificateEncDec.Decrypt<AesManaged>(info.DistributorPassword));

            return Task.FromResult<IImmutableDictionary<string, string>>(properties);
        }
    }
}
