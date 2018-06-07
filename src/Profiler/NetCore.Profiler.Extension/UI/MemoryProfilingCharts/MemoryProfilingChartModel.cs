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
    public class MemoryProfilingChartModel : ManagedMemoryProfilingChartModelBase, IMemoryProfilingChartModelBase
    {

        private const int MaxPointsPerChart = 1000;

        protected List<ManagedMemoryData> ManagedValuesSeries;

        protected List<UnmanagedMemoryData> UnmanagedValuesSeries;

        public List<ManagedMemoryData> ManagedViewPortValues { get; private set; } = new List<ManagedMemoryData>();

        public List<UnmanagedMemoryData> UnmanagedViewPortValues { get; private set; } = new List<UnmanagedMemoryData>();

        public event ViewPortChangedEventHandler ViewPortChanged;

        public void SetContent(List<ManagedMemoryData> managedValuesSeries, List<UnmanagedMemoryData> unmanagedValuesSeries)
        {
            ManagedValuesSeries = managedValuesSeries;
            UnmanagedValuesSeries = unmanagedValuesSeries;

            RangeMinValue = ViewPortMinValue = Math.Min(ManagedValuesSeries.Count > 0
                    ? ManagedValuesSeries[0].Timestamp
                    : 0,
                UnmanagedValuesSeries.Count > 0
                    ? UnmanagedValuesSeries[0].Timestamp
                    : 0);

            RangeMaxValue = ViewPortMaxValue = Math.Max(ManagedValuesSeries.Count > 0
                    ? ManagedValuesSeries[ManagedValuesSeries.Count - 1].Timestamp
                    : 0,
                UnmanagedValuesSeries.Count > 0
                    ? UnmanagedValuesSeries[UnmanagedValuesSeries.Count - 1].Timestamp
                    : 0);

            UpdateViewPort();
        }


        private List<ManagedMemoryData> CompressManagedValues(Tuple<int, int> range, int maxLength)
        {
            var start = range.Item1;
            var end = range.Item2;
            var count = end - start;
            if (count <= maxLength)
            {
                if (start == 0 && end == ManagedValuesSeries.Count + 1)
                {
                    return ManagedValuesSeries;
                }

                return ManagedValuesSeries.GetRange(start, count);
            }

            var itemsToProcess = count / maxLength + 1;
            var result = new List<ManagedMemoryData>();

            for (var i = start; i < end;)
            {
                result.Add(new ManagedMemoryData
                {
                    Timestamp = ManagedValuesSeries[i].Timestamp,
                    HeapAllocated = ManagedValuesSeries[i].HeapAllocated,
                    HeapReserved = ManagedValuesSeries[i].HeapReserved,
                });
                for (var j = 0; j < itemsToProcess && i < end; i++, j++)
                {
                }
            }

            return result;
        }

        private List<UnmanagedMemoryData> CompressUnmanagedValues(Tuple<int, int> range, int maxLength)
        {
            var start = range.Item1;
            var end = range.Item2;
            var count = end - start;
            if (count <= maxLength)
            {
                if (start == 0 && end == UnmanagedValuesSeries.Count + 1)
                {
                    return UnmanagedValuesSeries;
                }

                return UnmanagedValuesSeries.GetRange(start, count);
            }

            var itemsToProcess = count / maxLength + 1;
            var result = new List<UnmanagedMemoryData>();

            for (var i = start; i < end;)
            {
                result.Add(new UnmanagedMemoryData
                {
                    Timestamp = UnmanagedValuesSeries[i].Timestamp,
                    Unmanaged = UnmanagedValuesSeries[i].Unmanaged,
                });
                for (var j = 0; j < itemsToProcess && i < end; i++, j++)
                {
                }
            }

            return result;
        }

        protected override bool AcceptableVewPort(ulong min, ulong max)
        {
            var region = FindManagedViewPortValuesRange(min, max);
            if (region != null && region.Item2 - region.Item1 >= 10)
            {
                return true;
            }

            region = FindUnmanagedViewPortValuesRange(min, max);
            return (region != null && region.Item2 - region.Item1 >= 10);
        }

        protected override void UpdateViewPort()
        {
            var region = FindManagedViewPortValuesRange(ViewPortMinValue, ViewPortMaxValue);
            ManagedViewPortValues = GetManagedViewPortValues(region);
            region = FindUnmanagedViewPortValuesRange(ViewPortMinValue, ViewPortMaxValue);
            UnmanagedViewPortValues = GetUnmanagedViewPortValues(region);

            ViewPortChanged?.Invoke(this);
        }

        protected Tuple<int, int> FindManagedViewPortValuesRange(ulong minValue, ulong maxValue)
        {
            int i;
            var e = ManagedValuesSeries.Count;
            for (i = 0; i < e && ManagedValuesSeries[i].Timestamp < minValue; i++)
            {
            }

            if (i > 0)
            {
                i--;
            }

            if (i < e)
            {
                int j;
                for (j = i + 1; j < e && ManagedValuesSeries[j].Timestamp <= maxValue; j++)
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

        protected Tuple<int, int> FindUnmanagedViewPortValuesRange(ulong minValue, ulong maxValue)
        {
            int i;
            var e = UnmanagedValuesSeries.Count;
            for (i = 0; i < e && UnmanagedValuesSeries[i].Timestamp < minValue; i++)
            {
            }

            if (i > 0)
            {
                i--;
            }

            if (i < e)
            {
                int j;
                for (j = i + 1; j < e && UnmanagedValuesSeries[j].Timestamp <= maxValue; j++)
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

        protected List<ManagedMemoryData> GetManagedViewPortValues(Tuple<int, int> region)
        {
            if (region == null)
            {
                return new List<ManagedMemoryData>();
            }

            return CompressManagedValues(region, MaxPointsPerChart);
        }

        protected List<UnmanagedMemoryData> GetUnmanagedViewPortValues(Tuple<int, int> region)
        {
            if (region == null)
            {
                return new List<UnmanagedMemoryData>();
            }

            return CompressUnmanagedValues(region, MaxPointsPerChart);
        }

    }
}
