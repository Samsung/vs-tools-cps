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

using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Tizen.VisualStudio.ProjectSystem.VS.Utilities;

namespace Tizen.VisualStudio.ProjectSystem.VS.PropertyPages
{
    /// <summary>
    /// Interaction logic for TizenPropertyPageControl.xaml
    /// </summary>
    internal partial class TizenPropertyPageControl : PropertyPageControl
    {
        bool _customControlLayoutUpdateRequired;

        public bool CustomControlLayoutUpdateRequired { get => _customControlLayoutUpdateRequired; set => _customControlLayoutUpdateRequired = value; }

        public TizenPropertyPageControl()
        {
            InitializeComponent();
            DataContextChanged += DebugPageControlControl_DataContextChanged;
            //LayoutUpdated += DebugPageControl_LayoutUpdated;
        }

        void DebugPageControlControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null && e.OldValue is TizenPropertyPageViewModel)
            {
                TizenPropertyPageViewModel viewModel = e.OldValue as TizenPropertyPageViewModel;
                //viewModel.FocusEnvironmentVariablesGridRow -= OnFocusEnvironmentVariableGridRow;
                //viewModel.ClearEnvironmentVariablesGridError -= OnClearEnvironmentVariableGridError;
                viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            if (e.NewValue != null && e.NewValue is TizenPropertyPageViewModel)
            {
                TizenPropertyPageViewModel viewModel = e.NewValue as TizenPropertyPageViewModel;
                //viewModel.FocusEnvironmentVariablesGridRow += OnFocusEnvironmentVariableGridRow;
                //viewModel.ClearEnvironmentVariablesGridError += OnClearEnvironmentVariableGridError;
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }
        /*
        private void OnClearEnvironmentVariableGridError(object sender, EventArgs e)
        {
            ClearGridError(dataGridEnvironmentVariables);
        }

        private void OnFocusEnvironmentVariableGridRow(object sender, EventArgs e)
        {
            if (DataContext != null && DataContext is DebugPageViewModel)
            {
                Dispatcher.BeginInvoke(new DispatcherOperationCallback((param) =>
                {
                    if ((DataContext as DebugPageViewModel).EnvironmentVariables.Count > 0)
                    {
                        // get the new cell, set focus, then open for edit
                        var cell = WpfHelper.GetCell(dataGridEnvironmentVariables, (DataContext as DebugPageViewModel).EnvironmentVariables.Count - 1, 0);
                        cell.Focus();
                        dataGridEnvironmentVariables.BeginEdit();
                    }
                    return null;
                }), DispatcherPriority.Background, new object[] { null });
            }
        }
        */
        private void ClearGridError(DataGrid dataGrid)
        {
            try
            {
                BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
                PropertyInfo cellErrorInfo = dataGrid.GetType().GetProperty("HasCellValidationError", bindingFlags);
                PropertyInfo rowErrorInfo = dataGrid.GetType().GetProperty("HasRowValidationError", bindingFlags);
                cellErrorInfo.SetValue(dataGrid, false, null);
                rowErrorInfo.SetValue(dataGrid, false, null);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Called when a property changes on the view model. Used to detect when the custom UI changes so that
        /// the size of its grids can be determined
        /// </summary>
        private void ViewModel_PropertyChanged(Object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(TizenPropertyPageViewModel.ActiveProviderUserControl)))
            {
                CustomControlLayoutUpdateRequired = true;
            }
        }

        /// <summary>
        /// Used to make sure the custom controls layout matches the rest of the dialog. Assumes the custom control has a 3 column
        /// grid just like the main dialog page. If it doesn't no layout update is done.
        /// </summary>
        /*
        private void DebugPageControl_LayoutUpdated(Object sender, EventArgs e)
        {
            if (_customControlLayoutUpdateRequired && _mainGrid != null && DataContext != null)
            {
                _customControlLayoutUpdateRequired = false;

                // Get the control that was added to the grid
                var customControl = ((TizenPropertyPageViewModel)DataContext).ActiveProviderUserControl;
                if (customControl != null)
                {
                    if (customControl.Content is Grid childGrid && childGrid.ColumnDefinitions.Count == _mainGrid.ColumnDefinitions.Count)
                    {
                        for (int i = 0; i < childGrid.ColumnDefinitions.Count; i++)
                        {
                            childGrid.ColumnDefinitions[i].Width = new GridLength(_mainGrid.ColumnDefinitions[i].ActualWidth);
                        }
                    }
                }
            }
        }*/
    }
}
