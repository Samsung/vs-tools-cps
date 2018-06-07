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
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using Tizen.VisualStudio.Tools.DebugBridge;

namespace Tizen.VisualStudio.ConnectToolbar
{
    public class ConnectDeviceCombo
    {
        private static List<string> comboboxList = new List<string>();
        private static string currentDropDownComboChoice = string.Empty;

        private static void RefreshComboboxList()
        {
            comboboxList.Clear();

            foreach (var device in DeviceManager.DeviceInfoList)
            {
                comboboxList.Add(device.Serial);
            }
        }

        public static void HandleConnectCombo(object sender, EventArgs e)
        {
            OleMenuCmdEventArgs eventArgs = e as OleMenuCmdEventArgs;

            if (eventArgs != null)
            {
                string newChoice = eventArgs.InValue as string;
                IntPtr vOut = eventArgs.OutValue;

                RefreshComboboxList();

                comboboxList.Add("LocalWindow");

                if (vOut != IntPtr.Zero)
                {
                    bool validInput = false;
                    for (int i = 0; i < comboboxList.Count; i++)
                    {
                        if (comboboxList[i].Equals(DeviceManager.SelectedDevice.Name))
                        {
                            validInput = true;
                            currentDropDownComboChoice = comboboxList[i];
                            break;
                        }
                    }

                    if (!validInput)
                    {
                        currentDropDownComboChoice = "LocalWindow";
                    }

                    Marshal.GetNativeVariantForObject(currentDropDownComboChoice, vOut);
                    DeviceManager.SelectDevice(
                        //DeviceManager.DeviceInfoCollection[currentDropDownComboChoice]
                        DeviceManager.DeviceInfoList.Find(device => currentDropDownComboChoice.Equals(device.Serial)));
                    DeviceManager.UpdateDebugTargetList(false);
                }
                else if (newChoice != null)
                {
                    bool validInput = false;
                    int indexInput = -1;

                    if (comboboxList.Count == 1)
                    {
                        currentDropDownComboChoice = comboboxList[0];
                    }
                    else
                    {
                        for (indexInput = 0; indexInput < comboboxList.Count; indexInput++)
                        {
                            if (string.Compare(comboboxList[indexInput], newChoice, StringComparison.CurrentCultureIgnoreCase) == 0)
                            {
                                validInput = true;
                                break;
                            }
                        }

                        if (validInput)
                        {
                            currentDropDownComboChoice = comboboxList[indexInput];
                            DeviceManager.SelectDevice(
                                //DeviceManager.DeviceInfoCollection[currentDropDownComboChoice]
                                DeviceManager.DeviceInfoList.Find(device => currentDropDownComboChoice.Equals(device.Serial)));
                            DeviceManager.UpdateDebugTargetList(false);
                        }
                    }
                }
            }
            else
            {
                //throw (new ArgumentException("EventArgsRequired"));
            }
        }

        public static void HandleConnectComboList(object sender, EventArgs e)
        {
            OleMenuCmdEventArgs eventArgs = e as OleMenuCmdEventArgs;

            if (eventArgs != null)
            {
                object inParam = eventArgs.InValue;
                IntPtr vOut = eventArgs.OutValue;

                RefreshComboboxList();

                if (inParam != null)
                {
                    throw (new ArgumentException("InParamIllegal"));
                }
                else if (vOut != IntPtr.Zero)
                {
                    Marshal.GetNativeVariantForObject(comboboxList.ToArray(), vOut);
                }
                else
                {
                    throw (new ArgumentException("OutParamRequired"));
                }
            }
        }

        public static void HandleRemoteDevice(object sender, EventArgs e)
        {
            RemoteDeviceManager rManager = new RemoteDeviceManager();

            if (rManager.ShowDialog() == true)
            {

            }
        }
    }
}