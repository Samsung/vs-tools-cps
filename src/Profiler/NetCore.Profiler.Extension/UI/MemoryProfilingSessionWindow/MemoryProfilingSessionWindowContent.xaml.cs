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

namespace NetCore.Profiler.Extension.UI.MemoryProfilingSessionWindow
{
    /// <summary>
    /// Memory Profiling Session Window Content
    /// </summary>
    public partial class MemoryProfilingSessionWindowContent
    {
        private IMemoryProfilingSession _session;

        public MemoryProfilingSessionWindowContent()
        {
            InitializeComponent();
        }

        public void SetActiveSession(IMemoryProfilingSession session)
        {
            _session = session;
            HeapChart.SetContent(session.HeapStatistics);
            MemoryChart.SetContent(session.ManagedMemoryStatistics, session.UnmanagedMemoryStatistics);
            DataTypeAllocationStatisticsGrid.SetInputSource(session.DataTypeAllocationStatistics);
            DataTypeMemoryStatisticsGrid.SetInputSource(session.DataTypeMemoryStatistics, DisplayValueChanged);
        }

        private void DisplayValueChanged(ulong id, bool value)
        {
            if (value)
            {
                var x = _session.GetGarbageCollectorSamples(id);
                if (x != null)
                {
                    DataTypeMemoryStatisticsChart.AddSeries(id, _session.DataTypes[id], x);
                }
            }
            else
            {
                DataTypeMemoryStatisticsChart.RemoveSeries(id);
            }
        }

    }
}
