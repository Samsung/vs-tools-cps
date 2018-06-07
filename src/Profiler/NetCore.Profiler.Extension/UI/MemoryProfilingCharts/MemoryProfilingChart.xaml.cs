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
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using NetCore.Profiler.Analytics.Model;

namespace NetCore.Profiler.Extension.UI.MemoryProfilingCharts
{
    /// <summary>
    /// Memory Profiling Chart
    /// </summary>
    public partial class MemoryProfilingChart : INotifyPropertyChanged
    {

        private const string HeapAllocatedSeriesTitle = "Heap Allocated";
        private const string HeapReservedSeriesTitle = "Heap Reserved";
        private const string UnmanagedSeriesTitle = "Unmanaged";

        public Color HeapAllocatedSeriesColor { get; } = Color.FromRgb(31, 138, 112);
        public Color HeapReservedSeriesColor { get; } = Color.FromRgb(190, 219, 57);
        public Color UnmanagedSeriesColor { get; } = Color.FromRgb(255, 225, 36);

        private MemoryProfilingChartModel Model { get; } = new MemoryProfilingChartModel();

        public SolidColorBrush HeapAllocatedSeriesBrush => new SolidColorBrush(HeapAllocatedSeriesColor);

        public SolidColorBrush HeapReservedSeriesBrush => new SolidColorBrush(HeapReservedSeriesColor);

        public SolidColorBrush UnmanagedSeriesBrush => new SolidColorBrush(UnmanagedSeriesColor);

        public MemoryProfilingChart()
        {
            DataContext = this;

            SeriesCollection = new SeriesCollection()
            {
                new LineSeries(Mappers.Xy<ManagedMemoryData>()
                    .X(item => item.Timestamp)
                    .Y(item => PrepareMemorySize(item.HeapAllocated)))
                {
                    Stroke = HeapAllocatedSeriesBrush,
                    Fill = Brushes.Transparent,
                    LineSmoothness = 0,
                    Title = HeapAllocatedSeriesTitle,
                },
                new LineSeries(Mappers.Xy<ManagedMemoryData>()
                    .X(item => item.Timestamp)
                    .Y(item => PrepareMemorySize(item.HeapReserved)))
                {
                    Stroke = HeapReservedSeriesBrush,
                    Fill = Brushes.Transparent,
                    LineSmoothness = 0,
                    Title = HeapReservedSeriesTitle,
                },
                new LineSeries(Mappers.Xy<UnmanagedMemoryData>()
                    .X(item => item.Timestamp)
                    .Y(item => PrepareMemorySize(item.Unmanaged)))
                {
                    Stroke = UnmanagedSeriesBrush,
                    Fill = Brushes.Transparent,
                    LineSmoothness = 0,
                    Title = UnmanagedSeriesTitle,
                },
            };

            Model.ViewPortChanged += delegate
            {
                UpdateViewPort();
                UpdateScrollBar();
            };

            Formatter = value => Math.Pow(10, value).ToString("N");

            InitializeComponent();

            LiveTimeline.Zoom = ZoomingOptions.None;
            LiveTimeline.DisableAnimations = true;

            LiveTimeline.MouseWheel += OnMouseWheel;

        }

        public Func<double, string> Formatter { get; set; }

        public delegate void SelectionChangedEventHandler(object sender, EventArgs e);

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Series collection for the chart control
        /// </summary>
        public SeriesCollection SeriesCollection { get; private set; }

        public void SetContent(List<ManagedMemoryData> managedValuesSeries, List<UnmanagedMemoryData> unmanagedValuesSeries)
        {
            Model.SetContent(managedValuesSeries, unmanagedValuesSeries);
        }

        /// <summary>
        /// ScrollBar offset. ScrallBar Value property is bound to it.
        /// </summary>
        public double Offset
        {
            get => Model?.Offset ?? 0;
            set
            {
                if (Model != null)
                {
                    Model.Offset = (ulong)value;
                }
            }
        }

        public void ResetZoom()
        {
            Model.ResetZoom();
        }


        private void NotifyOffsetChange()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Offset)));
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                var relativeX = (e.GetPosition(LiveTimeline).X - LiveTimeline.Model.DrawMargin.Left) /
                                LiveTimeline.Model.DrawMargin.Width;
                var itemUnderCursor = Model.ViewPortMinValue +
                                      (Model.ViewPortMaxValue - Model.ViewPortMinValue) *
                                      relativeX;
                if (e.Delta > 0)
                {
                    Model.ZoomIn(itemUnderCursor, 0.5);
                }
                else
                {
                    Model.ZoomOut(itemUnderCursor, 0.5);
                }
            }
            else if (Keyboard.Modifiers == ModifierKeys.None)
            {
                double delta;
                if (e.Delta > 0)
                {
                    delta = Math.Min(ScrollBar.SmallChange, ScrollBar.Maximum - ScrollBar.Value);
                }
                else
                {
                    delta = -Math.Min(ScrollBar.SmallChange, ScrollBar.Value - ScrollBar.Minimum);
                }

                if (Math.Abs(delta) > 0)
                {
                    Offset += delta;
                }
            }
        }

        private void OnClickResetZoom(object sender, RoutedEventArgs e)
        {
            ResetZoom();
        }

        private void UpdateScrollBar()
        {
            var l = Model.ViewPortMaxValue - Model.ViewPortMinValue;
            ScrollBar.Minimum = Model.RangeMinValue;
            ScrollBar.Maximum = Model.RangeMaxValue - l;
            var change = Math.Max(10, (int)(l / 5));
            ScrollBar.SmallChange = change;
            ScrollBar.LargeChange = change * 10;
            ScrollBar.ViewportSize = l;
            NotifyOffsetChange();
        }

        private void UpdateViewPort()
        {
            if (Model.ViewPortMaxValue - Model.ViewPortMinValue == 0)
            {
                LiveTimeline.AxisX[0].MinValue = double.NaN;
                LiveTimeline.AxisX[0].MaxValue = double.NaN;
            }
            else
            {
                LiveTimeline.AxisX[0].MinValue = Model.ViewPortMinValue;
                LiveTimeline.AxisX[0].MaxValue = Model.ViewPortMaxValue;
            }

            var chartValues = new ChartValues<ManagedMemoryData>(Model.ManagedViewPortValues);
            SeriesCollection[0].Values = chartValues;
            SeriesCollection[1].Values = chartValues;
            SeriesCollection[2].Values = new ChartValues<UnmanagedMemoryData>(Model.UnmanagedViewPortValues);
        }

        private static double PrepareMemorySize(ulong value)
        {
            if (value == 0)
            {
                return -20;
            }

            return Math.Log(Math.Round((double)value / 1024 / 1024, 3), 10);
        }

    }
}
