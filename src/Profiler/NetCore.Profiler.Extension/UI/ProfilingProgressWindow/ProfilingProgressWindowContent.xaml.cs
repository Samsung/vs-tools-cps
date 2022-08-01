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

//#define DEBUG_VERBOSE

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Definitions.Series;
using LiveCharts.Wpf;
using NetCore.Profiler.Common.Helpers;
using NetCore.Profiler.Cperf.Core.Model;
using NetCore.Profiler.Extension.Common;
using NetCore.Profiler.Extension.Launcher.Model;
using NetCore.Profiler.Extension.VSPackage;
using Tizen.VisualStudio.Utilities;

namespace NetCore.Profiler.Extension.UI.ProfilingProgressWindow
{
    /// <summary>
    /// Profiling Progress Window Content.
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
        private ProfileSession _session;

        /// <summary>
        /// Listener object used to receive the controlled profiling session events
        /// </summary>
        private readonly ProfileSessionListener _sessionListener;

        /// <summary>
        /// First system information timestamp expressed as a UNIX epoch time (plus milliseconds).
        /// </summary>
        private double _sysInfoStartTimeSeconds;

        /// <summary>
        /// Previous SysInfo value. Used for calculating CPU utilization
        /// </summary>
        private SysInfoItem _previousSysInfo;

        /// <summary>
        /// Last added chart data. Used to track Pause/Resume in sessions
        /// </summary>
        private SysInfoData _previousChartData;

        /// <summary>
        /// Debug break state flag.
        /// </summary>
        private bool _isBreakState;

        /// <summary>
        /// Last profile session state known.
        /// </summary>
        private ProfileSessionState _lastSessionState;

        /// <summary>
        /// Values for the chart control
        /// </summary>
        private readonly ChartValues<SysInfoData> _chartValues = new ChartValues<SysInfoData>();

        /// <summary>
        /// Series collection for the chart control
        /// </summary>
        public SeriesCollection SeriesCollection { get; set; }

        /// <summary>
        /// Current Paused chart section
        /// </summary>
        private AxisSection _currentPausedSection;

        /// <summary>
        /// Current Debug Break chart section
        /// </summary>
        private AxisSection _currentDebugBreakSection;

        /// <summary>
        /// Total amount of seconds since section begin including time reserve
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
        /// ScrollBar offset. ScrollBar Value property is bound to it.
        /// </summary>
        public double Offset
        {
            get { return ViewPortMinValue; }
            set
            {
                long delta = (long)value - ViewPortMinValue;
                if (delta != 0)
                {
                    ViewPortMinValue = (long)value;
                    ViewPortMaxValue += delta;
                    NotifyOffsetChange();
                }
            }
        }

        private void NotifyOffsetChange()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Offset)));
        }

        /// <summary>
        /// The brush used to draw paused sections. Public visibility is requied for xaml binding.
        /// </summary>
        public static SolidColorBrush PausedSectionBrush { get; } = new SolidColorBrush(Colors.DarkGray) { Opacity = 0.4 };

        /// <summary>
        /// The brush used to draw debug break sections. Public visibility is requied for xaml binding.
        /// </summary>
        public static SolidColorBrush DebugBreakSectionBrush { get; } = new SolidColorBrush(Colors.DarkRed) { Opacity = 0.4 };


        private const string CpuSeriesTitle = "CPU (%)";

        private static readonly SolidColorBrush CpuSeriesStrokeBrush = Brushes.Blue;

        private static readonly SolidColorBrush CpuSeriesFillBrush = Brushes.Transparent;


        private const string MemorySeriesTitle = "Memory (MiB)";

        private static readonly SolidColorBrush MemorySeriesStrokeBrush = Brushes.Red;

        private static readonly SolidColorBrush MemorySeriesFillBrush = Brushes.Transparent;


        private const string StartButtonLabel = "Start";

        private const string PauseButtonLabel = "Pause";

        private const string ResumeButtonLabel = "Resume";


        private const int MaxQueuedEvents = 1048576;

        private QueuedAsynchronousProcessor<SysInfoItem> _sysInfoProcessor;

        private QueuedAsynchronousProcessor<Event> _eventsProcessor;

        private readonly ChartValues<ClrJobItem> _jitSeriesValues = new ChartValues<ClrJobItem>();
        private ProfilerEventProcessor _jitEventProcessor;

        private readonly ChartValues<ClrJobItem> _gcSeriesValues = new ChartValues<ClrJobItem>();
        private ProfilerEventProcessor _gcEventProcessor;

        /// <summary>
        /// The brush used to draw JIT (just-in-time compilation) sections. Public visibility is requied for xaml binding.
        /// </summary>
        public static SolidColorBrush JitSeriesBrush { get; } = new SolidColorBrush(Colors.DarkGreen) { Opacity = 0.4 };

        /// <summary>
        /// The brush used to draw GC (garbage collection) sections. Public visibility is requied for xaml binding.
        /// </summary>
        public static SolidColorBrush GcSeriesBrush { get; } = new SolidColorBrush(Colors.DarkOrange) { Opacity = 0.4 };

        /// <summary>
        /// This value shall be added to profiling events' timestamps to get correct display time
        /// </summary>
        private double _profilingEventsDeltaSeconds;

        /// <summary>
        /// This value shall be added to system information items' timestamps to get correct display time
        /// </summary>
        private double _sysInfoEventsDeltaSeconds;

        /// <summary>
        /// A flag telling are _profilingEventsDeltaSeconds and _sysInfoEventsDeltaSeconds fields set.
        /// </summary>
        private bool _areDeltasSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfilingProgressWindowContent"/> class.
        /// </summary>
        public ProfilingProgressWindowContent()
        {
            _sessionListener = new ProfileSessionListener
            {
                OnStateChanged = ProcessStateChange,
                OnSysInfoRead = EnqueueTargetSysInfo,
                OnProfilerEvent = EnqueueProfilerEvent,
                OnDebugStateChanged = ProcessDebugStateChange
            };

            _jitEventProcessor = new ProfilerEventProcessor(this);
            _gcEventProcessor = new ProfilerEventProcessor(this);

            InitializeComponent();

            InitialiseChart();

            InitializeControlComponents();

            DataContext = this;
        }

        private void InitializeControlComponents()
        {
            SessionControlButton.Visibility = Visibility.Collapsed;
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

            StopButton.Visibility = Visibility.Collapsed;
            StopButton.IsEnabled = false;

            StopButton.Click += (sender, args) => _session.Stop();
        }

        private FilledStepLineSeries CreateClrJobItemSeries(string title, int yAxis)
        {
            return new FilledStepLineSeries
            {
                Configuration = Mappers.Xy<ClrJobItem>()
                    .X(item => (item.TimeSeconds + _profilingEventsDeltaSeconds))
                    .Y(item => Convert.ToDouble(item.IsStart)),
                Fill = null,
                StrokeThickness = 1,
                PointGeometry = null,
                Title = title,
                ScalesYAt = yAxis
            };
        }

        private void InitialiseChart()
        {
            FilledStepLineSeries jitSeries = CreateClrJobItemSeries("Jit", 2);
            jitSeries.Fill = JitSeriesBrush;
            jitSeries.Values = _jitSeriesValues;

            FilledStepLineSeries gcSeries = CreateClrJobItemSeries("Gc", 2);
            gcSeries.Fill = GcSeriesBrush;
            gcSeries.Values = _gcSeriesValues;

            SeriesCollection = new SeriesCollection()
            {
                jitSeries,

                gcSeries,

                new LineSeries(Mappers.Xy<SysInfoData>()
                    .X(item => item.TimeSeconds + _sysInfoEventsDeltaSeconds)
                    .Y(item => item.CpuPercent))
                {
                    Stroke = CpuSeriesStrokeBrush,
                    Fill = CpuSeriesFillBrush,
                    PointGeometry = null,
                    Title = CpuSeriesTitle,
                    ScalesYAt = 0,
                    Values = _chartValues
                },

                new LineSeries(Mappers.Xy<SysInfoData>()
                    .X(item => item.TimeSeconds + _sysInfoEventsDeltaSeconds)
                    .Y(item => item.MemoryMiB))
                {
                    Stroke = MemorySeriesStrokeBrush,
                    Fill = MemorySeriesFillBrush,
                    PointGeometry = null,
                    Title = MemorySeriesTitle,
                    ScalesYAt = 1,
                    Values = _chartValues
                },
            };

            ScrollBar.Visibility = Visibility.Collapsed;
            ViewPortMaxValue = ViewPortSize;

            LiveTimeline.AxisX[0].Separator.Step = 5;
            LiveTimeline.AxisX[0].LabelFormatter = (double t) => (t == Math.Truncate(t)) ? t.ToString() : t.ToString("F02");

            LiveTimeline.AxisY[0].LabelFormatter = (double cpu) => (cpu == Math.Truncate(cpu)) ? cpu.ToString() : cpu.ToString("F01");

            LiveTimeline.AxisY[1].MaxValue = 1000;
            LiveTimeline.AxisY[1].LabelFormatter = (double mem) => (mem == 0) ? "0" : mem.ToString("F0");

            LiveTimeline.Zoom = ZoomingOptions.None;
            LiveTimeline.DisableAnimations = true;
        }

        /// <summary>
        /// Set a current session for <see cref="ProfilingProgressWindowContent"/>.
        /// </summary>
        /// <param name="session">The session interface.</param>
        public void SetSession(ProfileSession session)
        {
            ClearSession(false);

            _session = session;

            session.AddListener(_sessionListener);

            _sysInfoProcessor = new QueuedAsynchronousProcessor<SysInfoItem>(ProcessTargetSysInfo);
            _eventsProcessor = new QueuedAsynchronousProcessor<Event>(ProcessProfilerEvent);

            CaptionText.Text = Path.GetFileName(session.Configuration.TargetDll);

            LegendItemDebug.Visibility = (session.IsLiveProfiling ? Visibility.Visible : Visibility.Collapsed);
        }

        /// <summary>
        /// Clear the current session (reset both the %UI and internal data structures).
        /// </summary>
        public void ClearSession()
        {
            ClearSession(true);
            CaptionText.Text = "";
        }

        private void ClearSession(bool hideScrollbar)
        {
            if (_session != null)
            {
                _session.RemoveListener(_sessionListener);
                _session = null;
            }

            DisposeHelper.SafeDispose(ref _sysInfoProcessor);
            DisposeHelper.SafeDispose(ref _eventsProcessor);

            ViewPortMinValue = 0;
            ViewPortMaxValue = ViewPortSize;

            _chartValues.Clear();

            _jitSeriesValues.Clear();
            _jitEventProcessor.Clear();

            _gcSeriesValues.Clear();
            _gcEventProcessor.Clear();

            LiveTimeline.AxisX[0].Sections.Clear();
            LiveTimeline.AxisY[1].MaxValue = 1000;
            LiveTimeline.Update(true, true);

            _rangeMaxValue = ViewPortSize;
            _sysInfoStartTimeSeconds = -1;
            _profilingEventsDeltaSeconds = 0;
            _sysInfoEventsDeltaSeconds = 0;
            _areDeltasSet = false;
            _previousSysInfo = null;
            _previousChartData = null;
            _currentPausedSection = null;
            _currentDebugBreakSection = null;

            UpdateScrollBar(hideScrollbar);

            SessionControlButton.Visibility = Visibility.Collapsed;
            SessionControlButton.IsEnabled = false;

            _session = null;
        }

        private void ProcessStateChange(ProfileSessionState newState)
        {
            _lastSessionState = newState;

            Task.Run(() =>
                Dispatcher.Invoke(delegate ()
                {
                    Status.Text = StateToString(newState);
                    UpdateSessionControlButton(newState);
                }));

            if (newState >= ProfileSessionState.Finished)
            {
                CloseLog();
            }
        }

        private void UpdateSessionControlButton(ProfileSessionState state)
        {
            string sessionControlContent;

            bool sessionControlEnabled = true;
            Visibility sessionControlVisibility = Visibility.Visible;

            bool sessionStopEnabled = true;
            Visibility sessionStopVisibility = Visibility.Visible;

            switch (state)
            {
                case ProfileSessionState.Waiting:
                    sessionControlContent = StartButtonLabel;
                    break;

                case ProfileSessionState.Running:
                    sessionControlContent = PauseButtonLabel;
                    break;

                case ProfileSessionState.Paused:
                    sessionControlContent = ResumeButtonLabel;
                    break;

                default:
                    sessionControlContent = "";

                    sessionControlEnabled = false;
                    sessionControlVisibility = Visibility.Collapsed;

                    sessionStopEnabled = false;
                    sessionStopVisibility = Visibility.Collapsed;

                    break;
            }

            SessionControlButton.Content = sessionControlContent;

            SessionControlButton.IsEnabled = sessionControlEnabled;
            SessionControlButton.Visibility = sessionControlVisibility;

            StopButton.IsEnabled = sessionStopEnabled;
            StopButton.Visibility = sessionStopVisibility;
        }

        private void EnqueueTargetSysInfo(SysInfoItem sii)
        {
            QueuedAsynchronousProcessor<SysInfoItem> sysInfoProcessor = _sysInfoProcessor;
            if (sysInfoProcessor != null)
            {
                if (sysInfoProcessor.GetCount() < MaxQueuedEvents)
                {
                    sysInfoProcessor.Enqueue(sii);
                }
            }
        }

        private void ProcessTargetSysInfo(SysInfoItem sysInfo)
        {
            if (_session != null)
            {
                Dispatcher.Invoke(() =>
                {
                    SysInfoData chartData = CreateChartData(sysInfo);
                    if (chartData != null)
                    {
                        AddDataToChart(chartData);

                        _previousSysInfo = sysInfo;
                        _previousChartData = chartData;
                    }
                });
            }
        }

        private void EnqueueProfilerEvent(Event @event)
        {
            QueuedAsynchronousProcessor<Event> eventsProcessor = _eventsProcessor;
            if ((eventsProcessor != null) && IsProfilerEvent(@event))
            {
                if (eventsProcessor.GetCount() < MaxQueuedEvents)
                {
                    eventsProcessor.Enqueue(@event);
                }
            }
        }

        private bool IsProfilerEvent(Event @event)
        {
            switch (@event.EventType)
            {
                case EventType.CompilationStarted:
                case EventType.CompilationFinished:
                case EventType.CachedFunctionSearchStarted:
                case EventType.CachedFunctionSearchFinished:
                case EventType.GarbageCollectionStarted:
                case EventType.GarbageCollectionFinished:
                    return true;
            }
            return false;
        }

        private void ProcessProfilerEvent(Event @event)
        {
            if (_session == null)
            {
                return;
            }
            switch (@event.EventType)
            {
                case EventType.CompilationStarted:
                case EventType.CachedFunctionSearchStarted:
                    _jitEventProcessor.RegisterEvent(@event, true);
                    break;

                case EventType.GarbageCollectionStarted:
                    _gcEventProcessor.RegisterEvent(@event, true);
                    break;

                case EventType.CompilationFinished:
                case EventType.CachedFunctionSearchFinished:
                    _jitEventProcessor.RegisterEvent(@event, false);
                    break;

                case EventType.GarbageCollectionFinished:
                    _gcEventProcessor.RegisterEvent(@event, false);
                    break;
            }
        }

        private readonly object logLock = new object();
        private StreamWriter log;

        [Conditional("DEBUG_VERBOSE")]
        private void DebugWrite(string message)
        {
            lock (logLock)
            {
                DateTime nowUtc = DateTime.UtcNow;
                DateTime now = nowUtc.ToLocalTime();
                if (log == null)
                {
                    log = new StreamWriter(Path.Combine(_session.SessionDirectory,
                        $"{GetType().Name}_{(now.Year - 2000):D2}{now.Month:D2}{now.Day:D2}-" +
                        $"{now.Hour:D2}{now.Minute:D2}{now.Second:D2}.log"));
                }
                if (_session.ProfilerStartTimeUtc != DateTime.MinValue)
                {
                    message = $"[{(nowUtc - _session.ProfilerStartTimeUtc).TotalSeconds:F3}] " + message;
                }
                log.WriteLine($"{now.ToDebugString()} {message}");
            }
        }

        [Conditional("DEBUG_VERBOSE")]
        private void CloseLog()
        {
            lock (logLock)
            {
                if (log != null)
                {
                    log.WriteLine("*** JIT series ***");
                    WriteClrJobItems(log, _jitSeriesValues);
                    log.WriteLine("*** GC series ***");
                    WriteClrJobItems(log, _gcSeriesValues);
                    DisposeHelper.SafeDispose(ref log);
                }
            }
        }

        [Conditional("DEBUG_VERBOSE")]
        private static void WriteClrJobItems(StreamWriter writer, IList<ClrJobItem> items)
        {
            ClrJobItem jobStart = null;
            int n = 0;
            for (int i = 0, count = items.Count; i < count; ++i)
            {
                ClrJobItem job = items[i];
                if (i % 2 == 0)
                {
                    if (job != null && !job.IsStart)
                    {
                        writer.WriteLine($"WARNING: unexpected end item ({job.TimeSeconds:F3})");
                        continue;
                    }
                    jobStart = job;
                }
                else
                {
                    if (job.IsStart)
                    {
                        writer.WriteLine($"WARNING: unexpected start item ({job.TimeSeconds:F3})");
                        continue;
                    }
                    if (jobStart == null)
                    {
                        writer.WriteLine($"WARNING: end item without start item ({job.TimeSeconds:F3})");
                        continue;
                    }
                    writer.WriteLine($"#{++n:D4} {jobStart.TimeSeconds:F3} .. {job.TimeSeconds:F3}");
                    jobStart = null;
                }
            }
            if (jobStart != null)
            {
                writer.WriteLine($"WARNING: start item without end item ({jobStart.TimeSeconds:F3})");
            }
        }

        /// <summary>
        /// A class used to process incoming profiler events (<see cref="NetCore.Profiler.Cperf.Core.Model.Event"/>)
        /// of some type (e.g. garbage collection, GC) which can be a start or finish event (e.g. GC start / GC finish)
        /// and maintain a list of <see cref="ClrJobItem"/> objects which can be taken by an owner
        /// (<see cref="ProfilingProgressWindowContent"/>) to visualize them on the chart.
        /// </summary>
        private class ProfilerEventProcessor
        {
            public ProfilerEventProcessor(ProfilingProgressWindowContent parent)
            {
                _parent = parent;
            }

            public void Clear()
            {
                _counter = 0;
                TakeJobItems();
            }

            private static int _dbgIdx;

            public void RegisterEvent(Event @event, bool isStart)
            {
                _parent.DebugWrite(String.Format("#{0} Profiler event [{1}]",
                    ++_dbgIdx, @event.ToString()));

                if (isStart)
                {
                    if (++_counter == 1)
                    {
                        _startMilliseconds = @event.TimeMilliseconds;
                    }
                }
                else
                {
                    if (_counter > 0)
                    {
                        if (--_counter == 0)
                        {
                            AddProfilerEvent(@event);
                        }
                    }
                }
            }

            public List<ClrJobItem> TakeJobItems()
            {
                lock (_pendingJobItemsGuard)
                {
                    List<ClrJobItem> result = _pendingJobItems;
                    _pendingJobItems = null;
                    return result;
                }
            }

            private void AddProfilerEvent(Event @event)
            {
                string eventType = (@event.EventType == EventType.GarbageCollectionFinished) ? "GC" : "JIT";

                _parent.DebugWrite(String.Format("#{0} {1} (start={2}ms; end={3}ms)",
                    ++_dbgIdx, eventType, _startMilliseconds, @event.TimeMilliseconds));

                double startSeconds = _startMilliseconds / 1000.0;
                double endSeconds = @event.TimeMilliseconds / 1000.0;
                lock (_pendingJobItemsGuard)
                {
                    if (_pendingJobItems == null)
                    {
                        _pendingJobItems = new List<ClrJobItem>();
                    }
                    else
                    {
                        int count = _pendingJobItems.Count; // > 0 if pendingJobItems is not null
                        ClrJobItem lastEnd = _pendingJobItems[count - 1];
                        if (endSeconds <= lastEnd.TimeSeconds)
                        {
                            _parent.DebugWrite(String.Format("#{0} Ignored (1)", _dbgIdx));

                            return; // ignore duplicate or obsolete events
                        }
                        if (startSeconds <= lastEnd.TimeSeconds) // intersecting events
                        {
                            if (endSeconds <= lastEnd.TimeSeconds)
                            {
                                _parent.DebugWrite(String.Format("#{0} Ignored (2)", _dbgIdx));

                                return;
                            }
                            // special: merge events
                            _pendingJobItems.RemoveAt(count - 1);
                            _pendingJobItems.Add(new ClrJobItem(endSeconds, false));

                            _parent.DebugWrite(String.Format(
                                "#{0} Replaced last {1} from (start={2:F3}s; end={3:F3}s) to (start={4:F3}s; end={5:F3}s)",
                                _dbgIdx, eventType,
                                _pendingJobItems[count - 2].TimeSeconds, lastEnd.TimeSeconds,
                                _pendingJobItems[count - 2].TimeSeconds, endSeconds));

                            return;
                        }
                    }
                    _pendingJobItems.Add(new ClrJobItem(startSeconds, true));
                    if (startSeconds == endSeconds)
                    {
                        const double MinTimeDelta = 0.001; // 1 msec
                        endSeconds += MinTimeDelta;
                    }
                    _pendingJobItems.Add(new ClrJobItem(endSeconds, false));
                }

                _parent.DebugWrite(String.Format("#{0} Added {1} (start={2:F3}s; end={3:F3}s)",
                    _dbgIdx, eventType, startSeconds, endSeconds));
            }

            public int Counter => _counter;

            private ProfilingProgressWindowContent _parent;

            private int _counter;

            private ulong _startMilliseconds;

            private List<ClrJobItem> _pendingJobItems;

            private object _pendingJobItemsGuard = new object();
        } // ProfilerEventProcessor

        private void AddDataToChart(SysInfoData sysInfoData)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            if (!_areDeltasSet && (_sysInfoStartTimeSeconds >= 0))
            {
                DateTime profilerStartTimeUtc = _session.ProfilerStartTimeUtc;
                if (profilerStartTimeUtc != DateTime.MinValue)
                {
                    _areDeltasSet = true;

                    DateTime sysInfoStartTime = _sysInfoStartTimeSeconds.UnixTimeToDateTime();
                    _profilingEventsDeltaSeconds = (profilerStartTimeUtc - sysInfoStartTime).TotalSeconds;
                    if (_profilingEventsDeltaSeconds < 0)
                    {
                        _sysInfoEventsDeltaSeconds = -_profilingEventsDeltaSeconds;
                        _profilingEventsDeltaSeconds = 0;
                    }

                    // update existing chart items
                    var savedSeries = new List<ISeriesView>(SeriesCollection);
                    SeriesCollection.Clear();
                    SeriesCollection.AddRange(savedSeries);

                    DebugWrite(
                        $"Profiler start time: {profilerStartTimeUtc.ToString("yyyy-MM-dd HH:mm:ss.fff")} Z. " +
                        $"Profiling events delta: {_profilingEventsDeltaSeconds:F3} s. " +
                        $"System information events delta: {_sysInfoEventsDeltaSeconds:F3} s");
                }
            }

            bool isBreakState = (ProfilerPlugin.Instance.DebugMode == Microsoft.VisualStudio.Shell.Interop.DBGMODE.DBGMODE_Break);
            if (_isBreakState != isBreakState) // happens sometimes
            {
                _isBreakState = isBreakState;
                UpdateControlsOnDebugStateChange(isBreakState);
            }

            if (_currentDebugBreakSection != null)
            {
                _currentDebugBreakSection.SectionWidth = sysInfoData.TimeSeconds + _sysInfoEventsDeltaSeconds
                    - _currentDebugBreakSection.Value;
                if (!_isBreakState)
                {
                    DebugWrite($"Debug break section finished. Duration is {_currentDebugBreakSection.SectionWidth:F3} sec");
                    _currentDebugBreakSection = null;
                }
            }
            else if (_isBreakState)
            {
                if (_currentDebugBreakSection == null)
                {
                    _currentDebugBreakSection = new AxisSection()
                    {
                        Value = sysInfoData.TimeSeconds + _sysInfoEventsDeltaSeconds,
                        SectionWidth = 0.001, // 1 msec
                        Fill = DebugBreakSectionBrush
                    };
                    LiveTimeline.AxisX[0].Sections.Add(_currentDebugBreakSection);
                    DebugWrite($"Debug break section started at {_currentDebugBreakSection.Value:F3} sec");
                }
            }

            AxisSection newPausedSection = null;
            if (sysInfoData.Running)
            {
                if ((_previousChartData != null) && !_previousChartData.Running)
                {
                    _currentPausedSection = null;
                }
            }
            else
            {
                if (_previousChartData != null)
                {
                    if (_previousChartData.Running)
                    {
                        newPausedSection = new AxisSection()
                        {
                            Value = sysInfoData.TimeSeconds + _sysInfoEventsDeltaSeconds,
                            SectionWidth = 0.001, // 1 msec
/*!!                            Value = _previousChartData.TimeSeconds + _sysInfoEventsDeltaSeconds,
                            SectionWidth = sysInfoData.TimeSeconds - _previousChartData.TimeSeconds,*/
                        };
                    }
                }
                else
                {
                    newPausedSection = new AxisSection()
                    {
                        Value = 0,
                        SectionWidth = sysInfoData.TimeSeconds + _sysInfoEventsDeltaSeconds,
                    };
                }
            }

            if (newPausedSection != null)
            {
                newPausedSection.Fill = PausedSectionBrush;
                _currentPausedSection = newPausedSection;
                LiveTimeline.AxisX[0].Sections.Add(_currentPausedSection);
            }
            else if (_currentPausedSection != null)
            {
                _currentPausedSection.SectionWidth = sysInfoData.TimeSeconds + _sysInfoEventsDeltaSeconds
                    - _currentPausedSection.Value;
            }

            _chartValues.Add(sysInfoData);

            List<ClrJobItem> newClrJobItems;
            newClrJobItems = _jitEventProcessor.TakeJobItems();
            if (newClrJobItems != null)
            {
                AddClrJobItems(_jitSeriesValues, newClrJobItems, "JIT");
            }
            newClrJobItems = _gcEventProcessor.TakeJobItems();
            if (newClrJobItems != null)
            {
                AddClrJobItems(_gcSeriesValues, newClrJobItems, "GC");
            }
        }

        // adds new items to chart values; may need to combine the end of the existing chart values and
        // the beginning of the new items list
        private void AddClrJobItems(ChartValues<ClrJobItem> chartValues, List<ClrJobItem> jobItems,
            string eventTypeName)
        {
            Debug.Assert((jobItems.Count >= 2) && (jobItems.Count % 2 == 0));

            int chartValuesCount = chartValues.Count;
            if (chartValuesCount > 0)
            {
                Debug.Assert((chartValuesCount >= 2) &&
                    chartValues[chartValuesCount - 2].IsStart &&
                    !chartValues[chartValuesCount - 1].IsStart);

                double lastOldItemStartTime = chartValues[chartValuesCount - 2].TimeSeconds;
                double lastOldItemEndTime = chartValues[chartValuesCount - 1].TimeSeconds;

                Debug.Assert(lastOldItemStartTime <= lastOldItemEndTime);
                Debug.Assert(jobItems[0].IsStart && !jobItems[1].IsStart);

                double firstNewItemStartTime = jobItems[0].TimeSeconds;
                double firstNewItemEndTime = jobItems[1].TimeSeconds;

                Debug.Assert(firstNewItemStartTime <= firstNewItemEndTime);

                if (firstNewItemStartTime <= lastOldItemEndTime)
                {
                    // intersecting with previous event
                    if (firstNewItemEndTime <= lastOldItemEndTime)
                    {
                        // ignore duplicate events
                        if (firstNewItemStartTime >= lastOldItemStartTime)
                        {
                            jobItems.RemoveRange(0, 2);

                            DebugWrite($"Ignored duplicate {eventTypeName} event {firstNewItemStartTime:F3} .. {firstNewItemEndTime:F3}");
                        }
                        // else: (unexpected) the next event start is before the previous event start
                    }
                    else // firstNewItemEndTime > lastOldItemEndTime
                    {
                        // combine events
                        if (firstNewItemStartTime >= lastOldItemStartTime)
                        {
                            ClrJobItem saved = chartValues[chartValuesCount - 1];
                            saved.TimeSeconds = firstNewItemEndTime;
                            chartValues[chartValuesCount - 1] = saved; // allow 'chartValues' to notify it has been changed

                            jobItems.RemoveRange(0, 2);

                            DebugWrite($"Changed last {eventTypeName} event end time to {firstNewItemEndTime:F3}");
                        }
                        // else: unsupported case
                    }
                }
            }

            if (jobItems.Count > 0) // jobItems might be changed (first two items removed)
            {
                chartValues.AddRange(jobItems);

                DebugWrite($"{jobItems.Count} {eventTypeName} events added to chart");
            }
        }

        private void ProcessDebugStateChange(bool isBreakState)
        {
            _isBreakState = isBreakState;
            Dispatcher.Invoke(() => UpdateControlsOnDebugStateChange(isBreakState));
        }

        private void UpdateControlsOnDebugStateChange(bool isBreakState)
        {
            if (isBreakState)
            {
                SessionControlButton.IsEnabled = false;
            }
            else
            {
                UpdateSessionControlButton(_lastSessionState);
            }
        }

        private SysInfoData CreateChartData(SysInfoItem sysInfoItem)
        {
            SysInfoData chartData;
            if (_sysInfoStartTimeSeconds < 0)
            {
                _sysInfoStartTimeSeconds = sysInfoItem.TimeSeconds;

                DebugWrite($"System info start time: {_sysInfoStartTimeSeconds.UnixTimeToDateTime().ToString("yyyy-MM-dd HH:mm:ss.fff")} Z");

                LiveTimeline.AxisY[0].Title = String.Format($"CPU (% of {sysInfoItem.CoreNum} processors)");
                LiveTimeline.AxisY[1].MaxValue = Math.Ceiling(sysInfoItem.MemTotal / 1024.0);

                chartData = new SysInfoData();
            }
            else
            {
                if (sysInfoItem.TimeSeconds < _sysInfoStartTimeSeconds)
                {
                    return null;
                }

                double time = (sysInfoItem.TimeSeconds - _sysInfoStartTimeSeconds);
                if ((time + ViewPortTimeReserve) > _rangeMaxValue)
                {
                    bool atTheEnd = (ScrollBar.Maximum - ScrollBar.Value) < 1;
                    _rangeMaxValue += ViewPortTimeReserve;
                    UpdateScrollBar();

                    // Scroll to the end if we were at the end already
                    if (atTheEnd)
                    {
                        Offset = ScrollBar.Maximum;
                    }
                }

                chartData = new SysInfoData() { TimeSeconds = time };
                if (_previousSysInfo != null)
                {
                    double timeDelta = (sysInfoItem.TimeSeconds - _previousSysInfo.TimeSeconds);
                    if (timeDelta >= 0.001)
                    {
                        chartData.CpuPercent = Math.Min(Math.Max((sysInfoItem.UserSys - _previousSysInfo.UserSys) / timeDelta / sysInfoItem.CoreNum, 0), 100);
                    }
                }
            }
            chartData.Running = ProfilerStatusToBool(sysInfoItem.ProfilerStatus);
            chartData.MemoryMiB = sysInfoItem.MemLoad / 1024.0;
            return chartData;
        }

        private void UpdateScrollBar(bool hideScrollbar = false)
        {
            var l = ViewPortMaxValue - ViewPortMinValue;
            ScrollBar.Maximum = _rangeMaxValue - l;
            var change = Math.Max(10, l / 5);
            ScrollBar.SmallChange = change;
            ScrollBar.LargeChange = change * 10;
            ScrollBar.ViewportSize = l;
            ScrollBar.Visibility = (hideScrollbar ? Visibility.Collapsed : Visibility.Visible);
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
                case "R":
                case "Running":
                    return true;
                case "W":
                case "Waiting":
                default:
                    return false;
            }
        }

        private class RunData
        {
            /// <summary>
            /// Gets or sets a value indicating whether tracing is performed at this moment of time
            /// and the program is not in debug break state (in case of live profiling)
            /// </summary>
            public bool Running = true;

            /// <summary>
            /// Gets or sets time since profiling start in seconds
            /// </summary>
            public double TimeSeconds;
        }

        /// <summary>
        /// Part of SysInfo information obtained from the profiler to be presented on the chart
        /// </summary>
        private class SysInfoData : RunData
        {
            /// <summary>
            /// Gets or sets CPU usage in percents
            /// </summary>
            public double CpuPercent;

            /// <summary>
            /// Gets or sets memory usage in MiB (1 MiB = 2^20 bytes)
            /// </summary>
            public double MemoryMiB;
        }

        /// <summary>
        /// Represents a CLR job (JIT or GC) on the chart
        /// </summary>
        private class ClrJobItem
        {
            public ClrJobItem(double timeSeconds, bool isStart)
            {
                TimeSeconds = timeSeconds;
                IsStart = isStart;
            }

            /// <summary>
            /// A CRL job start time (if <see cref="IsStart"/> is <c>true</c>) or end time (if <c>false</c>)
            /// </summary>
            public double TimeSeconds;

            /// <summary>
            /// Is this object represents a CLR job start if <c>true</c> or stop if <c>false</c>
            /// </summary>
            public bool IsStart;
        }
    }
}
