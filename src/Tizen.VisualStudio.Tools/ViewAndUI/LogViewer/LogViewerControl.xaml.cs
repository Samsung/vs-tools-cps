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

namespace Tizen.VisualStudio.LogViewer
{
    using Tizen.VisualStudio.Tools.DebugBridge;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Timers;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Threading;

    /// <summary>
    /// Interaction logic for LogViewerControl.
    /// </summary>
    public partial class LogViewerControl : UserControl, ISDBDeviceChangeListener
    {
        DispatcherTimer _typingTimer;
        private Timer timerReconnectSDB = null;
        private static TabControl LogTabControl = null;
        private static LogViewerControl staticLogViewerControl = null;
        private LogViewModel LVM = new LogViewModel();

        public string ID
        {
            get
            {
                return "Tizen.Extension.LogView";
            }
        }

        public LogViewerControl()
        {
            this.InitializeComponent();
            this.DataContext = LVM;

            LogTabControl = this.logTabControl;
            staticLogViewerControl = this;

            Resource.InitEnvColor();
            //Resource.initLeveColorlDic();

            DeviceManager.SubscribeDeviceList(this as ISDBDeviceChangeListener);
            DeviceManager.SubscribeSelectedDevice(SelctedDeviceChanged);

            this.Loaded += LogViewerControl_Loaded;

            Microsoft.VisualStudio.PlatformUI.VSColorTheme.ThemeChanged += delegate
            {
                Resource.InitEnvColor();
                //Resource.initLeveColorlDic();
                CreateLogTabDispatcher(DeviceManager.SelectedDevice);
            };
        }

        void SelctedDeviceChanged(object sender, EventArgs e)
        {
            CreateLogTabDispatcher(DeviceManager.SelectedDevice);
        }

        private void LogViewerControl_Loaded(object sender, RoutedEventArgs e)
        {
            CreateLogTabDispatcher(DeviceManager.SelectedDevice);
        }

        #region UIComponent Event
        private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox filterTextBox = (TextBox)sender;
            if (_typingTimer == null)
            {
                _typingTimer = new DispatcherTimer();
                _typingTimer.Interval = TimeSpan.FromMilliseconds(500);

                _typingTimer.Tick += new EventHandler(this.HandleTypingTimerTimeout);
            }

            _typingTimer.Start();
            _typingTimer.Tag = filterTextBox.Text;
            _typingTimer.Start();
        }

        private void HandleTypingTimerTimeout(object sender, EventArgs e)
        {
            if (_typingTimer == null)
            {
                return;
            }

            SetChkFilter();
            _typingTimer.Stop();
        }

        private void SetlevelCheck_Checked(object sender, RoutedEventArgs e)
        {
            SetChkFilter();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            (logTabControl.SelectedItem as LogTab)?.logObserverCollection.Clear();
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceManager.SelectedDevice != null)
            {
                DataGrid dataGrid = logTabControl.SelectedContent as DataGrid;
                LogExporter lExporter = new LogExporter(dataGrid);

                lExporter.OpenSaveFileDialog();
            }
        }
        #endregion

        #region filter
        private void SetChkFilter()
        {
            if (logTabControl == null)
            {
                return;
            }

            if (logTabControl.SelectedItem == null)
            {
                return;
            }

            if ((logTabControl.SelectedItem as LogTab).logDataGrid != null)
            {
                ICollectionView cv = CollectionViewSource.GetDefaultView((logTabControl.SelectedItem as LogTab).logDataGrid.Items);

                cv.Filter = o =>
                {
                    Log log = o as Log;
                    return LogFilterController.CheckLevel(log.Level[0], this) && LogFilterController.CheckFilter(log, this);
                };
            }
        }

        public void OnSDBConnectFailed()
        {
            if (this.timerReconnectSDB != null)
            {
                this.timerReconnectSDB.Dispose();
                this.timerReconnectSDB = null;
            }

            this.timerReconnectSDB = new Timer();
            this.timerReconnectSDB.Elapsed +=
                                new ElapsedEventHandler(OnTimerReconnectSDB);
            this.timerReconnectSDB.Interval = 3000;
            this.timerReconnectSDB.AutoReset = false;
            this.timerReconnectSDB.Start();
        }

        public void OnSDBDeviceChanged()
        {
            /* Do nothing. Log tab is controlled by SelectedDeviceChangedEvent from DeviceManager */
        }

        void OnTimerReconnectSDB(object source, ElapsedEventArgs e)
        {
            this.timerReconnectSDB.Stop();
            this.timerReconnectSDB.Dispose();
            this.timerReconnectSDB = null;

            DeviceManager.StopDeviceMonitor();
            DeviceManager.StartDeviceMonitor();
        }

        #endregion

        private void CreateLogTabDispatcher(SDBDeviceInfo device)
        {
            try
            {
                LogTabControl?.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Input,
                            new Action(delegate()
                            {
                                LogTabControl.Items.Clear();
                                LogTabControl.Items.Add(new LogTab(device, staticLogViewerControl));
                            }));
            }
            catch
            {

            }
        }
    }

    public class LogViewModel
    {
        public ObservableCollection<SDBDeviceInfo> _comboItemList = new ObservableCollection<SDBDeviceInfo>();
        public SDBDeviceInfo SelectItem { get; set; }
        public ObservableCollection<SDBDeviceInfo> ComboItemList
        {
            get
            {
                return _comboItemList;
            }
        }
    }
}