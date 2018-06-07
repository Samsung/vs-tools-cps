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

using NetCore.Profiler.Extension.Session;
using NetCore.Profiler.Extension.UI.TimelineCharts;

namespace NetCore.Profiler.Extension.UI
{
    public interface IThreadListItem
    {
        bool IsSelected { get; set; }

        ulong InternalId { get; }

        ulong OsThreadId { get; }

        string CurrentValue { get; }

        ThreadCpuTimelineChartModel CpuUtilization { get; }

        TimeLineType TimeLineType { get; set; }

        void RefreshValue();
    }
}