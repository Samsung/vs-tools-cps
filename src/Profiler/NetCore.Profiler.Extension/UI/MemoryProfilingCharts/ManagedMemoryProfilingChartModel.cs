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

namespace NetCore.Profiler.Extension.UI.MemoryProfilingCharts
{
    public class ManagedMemoryProfilingChartModel : ManagedMemoryProfilingChartModelBase, IMemoryProfilingChartModelBase
    {

        private const int MaxPointsPerChart = 1000;

        protected List<MemoryData> ValuesSeries;

        public List<MemoryData> ViewPortValues { get; private set; } = new List<MemoryData>();

        public event ViewPortChangedEventHandler ViewPortChanged;

        public void SetContent(List<MemoryData> valuesSeries)
        {
            ValuesSeries = valuesSeries;

            RangeMinValue = ViewPortMinValue =
                ValuesSeries.Count > 0
                    ? ValuesSeries[0].Timestamp
                    : 0;

            RangeMaxValue = ViewPortMaxValue =
                ValuesSeries.Count > 0
                    ? ValuesSeries[ValuesSeries.Count - 1].Timestamp
                    : 0;

            UpdateViewPort();
        }


        private List<MemoryData> CompressValues(Tuple<int, int> range, int maxLength)
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
            var result = new List<MemoryData>();

            for (var i = start; i < end;)
            {
                result.Add(new MemoryData
                {
                    Timestamp = ValuesSeries[i].Timestamp,
                    SmallObjectsHeapGeneration0 = ValuesSeries[i].SmallObjectsHeapGeneration0,
                    SmallObjectsHeapGeneration1 = ValuesSeries[i].SmallObjectsHeapGeneration1,
                    SmallObjectsHeapGeneration2 = ValuesSeries[i].SmallObjectsHeapGeneration2,
                    LargeObjectsHeap = ValuesSeries[i].LargeObjectsHeap
                });
                for (var j = 0; j < itemsToProcess && i < end; i++, j++)
                {
                }
            }

            return result;
        }

        protected override bool AcceptableVewPort(ulong min, ulong max)
        {
            var region = FindViewPortValuesRange(min, max);
            return (region != null && region.Item2 - region.Item1 >= 10);
        }

        protected override void UpdateViewPort()
        {
            var region = FindViewPortValuesRange(ViewPortMinValue, ViewPortMaxValue);
            ViewPortValues = GetViewPortValues(region);
            ViewPortChanged?.Invoke(this);
        }

        protected Tuple<int, int> FindViewPortValuesRange(ulong minValue, ulong maxValue)
        {
            int i;
            var e = ValuesSeries.Count;
            for (i = 0; i < e && ValuesSeries[i].Timestamp < minValue; i++)
            {
            }

            if (i > 0)
            {
                i--;
            }

            if (i < e)
            {
                int j;
                for (j = i + 1; j < e && ValuesSeries[j].Timestamp <= maxValue; j++)
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

        protected List<MemoryData> GetViewPortValues(Tuple<int, int> region)
        {
            if (region == null)
            {
                return new List<MemoryData>();
            }

            return CompressValues(region, MaxPointsPerChart);
        }
    }
}
