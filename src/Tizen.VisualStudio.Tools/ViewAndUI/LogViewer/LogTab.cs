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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Tizen.VisualStudio.Tools.DebugBridge;

namespace Tizen.VisualStudio.LogViewer
{
    /// <summary>
    /// Tab to display the log for each device
    /// </summary>
    partial class LogTab : TabItem
    {
        public DataGrid logDataGrid;
        public ObservableCollection<Log> logObserverCollection;

        private const int maxLogSize = 1 << 16;
        private readonly char[] logHeaderDelimiter = { ' '/*, '/', '[', ']', ','*/ };

        private Process logProcess;
        private CollectionViewSource viewSource;
        private LogViewerControl parentControl;
        private DispatcherTimer refreshTimer;

        private List<Log> tempLogList = new List<Log>();
        private string tempHeader = string.Empty;
        private string tempMessage = string.Empty;

        public LogTab(SDBDeviceInfo device, LogViewerControl parent)
        {
            this.parentControl = parent;

            logDataGrid = CreateLogDataGrid();
            logDataGrid.LoadingRow += LogDataGrid_LoadingRow;

            ConnectLogObserver();

            ExcuteLogProcess(device?.Serial);

            viewSource.Filter += ViewSource_Filter;

            this.AddChild(logDataGrid);
            this.Loaded += LogTab_Loaded;
            this.Unloaded += LogTab_Unloaded;

            SetViewFont(logDataGrid);
            SetDataGridUpdateTimer();
        }

        private void LogTab_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.logProcess != null)
            {
                this.logProcess.Close();
                this.logProcess.Dispose();
            }
        }

        private void SetDataGridUpdateTimer()
        {
            refreshTimer = new DispatcherTimer();
            refreshTimer.Interval = TimeSpan.FromMilliseconds(200);
            refreshTimer.Tick += new EventHandler(this.HandleTypingTimerTimeout);
            refreshTimer.Start();
        }

        private void HandleTypingTimerTimeout(object sender, EventArgs e)
        {
            //DispatcherPriority is related to performance and speed.
            this.logDataGrid.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate()
            {
                lock (tempLogList)
                {
                    foreach (var input in tempLogList)
                    {
                        logObserverCollection.Add(input);
                        if (logObserverCollection.Count == maxLogSize)
                        {
                            logObserverCollection.RemoveAt(0);
                        }
                    }

                    tempLogList.Clear();
                }

                if (parentControl.scLockCheck.IsChecked == false)
                {
                    int count = logDataGrid.Items.Count;
                    if (count != 0)
                    {
                        logDataGrid.ScrollIntoView(logDataGrid.Items[count - 1]);
                    }
                }
            }));
        }

        private void ConnectLogObserver()
        {
            logObserverCollection = new ObservableCollection<Log>();

            this.viewSource = new CollectionViewSource();
            viewSource.Source = logObserverCollection;
            this.logDataGrid.SetBinding(DataGrid.ItemsSourceProperty, new Binding { Source = viewSource });
        }

        private void LogTab_Loaded(object sender, RoutedEventArgs e)
        {
            InitColWidth((int)this.logDataGrid.ActualWidth, this.logDataGrid);
            this.parentControl.logTabControl.SelectedItem = this;
        }

        void ViewSource_Filter(object sender, FilterEventArgs e)
        {
            Log log = e.Item as Log;
            e.Accepted = LogFilterController.CheckLevel(log.Level[0], parentControl) && LogFilterController.CheckFilter(log, parentControl);
        }

        void LogDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            var row = e.Row;
            Log item = row.DataContext as Log;

            row.Foreground = Resource.GetLevelColor(item.Level[0]);
            row.Background = row.GetIndex() % 2 == 0 ? Resource.evenRowColorBrush : Resource.oddRowColorBrush;
        }

        private void ExcuteLogProcess(string name)
        {
            if (!string.IsNullOrEmpty(name) && (logProcess = SDBLib.CreateSdbProcess(true, true)) != null)
            {
                logProcess.StartInfo.Arguments = " -s " + name + " dlog -v long";/* *:* */
                logProcess.StartInfo.RedirectStandardOutput = true;
                logProcess.StartInfo.RedirectStandardError = true;
                logProcess.OutputDataReceived += new DataReceivedEventHandler(Sdb_OutputDataReceived);
                logProcess.Start();
                logProcess.BeginErrorReadLine();
                logProcess.BeginOutputReadLine();
            }
        }

        private void Sdb_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    if (e.Data.StartsWith("[") /* && e.Data.EndsWith("]")*/)
                    {
                        tempHeader = e.Data;
                    }
                    else
                    {
                        Log input = new Log(tempHeader.Substring(1, tempHeader.Length - 2).Split(logHeaderDelimiter, StringSplitOptions.RemoveEmptyEntries), e.Data);
                        lock (tempLogList)
                        {
                            tempLogList.Add(input);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
    }
}
