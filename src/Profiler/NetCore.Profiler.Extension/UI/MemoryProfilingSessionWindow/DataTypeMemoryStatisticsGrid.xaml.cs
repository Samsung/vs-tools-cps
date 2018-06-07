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
using System.Collections.ObjectModel;
using System.Linq;
using NetCore.Profiler.Analytics.DataProvider;

namespace NetCore.Profiler.Extension.UI.MemoryProfilingSessionWindow
{
    /// <summary>
    /// Data Type Memory Statistics Grid
    /// </summary>
    public partial class DataTypeMemoryStatisticsGrid
    {

        public DataTypeMemoryStatisticsGrid()
        {
            InitializeComponent();
        }

        List<DataTypeMemoryStatisticsItem> Items { get; set; }

        public void SetInputSource(IEnumerable<DataTypeMemoryStatistics> list, Action<ulong, bool> displayValueChanged)
        {
            Items = BuildItems(list, displayValueChanged);
            DataTypesGrid.ItemsSource = Items;
            for (int i = 0; i < Items.Count && i < 3; i++)
            {
                Items[i].Display = true;
            }
        }

        private List<DataTypeMemoryStatisticsItem> BuildItems(IEnumerable<DataTypeMemoryStatistics> list, Action<ulong, bool> displayValueChanged)
        {
            return list.Select(s => new DataTypeMemoryStatisticsItem
            {
                DataTypeId = s.DataTypeId,
                DataTypeName = s.DataTypeName,
                ObjectsCountMax = s.ObjectsCountMax,
                ObjectsCountAvg = s.ObjectsCountAvg,
                MemorySizeMax = s.MemorySizeMax,
                MemorySizeAvg = s.MemorySizeAvg,
                DisplayValueChanged = displayValueChanged
            }).ToList();
        }
    }
}
