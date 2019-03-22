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
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using NetCore.Profiler.Analytics.DataProvider;
using NetCore.Profiler.Extension.Session;
using NetCore.Profiler.Extension.UI.TimelineCharts;

namespace NetCore.Profiler.Extension.UI.SessionWindow
{
    public class TimeLineTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                var x = (TimeLineType)value;
                switch (x)
                {
                    case TimeLineType.CpuUtilization:
                        return "CPU";
                    case TimeLineType.GarbageCollection:
                        return "Garbage Collection";
                    case TimeLineType.JustInTimeCompilation:
                        return "JIT";
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
    /// Timeline Panel
    /// </summary>
    public partial class TimelinePanel
    {
        private IActiveSession _session;

        public TimelinePanel(IActiveSession session)
        {
            _session = session;
            session.PropertyChanged += SessionOnPropertyChanged;
            CpuUtilizationSource = new AppCpuTimelineChartModel();
            CpuUtilizationSource.SetContent(session.SessionModel.GetApplicationCpuUtilization(), session.SessionModel.CpuCoreCount);

            Threads = session.SessionModel.Threads
                .Select<ISessionThread, IThreadListItem>(thread => new ThreadListItem(thread, session, CpuUtilizationSource)
                {
                    IsSelected = thread.InternalId == 0
                }).ToList();

            DataContext = this;

            InitializeComponent();

            CpuChart.SelectionChanged += delegate(object sender, EventArgs e)
            {
                if (sender is AppCpuTimelineChart chart)
                {
                    ulong from = 0;
                    ulong to = ulong.MaxValue;
                    if (chart.SelectionEndSeconds - chart.SelectionStartSeconds > 0)
                    {
                        from = (ulong)Math.Round(chart.SelectionStartSeconds * 1000);
                        to = (ulong)Math.Round(chart.SelectionEndSeconds * 1000);
                    }

                    session.UpdateDataForTimeFrame(new SelectedTimeFrame { Start = from, End = to });
                }
            };
        }

        public AppCpuTimelineChartModel CpuUtilizationSource { get; }

        public List<IThreadListItem> Threads { get; }

        private void SessionOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            var activeSession = (sender as IActiveSession);
            if (activeSession != null)
            {
                switch (propertyChangedEventArgs.PropertyName)
                {
                    case nameof(IActiveSession.SelectedTimeFrame):
                    case nameof(IActiveSession.StatisticsType):
                        RefreshValuePresentation();
                        break;
                    case nameof(IActiveSession.TimeLineType):
                        UpdateTimelineType(activeSession.TimeLineType);
                        break;
                }
            }
        }

        internal void UpdateTimelineType(TimeLineType type)
        {
            foreach (var threadListItem in Threads)
            {
                threadListItem.TimeLineType = type;
            }
        }

        internal void RefreshValuePresentation()
        {
            foreach (IThreadListItem item in DataGrid.Items)
            {
                item.RefreshValue();
            }
        }
    }
}
