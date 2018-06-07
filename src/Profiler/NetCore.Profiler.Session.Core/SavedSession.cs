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

namespace NetCore.Profiler.Session.Core
{
    public class SavedSession : ISavedSession
    {

        private SessionProperties _properties;

        public string SolutionFolder { get; set; }

        public string ProjectFolder { get; set; }

        public string ProjectName { get; private set; }

        public string DeviceName { get; private set; }

        public string SessionFolder { get; private set; }

        public string SessionFile { get; set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime StartedAt { get; }

        public string PresetName { get; private set; }

        public string Annotation
        {
            get => Properties.GetProperty("Annotation", "value");
            set => Properties.SetProperty("Annotation", "value", value);
        }

        public ISessionProperties Properties => _properties;

        public void Load()
        {
            SessionFolder = Path.GetDirectoryName(SessionFile);

            if (SessionFolder == null || !File.Exists(Path.GetFullPath(SessionFile)))
            {
                throw new Exception("Session File does not exist");
            }


            _properties = new SessionProperties(SessionFile);
            _properties.Load();

            CreatedAt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                .AddMilliseconds(Convert.ToDouble(_properties.GetProperty("Time", "value")
                        .Replace(',', '.'), //Temporay Fix to read sessions created before changing the format
                    CultureInfo.InvariantCulture));

            PresetName = _properties.GetProperty("ProfilingType", "value");

            ProjectName = _properties.GetProperty("ProjectName", "value");

            var deviceName = _properties.GetProperty("DeviceName", "value");
            DeviceName = string.IsNullOrEmpty(deviceName) ? "<Unknown>" : deviceName;

            foreach (var property in new List<string> { "CoreClrProfilerReport", "CoreClrProfilerReport", "CtfReport", "Proc" })
            {
                if (!_properties.PropertyExists(property))
                {
                    throw new Exception($"{property} Session Property not found");
                }
            }

        }

    }
}
