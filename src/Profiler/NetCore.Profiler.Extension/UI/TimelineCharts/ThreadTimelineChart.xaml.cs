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

using System.Linq;
using System.Windows;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using NetCore.Profiler.Cperf.Core.Model;
using NetCore.Profiler.Extension.Session;

namespace NetCore.Profiler.Extension.UI.TimelineCharts
{
    /// <summary>
    /// Thread Timeline Chart
    /// </summary>
    public partial class ThreadTimelineChart
    {
        public static readonly DependencyProperty ReferenceTimelineProperty = DependencyProperty.Register(
                "ReferenceTimeline",
                typeof(ITimelineChartModelBase),
                typeof(ThreadTimelineChart),
                new PropertyMetadata(null, OnReferenceTimelineChanged));

        public static readonly DependencyProperty CpuUtilizationSourceProperty = DependencyProperty.Register(
            "CpuUtilizationSource",
            typeof(ThreadCpuTimelineChartModel),
            typeof(ThreadTimelineChart),
            new PropertyMetadata(default(AppCpuTimelineChartModel), OnCpuUtilizationSourceChanged));

        public static readonly DependencyProperty GcJobsSourceProperty = DependencyProperty.Register(
            "GcJobsSource",
            typeof(ThreadClrJobTimelineChartModel),
            typeof(ThreadTimelineChart),
            new PropertyMetadata(default(ThreadClrJobTimelineChartModel), OnGcJobsSourceSourceChanged));

        public static readonly DependencyProperty JitJobsSourceProperty = DependencyProperty.Register(
            "JitJobsSource",
            typeof(ThreadClrJobTimelineChartModel),
            typeof(ThreadTimelineChart),
            new PropertyMetadata(default(ThreadClrJobTimelineChartModel), OnJitJobsSourceSourceChanged));

        public static readonly DependencyProperty TimeLineTypeProperty = DependencyProperty.Register(
            "TimeLineType",
            typeof(TimeLineType),
            typeof(ThreadTimelineChart),
            new PropertyMetadata(TimeLineType.CpuUtilization, OnTimeLineTypeChanged));

        private static readonly SolidColorBrush PausedSectionBrush = new SolidColorBrush(Colors.DarkGray) { Opacity = 0.4 };

        private SolidColorBrush _cpuNormalBrush;

        private SolidColorBrush _cpuLightBrush;

        private SolidColorBrush _gcFillBrush;

        private SolidColorBrush _jitFillBrush;

        private readonly FilledStepLineSeries _gcSeries = new FilledStepLineSeries
        {
            Configuration = Mappers.Xy<ClrJobItem>()
                .X(item => item.TimeMilliseconds / 1000.0)
                .Y(item => item.Value),
            Fill = null,
            StrokeThickness = 1,
            PointGeometry = null,
            Title = "Gc",
            ScalesYAt = 0,
            Values = new ChartValues<ClrJobItem>(),
        };

        private readonly FilledStepLineSeries _jitSeries = new FilledStepLineSeries
        {
            Configuration = Mappers.Xy<ClrJobItem>()
                .X(item => item.TimeMilliseconds / 1000.0)
                .Y(item => item.Value),
            Fill = null,
            StrokeThickness = 1,
            PointGeometry = null,
            Title = "Jit",
            ScalesYAt = 0,
            Values = new ChartValues<ClrJobItem>(),
        };

        private readonly LineSeries _cpuSeries = new LineSeries(
            Mappers.Xy<CpuUtilization>()
                .X(item => item.TimeMilliseconds / 1000.0)
                .Y(item => item.Utilization))
        {
            Stroke = null,
            Fill = null,
            StrokeThickness = 1,
            PointGeometry = null,
            Title = "Cpu",
            ScalesYAt = 1,
            Values = new ChartValues<CpuUtilization>()
        };

        public ThreadTimelineChart()
        {
            InitializeComponent();
            LiveTimeline.Series = new SeriesCollection { _gcSeries, _jitSeries, _cpuSeries };
        }

        public ITimelineChartModelBase ReferenceTimeline
        {
            get => (ITimelineChartModelBase)GetValue(ReferenceTimelineProperty);
            set => SetValue(ReferenceTimelineProperty, value);
        }

        public ThreadCpuTimelineChartModel CpuUtilizationSource
        {
            get => (ThreadCpuTimelineChartModel)GetValue(CpuUtilizationSourceProperty);
            set => SetValue(CpuUtilizationSourceProperty, value);
        }

        public Color CpuSeriesColor
        {
            set
            {
                _cpuNormalBrush = new SolidColorBrush(value) { Opacity = 0.4 };
                _cpuNormalBrush.Freeze();
                _cpuLightBrush = new SolidColorBrush(value) { Opacity = 0.1 };
                _cpuLightBrush.Freeze();
            }
        }

        public ThreadClrJobTimelineChartModel GcJobsSource
        {
            get => (ThreadClrJobTimelineChartModel)GetValue(GcJobsSourceProperty);
            set => SetValue(GcJobsSourceProperty, value);
        }

        public Color GcSeriesColor
        {
            set
            {
                _gcFillBrush = new SolidColorBrush(value) { Opacity = 0.4 };
                _gcFillBrush.Freeze();
            }
        }

        public ThreadClrJobTimelineChartModel JitJobsSource
        {
            get => (ThreadClrJobTimelineChartModel)GetValue(JitJobsSourceProperty);
            set => SetValue(JitJobsSourceProperty, value);
        }

        public Color JitSeriesColor
        {
            set
            {
                _jitFillBrush = new SolidColorBrush(value) { Opacity = 0.4 };
                _jitFillBrush.Freeze();
            }
        }

        public TimeLineType TimeLineType
        {
            get => (TimeLineType)GetValue(TimeLineTypeProperty);
            set
            {
                SetValue(TimeLineTypeProperty, value);
                UpdateTimeLineType();
            }
        }

        private static void OnGcJobsSourceSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((ThreadTimelineChart)o).UpdateGcJobsSource();
        }

        private void UpdateGcJobsSource()
        {
            if (GcJobsSource != null)
            {
                GcJobsSource.ViewPortChanged += sender => UpdateGcViewPort();
                UpdateGcViewPort();
            }
        }

        private static void OnJitJobsSourceSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((ThreadTimelineChart)o).UpdateJitJobsSource();
        }

        private void UpdateJitJobsSource()
        {
            if (JitJobsSource != null)
            {
                JitJobsSource.ViewPortChanged += sender => UpdateJitViewPort();
                UpdateJitViewPort();
            }
        }

        private static void OnCpuUtilizationSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((ThreadTimelineChart)o).UpdateCpuUtilizationSource();
        }

        private void UpdateCpuUtilizationSource()
        {
            if (CpuUtilizationSource != null)
            {
                CpuUtilizationSource.ViewPortChanged += sender => UpdateCpuViewPort();
                SetCpuNormalColor();
                UpdateCpuViewPort();
            }
        }

        private static void OnReferenceTimelineChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((ThreadTimelineChart)o).UpdateReferenceTimelineProperty();
        }

        private void UpdateReferenceTimelineProperty()
        {
            if (ReferenceTimeline == null)
            {
                LiveTimeline.AxisX[0].Sections.Clear();
                LiveTimeline.AxisX[0].MinValue = double.NaN;
                LiveTimeline.AxisX[0].MaxValue = double.NaN;
            }
            else
            {
                ReferenceTimeline.ViewPortChanged += delegate(ITimelineChartModelBase sender)
                {
                    LiveTimeline.AxisX[0].MinValue = sender.ViewPortMinValueMilliseconds / 1000.0;
                    if (sender.ViewPortMaxValueMilliseconds != 0)
                    {
                        LiveTimeline.AxisX[0].MaxValue = sender.ViewPortMaxValueMilliseconds / 1000.0;
                    }
                };

                LiveTimeline.AxisX[0].Sections.Clear();
                LiveTimeline.AxisX[0]
                    .Sections.AddRange(ReferenceTimeline.PauseSections.Select(
                        s => new AxisSection
                        {
                            Value = s.StartSeconds,
                            SectionWidth = s.WidthSeconds,
                            Fill = PausedSectionBrush
                        }));
            }
        }

        private static void OnTimeLineTypeChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((ThreadTimelineChart)o).UpdateTimeLineType();
        }

        private void UpdateCpuViewPort()
        {
            _cpuSeries.Values = CpuUtilizationSource == null ? new ChartValues<CpuUtilization>() : new ChartValues<CpuUtilization>(CpuUtilizationSource.ViewPortValues);
        }

        private void UpdateGcViewPort()
        {
            _gcSeries.Values = GcJobsSource == null ? new ChartValues<ClrJobItem>() : new ChartValues<ClrJobItem>(GcJobsSource.ViewPortValues);
        }

        private void UpdateJitViewPort()
        {
            if (JitJobsSource != null)
                _jitSeries.Values = GcJobsSource == null ? new ChartValues<ClrJobItem>() : new ChartValues<ClrJobItem>(JitJobsSource.ViewPortValues);
        }

        private void UpdateTimeLineType()
        {
            switch (TimeLineType)
            {
                case TimeLineType.CpuUtilization:
                    SetCpuNormalColor();
                    _gcSeries.Fill = Brushes.Transparent;
                    _jitSeries.Fill = Brushes.Transparent;
                    break;
                case TimeLineType.GarbageCollection:
                    SetCpuLightColor();
                    _gcSeries.Fill = _gcFillBrush;
                    _jitSeries.Fill = Brushes.Transparent;
                    break;
                case TimeLineType.JustInTimeCompilation:
                    SetCpuLightColor();
                    _gcSeries.Fill = Brushes.Transparent;
                    _jitSeries.Fill = _jitFillBrush;
                    break;
            }

            LiveTimeline.Update();
        }

        private void SetCpuNormalColor()
        {
            _cpuSeries.Stroke = _cpuNormalBrush;
            _cpuSeries.Fill = _cpuNormalBrush;
        }

        private void SetCpuLightColor()
        {
            _cpuSeries.Stroke = _cpuLightBrush;
            _cpuSeries.Fill = _cpuLightBrush;
        }
    }
}
