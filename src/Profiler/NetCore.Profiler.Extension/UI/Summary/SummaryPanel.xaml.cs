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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NetCore.Profiler.Analytics.Model;
using NetCore.Profiler.Extension.Session;
using NetCore.Profiler.Extension.UI.Functions;
using NetCore.Profiler.Extension.UI.SourceLines;
using NetCore.Profiler.Extension.VSPackage;

namespace NetCore.Profiler.Extension.UI.Summary
{
    /// <summary>
    /// Summary Panel
    /// </summary>
    public partial class SummaryPanel
    {
        private readonly IActiveSession _activeSession;

        public SummaryPanel(IActiveSession session)
        {
            _activeSession = session;
            InitializeComponent();

            InitProcTimeline();
            SetTopMethodsData();
        }

        public void InitProcTimeline()
        {
            ProcTimeline.LoadChartData(_activeSession.SessionModel.GetSysInfoItems());
        }

        public void SetTopMethodsData()
        {
            var row = 0;
            var topMethodsByMemory = _activeSession.SessionModel.GetTopMethodsByMemory();
            if (topMethodsByMemory.Methods.Count > 0)
            {
                ListsGrid.RowDefinitions.Add(new RowDefinition
                {
                    Height = new GridLength(1,GridUnitType.Star)
                });
                var x = new TopMethodsView()
                {
                    Margin = new Thickness(4),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Header = "Top Methods by Memory",
                    Inclusive = false,
                    FilterType = StatisticsType.Memory
                };
                x.SetInputSource(topMethodsByMemory);
                ListsGrid.Children.Add(x);
                Grid.SetRow(x,row++);
                Grid.SetColumn(x, 0);
            }

            var topMethodsByTime = _activeSession.SessionModel.GetTopMethodsByTime();
            if (topMethodsByTime.Methods.Count > 0)
            {
                ListsGrid.RowDefinitions.Add(new RowDefinition
                {
                    Height = new GridLength(1, GridUnitType.Star)
                });
                var x = new TopMethodsView()
                {
                    Margin = new Thickness(4),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Header = "Top Methods by Time",
                    Inclusive = false,
                    FilterType = StatisticsType.Time
                };
                x.SetInputSource(topMethodsByTime);
                ListsGrid.Children.Add(x);
                Grid.SetRow(x, row++);
                Grid.SetColumn(x, 0);
            }

            var topLinesBySamples = _activeSession.SessionModel.GetTopLinesBySamples();
            if (topLinesBySamples.Lines.Count > 0)
            {
                ListsGrid.RowDefinitions.Add(new RowDefinition
                {
                    Height = new GridLength(1, GridUnitType.Star)
                });
                var x = new TopLinesView()
                {
                    Margin = new Thickness(4),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Header = "Top Lines",
                    Inclusive = true,
                    FilterType = StatisticsType.Sample,
                };
                x.SetItemsSource(topLinesBySamples);
                x.MouseDoubleClick += TopLines_MouseDoubleClick;
                ListsGrid.Children.Add(x);
                Grid.SetRow(x, row++);
                Grid.SetColumn(x, 0);
            }

            var topLinesByMemory = _activeSession.SessionModel.GetTopLinesByMemory();
            if (topLinesByMemory.Lines.Count > 0)
            {
                ListsGrid.RowDefinitions.Add(new RowDefinition
                {
                    Height = new GridLength(1, GridUnitType.Star)
                });
                var x = new TopLinesView()
                {
                    Margin = new Thickness(4),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Header = "Top Lines By Memory",
                    Inclusive = true,
                    FilterType = StatisticsType.Sample
                };
                x.SetItemsSource(topLinesByMemory);
                x.MouseDoubleClick += TopLinesyMem_MouseDoubleClick;
                ListsGrid.Children.Add(x);
                Grid.SetRow(x, row);
                Grid.SetColumn(x, 0);
            }
        }

        private void TopLines_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
        }

        private void TopLinesyMem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
        }
    }
}
