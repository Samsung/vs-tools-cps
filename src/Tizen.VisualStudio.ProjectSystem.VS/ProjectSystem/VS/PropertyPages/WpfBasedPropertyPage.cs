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
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;

namespace Tizen.VisualStudio.ProjectSystem.VS.PropertyPages
{
    internal abstract partial class WpfBasedPropertyPage : PropertyPage
    {
        private PropertyPageElementHost host;
        private PropertyPageControl control;
        private PropertyPageViewModel viewModel;

        public WpfBasedPropertyPage()
        {
            InitializeComponent();
        }

        protected abstract PropertyPageViewModel CreatePropertyPageViewModel();

        protected abstract PropertyPageControl CreatePropertyPageControl();

        protected async override Task OnSetObjectsAsync(bool isClosing)
        {
            if (isClosing)
            {
                control.DetachViewModel();
                return;
            }
            else
            {
                //viewModel can be non-null when the configuration is chaged.
                if (control == null)
                {
                    control = CreatePropertyPageControl();
                }
            }

            viewModel = CreatePropertyPageViewModel();
            viewModel.UnconfiguredProject = UnconfiguredProject;
            await viewModel.InitializeAsync().ConfigureAwait(false);
            control.InitializePropertyPage(viewModel);
        }

        protected async override Task<int> OnApplyAsync()
        {
            return await control.ApplyAsync().ConfigureAwait(false);
        }

        protected async override Task OnDeactivateAsync()
        {
            if (IsDirty)
            {
                await OnApplyAsync().ConfigureAwait(false);
            }
        }

        private void WpfPropertyPage_Load(object sender, EventArgs e)
        {
            SuspendLayout();

            host = new PropertyPageElementHost();
            host.AutoSize = false;
            host.Dock = DockStyle.Fill;

            if (control == null)
            {
                control = CreatePropertyPageControl();
            }

            ScrollViewer viewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            viewer.Content = control;
            host.Child = viewer;

            wpfHostPanel.Dock = DockStyle.Fill;
            wpfHostPanel.Controls.Add(host);

            ResumeLayout(true);
            control.StatusChanged += OnControlStatusChanged;
        }

        private void OnControlStatusChanged(object sender, EventArgs e)
        {
            if (IsDirty != control.IsDirty)
            {
                IsDirty = control.IsDirty;
            }
        }
    }
}
