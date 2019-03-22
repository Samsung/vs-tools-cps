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
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NetCore.Profiler.Extension.Common;
using NetCore.Profiler.Extension.VSPackage;
using NetCore.Profiler.Session.Core;
using Tizen.VisualStudio.Utilities;

namespace NetCore.Profiler.Extension.UI.SessionExplorer
{
    /// <summary>
    /// Session Explorer Window Content
    /// </summary>
    public partial class SessionExplorerWindowContent : INotifyPropertyChanged
    {
        private Visibility _openProfilePossible;

        private Visibility _openMemoryProfilePossible;

        private Visibility _openHeaptrackProfileManagedPossible;

        private Visibility _openHeaptrackProfileManagedHideUnmanagedStacksPossible;

        private ITrackSelection _trackSelection;

        private bool _documentLoading;

        private readonly object _lock = new object();

        internal SessionExplorerWindowContent()
        {
            InitializeComponent();
            ProfilerPlugin.Instance.SessionsContainer.SessionsListUpdated += UpdateList;
            UpdateList();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        internal ITrackSelection TrackSelection
        {
            get => _trackSelection;
            set
            {
                _trackSelection = value;
                _trackSelection?.OnSelectChange(
                    new SelectionContainer { SelectableObjects = null, SelectedObjects = null });
            }
        }

        public ObservableCollection<SessionListItem> Sessions { get; set; } = new ObservableCollection<SessionListItem>();

        public Visibility OpenProfilePossible
        {
            get => _openProfilePossible;
            set
            {
                _openProfilePossible = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OpenProfilePossible)));
            }
        }

        public Visibility OpenMemoryProfilePossible
        {
            get => _openMemoryProfilePossible;
            set
            {
                _openMemoryProfilePossible = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OpenMemoryProfilePossible)));
            }
        }

        public Visibility OpenHeaptrackProfilePossible
        {
            get => _openHeaptrackProfileManagedPossible;
            set
            {
                _openHeaptrackProfileManagedPossible = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OpenHeaptrackProfilePossible)));
            }
        }

        public Visibility OpenHeaptrackProfileManagedHideUnmanagedStacksPossible
        {
            get => _openHeaptrackProfileManagedHideUnmanagedStacksPossible;
            set
            {
                _openHeaptrackProfileManagedHideUnmanagedStacksPossible = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OpenHeaptrackProfileManagedHideUnmanagedStacksPossible)));
            }
        }

        private void OnClickProfile(object sender, RoutedEventArgs e)
        {
            OpenCurrentSession(false);
        }

        private void OnClickMemoryProfile(object sender, RoutedEventArgs e)
        {
            OpenCurrentSession(true);
        }

        private void OnClickMemoryProfileManaged(object sender, RoutedEventArgs e)
        {
            OpenCurrentSession(false, "--managed ");
        }

        private void OnClickMemoryProfileManagedHideUnmanagedStacks(object sender, RoutedEventArgs e)
        {
            OpenCurrentSession(false, "--hide-unmanaged-stacks --managed ");
        }

        private void OnClickDelete(object sender, RoutedEventArgs e)
        {
            if (SessionsGrid.SelectedIndex != -1 && SessionsGrid.SelectedItem is SessionListItem item)
            {
                if (ProfilerPlugin.Instance.ShowMessage(MessageDialogType.Question,
                    "Do you really want to delete the selected session?") == 6)
                {
                    ProfilerPlugin.Instance.SessionsContainer.DeleteSession(item.Session);
                }
            }
        }

        private void OnClickEdit(object sender, RoutedEventArgs e)
        {
            if (SessionsGrid.SelectedIndex != -1 && SessionsGrid.SelectedItem is SessionListItem item)
            {
                var dlg = new EditAnnotationDialog(item.Annotation);
                dlg.ShowDialog();
                if (dlg.DialogResult ?? false)
                {
                    item.Annotation = dlg.Annotation;
                    UpdateSelection(item);
                }
            }
        }

        private void SessionsGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                UpdateSelection((SessionListItem)e.AddedItems[0]);
            }

            Visibility openMemoryProfilePossible = Visibility.Collapsed;
            Visibility openProfilePossible = Visibility.Collapsed;
            Visibility openHeaptrackProfilePossible = Visibility.Collapsed;
            Visibility openHeaptrackProfileManagedHideUnmanagedStacksPossible = Visibility.Collapsed;
            if (SessionsGrid.SelectedIndex != -1 && SessionsGrid.SelectedItem is SessionListItem item)
            {
                openMemoryProfilePossible = item.Session.Properties.GetBoolProperty("ProfilingOptions", "ProfMemoryTrace", false) ? Visibility.Visible : Visibility.Collapsed;
                openProfilePossible = item.Session.Properties.GetBoolProperty("ProfilingOptions", "ProfExecutionTrace", false) ? Visibility.Visible : Visibility.Collapsed;
                openHeaptrackProfilePossible = item.Preset == "Memory Profiling" ? Visibility.Visible : Visibility.Collapsed;
                openHeaptrackProfileManagedHideUnmanagedStacksPossible = item.Preset == "Memory Profiling" ? Visibility.Visible : Visibility.Collapsed;
            }

            OpenMemoryProfilePossible = openMemoryProfilePossible;
            OpenProfilePossible = openProfilePossible;
            OpenHeaptrackProfilePossible = openHeaptrackProfilePossible;
            OpenHeaptrackProfileManagedHideUnmanagedStacksPossible = openHeaptrackProfileManagedHideUnmanagedStacksPossible;
        }

        private void SessionsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenCurrentSession((OpenMemoryProfilePossible == Visibility.Visible) && !(OpenProfilePossible == Visibility.Visible));
        }

        private void UpdateSelection(SessionListItem o)
        {
            TrackSelection?.OnSelectChange(CreateSelectionContainer(o));
        }

        private void UpdateList()
        {
            Sessions.Clear();
            foreach (var x in ProfilerPlugin.Instance.SessionsContainer.Sessions.Select(s => new SessionListItem(s)).OrderBy(s => s.CreatedAt))
            {
                Sessions.Add(x);
            }
        }

        private ISelectionContainer CreateSelectionContainer(SessionListItem o)
        {
            var x = new ArrayList { new SessionProperties(o) };
            var result = new SelectionContainer(false, false)
            {
                SelectedObjects = x,
                SelectableObjects = x
            };
            return result;
        }

        private void OpenCurrentSession(bool openMemorySession, string args = null)
        {
            lock (_lock)
            {
                if (!_documentLoading && SessionsGrid.SelectedIndex != -1 &&
                    SessionsGrid.SelectedItem is SessionListItem item)
                {
                    _documentLoading = true;
                    ShowProgressBar();

                    if (OpenHeaptrackProfilePossible == Visibility.Visible)
                    {
                        string path = Path.GetDirectoryName(item.Path);
                        args = args ?? "--managed ";
                        ProfilerPlugin.Instance.MemoryProfilerGuiOpenSession(path, args);
                        HideProgressBar();
                        return;
                    }

                    if (openMemorySession)
                    {
                        ProfilerPlugin.Instance.ShowMemorySession(item.Path, HideProgressBar);
                    }
                    else
                    {
                        ProfilerPlugin.Instance.ShowSession(item.Path, HideProgressBar);
                    }
                }
            }
        }

        private void ShowProgressBar()
        {
            ProgressBar.Visibility = Visibility.Visible;
        }

        private void HideProgressBar()
        {
            Dispatcher.InvokeAsync(delegate()
            {
                lock (_lock)
                {
                    _documentLoading = false;
                    ProgressBar.Visibility = Visibility.Collapsed;
                }
            });
        }

        public class SessionListItem : NotifyPropertyChanged
        {
            public SessionListItem(ISavedSession session)
            {
                Session = session;

                var dt = Session.CreatedAt.ToLocalTime();
                var now = DateTime.Now;
                var dateString = (now.Date.Equals(dt.Date) ? "Today" : now.Date.Year == dt.Date.Year ? $"{dt:MM/dd}" : $"{dt:MM/dd/yy}")
                    + $" {dt:HH:mm}";
                CreatedAt = dateString;
            }

            public ISavedSession Session { get; }

            public string CreatedAt { get; private set; }

            public string ProjectName => Session.ProjectName;

            public string Preset => Session.PresetName;

            public string DeviceName => Session.DeviceName;

            public string Path => Session.SessionFile;

            public string Annotation
            {
                get => Session.Annotation;
                set
                {
                    Session.Annotation = value;
                    OnPropertyChanged();

                    // ReSharper disable once ExplicitCallerInfoArgument
                    OnPropertyChanged("HasAnnotation");
                }
            }

            public bool HasAnnotation => !string.IsNullOrEmpty(Annotation);
        }

        private class SessionProperties : PropertiesContainer
        {
            private readonly SessionListItem _sessionItem;

            public SessionProperties(SessionListItem sessionItem) : base(sessionItem.CreatedAt, "Session Properties")
            {
                _sessionItem = sessionItem;
            }

            [Category("Common")]
            [DisplayName("Created")]
            [Description("The DateteTime of the Session.")]
            public string CreatedAt => _sessionItem.CreatedAt;

            [Category("Common")]
            [DisplayName("Project Name")]
            [Description("The Project of Profiled Application.")]
            public string ProjectName => _sessionItem.ProjectName;

            [Category("Common")]
            [DisplayName("Device Name")]
            [Description("The Device on which Application was run.")]
            public string DeviceName => _sessionItem.DeviceName;

            [Category("Common")]
            [DisplayName("Preset")]
            [Description("The profiling Preset.")]
            public string Preset => _sessionItem.Preset;

            [Category("Common")]
            [DisplayName("Session Path")]
            [Description("The Path to the Session Data.")]
            public string Path => _sessionItem.Session.SessionFolder;

            [Category("Common")]
            [DisplayName("Annotation")]
            [Description("The Annotation of the Session.")]
            public string Annotation
            {
                get => _sessionItem.Annotation;
                set => _sessionItem.Annotation = value;
            }

            [Category("Sampling Options")]
            [DisplayName("Sampling Interval")]
            public int SamplingInterval => _sessionItem.Session.Properties.GetIntProperty("ProfilingOptions", "ProfSamplingTimeout", 0);

            [Category("Sampling Options")]
            [DisplayName("High Granularity Sampling")]
            public bool HighGranularitySampling => _sessionItem.Session.Properties.GetBoolProperty("ProfilingOptions", "ProfHighGran", false);

            [Category("Sampling Options")]
            [DisplayName("Trace Execution")]
            public bool TraceExecution => _sessionItem.Session.Properties.GetBoolProperty("ProfilingOptions", "ProfExecutionTrace", false);

            [Category("Sampling Options")]
            [DisplayName("Trace Memory Allocation")]
            public bool TraceMemoryAllocation => _sessionItem.Session.Properties.GetBoolProperty("ProfilingOptions", "ProfMemoryTrace", false);

            [Category("Sampling Options")]
            [DisplayName("Trace Garbage Collection")]
            public bool TraceGarbageCollection => _sessionItem.Session.Properties.GetBoolProperty("ProfilingOptions", "ProfTraceGarbageCollection", false);

            [Category("Sampling Options")]
            [DisplayName("Trace Source Lines")]
            public bool TraceSourceLines => _sessionItem.Session.Properties.GetBoolProperty("ProfilingOptions", "ProfLineTrace", false);

            [Category("Sampling Options")]
            [DisplayName("Stack Track")]
            public bool StackTrack => _sessionItem.Session.Properties.GetBoolProperty("ProfilingOptions", "ProfStackTrack", false);

            [Category("CPU Tracing Options")]
            [DisplayName("Enable CPU Tracing")]
            public bool TraceCpu => _sessionItem.Session.Properties.GetBoolProperty("ProfilingOptions", "ProfCpuTrace", false);

            [Category("CPU Tracing Options")]
            [DisplayName("Trace Process")]
            public bool TraceProcessCpu => _sessionItem.Session.Properties.GetBoolProperty("ProfilingOptions", "ProfCpuTraceProc", false);

            [Category("CPU Tracing Options")]
            [DisplayName("Trace Threads")]
            public bool TraceThreadCpu => _sessionItem.Session.Properties.GetBoolProperty("ProfilingOptions", "ProfCpuTraceThread", false);

            [Category("CPU Tracing Options")]
            [DisplayName("Trace Interval")]
            public int CpuTraceInterval => _sessionItem.Session.Properties.GetIntProperty("ProfilingOptions", "ProfCpu_traceTimeout", 0);

            [Category("Misc Tracing Options")]
            [DisplayName("Delayed Start")]
            public bool DelayedStart => _sessionItem.Session.Properties.GetBoolProperty("ProfilingOptions", "ProfDelayedStart", false);

            [Category("Misc Tracing Options")]
            [DisplayName("Sleep")]
            public int SleepTime => _sessionItem.Session.Properties.GetIntProperty("ProfilingOptions", "Sleep", 0);
        }
    }
}
