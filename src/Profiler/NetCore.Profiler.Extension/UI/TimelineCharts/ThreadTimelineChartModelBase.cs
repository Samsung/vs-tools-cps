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

using System.Collections.Generic;

namespace NetCore.Profiler.Extension.UI.TimelineCharts
{
    public abstract class ThreadTimelineChartModelBase
    {

        protected AppCpuTimelineChartModel MasterChart;

        protected ThreadTimelineChartModelBase(AppCpuTimelineChartModel masterChart)
        {
            MasterChart = masterChart;
            MasterChart.ViewPortChanged += sender => UpdateViewPort();
        }

        public ulong RangeMaxValueMilliseconds => MasterChart.RangeMaxValueMilliseconds;

        public ulong ViewPortMinValueMilliseconds => MasterChart.ViewPortMinValueMilliseconds;

        public ulong ViewPortMaxValueMilliseconds => MasterChart.ViewPortMaxValueMilliseconds;

        public List<TimeLineSection> PauseSections => MasterChart.PauseSections;

        protected abstract void UpdateViewPort();

    }
}
