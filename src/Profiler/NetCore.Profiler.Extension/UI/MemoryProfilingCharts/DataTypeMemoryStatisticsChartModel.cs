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
using System.Linq;
using NetCore.Profiler.Cperf.Core.Model;

namespace NetCore.Profiler.Extension.UI.MemoryProfilingCharts
{
    public class DataTypeMemoryStatisticsChartModel : ManagedMemoryProfilingChartModelBase, IMemoryProfilingChartModelBase
    {

        private const int MaxPointsPerChart = 1000;

        private List<SeriesData> _series = new List<SeriesData>();

        public List<SeriesData> ViewPortValues = new List<SeriesData>();

        public event ViewPortChangedEventHandler ViewPortChanged;

        public Action<SeriesData> SeriesAdded { get; set; }

        public Action<int> SeriesRemoved { get; set; }

        public void AddSeries(ulong id, string dataType, List<DataTypeMemoryUsage> valueSeries)
        {
            var sd = new SeriesData
            {
                DataTypeId = id,
                DataTypeName = dataType,
                Values = valueSeries
            };
            _series.Add(sd);

            UpdateRanges();
            SeriesAdded?.Invoke(sd);
            UpdateViewPort();
        }

        public void RemoveSeries(ulong id)
        {
            int index = -1;
            for (int i = 0, j = _series.Count; i < j; i++)
            {
                if (_series[i].DataTypeId == id)
                {
                    index = i;
                    _series.RemoveAt(i);
                    break;
                }
            }

            if (index >= 0)
            {
                UpdateRanges();
                SeriesRemoved?.Invoke(index);
                UpdateViewPort();
            }
        }

        private List<DataTypeMemoryUsage> CompressValues(List<DataTypeMemoryUsage> valueSeries, Tuple<int, int> range, int maxLength)
        {
            var start = range.Item1;
            var end = range.Item2;
            var count = end - start;
            if (count <= maxLength)
            {
                if (start == 0 && end == valueSeries.Count + 1)
                {
                    return valueSeries;
                }

                return valueSeries.GetRange(start, count);
            }

            var itemsToProcess = count / maxLength + 1;
            var result = new List<DataTypeMemoryUsage>();

            for (var i = start; i < end;)
            {
                result.Add(new DataTypeMemoryUsage()
                {
                    TimeMilliseconds = valueSeries[i].TimeMilliseconds,
                    ObjectsCount = valueSeries[i].ObjectsCount,
                    MemorySize = valueSeries[i].MemorySize
                });
                for (var j = 0; j < itemsToProcess && i < end; i++, j++)
                {
                }
            }

            return result;
        }

        protected override bool AcceptableVewPort(ulong min, ulong max)
        {
            return _series.Select(s => FindViewPortValuesRange(s.Values, min, max)).Any(region => region != null && region.Item2 - region.Item1 >= 10);
        }

        protected override void UpdateViewPort()
        {
            ViewPortValues.Clear();

            foreach (var s in _series)
            {
                var region = FindViewPortValuesRange(s.Values, ViewPortMinValue, ViewPortMaxValue);
                var values = GetViewPortValues(s.Values, region);
                ViewPortValues.Add(new SeriesData
                {
                    DataTypeId = s.DataTypeId,
                    DataTypeName = s.DataTypeName,
                    Values = values
                });
            }

            ViewPortChanged?.Invoke(this);
        }

        protected Tuple<int, int> FindViewPortValuesRange(List<DataTypeMemoryUsage> valueSeries, ulong minValue, ulong maxValue)
        {
            int i;
            var e = valueSeries.Count;
            for (i = 0; i < e && valueSeries[i].TimeMilliseconds < minValue; i++)
            {
            }

            if (i > 0)
            {
                i--;
            }

            if (i < e)
            {
                int j;
                for (j = i + 1; j < e && valueSeries[j].TimeMilliseconds <= maxValue; j++)
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

        protected List<DataTypeMemoryUsage> GetViewPortValues(List<DataTypeMemoryUsage> valueSeries, Tuple<int, int> region)
        {
            if (region == null)
            {
                return new List<DataTypeMemoryUsage>();
            }

            return CompressValues(valueSeries, region, MaxPointsPerChart);
        }


        private void UpdateRanges()
        {
            ulong min = 0;
            ulong max = 0;

            foreach (var s in _series)
            {
                min = Math.Min(
                    min,
                    s.Values.Count > 0
                        ? s.Values[0].TimeMilliseconds
                        : 0);

                max = Math.Max(
                    max,
                    s.Values.Count > 0
                        ? s.Values[s.Values.Count - 1].TimeMilliseconds
                        : 0);
            }

            RangeMinValue = min;
            RangeMaxValue = max;

            ViewPortMinValue = Math.Max(ViewPortMinValue, RangeMinValue);
            ViewPortMaxValue = Math.Min(ViewPortMaxValue > 0 ? ViewPortMaxValue : ulong.MaxValue, RangeMaxValue);

        }

        public class SeriesData
        {
            public ulong DataTypeId { get; set; }

            public string DataTypeName { get; set; }

            public List<DataTypeMemoryUsage> Values { get; set; }
        }

    }
}
