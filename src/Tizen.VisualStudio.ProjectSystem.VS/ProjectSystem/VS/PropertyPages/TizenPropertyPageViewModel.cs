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

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ProjectSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using Tizen.VisualStudio.ProjectSystem.Debug;
using Tizen.VisualStudio.ProjectSystem.VS.Utilities;

namespace Tizen.VisualStudio.ProjectSystem.VS.PropertyPages
{
    internal class TizenPropertyPageViewModel : PropertyPageViewModel
    {
        //private System.Windows.Forms.CheckBox chkExtraArgs;

        //public event EventHandler ClearEnvironmentVariablesGridError;
        //public event EventHandler FocusEnvironmentVariablesGridRow;

        //private OrderPrecedenceImportCollection<string> _uiProviders;

        public override Task InitializeAsync()
        {
            /*
            _uiProviders = new OrderPrecedenceImportCollection<string>(ImportOrderPrecedenceComparer.PreferenceOrder.PreferredComesFirst, UnconfiguredProject);
            var uiProviders = GetUIProviders();
            foreach( var uiProvider in uiProviders )
            {
                _uiProviders.Add(uiProvider);
            }
            InitializeTizenProjectPage();
            */
            return System.Threading.Tasks.Task.CompletedTask;
        }

        protected void InitializeTizenProjectPage()
        {
        }

        private ITizenLaunchSettings TizenLaunchSettings { get; set; }
        /// <summary>
        /// Gets the UI providers
        /// </summary>
        protected virtual IEnumerable<Lazy<string, IOrderPrecedenceMetadataView>> GetUIProviders()
        {
            return UnconfiguredProject.Services.ExportProvider.GetExports<string, IOrderPrecedenceMetadataView>();
        }

        public async override Task<int> SaveAsync()
        {
            //throw new NotImplementedException();
            await SaveTizenLaunchSettingsAsync().ConfigureAwait(false);
            return VSConstants.S_OK;
        }

        private ObservableList<NameValuePair> _environmentVariables;
        public ObservableList<NameValuePair> EnvironmentVariables
        {
            get
            {
                return _environmentVariables;
            }
            set
            {
                OnPropertyChanged(ref _environmentVariables, value);
            }
        }

        /// <summary>
        /// Provides binding to the current UI Provider usercontrol.
        /// </summary>
        public UserControl ActiveProviderUserControl
        {
            get
            {
                //var provider = ActiveProvider;
                return null;//ActiveProvider?.CustomUI;
            }
        }

        public string ExtraArgument
        {
            get
            {
                if (TizenLaunchSettings == null)
                {
                    TizenLaunchSettings = GetTizenLaunchSetting().TizenLaunchSetting;
                }
                return TizenLaunchSettings.ExtraArguments;
            }
            set
            {
                if (TizenLaunchSettings != null && TizenLaunchSettings.ExtraArguments != null)
                {
                    TizenLaunchSettings.ExtraArguments = value;
                    OnPropertyChanged(nameof(ExtraArgument));
                }
            }
        }

        public async Task SaveTizenLaunchSettingsAsync()
        {
            ITizenLaunchSettingsProvider provider = GetTizenLaunchSetting();

            await provider.UpdateAndSaveTizenSettingsAsync(TizenLaunchSettings).ConfigureAwait(false);
            //VsProjectHelper.GetInstance.ExtraArg = _extraArgument;
        }

        ITizenLaunchSettingsProvider _tizenLaunchSettingsProvider;
        protected virtual ITizenLaunchSettingsProvider GetTizenLaunchSetting()
        {
            if (_tizenLaunchSettingsProvider == null)
            {
                _tizenLaunchSettingsProvider = UnconfiguredProject.Services.ExportProvider.GetExportedValue<ITizenLaunchSettingsProvider>();
            }
            return _tizenLaunchSettingsProvider;
        }

        public bool IsProfileSelected
        {
            get
            {
                return true ;
            }
        }
    }
}