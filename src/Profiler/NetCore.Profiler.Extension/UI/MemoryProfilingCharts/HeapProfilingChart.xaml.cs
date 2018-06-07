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
    /// Heap Profiling Char
    /// </summary>
    public partial class HeapProfilingChart : INotifyPropertyChanged
    {

        private const int LohSeriesIndex = 0;
        private const int Gen2SeriesIndex = 1;
        private const int Gen1SeriesIndex = 2;
        private const int Gen0SeriesIndex = 3;

        private const string Gen0SeriesTitle = "Generation 0";
        private const string Gen1SeriesTitle = "Generation 1";
        private const string Gen2SeriesTitle = "Generation 2";
        private const string LohSeriesTitle = "Large Objects";

        private readonly bool[] _seriesVisibility = { true, true, true, true};

        public Color Gen0SeriesColor { get; } = Color.FromRgb(31, 138, 112);
        public Color Gen1SeriesColor { get; } = Color.FromRgb(190, 219, 57);
        public Color Gen2SeriesColor { get; } = Color.FromRgb(255, 225, 36);
        public Color LohSeriesColor { get; } = Color.FromRgb(0, 67, 88);

        public bool Gen0SeriesEnabled
        {
            get => _seriesVisibility[Gen0SeriesIndex];
            set => SetSeriesVisibility(Gen0SeriesIndex, value);
        }

        public bool Gen1SeriesEnabled
        {
            get => _seriesVisibility[Gen1SeriesIndex];
            set => SetSeriesVisibility(Gen1SeriesIndex, value);
        }

        public bool Gen2SeriesEnabled
        {
            get => _seriesVisibility[Gen2SeriesIndex];
            set => SetSeriesVisibility(Gen2SeriesIndex, value);
        }

        public bool LohSeriesEnabled
        {
            get => _seriesVisibility[LohSeriesIndex];
            set => SetSeriesVisibility(LohSeriesIndex, value);
        }

        private ManagedMemoryProfilingChartModel Model { get; } = new ManagedMemoryProfilingChartModel();

        public SolidColorBrush Gen0SeriesBrush => new SolidColorBrush(Gen0SeriesColor);

        public SolidColorBrush Gen1SeriesBrush => new SolidColorBrush(Gen1SeriesColor);

        public SolidColorBrush Gen2SeriesBrush => new SolidColorBrush(Gen2SeriesColor);

        public SolidColorBrush LohSeriesBrush => new SolidColorBrush(LohSeriesColor);


        private void SetSeriesVisibility(int index, bool value)
        {
            _seriesVisibility[index] = value;
            ((LineSeries)SeriesCollection[index]).Visibility = value ? Visibility.Visible : Visibility.Hidden;
            LiveTimeline.Update();
        }

        public HeapProfilingChart()
        {
            DataContext = this;

            SeriesCollection = new SeriesCollection()
            {
                new StackedAreaSeries(Mappers.Xy<MemoryData>()
                    .X(item => item.Timestamp)
                    .Y(item => PrepareMemorySize(item.LargeObjectsHeap)))
                {
                    //StrokeThickness = 1,
                    Stroke = LohSeriesBrush,
                    Fill = LohSeriesBrush,
                    LineSmoothness = 0,
                    Title = LohSeriesTitle,
                },
                new StackedAreaSeries(Mappers.Xy<MemoryData>()
                    .X(item => item.Timestamp)
                    .Y(item => PrepareMemorySize(item.SmallObjectsHeapGeneration2)))
                {
                    //StrokeThickness = 1,
                    Stroke = Gen2SeriesBrush,
                    Fill = Gen2SeriesBrush,
                    LineSmoothness = 0,
                    Title = Gen2SeriesTitle,
                },
                new StackedAreaSeries(Mappers.Xy<MemoryData>()
                    .X(item => item.Timestamp)
                    .Y(item => PrepareMemorySize(item.SmallObjectsHeapGeneration1)))
                {
                    //StrokeThickness = 1,
                    Stroke = Gen1SeriesBrush,
                    Fill = Gen1SeriesBrush,
                    LineSmoothness = 0,
                    Title = Gen1SeriesTitle,
                },
                new StackedAreaSeries(Mappers.Xy<MemoryData>()
                    .X(item => item.Timestamp)
                    .Y(item => PrepareMemorySize(item.SmallObjectsHeapGeneration0)))
                {
                    //StrokeThickness = 1,
                    Stroke = Gen0SeriesBrush,
                    Fill = Gen0SeriesBrush,
                    LineSmoothness = 0,
                    Title = Gen0SeriesTitle,
                }
            };

            Model.ViewPortChanged += delegate
            {
                UpdateViewPort();
                UpdateScrollBar();
            };

            InitializeComponent();

            LiveTimeline.Zoom = ZoomingOptions.None;
            LiveTimeline.DisableAnimations = true;

            LiveTimeline.MouseWheel += OnMouseWheel;

        }

        public delegate void SelectionChangedEventHandler(object sender, EventArgs e);

        //public event SelectionChangedEventHandler SelectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Series collection for the chart control
        /// </summary>
        public SeriesCollection SeriesCollection { get; private set; }

        public void SetContent(List<MemoryData> valuesSeries)
        {
            Model.SetContent(valuesSeries);
            UpdateSeriesVisibility();
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

            var chartValues = new ChartValues<MemoryData>(Model.ViewPortValues);
            SeriesCollection[0].Values = chartValues;
            SeriesCollection[1].Values = chartValues;
            SeriesCollection[2].Values = chartValues;
            SeriesCollection[3].Values = chartValues;
        }

        private void UpdateSeriesVisibility()
        {
            for (var i = 0; i < 4; i++)
            {
                ((StackedAreaSeries)SeriesCollection[i]).Visibility = _seriesVisibility[i] ? Visibility.Visible : Visibility.Hidden;
            }
        }

        private static double PrepareMemorySize(ulong value)
        {
            return Math.Round((double)value / 1024 / 1024, 6);
        }

    }
}
