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

using System.Windows;

namespace Tizen.VisualStudio.ConnectToolbar
{
    /// <summary>
    /// Window to get device info
    /// </summary>
    public partial class DeviceInfo : Window
    {
        public string DeviceName { get; set; }
        public string IP { get; set; }
        public string Port { get; set; }

        public DeviceInfo()
        {
            InitializeComponent();
        }

        public void SetDeviceData(string DeviceName, string IP, string Port)
        {
            this.DeviceName = DeviceName;
            this.IP = IP;
            this.Port = Port;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (NameTextBox.Text.Split(new char[1] { ' ' })[0].Equals(""))
            {
                MessageBox.Show("Please set then name of the device.", "Error");
                return;
            }

            if (IPTextBox.Text.Split(new char[1] { ' ' })[0].Equals(""))
            {
                MessageBox.Show("Please set then IP address of the device.", "Error");
                return;
            }

            if (PortTextBox.Text.Split(new char[1] { ' ' })[0].Equals(""))
            {
                MessageBox.Show("Please set then port of the device", "Error");
                return;
            }

            this.DeviceName = this.NameTextBox.Text;
            this.IP = this.IPTextBox.Text;
            this.Port = this.PortTextBox.Text;

            this.DialogResult = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(this.DeviceName))
            {
                this.NameTextBox.Text = this.DeviceName;
            }

            if (!string.IsNullOrEmpty(this.IP))
            {
                this.IPTextBox.Text = this.IP;
            }

            if (!string.IsNullOrEmpty(this.Port))
            {
                this.PortTextBox.Text = this.Port;
            }
        }
    }
}
