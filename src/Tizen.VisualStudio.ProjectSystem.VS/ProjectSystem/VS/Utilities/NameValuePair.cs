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



namespace Tizen.VisualStudio.ProjectSystem.VS.Utilities
{

    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using Tizen.VisualStudio.ProjectSystem.VS.PropertyPages;

    internal class NameValuePair : INotifyPropertyChanged, IDataErrorInfo
    {
        public ObservableList<NameValuePair> ParentCollection;
        public bool HasValidationError { get; set; }

        public NameValuePair(ObservableList<NameValuePair> parentCollection = null) { ParentCollection = parentCollection; }

        public NameValuePair(string name, string value, ObservableList<NameValuePair> parentCollection = null)
        {
            ParentCollection = parentCollection; Name = name; Value = value;
        }

        public NameValuePair(NameValuePair nvPair, ObservableList<NameValuePair> parentCollection = null)
        {
            ParentCollection = parentCollection; Name = nvPair.Name; Value = nvPair.Value;
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { NotifyPropertyChanged(ref _name, value); }
        }

        private string _value;
        public string Value
        {
            get { return _value; }
            set { NotifyPropertyChanged(ref _value, value); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool NotifyPropertyChanged<T>(ref T refProperty, T value, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(refProperty, value))
            {
                refProperty = value;
                NotifyPropertyChanged(propertyName);
                return true;
            }

            return false;
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region IDataErrorInfo

        public string Error
        {
            get
            {
                Debug.Fail("Checking for EntireRow of NameValuePair is not supposed to be invoked");
                throw new NotImplementedException();
            }
        }

        public string this[string propertyName]
        {
            get
            {
                //Reset error condition
                string error = null;
                HasValidationError = false;

                if (propertyName.Equals("Name", StringComparison.OrdinalIgnoreCase))
                {
                    if (IsNamePropertyEmpty())
                    {
                        error = TizenPropertyPageResources.NameCannotBeEmpty;
                        HasValidationError = true;
                    }
                    else
                    {
                        if (IsNamePropertyDuplicate())
                        {
                            error = TizenPropertyPageResources.DuplicateKey;
                            HasValidationError = true;
                        }
                    }
                    //We are doing Row Validation - make sure that in addition to Name - Value is valid
                    if (!HasValidationError) { HasValidationError = IsValuePropertyEmpty(); }
                }
                if (propertyName.Equals("Value", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(Value))
                    {
                        error = TizenPropertyPageResources.ValueCannotBeEmpty;
                        HasValidationError = true;
                    }
                    //We are doing Row Validation - make sure that in addition to Value - Name is valid
                    if (!HasValidationError) { HasValidationError = IsNamePropertyEmpty() || IsNamePropertyDuplicate(); }
                }
                SendNotificationAfterValidation();
                return error;
            }
        }

        private bool IsNamePropertyEmpty()
        {
            return string.IsNullOrWhiteSpace(Name);
        }

        private bool IsNamePropertyDuplicate()
        {
            if (ParentCollection != null)
            {
                foreach (NameValuePair nvp in ParentCollection)
                {
                    if (!nvp.Equals(this))
                    {
                        if (string.Compare(nvp.Name, Name, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool IsValuePropertyEmpty()
        {
            return string.IsNullOrWhiteSpace(Value);
        }

        private void SendNotificationAfterValidation()
        {
            if (ParentCollection != null)
            {
                ParentCollection.RaiseValidationStatus(!HasValidationError);
            }
        }

        #endregion

    }
}
