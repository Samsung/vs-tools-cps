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
using System.Globalization;
using System.IO;
using NetCore.Profiler.Common;
using NetCore.Profiler.Common.Helpers;
using NetCore.Profiler.Session.Core;
using NetCore.Profiler.Extension.VSPackage;

namespace NetCore.Profiler.Extension.Session
{
    /// <summary>
    /// A base class representing a completed (saved previously) %Core %Profiler profiling or memory profiling
    /// session loaded for viewing in %UI components provided by the plugin.
    /// </summary>
    public abstract class BaseSession
    {
        public string ProjectFolder { get; set; } = "";

        public string SessionFolder { get; protected set; } = "";

        public string ProjectName { get; private set; }

        public string SessionFile { get; private set; }

        public DateTime CreatedAt { get; protected set; }

        public ISessionProperties Properties => _sessionProperties;

        protected SessionProperties _sessionProperties;

        public BaseSession(string path)
        {
            Load(path);
        }

        /// <summary>
        /// Load profiling data.
        /// </summary>
        private void Load(string path)
        {
            Initialize(path);

            var startTime = DateTime.Now;
            var lastTime = startTime;
            ulong cnt = 0;
            var progressMonitor = new ProgressMonitor()
            {
                Start = delegate
                {
                    ProfilerPlugin.Instance.SaveExplorerWindowCaption();
                    ProfilerPlugin.Instance.UpdateExplorerWindowProgress(0);
                },

                Stop = delegate
                {
                    ProfilerPlugin.Instance.RestoreExplorerWindowCaption();
                },

                Tick = delegate
                {
                    if (++cnt % 1000 == 0)
                    {
                        var now = DateTime.Now;
                        if ((now - lastTime).TotalSeconds >= 0.5)
                        {
                            ProfilerPlugin.Instance.UpdateExplorerWindowProgress((long)Math.Min(((now - startTime).TotalSeconds) * 5, 99));
                            lastTime = now;
                        }
                    }
                }
            };
            try
            {
                progressMonitor.Start();
                LoadData(progressMonitor);
            }
            finally
            {
                progressMonitor.Stop();
            }
        }

        /// <summary>
        /// Perform basic initialization. Check for file existance, read properties etc.
        /// </summary>
        /// <remarks>
        /// Trace Data is not loaded at this moment. It's done in <code>Load</code> method.
        /// </remarks>
        /// <param name="path">Path to the session properties file.</param>
        protected virtual void Initialize(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            SessionFolder = Path.GetDirectoryName(path);

            if (SessionFolder == null || !File.Exists(Path.GetFullPath(path)))
            {
                throw new Exception($"Session file {path} not found");
            }

            SessionFile = Path.GetFullPath(path);

            _sessionProperties = new SessionProperties(SessionFile);
            _sessionProperties.Load();

            CreatedAt = TimeStampHelper.UnixEpochTime
                .AddMilliseconds(Convert.ToDouble(
                    _sessionProperties.GetProperty("Time", "value")
                        .Replace(',', '.'), //Temporary fix to read sessions created before changing the format
                    CultureInfo.InvariantCulture));

            ProjectName = _sessionProperties.GetProperty("ProjectName", "value");

            foreach (var property in new List<string> { "CoreClrProfilerReport", "Proc" })
            {
                if (!_sessionProperties.PropertyExists(property))
                {
                    throw new Exception($"{property} session property not found");
                }
            }
        }

        protected abstract void LoadData(ProgressMonitor progressMonitor);

        protected string GetProfilerDataFileName()
        {
            string profilerReportDirectory = _sessionProperties.GetProperty("CoreClrProfilerReport", "path");
            if (string.IsNullOrEmpty(profilerReportDirectory))
            {
                throw new Exception("Invalid session directory");
            }

            return Path.Combine(
                SessionFolder,
                profilerReportDirectory,
                _sessionProperties.GetProperty("CoreClrProfilerReport", "name"));
        }
    }
}
