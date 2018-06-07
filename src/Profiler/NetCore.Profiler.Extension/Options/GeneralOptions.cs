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
using NetCore.Profiler.Extension.Common;

namespace NetCore.Profiler.Extension.Options
{
    public class GeneralOptions : NotifyPropertyChanged
    {

        private readonly SettingsStore _settingsStore;
        
        public int SleepTime
        {
            get { return _sleepTime; }
            set { SetProperty(ref _sleepTime, value); }
        }

        private int _sleepTime = 10;


        public bool BatchMode
        { 
            get { return _batchMode; }
            set { SetProperty(ref _batchMode, value); }
        }

        private bool _batchMode = true;

        public GeneralOptions(SettingsStore settingsStore)
        {
            if (settingsStore == null)
            {
                throw new ArgumentNullException(nameof(settingsStore));
            }

            _settingsStore = settingsStore;
            LoadSettings();
        }

        public void LoadSettings()
        {
            SleepTime = _settingsStore.GetInt32("SleepTime");
            BatchMode = _settingsStore.GetBoolean("BatchMode");
        }

        public void SaveSettings()
        {
            _settingsStore.SetInt32("SleepTime", SleepTime);
            _settingsStore.SetBoolean("BatchMode", BatchMode);
        }
    }
}
