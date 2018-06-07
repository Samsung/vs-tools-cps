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
using System.ComponentModel;
using NetCore.Profiler.Analytics.Model;
using NetCore.Profiler.Common.Helpers;
using NetCore.Profiler.Extension.Session;
using NetCore.Profiler.Extension.UI.TimelineCharts;

namespace NetCore.Profiler.Extension.UI
{
    public class ThreadListItem : IThreadListItem, INotifyPropertyChanged
    {
        private readonly IActiveSession _parent;
        private readonly ISessionThread _sessionThread;

        private bool _isSelected;
        private TimeLineType _timeLineType = TimeLineType.CpuUtilization;

        public ThreadListItem(ISessionThread sessionThread, IActiveSession parent, AppCpuTimelineChartModel applicationChartModel)
        {
            _sessionThread = sessionThread;
            _parent = parent;
            AppCpuTimelineChartModel = applicationChartModel;

            CpuUtilization = new ThreadCpuTimelineChartModel(applicationChartModel);
            CpuUtilization.SetContent(_sessionThread.CpuUtilization);

            JitJobsSource = new ThreadClrJobTimelineChartModel(applicationChartModel);
            JitJobsSource.SetContent(_sessionThread.ClrJobs, ClrJobType.JustInTimeCompilation, parent.SessionModel.StartedNanoseconds);

            GcJobsSource = new ThreadClrJobTimelineChartModel(applicationChartModel);
            GcJobsSource.SetContent(_sessionThread.ClrJobs, ClrJobType.GarbageCollection, parent.SessionModel.StartedNanoseconds);

        }

        public ulong OsThreadId => _sessionThread.OsThreadId;

        public ulong InternalId => _sessionThread.InternalId;

        public AppCpuTimelineChartModel AppCpuTimelineChartModel { get; }

        public ThreadCpuTimelineChartModel CpuUtilization { get; }

        public ThreadClrJobTimelineChartModel JitJobsSource { get; }

        public ThreadClrJobTimelineChartModel GcJobsSource { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                if (value)
                {
                    _parent.CurrentThreadId = _sessionThread.InternalId;
                }

                OnPropertyChanged("IsSelected");
            }
        }

        public TimeLineType TimeLineType
        {
            get => _timeLineType;
            set
            {
                if (_timeLineType != value)
                {
                    _timeLineType = value;
                    OnPropertyChanged("TimeLineType");
                }
            }
        }

        public string CurrentValue
        {
            get
            {
                var value = _sessionThread.Totals.GetValue(_parent.StatisticsType);
                switch (_parent.StatisticsType)
                {
                    case StatisticsType.Memory:
                        return value.SizeBytesToString();
                    case StatisticsType.Sample:
                        return value.ToString();
                    case StatisticsType.Time:
                        return value.MilliSecondsToString();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public void RefreshValue()
        {
            OnPropertyChanged("CurrentValue");
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }
}
