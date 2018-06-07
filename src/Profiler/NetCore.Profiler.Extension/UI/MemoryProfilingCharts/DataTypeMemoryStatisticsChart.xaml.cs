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
using NetCore.Profiler.Cperf.Core.Model;

namespace NetCore.Profiler.Extension.UI.MemoryProfilingCharts
{
    /// <summary>
    /// Data Type Memory Statistics Chart
    /// </summary>
    public partial class DataTypeMemoryStatisticsChart : INotifyPropertyChanged
    {

        private DataTypeMemoryStatisticsChartModel Model { get; } = new DataTypeMemoryStatisticsChartModel();

        public SeriesCollection Series { get; } = new SeriesCollection();

        public DataTypeMemoryStatisticsChart()
        {
            DataContext = this;

            Model.ViewPortChanged += delegate
            {
                UpdateViewPort();
                UpdateScrollBar();
            };

            Model.SeriesAdded = SeriesAdded;
            Model.SeriesRemoved = SeriesRemoved;

            //Formatter = value => Math.Pow(10, value).ToString("N");

            InitializeComponent();

            LiveTimeline.Zoom = ZoomingOptions.None;
            LiveTimeline.DisableAnimations = true;

            LiveTimeline.MouseWheel += OnMouseWheel;

        }

        public Func<double, string> Formatter { get; set; }

        public delegate void SelectionChangedEventHandler(object sender, EventArgs e);

        public event PropertyChangedEventHandler PropertyChanged;

        public void AddSeries(ulong id, string dataType, List<DataTypeMemoryUsage> valueSeries)
        {
            Model.AddSeries(id, dataType, valueSeries);
        }

        public void RemoveSeries(ulong id)
        {
            Model.RemoveSeries(id);
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

        private void SeriesAdded(DataTypeMemoryStatisticsChartModel.SeriesData sd)
        {
            LiveTimeline.Series.Add(
                new LineSeries(Mappers.Xy<DataTypeMemoryUsage>()
                    .X(item => item.Timestamp)
                    .Y(item => PrepareMemorySize(item.MemorySize)))
                {
                    Fill = Brushes.Transparent,
                    LineSmoothness = 0,
                    Title = sd.DataTypeName,
                    PointGeometry = null,
                });
        }

        private void SeriesRemoved(int index)
        {
            LiveTimeline.Series.RemoveAt(index);
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

            for (int i = 0, e = Model.ViewPortValues.Count; i < e; i++)
            {
                LiveTimeline.Series[i].Values = new ChartValues<DataTypeMemoryUsage>(Model.ViewPortValues[i].Values);
            }
        }

        private static double PrepareMemorySize(ulong value)
        {
            return Math.Round((double)value / 1024 / 1024, 4);
            //if (value == 0)
            //{
            //    return -20;
            //}
            //return Math.Log(Math.Round((double)value / 1024 / 1024, 4), 10);
        }

    }
}
