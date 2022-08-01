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
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using Tizen.VisualStudio.Tools.DebugBridge;

namespace Tizen.VisualStudio.ConnectToolbar
{
    /// <summary>
    /// Main window
    /// </summary>
    public partial class RemoteDeviceManager : Window
    {
        private const string MessageDialogTitle = "Tizen Plugin";

        private bool IsConnect = false;

        public RemoteDeviceManager()
        {
            InitializeComponent();
            this.Closing += RemoteDeviceManager_Closing;

            string remoteListPath = GetRemoteListPath();

            FileInfo toolsFileInfo = new FileInfo(remoteListPath);

            if (toolsFileInfo.Exists)
            {
                using (StreamReader sr = new StreamReader(remoteListPath))
                {
                    while ((sr.Peek() >= 0))
                    {
                        string readstr = sr.ReadLine();

                        if (readstr != null)
                        {
                            var item = GetDeviceDataFromString(readstr);

                            if (item != null)
                            {
                                RDMListView.Items.Add(item);
                            }
                        }
                    }
                }
            }
        }

        private void RemoteDeviceManager_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.DialogResult = this.IsConnect;
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            DeviceInfo DVInfo = new DeviceInfo();
            DVInfo.ShowDialog();

            if (DVInfo.DialogResult == true)
            {
                RDMListView.Items.Add(
                    new ItemsData { Name = DVInfo.DeviceName, IP = DVInfo.IP, Port = DVInfo.Port });
                SaveRemoteList();
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (RDMListView.SelectedItem != null)
            {
                ItemsData selecteditem = RDMListView.SelectedItem as ItemsData;

                DeviceInfo DVInfo = new DeviceInfo();
                DVInfo.SetDeviceData(selecteditem.Name,
                                        selecteditem.IP,
                                        selecteditem.Port);

                DVInfo.ShowDialog();

                if (DVInfo.DialogResult == true)
                {
                    selecteditem.Name = DVInfo.DeviceName;
                    selecteditem.IP = DVInfo.IP;
                    selecteditem.Port = DVInfo.Port;

                    RDMListView.Items.Refresh();
                    SaveRemoteList();
                }
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = RDMListView.SelectedIndex;
            if (selectedIndex == -1)
            {
                return;
            }

            string ip = ((ItemsData)RDMListView.Items[selectedIndex]).IP;
            string lastNonEmptyLine = "";
            bool connected = false;
            int exitCode;
            SDBLib.SdbRunResult sdbResult;

            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
            try
            {
                sdbResult = SDBLib.RunSdbCommand(null, "connect " + ip,
                    (bool isStdOut, string line) =>
                    {
                        if (isStdOut)
                        {
                            if (line != "")
                            {
                                lastNonEmptyLine = line;
                            }
                            if (line.StartsWith("connected to"))
                            {
                                connected = true;
                                return true;
                            }
                        }
                        return false;
                    },
                    out exitCode);
            }
            finally
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
            }

            if (sdbResult != SDBLib.SdbRunResult.Success)
            {
                MessageBox.Show(SDBLib.FormatSdbRunResult(sdbResult), MessageDialogTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (connected)
            {
                this.IsConnect = true;
                MessageBox.Show($"Completed connection to {ip}");
                this.Close();
                return;
            }

            if (lastNonEmptyLine.StartsWith("error:"))
            {
                MessageBox.Show(lastNonEmptyLine, MessageDialogTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show($"Got unexpected result while connecting to {ip}.\n{lastNonEmptyLine}",
                    MessageDialogTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = RDMListView.SelectedIndex;
            if (selectedIndex == -1)
            {
                return;
            }

            ItemsData selectedItem = (ItemsData)RDMListView.Items[selectedIndex];
            string ip = selectedItem.IP;
            string lastNonEmptyLine = "";
            int exitCode;
            SDBLib.SdbRunResult sdbResult = SDBLib.RunSdbCommand(null, "disconnect " + ip,
                (bool isStdOut, string line) =>
                {
                    if (isStdOut)
                    {
                        if (line != "")
                        {
                            lastNonEmptyLine = line;
                            return true;
                        }
                    }
                    return false;
                },
                out exitCode);

            if (sdbResult != SDBLib.SdbRunResult.Success)
            {
                MessageBox.Show(SDBLib.FormatSdbRunResult(sdbResult), MessageDialogTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (lastNonEmptyLine.StartsWith("error:"))
            {
                MessageBox.Show(lastNonEmptyLine, MessageDialogTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                this.IsConnect = false;
                MessageBox.Show($"Disconnected from {ip}:{selectedItem.Port}", MessageDialogTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = RDMListView.SelectedIndex;

            object selecteditem = RDMListView.Items[selectedIndex];

            RDMListView.Items.Remove(selecteditem);
            SaveRemoteList();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private string GetRemoteListPath()
        {
            string appData = Environment.GetFolderPath(
                                Environment.SpecialFolder.LocalApplicationData);
            string remoteListPath = string.Format("{0}{1}{2}",
                                        appData, @"\Tizen.NET\3.0", @"\remote.list");

            return remoteListPath;
        }

        private void SaveRemoteList()
        {
            string remoteListPath = GetRemoteListPath();

            if (Directory.Exists(Path.GetDirectoryName(remoteListPath)) == false)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(remoteListPath));
                }
                catch (Exception)
                {

                }
            }

            using (StreamWriter sw = new StreamWriter(remoteListPath))
            {
                foreach (object selecteditem in RDMListView.Items)
                {
                    sw.Write((selecteditem as ItemsData).Name + "/"
                        + (selecteditem as ItemsData).IP + "/"
                        + (selecteditem as ItemsData).Port + "\n");
                }
            }
        }

        private static ItemsData GetDeviceDataFromString(string input)
        {
            const string deviceName = "(.*?)";
            const string token = "(\\/)";
            const string deviceIP = "((?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?))(?![\\d])";
            const string devicePort = "(\\d+)";

            Regex r = new Regex(deviceName + token + deviceIP + token + devicePort,
                                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            Match m = r.Match(input);

            if (m.Success)
            {
                return new ItemsData
                {
                    Name = m.Groups[1].ToString(),
                    IP = m.Groups[3].ToString(),
                    Port = m.Groups[5].ToString()
                };
            }
            else
            {
                return null;
            }
        }

        public class ItemsData
        {
            public string Name { get; set; }
            public string IP { get; set; }
            public string Port { get; set; }
        }
    }
}
