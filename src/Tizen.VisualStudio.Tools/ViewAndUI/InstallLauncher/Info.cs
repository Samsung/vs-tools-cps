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

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Tizen.VisualStudio.InstallLauncher
{
    public class Info : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _NewPath = true;
        public bool NewPath
        {
            get => _NewPath;
            set
            {
                _NewPath = value;
                OnPropertyChanged();
            }
        }

        private bool _ExistPath;
        public bool ExistPath
        {
            get => _ExistPath;
            set
            {
                _ExistPath = value;
                OnPropertyChanged();
            }
        }

        private string _Path;
        public string Path
        {
            get => _Path;
            set
            {
                _Path = value;
                OnPropertyChanged();
            }
        }

        private bool _ValidatePath = false;
        public bool ValidatePath
        {
            get => _ValidatePath;
            set
            {
                _ValidatePath = value;
                OnPropertyChanged();
            }
        }

        private int _ProgressDown = 0;
        public int ProgressDown
        {
            get => _ProgressDown;
            set
            {
                _ProgressDown = value;
                OnPropertyChanged();
            }
        }

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
