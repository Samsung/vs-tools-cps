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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using NetCore.Profiler.Analytics.Model;
using NetCore.Profiler.Extension.Session;
using NetCore.Profiler.Extension.UI.CallTree;
using NetCore.Profiler.Extension.UI.Functions;
using NetCore.Profiler.Extension.UI.HotPath;
using NetCore.Profiler.Extension.UI.SourceLines;
using NetCore.Profiler.Extension.VSPackage;

namespace NetCore.Profiler.Extension.UI.SessionWindow
{
    public class StatisticsTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                var x = (StatisticsType)value;
                switch (x)
                {
                    case StatisticsType.Memory:
                        return "Memory";
                    case StatisticsType.Sample:
                        return "Sample";
                    case StatisticsType.Time:
                        return "Time";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Call Stack Panel
    /// </summary>
    public partial class CallStackPanel
    {
        private IActiveSession _activeSession;

        private TopMethodsView _topMethods;

        private HotPathView1 _hotPathes;

        private CallTreeView _callTree;

        private ISourceLinesQueryResult _sourceLines;


        public CallStackPanel(IActiveSession activeSession)
        {
            _activeSession = activeSession;
            activeSession.PropertyChanged += ActiveSession_PropertyChanged;

            DataContext = this;

            InitializeComponent();
            CreateControls();
            UpdateControls(activeSession, true);
        }

        /// <summary>
        /// Property for UI actually. 
        /// Most likely we should remove StatisticsType from Session but for now we store it there.
        /// </summary>
        public StatisticsType StatisticsType
        {
            get { return _activeSession.StatisticsType; }
            set { _activeSession.StatisticsType = value; }
        }

        public IEnumerable<StatisticsType> StatisticsTypes { get; } = Enum.GetValues(typeof(StatisticsType)).Cast<StatisticsType>();

        private void ActiveSession_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var activeSession = (sender as IActiveSession);
            if (activeSession != null)
            {
                switch (e.PropertyName)
                {
                    case nameof(IActiveSession.StatisticsType):
                        UpdateControls(activeSession, false);
                        break;
                    case nameof(IActiveSession.SelectedTimeFrame):
                    case nameof(IActiveSession.CurrentThreadId):
                        UpdateControls(activeSession, true);
                        break;
                }
            }
        }

        private void CreateControls()
        {
            _topMethods = new TopMethodsView
            {
                Margin = new Thickness(4),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Header = "Top Methods",
                Inclusive = false
            };

            _callTree = new CallTreeView
            {
                Margin = new Thickness(4),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Header = "Call Tree",
                Inclusive = true
            };

            _hotPathes = new HotPathView1
            {
                Margin = new Thickness(4),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Header = "Hot Paths",
                Inclusive = false
            };

        }

        private void UpdateControls(IActiveSession activeSession, bool updateTreeInput)
        {
            ListsGrid1.RowDefinitions.Clear();
            ListsGrid1.Children.Clear();

            var row = 0;
            var methods =
                activeSession.SessionModel.GetTopMethods(activeSession.CurrentThreadId, activeSession.StatisticsType);
            if (methods.Methods.Count > 0)
            {
                ListsGrid1.RowDefinitions.Add(new RowDefinition
                {
                    Height = new GridLength(1, GridUnitType.Star)
                });
                _topMethods.SetInputSource(methods);
                ListsGrid1.Children.Add(_topMethods);
                Grid.SetRow(_topMethods, row++);
                Grid.SetColumn(_topMethods, 0);

                ListsGrid1.RowDefinitions.Add(new RowDefinition
                {
                    Height = GridLength.Auto
                });
                var splitter = new GridSplitter
                {
                    Height = 5,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                ListsGrid1.Children.Add(splitter);
                Grid.SetRow(splitter, row++);
                Grid.SetColumn(splitter, 0);
                Grid.SetColumn(splitter, 0);

            }

            ListsGrid1.RowDefinitions.Add(new RowDefinition
            {
                Height = new GridLength(1, GridUnitType.Star)
            });
            ListsGrid1.Children.Add(_callTree);
            Grid.SetRow(_callTree, row);
            Grid.SetColumn(_callTree, 0);
            if (updateTreeInput)
            {
                _callTree.SetItemsSource(activeSession.SessionModel.GetCallTree(activeSession.CurrentThreadId),
                    activeSession.StatisticsType);
            }
            else
            {
                _callTree.StatisticsType = activeSession.StatisticsType;
            }


            ListsGrid2.RowDefinitions.Clear();
            ListsGrid2.Children.Clear();

            row = 0;
            var paths = activeSession.SessionModel.GetHotPathes(activeSession.CurrentThreadId, activeSession.StatisticsType);
            if (paths.Methods.Count > 0)
            {
                ListsGrid2.RowDefinitions.Add(new RowDefinition
                {
                    Height = new GridLength(1, GridUnitType.Star)
                });
                _hotPathes.SetItemsSource(paths);
                ListsGrid2.Children.Add(_hotPathes);
                Grid.SetRow(_hotPathes, row);
                Grid.SetColumn(_hotPathes, 0);
            }

            _sourceLines = activeSession.SessionModel.GetTopLines(activeSession.CurrentThreadId, activeSession.StatisticsType);
            TopLines.SetItemsSource(_sourceLines);

        }

        private void TopLines_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((sender as TopLinesView)?.LinesGrid.CurrentItem is ISourceLineStatistics plSourceLineStatistics)
            {
                ProfilerPlugin.Instance.ShowSourceFile(_activeSession.SessionModel, plSourceLineStatistics, _sourceLines);
            }
        }
    }
}