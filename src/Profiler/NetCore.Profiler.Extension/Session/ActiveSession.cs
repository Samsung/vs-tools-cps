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

using NetCore.Profiler.Analytics.DataProvider;
using NetCore.Profiler.Analytics.Model;
using NetCore.Profiler.Extension.Common;

namespace NetCore.Profiler.Extension.Session
{
    class ActiveSession : NotifyPropertyChanged, IActiveSession
    {
        private ISession _sessionModel;
        private ISelectedTimeFrame _selectedTimeFrame = new SelectedTimeFrame();
        private ulong _currentThreadId;
        private StatisticsType _statisticsType = StatisticsType.Sample;
        private TimeLineType _timeLineType = TimeLineType.CpuUtilization;

        public ActiveSession(ISession sessionModel)
        {
            _sessionModel = sessionModel;
        }

        public string Label => $"{SessionModel.CreatedAt.ToLocalTime()} - {SessionModel.ProjectName}";

        public ISession SessionModel
        {
            get { return _sessionModel; }
            set { SetProperty(ref _sessionModel,value); }
        }

        public ISelectedTimeFrame SelectedTimeFrame
        {
            get { return _selectedTimeFrame; }
            set { SetProperty(ref _selectedTimeFrame,value); }
        }

        public ulong CurrentThreadId
        {
            get { return _currentThreadId; }
            set { SetProperty(ref _currentThreadId,value); }
        }

        public StatisticsType StatisticsType
        {
            get { return _statisticsType; }
            set { SetProperty(ref _statisticsType,value); }
        }

        public TimeLineType TimeLineType
        {
            get { return _timeLineType; }
            set { SetProperty(ref _timeLineType, value); }
        }

        public void UpdateDataForTimeFrame(ISelectedTimeFrame timeFrame)
        {
            if (!SelectedTimeFrame.Equals(timeFrame))
            {
                SessionModel.BuildStatistics(timeFrame);
                SelectedTimeFrame = timeFrame;
            }

        }
    }
}
