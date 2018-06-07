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
using NetCore.Profiler.Analytics.Model;
using NetCore.Profiler.Extension.Session;

namespace NetCore.Profiler.Extension.UI.SessionWindow
{
    /// <summary>
    /// Filter Panel
    /// </summary>
    public partial class FilterPanel
    {
        private readonly IActiveSession _activeSession;

        public FilterPanel(IActiveSession activeSession)
        {
            _activeSession = activeSession;
            InitializeComponent();
            InitializeButtons();
        }

        private void StatisticsType_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as RadioButton)?.Name)
            {
                case "TimeRb":
                    _activeSession.StatisticsType = StatisticsType.Time;
                    break;
                case "SamplesRb":
                    _activeSession.StatisticsType = StatisticsType.Sample;
                    break;
                case "MemoryRb":
                    _activeSession.StatisticsType = StatisticsType.Memory;
                    break;
            }
        }

        private void TimeLineType_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as RadioButton)?.Name)
            {
                case "GcRb":
                    _activeSession.TimeLineType = TimeLineType.GarbageCollection;
                    break;
                case "JitRb":
                    _activeSession.TimeLineType = TimeLineType.JustInTimeCompilation;
                    break;
                case "UtilizationRb":
                    _activeSession.TimeLineType = TimeLineType.CpuUtilization;
                    break;
            }
        }

        private void InitializeButtons()
        {
            switch (_activeSession.StatisticsType)
            {
                case StatisticsType.Memory:
                    MemoryRb.IsChecked = true;
                    break;
                case StatisticsType.Sample:
                    SamplesRb.IsChecked = true;
                    break;
                case StatisticsType.Time:
                    TimeRb.IsChecked = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch (_activeSession.TimeLineType)
            {
                case TimeLineType.GarbageCollection:
                    GcRb.IsChecked = true;
                    break;
                case TimeLineType.JustInTimeCompilation:
                    JitRb.IsChecked = true;
                    break;
                case TimeLineType.CpuUtilization:
                    UtilizationRb.IsChecked = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
