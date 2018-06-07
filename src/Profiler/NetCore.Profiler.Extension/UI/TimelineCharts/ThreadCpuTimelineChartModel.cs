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
using NetCore.Profiler.Cperf.Core.Model;

namespace NetCore.Profiler.Extension.UI.TimelineCharts
{
    public class ThreadCpuTimelineChartModel : ThreadTimelineChartModelBase, ITimelineChartModelBase
    {
        private const int MaxPointsPerChart = 1000;

        protected List<CpuUtilization> ValuesSeries;

        public List<CpuUtilization> ViewPortValues { get; private set; } = new List<CpuUtilization>();

        public event ViewPortChangedEventHandler ViewPortChanged;

        public ThreadCpuTimelineChartModel(AppCpuTimelineChartModel masterChart) : base(masterChart)
        {
        }

        public void SetContent(List<CpuUtilization> valuesSeries)
        {
            ValuesSeries = valuesSeries;
            ProcessPausedRegionsAndCreateSections();
            UpdateViewPort();
        }

        private void ProcessPausedRegionsAndCreateSections()
        {
            var cpuUtilizationNew = new List<CpuUtilization>(ValuesSeries.Count + 10);
            foreach (var cpuUtilization in ValuesSeries)
            {
                cpuUtilizationNew.Add(cpuUtilization);
                if (cpuUtilization.ProfilingWasPaused)
                {
                    //"Missing Point" to indicate the end of the region
                    cpuUtilizationNew.Add(new CpuUtilization
                    {
                        Timestamp = cpuUtilization.Timestamp,
                        Utilization = double.NaN
                    });
                }
            }

            ValuesSeries = cpuUtilizationNew;
        }

        protected override void UpdateViewPort()
        {
            var region = FindViewPortValuesRange();
            ViewPortValues = GetViewPortValues(region);
            ViewPortChanged?.Invoke(this);
        }

        protected virtual Tuple<int, int> FindViewPortValuesRange()
        {
            int i;
            var e = ValuesSeries.Count;
            for (i = 0; i < e && ValuesSeries[i].Timestamp < ViewPortMinValue; i++)
            {
            }

            if (i > 0)
            {
                i--;
            }

            if (i < e)
            {
                int j;
                for (j = i + 1; j < e && ValuesSeries[j].Timestamp <= ViewPortMaxValue; j++)
                {
                }

                if (j + 1 < e)
                {
                    j++;
                }

                return new Tuple<int, int>(i, j);
            }

            return null;
        }


        private List<CpuUtilization> CompressValues(Tuple<int, int> range, int maxLength)
        {
            var start = range.Item1;
            var end = range.Item2;
            var count = end - start;
            if (count <= maxLength)
            {
                if (start == 0 && end == ValuesSeries.Count + 1)
                {
                    return ValuesSeries;
                }

                return ValuesSeries.GetRange(start, count);
            }

            var itemsToProcess = count / maxLength + 1;
            var result = new List<CpuUtilization>();

            double prev = 0;
            for (var i = start; i < end;)
            {
                //Skip "Missing Points" indicating end of the region. Profiling was paused at that moment.
                if (double.IsNaN(ValuesSeries[i].Utilization))
                {
                    result.Add(ValuesSeries[i]);
                    i++;
                    continue;
                }

                var current = ValuesSeries[i].Utilization;
                var timestamp = ValuesSeries[i].Timestamp;
                int j;
                for (j = 1; j < itemsToProcess && i + j < end && !double.IsNaN(ValuesSeries[i + j].Utilization); j++)
                {
                    if (Math.Abs(ValuesSeries[i + j].Utilization - prev) > Math.Abs(current - prev))
                    {
                        current = ValuesSeries[i + j].Utilization;
                        timestamp = ValuesSeries[i + j].Timestamp;
                    }
                }

                if (j > 0)
                {
                    result.Add(new CpuUtilization
                    {
                        Timestamp = timestamp,
                        Utilization = current
                    });
                }

                prev = current;
                i += j;
            }

            return result;
        }

        protected List<CpuUtilization> GetViewPortValues(Tuple<int, int> region)
        {
            if (region == null)
            {
                return new List<CpuUtilization>();
            }

            return CompressValues(region, MaxPointsPerChart);
        }
    }
}
