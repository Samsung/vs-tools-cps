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
using NetCore.Profiler.Analytics.Model;

namespace NetCore.Profiler.Extension.UI.TimelineCharts
{
    public class ClrJobItem
    {
        public ulong Timestamp { get; set; }

        public double Value { get; set; }
    }

    public class ThreadClrJobTimelineChartModel : ThreadTimelineChartModelBase, ITimelineChartModelBase
    {
        private List<ClrJob> _valuesSeries;

        private double Width = 1000;

        public List<ClrJobItem> ViewPortValues { get; private set; } = new List<ClrJobItem>();

        public event ViewPortChangedEventHandler ViewPortChanged;


        public ThreadClrJobTimelineChartModel(AppCpuTimelineChartModel masterChart) : base(masterChart)
        {
        }

        public void SetContent(List<ClrJob> valuesSeries, ClrJobType type, ulong startTime)
        {
            _valuesSeries = new List<ClrJob>(valuesSeries.Count);
            ClrJob lastItem = null;
            foreach (var job in valuesSeries)
            {
                if (job.Type != type)
                {
                    continue;
                }

                var start = (job.StartTime - startTime) / 1000000;
                var end = (job.StartTime - startTime) / 1000000;
                if (end == start)
                {
                    end++;
                }

                if (lastItem == null)
                {
                    lastItem = new ClrJob
                    {
                        StartTime = start,
                        EndTime = end,
                    };
                    _valuesSeries.Add(lastItem);
                    continue;
                }

                if (start <= lastItem.EndTime)
                {
                    lastItem.EndTime = end;
                    continue;
                }

                _valuesSeries.Add(lastItem = new ClrJob
                {
                    StartTime = start,
                    EndTime = end,
                });
            }

            UpdateViewPort();
        }


        protected override void UpdateViewPort()
        {
            var region = FindViewPortValuesRange();
            ViewPortValues = GetViewPortValues(region);
            ViewPortChanged?.Invoke(this);
        }

        protected Tuple<int, int> FindViewPortValuesRange()
        {
            int i;
            var e = _valuesSeries.Count;
            for (i = 0; i < e && _valuesSeries[i].StartTime < ViewPortMinValue; i++)
            {
            }

            if (i < e)
            {
                int j;
                for (j = i + 1; j < e && _valuesSeries[j].EndTime <= ViewPortMaxValue; j++)
                {
                }

                return new Tuple<int, int>(i, j);
            }

            return null;
        }


        protected List<ClrJobItem> GetViewPortValues(Tuple<int, int> region)
        {
            if (region == null)
            {
                return new List<ClrJobItem>();
            }

            var start = region.Item1;
            var end = region.Item2;
            var count = end - start;

            var compressed = new List<ClrJob>();
            ClrJob lastItem = null;

            ulong min = (ulong)((ViewPortMaxValue - ViewPortMinValue) / Width);
            for (var i = start; i < end;i++)
            {
                if (lastItem == null)
                {
                    lastItem = new ClrJob
                    {
                        StartTime = _valuesSeries[i].StartTime,
                        EndTime = _valuesSeries[i].EndTime
                    };
                    compressed.Add(lastItem);
                    continue;
                }

                if ((_valuesSeries[i].StartTime - lastItem.EndTime) < min)
                {
                    lastItem.EndTime = _valuesSeries[i].EndTime;
                    continue;
                }

                lastItem = new ClrJob
                {
                    StartTime = _valuesSeries[i].StartTime,
                    EndTime = _valuesSeries[i].EndTime
                };

                compressed.Add(lastItem);
            }

            var result = new List<ClrJobItem>();
            foreach (var clrJob in compressed)
            {
                result.Add(new ClrJobItem
                {
                    Timestamp = clrJob.StartTime,
                    Value = 1
                });
                result.Add(new ClrJobItem
                {
                    Timestamp = clrJob.EndTime,
                    Value = 0
                });
            }
            //result.Add(new ClrJobItem
            //{
            //    Timestamp = ViewPortMaxValue+1,
            //    Value = 0
            //});
            return result;
        }

        protected List<ClrJobItem> GetViewPortValuesOrg(Tuple<int, int> region)
        {
            if (region == null)
            {
                return new List<ClrJobItem>();
            }

            var start = region.Item1;
            var end = region.Item2;
            var count = end - start;

            var compressed = new List<ClrJob>();
            ClrJob lastItem = null;

            ulong min = (ulong)((ViewPortMaxValue - ViewPortMinValue) / Width);
            for (var i = start; i < end; i++)
            {
                if (lastItem == null)
                {
                    lastItem = new ClrJob
                    {
                        StartTime = _valuesSeries[i].StartTime,
                        EndTime = _valuesSeries[i].EndTime
                    };
                    compressed.Add(lastItem);
                    continue;
                }

                if ((_valuesSeries[i].StartTime - lastItem.EndTime) < min)
                {
                    lastItem.EndTime = _valuesSeries[i].EndTime;
                    continue;
                }

                lastItem = new ClrJob
                {
                    StartTime = _valuesSeries[i].StartTime,
                    EndTime = _valuesSeries[i].EndTime
                };

                compressed.Add(lastItem);
            }

            var result = new List<ClrJobItem> { new ClrJobItem { Timestamp = 0, Value = 0 } };
            foreach (var clrJob in compressed)
            {
                result.Add(new ClrJobItem
                {
                    Timestamp = clrJob.StartTime,
                    Value = 0
                });
                result.Add(new ClrJobItem
                {
                    Timestamp = clrJob.StartTime,
                    Value = 1
                });
                result.Add(new ClrJobItem
                {
                    Timestamp = clrJob.EndTime,
                    Value = 1
                });
                result.Add(new ClrJobItem
                {
                    Timestamp = clrJob.EndTime,
                    Value = 0
                });
            }

            result.Add(new ClrJobItem
            {
                Timestamp = ViewPortMaxValue + 1,
                Value = 0
            });

            return result;
        }
    }
}
