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
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Tizen.VisualStudio.Tools.DebugBridge;

namespace Tizen.VisualStudio.LogViewer
{
    class LogExporter
    {
        private List<Log> logList;

        public LogExporter(System.Windows.Controls.DataGrid dataGrid)
        {
            logList = dataGrid.Items.Cast<Log>().ToList();
        }

        public void OpenSaveFileDialog()
        {
            using (SaveFileDialog savePanel = new SaveFileDialog())
            {
                savePanel.FileName = string.Format("{0} ({1})-log.txt", DeviceManager.SelectedDevice.Serial, DeviceManager.SelectedDevice.Name);
                savePanel.Filter = "*.txt|*.txt";

                if (savePanel.ShowDialog() == DialogResult.OK)
                {
                    WriteToFile(savePanel.FileName);
                }
            }
        }

        private void WriteToFile(string filePath)
        {
            try
            {
                using (StreamWriter file = new StreamWriter(filePath))
                {
                    foreach (Log log in logList)
                    {
                        string line = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", log.Time, log.Level, log.Pid, log.Tid, log.Tag, log.Msg);
                        file.WriteLine(line);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to export log. " + e.Message);
            }
        }
    }
}
