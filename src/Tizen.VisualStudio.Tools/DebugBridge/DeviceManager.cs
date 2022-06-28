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
using Microsoft.VisualStudio.Shell.Interop;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Tizen.VisualStudio.Tools.DebugBridge.SDBCommand;

namespace Tizen.VisualStudio.Tools.DebugBridge
{
    public class TizenDeviceProfile
    {
        public Guid uniqueName;
        public string displayName;

        public SDBDeviceInfo deviceInfo;
    }

    public class DeviceManager : ISDBDeviceChangeListener
    {
        private static DeviceManager deviceManager = null;
        private SDBDeviceMoniter DeviceMoniter;
        private event EventHandler SelectedDeviceChangedEvent;

        public const string LaunchEmulator = "Launch Tizen Emulator";

        public static List<SDBDeviceInfo> DeviceInfoList
        {
            get { return deviceManager?.DeviceMoniter.DeviceInfoList; }
        }

        public static Dictionary<string, SDBDeviceInfo> DeviceInfoCollection
        {
            get { return deviceManager?.DeviceMoniter.DeviceInfoCollection; }
        }

        public static Dictionary<string, SDBCapability> SdbCapsMap
        {
            get { return deviceManager?.DeviceMoniter.SdbCapsMap; }
        }

        public static bool isDebuggerInstalled
        {
            get { return deviceManager.DeviceMoniter.isDebuggerInstalled; }
        }

        public static SDBDeviceInfo SelectedDevice
        {
            get; private set;
        }

        string ISDBDeviceChangeListener.ID
        {
            get { return "Tizen.NET.Tools.DeviceManager"; }
        }

        public static List<TransformBlock<string, IProjectVersionedValue<IReadOnlyList<IEnumValue>>>> DebugProfilesBlockList
        {
            get;
            private set;
        } = new List<TransformBlock<string, IProjectVersionedValue<IReadOnlyList<IEnumValue>>>>();

        public static void Initialize(IVsOutputWindowPane outputPane)
        {
            deviceManager = new DeviceManager();
            deviceManager.InitDeviceMonitor(outputPane);
        }

        public static void SubscribeDeviceList(ISDBDeviceChangeListener listener)
        {
            deviceManager.DeviceMoniter.Subscribe(listener);
        }

        public static void SubscribeSelectedDevice(EventHandler deviceChangedEventHandler)
        {
            if (deviceManager != null)
            {
                deviceManager.SelectedDeviceChangedEvent += deviceChangedEventHandler;
            }
        }

        public static void ResetDeviceMonitorRetry()
        {
            deviceManager.DeviceMoniter.ResetRetry();
        }

        public static void StartDeviceMonitor()
        {
            deviceManager?.DeviceMoniter?.StartService();
        }

        public static void StopDeviceMonitor()
        {
            deviceManager?.DeviceMoniter?.StopService();
        }

        public static void SelectDevice(SDBDeviceInfo newlySelectedDevice)
        {
            bool isNoDeviceSelected = (newlySelectedDevice == null);
            bool isNewDeviceSelected = !isNoDeviceSelected && !newlySelectedDevice.Serial.Equals(SelectedDevice?.Serial);
            bool isSelectedDeviceChanged = isNewDeviceSelected && !(SelectedDevice == null && newlySelectedDevice == null);

            SelectedDevice = newlySelectedDevice;

            if (isSelectedDeviceChanged)
            {
                deviceManager?.BroadcastSelectedDeviceChangedEvent();
            }
        }

        public static string AdjustSdbArgument(SDBDeviceInfo device, string argument)
        {
            if (device == null)
            {
                List<SDBDeviceInfo> devInfoList = DeviceInfoList;
                int itemCount = (devInfoList != null) ? devInfoList.Count : 0;
                if (itemCount > 1)
                {
                    device = SelectedDevice;
                }
            }
            if (device != null)
            {
                return $"-s {device.Serial} {argument}";
            }
            return argument;
        }

        public static void UpdateDebugTargetList(bool wasSelectedDeviceDetached)
        {
            string msg = string.Empty;

            if (!wasSelectedDeviceDetached && DeviceInfoList.Count > 1)
            {
                PrioritizeSelectedDevice();
            }

            foreach (var block in DebugProfilesBlockList)
            {
                block?.Post(msg);
            }
        }

        private DeviceManager()
        {
        }

        private void InitDeviceMonitor(IVsOutputWindowPane outputPane)
        {
            this.DeviceMoniter = new SDBDeviceMoniter();
            this.DeviceMoniter.Initialize(outputPane);
            // add to first item, so that be called first to prepare
            // display and and stuff
            this.DeviceMoniter.Subscribe(this as ISDBDeviceChangeListener);
        }

        private void BroadcastSelectedDeviceChangedEvent()
        {
            if (deviceManager?.SelectedDeviceChangedEvent != null)
            {
                deviceManager?.SelectedDeviceChangedEvent(deviceManager, EventArgs.Empty);
            }
        }

        private static void PrioritizeSelectedDevice()
        {
            if (SelectedDevice != null)
            {
                SDBDeviceInfo movedUpDevice = DeviceInfoList.FindLast(device => SelectedDevice.Serial.Equals(device.Serial));
                DeviceInfoList.Remove(movedUpDevice);
                DeviceInfoList.Insert(DeviceInfoList.Count, SelectedDevice);
            }
        }

        void ISDBDeviceChangeListener.OnSDBConnectFailed()
        {
            // nothing to do
        }

        void ISDBDeviceChangeListener.OnSDBDeviceChanged()
        {
            bool wasSelectedDeviceDetached = (SelectedDevice != null) && !DeviceInfoList.Exists(device => SelectedDevice.Serial.Equals(device.Serial));//!DeviceInfoCollection.ContainsKey(SelectedDevice.serial);
            UpdateDebugTargetList(wasSelectedDeviceDetached);
            SelectDevice(DeviceInfoList.FindLast(_ => true));
        }
    }
}
