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
using System.Windows;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using NetCore.Profiler.Cperf.Core.Model;
using NetCore.Profiler.Extension.Launcher.Model;
using NetCore.Profiler.Extension.Session;

namespace NetCore.Profiler.Extension.UI.ProfilingProgressWindow
{
    /// <summary>
    /// Profiling Progress Window Content
    /// </summary>
    public partial class ProfilingProgressWindowContent : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Time interval displayed in the chart in seconds
        /// </summary>
        private const long ViewPortSize = 60;

        /// <summary>
        /// Empty space reserved at the end of the chart in seconds
        /// </summary>
        private const int ViewPortTimeReserve = 10;

        /// <summary>
        /// Controlled profiling session
        /// </summary>
        private IProfileSession _session;

        /// <summary>
        /// Programm start timestamp. Initialized upon receiving first data from profiler
        /// </summary>
        private long _startTimestamp;

        /// <summary>
        /// Previous value of the SysInfo UserSys value. Used for calculation CPU utilization
        /// </summary>
        private long _previousUserSys;

        /// <summary>
        /// Last added chart data. Used to track Pause/Resume sessions
        /// </summary>
        private ChartData _previousChartData;


        /// <summary>
        /// Values for the chart control
        /// </summary>
        private readonly ChartValues<ChartData> _chartValues = new ChartValues<ChartData>();

        /// <summary>
        /// Series collection for the chart control
        /// </summary>
        public SeriesCollection SeriesCollection { get; set; }

        /// <summary>
        /// Current Paused Chart section
        /// </summary>
        private AxisSection _currentPausedSection;

        /// <summary>
        /// Total ammount of seconds since section beggining including time reserve
        /// </summary>
        private long _rangeMaxValue;

        /// <summary>
        /// Displayed interval minimal value
        /// </summary>
        private long ViewPortMinValue
        {
            get { return (long)LiveTimeline.AxisX[0].MinValue; }
            set { LiveTimeline.AxisX[0].MinValue = value; }
        }


        /// <summary>
        /// Displayed interval maximum value
        /// </summary>
        private long ViewPortMaxValue
        {
            get { return (long)LiveTimeline.AxisX[0].MaxValue; }
            set { LiveTimeline.AxisX[0].MaxValue = value; }
        }

        /// <summary>
        /// ScrollBar offset. ScrallBar Value property is bound to it.
        /// </summary>
        public double Offset
        {
            get { return ViewPortMinValue; }
            set
            {
                var delta = (long)value - ViewPortMinValue;
                ViewPortMinValue = (long)value;
                ViewPortMaxValue += delta;
                NotifyOffsetChange();
            }
        }

        private void NotifyOffsetChange()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Offset)));
        }

        private static readonly SolidColorBrush PausedSectionBrush = new SolidColorBrush(Colors.AliceBlue) { Opacity = 0.4 };

        private const string CpuSeriesTitle = "CPU (%)";

        private static readonly SolidColorBrush CpuSeriesStrokeBrush = Brushes.Blue;

        private static readonly SolidColorBrush CpuSeriesFillBrush = Brushes.Transparent;


        private const string MemorySeriesTitle = "Memory (Gb)";

        private static readonly SolidColorBrush MemorySeriesStrokeBrush = Brushes.Red;

        private static readonly SolidColorBrush MemorySeriesFillBrush = Brushes.Transparent;


        private const string StartButtonLabel = "Start";

        private const string PauseButtonLabel = "Pause";

        private const string ResumeButtonLabel = "Resume";

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfilingProgressWindowContent"/> class.
        /// </summary>
        public ProfilingProgressWindowContent()
        {
            _sessionListener = new ProfileSessionListener(ProcessStatusChange, ProcessTargetSysInfo);

            InitializeComponent();

            InitialiseChart();

            InitializeControlComponents();

            DataContext = this;
        }

        private void InitializeControlComponents()
        {
            SessionControlButton.Visibility = Visibility.Hidden;
            SessionControlButton.IsEnabled = false;

            SessionControlButton.Click += delegate
            {
                switch (SessionControlButton.Content.ToString())
                {
                    case StartButtonLabel:
                    case ResumeButtonLabel:
                        _session.Resume();
                        break;
                    case PauseButtonLabel:
                        _session.Pause();
                        break;
                }
            };

            StopButton.Visibility = Visibility.Hidden;
            StopButton.IsEnabled = false;

            StopButton.Click += (sender, args) => _session.Stop();
        }

        private void InitialiseChart()
        {
            SeriesCollection = new SeriesCollection()
            {
                new LineSeries(Mappers.Xy<ChartData>()
                    .X(item => item.Time)
                    .Y(item => item.Cpu))
                {
                    Stroke = CpuSeriesStrokeBrush,
                    Fill = CpuSeriesFillBrush,
                    PointGeometry = null,
                    Title = CpuSeriesTitle,
                    ScalesYAt = 0,
                    Values = _chartValues
                },

                new LineSeries(Mappers.Xy<ChartData>()
                    .X(item => item.Time)
                    .Y(item => item.Mem))
                {
                    Stroke = MemorySeriesStrokeBrush,
                    Fill = MemorySeriesFillBrush,
                    PointGeometry = null,
                    Title = MemorySeriesTitle,
                    ScalesYAt = 1,
                    Values = _chartValues
                }
            };

            LiveTimeline.AxisX[0].Separator.Step = 5;
            LiveTimeline.Zoom = ZoomingOptions.None;
            LiveTimeline.DisableAnimations = true;
        }

        public void SetSession(IProfileSession session)
        {

            ClearSession();

            _session = session;
            session.AddListener(_sessionListener);

        }

        public void ClearSession()
        {
            if (_session != null)
            {
                _session.RemoveListener(_sessionListener);
                _session = null;
            }

            ViewPortMinValue = 0;
            ViewPortMaxValue = ViewPortSize;
            _chartValues.Clear();
            LiveTimeline.AxisX[0].Sections.Clear();
            LiveTimeline.Update(true, true);

            _rangeMaxValue = ViewPortSize;
            _startTimestamp = 0;
            _previousUserSys = 0;

            UpdateScrollBar();

            SessionControlButton.Visibility = Visibility.Hidden;
            SessionControlButton.IsEnabled = false;

            _session = null;

        }

        private void ProcessStatusChange(ProfileSessionState newState)
        {

            Dispatcher.Invoke(() =>
            {
                Status.Text = StateToString(newState);
                UpdateSessionControlButton(newState);
            });

        }

        private void UpdateSessionControlButton(ProfileSessionState state)
        {
            SessionControlButton.Visibility = Visibility.Visible;
            SessionControlButton.IsEnabled = true;

            switch (state)
            {
                case ProfileSessionState.Waiting:
                    {
                        SessionControlButton.Content = StartButtonLabel;
                        SessionControlButton.Visibility = Visibility.Visible;
                        SessionControlButton.IsEnabled = true;

                        StopButton.IsEnabled = true;
                        StopButton.Visibility = Visibility.Visible;

                    }

                    break;
                case ProfileSessionState.Running:
                    {
                        SessionControlButton.Content = PauseButtonLabel;
                        SessionControlButton.Visibility = Visibility.Visible;
                        SessionControlButton.IsEnabled = true;

                        StopButton.IsEnabled = true;
                        StopButton.Visibility = Visibility.Visible;

                    }

                    break;
                case ProfileSessionState.Paused:
                    {
                        SessionControlButton.Content = ResumeButtonLabel;
                        SessionControlButton.Visibility = Visibility.Visible;
                        SessionControlButton.IsEnabled = true;

                    }

                    break;
                default:
                    SessionControlButton.Visibility = Visibility.Hidden;
                    SessionControlButton.IsEnabled = false;

                    StopButton.IsEnabled = false;
                    StopButton.Visibility = Visibility.Hidden;

                    break;
            }
        }

        private void ProcessTargetSysInfo(SysInfoItem sii)
        {
            Dispatcher.Invoke(() =>
            {
                var chartData = CreateChartData(sii);
                AddDataToChart(chartData);

                _previousUserSys = sii.UserSys;
                _previousChartData = chartData;
            });
        }

        private void AddDataToChart(ChartData chartData)
        {
            if (_currentPausedSection != null)
            {
                _currentPausedSection.SectionWidth = chartData.Time - _currentPausedSection.Value;
            }

            if (_previousChartData != null)
            {
                if (_previousChartData.Running && !chartData.Running)
                {
                    _currentPausedSection = new AxisSection()
                    {
                        Value = _previousChartData.Time,
                        SectionWidth = chartData.Time - _previousChartData.Time,
                        Fill = PausedSectionBrush
                    };

                    LiveTimeline.AxisX[0].Sections.Add(_currentPausedSection);
                }
                else if (!_previousChartData.Running && chartData.Running)
                {
                    _currentPausedSection = null;
                }
            }

            AddToChartData(chartData);
        }

        private ChartData CreateChartData(SysInfoItem sii)
        {
            ChartData chartData;
            if (_startTimestamp == 0)
            {
                _startTimestamp = sii.Timestamp;
                LiveTimeline.AxisY[0].MaxValue = sii.CoreNum * 100;
                LiveTimeline.AxisY[1].MaxValue = Math.Ceiling(((double)sii.MemTotal) / 1024 / 1024);

                chartData = new ChartData()
                {
                    Time = 0,
                    Mem = 0,
                    Cpu = 0
                };
            }
            else
            {
                var time = sii.Timestamp - _startTimestamp;
                if ((time + ViewPortTimeReserve) > _rangeMaxValue)
                {
                    var atTheEnd = (ScrollBar.Maximum - ScrollBar.Value) < 1 ;
                    _rangeMaxValue += ViewPortTimeReserve;
                    UpdateScrollBar();

                    //Scroll to the end if we were at the end already
                    if (atTheEnd)
                    {
                        Offset = ScrollBar.Maximum;
                    }
                }

                chartData = new ChartData()
                {
                    Running = ProfilerStatusToBool(sii.ProfilerStatus),
                    Time = sii.Timestamp - _startTimestamp,
                    Mem = Math.Round(((double)sii.MemLoad) / 1024 / 1024, 2),
                    Cpu = sii.UserSys - _previousUserSys
                };
            }

            return chartData;
        }

        private void AddToChartData(ChartData chartData)
        {
            _chartValues.Add(chartData);
        }

        private void UpdateScrollBar()
        {
            var l = ViewPortMaxValue - ViewPortMinValue;
            ScrollBar.Maximum = _rangeMaxValue - l;
            var change = Math.Max(10, l / 5);
            ScrollBar.SmallChange = change;
            ScrollBar.LargeChange = change * 10;
            ScrollBar.ViewportSize = l;
        }

        private string StateToString(ProfileSessionState state)
        {
            switch (state)
            {
                case ProfileSessionState.Initial:
                    return "Initial";
                case ProfileSessionState.Starting:
                    return "Starting";
                case ProfileSessionState.UploadFiles:
                    return "Upload Files";
                case ProfileSessionState.StartHost:
                    return "Starting Host";
                case ProfileSessionState.Waiting:
                    return "Waiting";
                case ProfileSessionState.Running:
                    return "Running";
                case ProfileSessionState.Paused:
                    return "Paused";
                case ProfileSessionState.Stopping:
                    return "Stopping";
                case ProfileSessionState.DownloadFiles:
                    return "Download Files";
                case ProfileSessionState.WritingSession:
                    return "Writing Session";
                case ProfileSessionState.Finished:
                    return "Finished";
                case ProfileSessionState.Failed:
                    return "Failed";
                default:
                    return "<Unknown>";
            }
        }

        private bool ProfilerStatusToBool(string status)
        {
            switch (status)
            {
                case "Waiting":
                    return false;
                case "Running":
                    return true;
                default:
                    return false;
            }
        }

        private readonly ProfileSessionListener _sessionListener;

        private class ProfileSessionListener : IProfileSessionListener
        {
            internal delegate void StateChangedEventHandler(ProfileSessionState newState);

            internal delegate void SysInfoReadHandler(SysInfoItem siItem);

            private readonly StateChangedEventHandler _stateHandler;

            private readonly SysInfoReadHandler _sysInfoReadHandler;

            public ProfileSessionListener(StateChangedEventHandler handler, SysInfoReadHandler sysInfoReadHandler)
            {
                _stateHandler = handler;
                _sysInfoReadHandler = sysInfoReadHandler;
            }

            public void StateChanged(ProfileSessionState newState)
            {
                _stateHandler(newState);
            }

            public void SysInfoRead(SysInfoItem siItem)
            {
                _sysInfoReadHandler(siItem);
            }
        }

        /// <summary>
        /// Part of SysInfo information obtained from profiler to be presented on the chart
        /// </summary>
        private class ChartData
        {
            /// <summary>
            /// Gets or sets a value indicating whether tracing is performed at this moment of time
            /// </summary>
            public bool Running { get; set; } = true;

            /// <summary>
            /// Gets or sets time since profiling start in seconds
            /// </summary>
            public long Time { get; set; }

            /// <summary>
            /// Gets or sets CPU usage in percents
            /// </summary>
            public double Cpu { get; set; }

            /// <summary>
            /// Gets or sets memory usage in Gb
            /// </summary>
            public double Mem { get; set; }

        }

    }
}
