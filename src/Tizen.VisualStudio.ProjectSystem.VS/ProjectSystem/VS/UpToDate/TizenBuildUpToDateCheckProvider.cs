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

using Microsoft;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Tizen.VisualStudio;

namespace Tizen.VisualStudio.ProjectSystem.VS.UpToDate
{
    // Check tizen-manifest.xml is upToDate
    // https://github.com/Microsoft/VSProjectSystem/blob/master/doc/extensibility/IBuildUpToDateCheckProvider.md
    [Export(typeof(IBuildUpToDateCheckProvider))]
    [ExportMetadata("BeforeDrainCriticalTasks", true)] // Optional, default value is false
    [AppliesTo(MyUnconfiguredProject.UniqueCapability)]
    public class TizenBuildUpToDateCheckProvider : IBuildUpToDateCheckProvider
    {
        private ConfiguredProject _configuredProject;
        private string _msBuildProjectName;
        private string _msBuildProjectDirectory;
        private string _msBuildProjectFullPath;
        private DateTime _prevWriteTime = DateTime.MinValue;

        [ImportingConstructor]
        public TizenBuildUpToDateCheckProvider(ConfiguredProject configuredProject)
        {
            _configuredProject = configuredProject;

            _msBuildProjectFullPath = _configuredProject.UnconfiguredProject.FullPath;

            _msBuildProjectName = Path.GetFileNameWithoutExtension(_msBuildProjectFullPath);

            _msBuildProjectDirectory = Path.GetDirectoryName(_msBuildProjectFullPath);
        }

        /// <summary>
        /// Check if project tizen-manifest.xml is up-to-date (i.e there is no need to build)
        /// </summary>
        /// <param name="buildAction">The build action to perform.</param>
        /// <param name="logger">A logger that may be used to write out status or information messages regarding the up-to-date check.</param>
        /// <param name="cancellationToken">A token that is cancelled if the caller loses interest in the result.</param>
        /// <returns>A task whose result is true if project is up-to-date</returns>
        public Task<bool> IsUpToDateAsync(BuildAction buildAction, TextWriter logger, CancellationToken cancellationToken = default(CancellationToken))
        {
            var configFilePath = Path.Combine(_msBuildProjectDirectory, "config.xml");
            var hasConfigFile = File.Exists(configFilePath);
            if (hasConfigFile)
                return Task.FromResult(false); //build will be handled by tizen-core

            var manifestFilePath = Path.Combine(_msBuildProjectDirectory, "tizen-manifest.xml");
            var hasManifestFile = File.Exists(manifestFilePath);
            var lastWritetime = hasManifestFile ? File.GetLastWriteTimeUtc(manifestFilePath) : DateTime.MinValue;

            var isUpToDate = (!hasManifestFile) || (hasManifestFile && _prevWriteTime != DateTime.MinValue && lastWritetime == _prevWriteTime);

            if (isUpToDate)
            {
                if (hasManifestFile)
                {
                    logger.WriteLineAsync($"FastUpToDateForTizen:  '{manifestFilePath}' ({_msBuildProjectName})");
                }
                else
                {
                    logger.WriteLineAsync($"FastUpToDateForTizen:  '{manifestFilePath}' was not found. skip to check ... ({_msBuildProjectName})");
                }
            }

            if (lastWritetime > _prevWriteTime)
            {
                _prevWriteTime = lastWritetime;
            }

            return Task.FromResult(isUpToDate);
        }

        /// <summary>
        /// Gets a value indicating whether the up-to-date check is available at the moment.
        /// </summary>
        /// <param name="cancellationToken">A token that is cancelled if the caller loses interest in the result.</param>
        /// <returns>A task whose result is <c>true</c> if the up-to-date check is enabled.</returns>
        public Task<bool> IsUpToDateCheckEnabledAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(true); // check every time
        }
    }
}
