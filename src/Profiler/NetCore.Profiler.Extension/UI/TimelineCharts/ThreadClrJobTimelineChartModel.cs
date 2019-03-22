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
        public ulong TimeMilliseconds { get; set; }

        public double Value { get; set; }
    }

    public class ThreadClrJobTimelineChartModel : ThreadTimelineChartModelBase, ITimelineChartModelBase
    {
        private List<ChartClrJob> _valuesSeries;

        private double Width = 1000;

        public List<ClrJobItem> ViewPortValues { get; private set; } = new List<ClrJobItem>();

        public event ViewPortChangedEventHandler ViewPortChanged;

        public ThreadClrJobTimelineChartModel(AppCpuTimelineChartModel masterChart) : base(masterChart)
        {
        }

        public void SetContent(List<ClrJob> clrJobs, ClrJobType type)
        {
            _valuesSeries = new List<ChartClrJob>(clrJobs.Count);

            ChartClrJob lastItem = null;

            foreach (ClrJob job in clrJobs)
            {
                if (job.Type != type)
                {
                    continue;
                }

                ulong startMilliseconds = job.StartNanoseconds / 1000000;
                ulong endMilliseconds = job.EndNanoseconds / 1000000;

                if ((lastItem != null) && (startMilliseconds <= lastItem.EndMilliseconds))
                {
                    if (endMilliseconds > lastItem.EndMilliseconds)
                    {
                        lastItem.EndMilliseconds = endMilliseconds;
                    }
                    continue;
                }

                if (startMilliseconds == endMilliseconds)
                {
                    ++endMilliseconds;
                }

                lastItem = new ChartClrJob
                {
                    StartMilliseconds = startMilliseconds,
                    EndMilliseconds = endMilliseconds,
                };

                _valuesSeries.Add(lastItem);
            }

            _valuesSeries.Capacity = _valuesSeries.Count;

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
            int e = _valuesSeries.Count;
            for (i = 0; i < e && _valuesSeries[i].StartMilliseconds < ViewPortMinValueMilliseconds; i++)
            {
            }

            if (i < e)
            {
                int j;
                for (j = i + 1; j < e && _valuesSeries[j].EndMilliseconds <= ViewPortMaxValueMilliseconds; j++)
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

            int start = region.Item1;
            int end = region.Item2;
            int count = end - start;

            var compressed = new List<ChartClrJob>();
            ChartClrJob lastItem = null;

            ulong min = (ulong)((ViewPortMaxValueMilliseconds - ViewPortMinValueMilliseconds) / Width);
            for (int i = start; i < end; i++)
            {
                if (lastItem == null)
                {
                    lastItem = new ChartClrJob
                    {
                        StartMilliseconds = _valuesSeries[i].StartMilliseconds,
                        EndMilliseconds = _valuesSeries[i].EndMilliseconds
                    };
                    compressed.Add(lastItem);
                    continue;
                }

                if ((_valuesSeries[i].StartMilliseconds - lastItem.EndMilliseconds) < min)
                {
                    lastItem.EndMilliseconds = _valuesSeries[i].EndMilliseconds;
                    continue;
                }

                lastItem = new ChartClrJob
                {
                    StartMilliseconds = _valuesSeries[i].StartMilliseconds,
                    EndMilliseconds = _valuesSeries[i].EndMilliseconds
                };

                compressed.Add(lastItem);
            }

            var result = new List<ClrJobItem>(compressed.Count * 2);
            foreach (ChartClrJob clrJob in compressed)
            {
                result.Add(new ClrJobItem
                {
                    TimeMilliseconds = clrJob.StartMilliseconds,
                    Value = 1
                });
                result.Add(new ClrJobItem
                {
                    TimeMilliseconds = clrJob.EndMilliseconds,
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

            int start = region.Item1;
            int end = region.Item2;
            int count = end - start;

            var compressed = new List<ChartClrJob>();
            ChartClrJob lastItem = null;

            ulong min = (ulong)((ViewPortMaxValueMilliseconds - ViewPortMinValueMilliseconds) / Width);
            for (var i = start; i < end; i++)
            {
                if (lastItem == null)
                {
                    lastItem = new ChartClrJob
                    {
                        StartMilliseconds = _valuesSeries[i].StartMilliseconds,
                        EndMilliseconds = _valuesSeries[i].EndMilliseconds
                    };
                    compressed.Add(lastItem);
                    continue;
                }

                if ((_valuesSeries[i].StartMilliseconds - lastItem.EndMilliseconds) < min)
                {
                    lastItem.EndMilliseconds = _valuesSeries[i].EndMilliseconds;
                    continue;
                }

                lastItem = new ChartClrJob
                {
                    StartMilliseconds = _valuesSeries[i].StartMilliseconds,
                    EndMilliseconds = _valuesSeries[i].EndMilliseconds
                };

                compressed.Add(lastItem);
            }

            var result = new List<ClrJobItem> { new ClrJobItem() };
            foreach (ChartClrJob clrJob in compressed)
            {
                result.Add(new ClrJobItem
                {
                    TimeMilliseconds = clrJob.StartMilliseconds,
                    Value = 0
                });
                result.Add(new ClrJobItem
                {
                    TimeMilliseconds = clrJob.StartMilliseconds,
                    Value = 1
                });
                result.Add(new ClrJobItem
                {
                    TimeMilliseconds = clrJob.EndMilliseconds,
                    Value = 1
                });
                result.Add(new ClrJobItem
                {
                    TimeMilliseconds = clrJob.EndMilliseconds,
                    Value = 0
                });
            }

            result.Add(new ClrJobItem
            {
                TimeMilliseconds = ViewPortMaxValueMilliseconds + 1,
                Value = 0
            });

            return result;
        }

        private class ChartClrJob
        {
            public ulong StartMilliseconds { get; set; }

            public ulong EndMilliseconds { get; set; }
        }
    }
}
