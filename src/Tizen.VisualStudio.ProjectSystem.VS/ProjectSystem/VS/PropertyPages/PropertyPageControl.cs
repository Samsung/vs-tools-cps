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
using Microsoft.VisualStudio;
using System;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Tizen.VisualStudio.ProjectSystem.VS.PropertyPages
{
    internal class PropertyPageControl : UserControl
    {
        private bool _isDirty;
        private bool _ignoreEvents;

        public PropertyPageControl()
        {
        }

        public event EventHandler StatusChanged;

        public PropertyPageViewModel ViewModel
        {
            get
            {
                return DataContext as PropertyPageViewModel;
            }
            set
            {
                DataContext = value;
            }
        }

        public bool IsDirty
        {
            get { return _isDirty; }
            set
            {
                // Only process real changes
                if (value != _isDirty && !_ignoreEvents)
                {
                    _isDirty = value;
                    OnStatusChanged(new EventArgs());
                }
            }
        }

        public virtual void InitializePropertyPage(PropertyPageViewModel viewModel)
        {
            _ignoreEvents = true;
            IsDirty = false;
            ViewModel = viewModel;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.ParentControl = this;
            _ignoreEvents = false;
        }

        public virtual void DetachViewModel()
        {
            _ignoreEvents = true;
            IsDirty = false;
            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;

            // Let the view model know we are done.
            ViewModel.ViewModelDetached();
            ViewModel.ParentControl = null;
            ViewModel = null;
            _ignoreEvents = false;
        }

        public async Task<int> Apply()
        {
            int result = VSConstants.S_OK;

            if (IsDirty)
            {
                result = await OnApply().ConfigureAwait(false);
                if (result == VSConstants.S_OK)
                {
                    IsDirty = false;
                }
            }

            return result;
        }

        protected virtual Task<int> OnApply() { return ViewModel.Save(); }
        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!_ignoreEvents && !ViewModel.IgnoreEvents)
            {
                IsDirty = true;
            }
        }

        protected virtual void OnStatusChanged(EventArgs args)
        {
            EventHandler handler = StatusChanged;
            if (handler != null)
            {
                handler.Invoke(this, args);
            }
        }
    }
}
