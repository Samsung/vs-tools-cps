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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using System.Windows.Forms;

namespace Tizen.VisualStudio.Tools.DebugBridge
{
    // TODO: change this to delegate
    public interface ISDBDeviceChangeListener
    {
        string ID { get; }

        void OnSDBConnectFailed();
        void OnSDBDeviceChanged();
    }

    public class SDBDeviceInfo
    {
        // https://developer.tizen.org/dev-guide/2.4/org.tizen.devtools/html/common_tools/smart_dev_bridge.htm
        public string Serial { get; set; }
        public string Status { get; set; }   // offline or device
        public string Name { get; set; }

        public SDBDeviceInfo(string serial, string status, string name)
        {
            this.Serial = serial;
            this.Status = status;
            this.Name = name;
        }
    }

    public class SDBDeviceMoniter
    {
        private List<SDBDeviceInfo> deviceInfoList = null;
        private SDBConnection sdbconnection = null;
        private List<ISDBDeviceChangeListener> listeners = null;
        private Task<DeviceMonitorResult> taskMonitor = null;
        private CancellationTokenSource taskCancelSource = null;
        private IVsOutputWindowPane outputPane = null;
        private Object lockDeviceInfoList;
        private int retryConntectServerCount = 0;

        private enum DeviceMonitorResult
        {
            CanceledByUser = 0,
            SdbEstablishmentError = 1 << 1,
            SdbResponseError = 1 << 2,
            SdbServerKilled = 1 << 3
        }

        public Dictionary<string, SDBDeviceInfo> DeviceInfoCollection
        {
            get;
        }

        public static readonly char[] DeviceToken = { '\t' };

        public bool NeedsRestart { get; set; }

        public List<SDBDeviceInfo> DeviceInfoList
        {
            get { return deviceInfoList; }
        }

        public SDBDeviceMoniter()
        {
            this.lockDeviceInfoList = new Object();
            this.deviceInfoList = new List<SDBDeviceInfo>();
            this.DeviceInfoCollection = new Dictionary<string, SDBDeviceInfo>();
            this.NeedsRestart = false;
        }

        public void Initialize(IVsOutputWindowPane outputPane)
        {
            this.listeners = new List<ISDBDeviceChangeListener>();
            this.outputPane = outputPane;
        }

        public void Subscribe(ISDBDeviceChangeListener listener)
        {
            ISDBDeviceChangeListener result;
            result = this.listeners.Find(item => item.ID == listener.ID);
            if (result != listener)
            {
                this.listeners.Add(listener);
            }
        }

        public void ResetRetry()
        {
            retryConntectServerCount = 10;
        }

        public void StartService()
        {
            OutputDeviceInfoMsg("Start Device Monitor...");
            string sdbPath = Data.ToolsPathInfo.SDBPath;

            if (!File.Exists(sdbPath))
            {
                OutputDeviceInfoMsg("Failed to get SDB : " + sdbPath);
            }
            else
            {
                // cleanup any existing using StopService().
                // this may change in the future so, user should call
                // StopService() when required.
                StopService();

                // start a clean service
                this.NeedsRestart = false;
                this.taskCancelSource = new CancellationTokenSource();
                StartDeviceChangeService();
            }
        }

        public void StopService()
        {
            if (this.taskCancelSource != null)
            {
                this.taskCancelSource.Cancel();
            }

            if (this.taskMonitor != null)
            {
                try
                {
                    if (this.sdbconnection != null)
                    {
                        this.sdbconnection.Shutdown();
                    }

                    this.taskMonitor.Wait();
                }
                catch (AggregateException e)
                {
                    foreach (var v in e.InnerExceptions)
                    {
                        Console.WriteLine(e.Message + " " + v.Message);
                    }
                }
            }

            if (this.taskCancelSource != null)
            {
                this.taskCancelSource.Dispose();
                this.taskCancelSource = null;
            }

            if (this.sdbconnection != null)
            {
                this.sdbconnection.Close();
                this.sdbconnection = null;
            }
        }

        private void StartDeviceChangeService()
        {
            if (this.taskMonitor == null ||
                (this.taskMonitor != null && this.taskMonitor.IsCompleted))
            {
                taskMonitor = Task.Run(() => DeviceChangeDetectTask());
                taskMonitor.ContinueWith((i) => HandleDeviceMonitorResult(i));
            }
        }

        private DeviceMonitorResult DeviceChangeDetectTask()
        {
            DeviceMonitorResult ret = DeviceMonitorResult.CanceledByUser;

            this.sdbconnection = SDBConnection.Create();
            if (this.sdbconnection == null)
            {
                OutputDeviceInfoMsg("Failed to connect to SDB Server. ");
                this.taskCancelSource.Cancel();
                retryConntectServerCount--;
                if (retryConntectServerCount > 0)
                {
                    NotifyConnectServerFailed();
                }
                else
                {
                    OutputDeviceInfoMsg("Too many retries.\n" +
                        "  Please check Tools option and sdb.exe is installed correctly.\n" +
                        "  After the problem is fixed, reopen the Solution to start the Device Monitor");
                }

                return DeviceMonitorResult.SdbEstablishmentError;
            }

            Thread.Sleep(100);

            SDBRequest request = SDBConnection.MakeRequest("host:track-devices");
            SDBResponse response = this.sdbconnection.Send(request);

            if (!response.IOSuccess || !response.Okay)
            {
                this.sdbconnection.Close();
                this.sdbconnection = null;

                OutputDeviceInfoMsg("Failed to start device monitor.");
                this.taskCancelSource.Cancel();
                NotifyConnectServerFailed();
                return DeviceMonitorResult.SdbResponseError;
            }

            OutputDeviceInfoMsg("Device monitor started.");

            CancellationToken cancelToken = this.taskCancelSource.Token;
            bool connectionError = false;

            while (true)
            {
                if (this.sdbconnection.ConnectionError())
                {
                    connectionError = true;
                    OutputDeviceInfoMsg("SDB Server disconnected.");
                    ret = DeviceMonitorResult.SdbServerKilled;
                    break;
                }

                if (this.sdbconnection.DataAvailable())
                {
                    int length = this.sdbconnection.ReadLength();
                    if (length > 0)
                    {
                        OutputDeviceChangeMsg();

                        byte[] buffer = new byte[length];
                        string result = this.sdbconnection.ReadData(buffer);
                        ProcessDeviceData(result);
                        NotifySubscribers();
                    }
                    else if (length == 0)
                    {
                        if (this.deviceInfoList.Count > 0)
                        {
                            // show message only if there was any device connected
                            // we don't have to show when 0 changed to 0
                            OutputDeviceChangeMsg();
                        }

                        ProcessDeviceData(String.Empty);
                        NotifySubscribers();
                    }
                    else
                    {
                        if (cancelToken.IsCancellationRequested)
                        {
                            ret = DeviceMonitorResult.CanceledByUser;
                            break;
                        }

                        connectionError = true;
                        OutputDeviceInfoMsg("SDB Server disconnected.");
                        ret = DeviceMonitorResult.SdbServerKilled;
                        break;
                    }
                }
                else
                {
#if DEBUG
                    ////OutputDeviceInfoMsg("sdb host:track-devices timeout...", false);
#endif
                    Thread.Sleep(1);
                }

                if (cancelToken.IsCancellationRequested)
                {
                    ret = DeviceMonitorResult.CanceledByUser;
                    break;
                }

            }

            this.sdbconnection.Close();
            this.sdbconnection = null;

            if (connectionError)
            {
                NeedsRestart = true;
            }

            return ret;
        }

        private DeviceMonitorResult HandleDeviceMonitorResult(Task<DeviceMonitorResult> devMonitorResult)
        {
            DeviceMonitorResult ret = devMonitorResult.Result;

            if (ret != DeviceMonitorResult.CanceledByUser)
            {
                DeviceManager.SelectDevice(null);
                DeviceManager.DeviceInfoList?.Clear();
                DeviceManager.UpdateDebugTargetList(true);

                string msg = string.Format("SDB Server disconnected.\n({0})\n\nClick 'Retry' or 'Tools -> Tizen -> Restart Sdb Server' to establish the connection.", SDBLib.GetSdbFilePath());

                if (MessageBox.Show(msg, "Visual Studio Tools for Tizen", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry)
                {
                    StartService();
                }
            }

            return ret;
        }

        private void ProcessDeviceData(string deviceinfo)
        {
            string[] devices = deviceinfo.Split(
                                    new string[] { "\r\n", "\n" },
                                    StringSplitOptions.RemoveEmptyEntries);
            List<SDBDeviceInfo> deviceInfoList = new List<SDBDeviceInfo>();

            DeviceInfoCollection.Clear();

            foreach (string item in devices)
            {
                string[] onedevice = item.Split(DeviceToken,
                                        StringSplitOptions.RemoveEmptyEntries);

                SDBDeviceInfo devinfo = new SDBDeviceInfo(onedevice[0].TrimEnd(), onedevice[1].TrimEnd(), onedevice[2].TrimEnd());

                deviceInfoList.Add(devinfo);

                DeviceInfoCollection.Add(devinfo.Serial, devinfo);
            }

            lock (this.lockDeviceInfoList)
            {
                this.deviceInfoList = deviceInfoList;
            }
        }

        private void NotifyConnectServerFailed()
        {
            foreach (ISDBDeviceChangeListener listener in this.listeners)
            {
                listener.OnSDBConnectFailed();
            }
        }

        private void NotifySubscribers()
        {
            foreach (ISDBDeviceChangeListener listener in this.listeners)
            {
                listener.OnSDBDeviceChanged();
            }
        }

        //
        // TODO
        // we may have to make a formal format for outputs
        //
        private void OutputDeviceChangeMsg()
        {
            DateTime localDate = DateTime.Now;
            string message =
                String.Format("{0} : {1}\n",
                              localDate.ToString(),
                              "Device attach/detach detected.");
            this.outputPane.OutputString(message);
        }

        private void OutputDeviceInfoMsg(string msg, bool activate = true)
        {
            DateTime localDate = DateTime.Now;
            string message =
                String.Format("{0} : {1}\n",
                              localDate.ToString(),
                              msg);
            if (activate)
            {
                this.outputPane.Activate();
            }

            this.outputPane.OutputString(message);
        }
    }
}

/*
 * reference of codes
 * https://msdn.microsoft.com/en-us/library/system.threading.cancellationtokensource.aspx
 */
