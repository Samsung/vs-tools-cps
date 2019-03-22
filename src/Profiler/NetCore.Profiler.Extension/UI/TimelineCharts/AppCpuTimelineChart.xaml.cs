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
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using NetCore.Profiler.Cperf.Core.Model;

namespace NetCore.Profiler.Extension.UI.TimelineCharts
{
    /// <summary>
    /// Application CPU Timeline Chart
    /// </summary>
    public partial class AppCpuTimelineChart : INotifyPropertyChanged
    {
        public static readonly DependencyProperty CpuUtilizationSourceProperty = DependencyProperty.Register(
            "CpuUtilizationSource",
            typeof(AppCpuTimelineChartModel),
            typeof(AppCpuTimelineChart),
            new PropertyMetadata(default(AppCpuTimelineChartModel), OnCpuUtilizationSourceChanged));

        private const string CpuSeriesTitle = "CPU (%)";

        private static readonly SolidColorBrush PausedSectionBrush = new SolidColorBrush(Colors.DarkGray) { Opacity = 0.4 };

        private static readonly SolidColorBrush SelectionSectionBrush = new SolidColorBrush(Colors.MediumPurple) { Opacity = 0.4 };

        private static readonly SolidColorBrush CpuSeriesStrokeBrush = new SolidColorBrush(Colors.Blue) { Opacity = 0.4 };

        private static readonly SolidColorBrush CpuSeriesFillBrush = new SolidColorBrush(Colors.Blue) { Opacity = 0.4 };

        private double? _selectionStart;

        private double? _selectionEnd;

        private readonly AxisSection _selectionSection = new AxisSection
        {
            Fill = SelectionSectionBrush
        };

        public AppCpuTimelineChart()
        {
            DataContext = this;

            SeriesCollection = new SeriesCollection()
            {
                new LineSeries(Mappers.Xy<CpuUtilization>()
                    .X(item => item.TimeMilliseconds / 1000.0)
                    .Y(item => item.Utilization))
                {
                    StrokeThickness = 1,
                    Stroke = CpuSeriesStrokeBrush,
                    Fill = CpuSeriesFillBrush,
                    PointGeometry = null,
                    Title = CpuSeriesTitle,
                    ScalesYAt = 0,
                    Values = new ChartValues<CpuUtilization>()
                }
            };

            InitializeComponent();

            LiveTimeline.Zoom = ZoomingOptions.None;
            LiveTimeline.DisableAnimations = true;

            LiveTimeline.MouseWheel += OnMouseWheel;
            LiveTimeline.MouseMove += OnMouseMove;
            LiveTimeline.MouseLeftButtonUp += OnMouseUp;
        }

        public delegate void SelectionChangedEventHandler(object sender, EventArgs e);

        public event SelectionChangedEventHandler SelectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public AppCpuTimelineChartModel CpuUtilizationSource
        {
            get => (AppCpuTimelineChartModel)GetValue(CpuUtilizationSourceProperty);
            set => SetValue(CpuUtilizationSourceProperty, value);
        }

        /// <summary>
        /// Series collection for the chart control
        /// </summary>
        public SeriesCollection SeriesCollection { get; }

        /// <summary>
        /// ScrollBar offset. ScrollBar Value property is bound to it.
        /// </summary>
        public double Offset // seconds
        {
            get => CpuUtilizationSource?.OffsetMilliseconds / 1000.0 ?? 0;
            set
            {
                if (CpuUtilizationSource != null)
                {
                    CpuUtilizationSource.OffsetMilliseconds = (ulong)Math.Round(value * 1000);
                }
            }
        }

        /// <summary>
        /// Start of the selected region
        /// </summary>
        public double SelectionStartSeconds => (double.IsNaN(_selectionSection.Value) ? 0 : _selectionSection.Value);

        /// <summary>
        /// The width of the selected region
        /// </summary>
        public double SelectionWidthSeconds => (double.IsNaN(_selectionSection.SectionWidth) ? 0 : _selectionSection.SectionWidth);

        /// <summary>
        /// End of the selected region
        /// </summary>
        public double SelectionEndSeconds => SelectionStartSeconds + SelectionWidthSeconds;

        public string SelectionLabel => SelectionWidthSeconds == 0 ? "<empty>" : $"{SelectionStartSeconds:F03} sec .. {SelectionEndSeconds:F03} sec";

        public bool ZoomToSelectionPossible => SelectionWidthSeconds != 0;

        public bool RevealSelectionPossible => SelectionWidthSeconds != 0;

        public void ZoomToSelection()
        {
            if (SelectionWidthSeconds == 0)
            {
                return;
            }

            CpuUtilizationSource.ZoomTo(
                (ulong)Math.Round(SelectionStartSeconds * 1000), (ulong)Math.Round(SelectionEndSeconds * 1000));
        }

        public void ResetZoom()
        {
            CpuUtilizationSource.ResetZoom();
        }

        public void RevealSelection()
        {
            if (SelectionWidthSeconds == 0)
            {
                return;
            }

            CpuUtilizationSource.RevealSelection(
                (ulong)Math.Round(SelectionStartSeconds * 1000), (ulong)Math.Round(SelectionEndSeconds * 1000));
        }

        private static void OnCpuUtilizationSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var chart = (AppCpuTimelineChart)o;
            if (chart.CpuUtilizationSource != null)
            {
                chart.CpuUtilizationSource.ViewPortChanged += delegate
                    {
                        chart.UpdateViewPort();
                        chart.UpdateScrollBar();
                    };
                chart.LiveTimeline.AxisY[0].Title = String.Format($"CPU (% of {chart.CpuUtilizationSource.CpuCoreCount} processors)");
                chart.LiveTimeline.AxisX[0].Sections.Add(chart._selectionSection);
                chart.LiveTimeline.AxisX[0].Sections.AddRange(chart.CpuUtilizationSource.PauseSections.Select(
                    s => new AxisSection
                    {
                        Value = s.StartSeconds,
                        SectionWidth = s.WidthSeconds,
                        Fill = PausedSectionBrush
                    }));
                chart.UpdateViewPort();
                chart.UpdateScrollBar();
            }
        }

        private void NotifyOffsetChange()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Offset)));
        }

        private void NotifySelectionChange()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionLabel)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ZoomToSelectionPossible)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RevealSelectionPossible)));
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var x = e.GetPosition(LiveTimeline).X;
                if (x < LiveTimeline.Model.DrawMargin.Left)
                {
                    x = LiveTimeline.Model.DrawMargin.Left;
                }
                else if (x > LiveTimeline.Model.DrawMargin.Left + LiveTimeline.Model.DrawMargin.Width)
                {
                    x = LiveTimeline.Model.DrawMargin.Left + LiveTimeline.Model.DrawMargin.Width + 1;
                }

                if (_selectionStart == null)
                {
                    _selectionStart = x;
                }
                else
                {
                    _selectionEnd = x;
                }

                if (_selectionEnd.HasValue && Math.Abs(_selectionEnd.Value - _selectionStart.Value) > 2)
                {
                    UpdateSelection();
                }
            }

            e.Handled = true;
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_selectionStart != null && _selectionEnd != null)
            {
                _selectionStart = null;
                _selectionEnd = null;
                ScheduleSelectionChanged();
            }
            else
            {
                var selectionExists = _selectionSection.SectionWidth > 0;
                UpdateSelection();
                if (selectionExists)
                {
                    ScheduleSelectionChanged();
                }
            }
        }

        private void ScheduleSelectionChanged()
        {
            //Dispatcher.InvokeAsync(() => SelectionChanged?.Invoke(this),DispatcherPriority.ApplicationIdle);
            SelectionChanged?.Invoke(this, null);
        }

        private void UpdateSelection()
        {
            double sectionValue = 0;
            double sectionWidth = 0;
            if (_selectionStart.HasValue && _selectionEnd.HasValue)
            {
                double start = Math.Max(ChartFunctions.FromPlotArea(_selectionStart.Value, AxisOrientation.X, LiveTimeline.Model), 0);
                double end = Math.Max(ChartFunctions.FromPlotArea(_selectionEnd.Value, AxisOrientation.X, LiveTimeline.Model), 0);
                double diff = end - start;
                sectionValue = diff >= 0 ? start : end;
                sectionWidth = Math.Abs(diff);
            }

            _selectionSection.Value = sectionValue;
            _selectionSection.SectionWidth = sectionWidth;
            NotifySelectionChange();
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                var relativeX = (e.GetPosition(LiveTimeline).X - LiveTimeline.Model.DrawMargin.Left) /
                                LiveTimeline.Model.DrawMargin.Width;
                var itemUnderCursor = CpuUtilizationSource.ViewPortMinValueMilliseconds +
                                      (CpuUtilizationSource.ViewPortMaxValueMilliseconds - CpuUtilizationSource.ViewPortMinValueMilliseconds) *
                                      relativeX;
                if (e.Delta > 0)
                {
                    CpuUtilizationSource.ZoomIn(itemUnderCursor, 0.5);
                }
                else
                {
                    CpuUtilizationSource.ZoomOut(itemUnderCursor, 0.5);
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
                    delta = -Math.Min(ScrollBar.SmallChange, ScrollBar.Value);
                }

                if (Math.Abs(delta) > 0)
                {
                    Offset += delta;
                }
            }
        }

        private void OnClickZoomToSelection(object sender, RoutedEventArgs e)
        {
            ZoomToSelection();
        }

        private void OnClickResetZoom(object sender, RoutedEventArgs e)
        {
            ResetZoom();
        }

        private void OnClickRevealSelection(object sender, RoutedEventArgs e)
        {
            RevealSelection();
        }

        private void UpdateScrollBar()
        {
            ulong range_msec = CpuUtilizationSource.ViewPortMaxValueMilliseconds - CpuUtilizationSource.ViewPortMinValueMilliseconds;
            ScrollBar.Maximum = (CpuUtilizationSource.RangeMaxValueMilliseconds - range_msec) / 1000.0;
            double change = Math.Max(10, range_msec / 5) / 1000.0;
            ScrollBar.SmallChange = change;
            ScrollBar.LargeChange = change * 10;
            ScrollBar.ViewportSize = range_msec / 1000.0;
            NotifyOffsetChange();
        }

        private void UpdateViewPort()
        {
            if (CpuUtilizationSource.ViewPortMaxValueMilliseconds - CpuUtilizationSource.ViewPortMinValueMilliseconds == 0)
            {
                LiveTimeline.AxisX[0].MinValue = double.NaN;
                LiveTimeline.AxisX[0].MaxValue = double.NaN;
            }
            else
            {
                LiveTimeline.AxisX[0].MinValue = CpuUtilizationSource.ViewPortMinValueMilliseconds / 1000.0;
                LiveTimeline.AxisX[0].MaxValue = CpuUtilizationSource.ViewPortMaxValueMilliseconds / 1000.0;
            }

            SeriesCollection[0].Values = new ChartValues<CpuUtilization>(CpuUtilizationSource.ViewPortValues);
        }
    }
}
