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
using System.Diagnostics;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;

namespace NetCore.Profiler.Extension.Options
{
    public class SettingsStore
    {
        public string CollectionPath { get; private set; }

        private readonly WritableSettingsStore _settingsStore;

        public SettingsStore(IServiceProvider serviceProvider, string collectionPath)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (string.IsNullOrWhiteSpace(collectionPath))
            {
                throw new ArgumentNullException(nameof(collectionPath));
            }

            CollectionPath = collectionPath;

            _settingsStore = new ShellSettingsManager(serviceProvider).GetWritableSettingsStore(SettingsScope.UserSettings);

            if (_settingsStore != null && !_settingsStore.CollectionExists(CollectionPath))
            {
                _settingsStore.CreateCollection(CollectionPath);
            }
        }

        public void DeleteCollection()
        {
            if (_settingsStore != null && _settingsStore.CollectionExists(CollectionPath))
            {
                _settingsStore.DeleteCollection(CollectionPath);
            }
        }

        public bool GetBoolean(string propertyName)
        {
            try
            {
                if (_settingsStore.PropertyExists(CollectionPath, propertyName))
                {
                    return _settingsStore.GetBoolean(CollectionPath, propertyName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return false;
        }

        public int GetInt32(string propertyName)
        {
            try
            {
                if (_settingsStore.PropertyExists(CollectionPath, propertyName))
                {
                    return _settingsStore.GetInt32(CollectionPath, propertyName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return 0;
        }

        public string GetString(string propertyName)
        {
            try
            {
                if (_settingsStore.PropertyExists(CollectionPath, propertyName))
                {
                    return _settingsStore.GetString(CollectionPath, propertyName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return "";
        }

        public void SetBoolean(string propertyName, bool val)
        {
            try
            {
                _settingsStore.SetBoolean(CollectionPath, propertyName, val);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public void SetInt32(string propertyName, int val)
        {
            try
            {
                _settingsStore.SetInt32(CollectionPath, propertyName, val);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
