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

namespace Tizen.VisualStudio.LogViewer
{
    class LogFilterController
    {
        static char delimeter = ' ';

        static public bool CheckLevel(char level, LogViewerControl lvControl)
        {
            bool result =
                (lvControl.verboseCheck.IsChecked == true && level == 'V') ||
                (lvControl.debugCheck.IsChecked == true && level == 'D') ||
                (lvControl.infoCheck.IsChecked == true && level == 'I') ||
                (lvControl.warningCheck.IsChecked == true && level == 'W') ||
                (lvControl.errorCheck.IsChecked == true && level == 'E') ||
                (lvControl.fatalCheck.IsChecked == true && level == 'F');

            return result;
        }

        static public bool CheckFilter(Log item, LogViewerControl lvControl)
        {
            bool result =
                (string.IsNullOrWhiteSpace(lvControl.pidTextBox.Text) ? true : Array.Exists(lvControl.pidTextBox.Text.Split(delimeter), element => item.Pid.Equals(element))) &&
                (string.IsNullOrWhiteSpace(lvControl.tagTextBox.Text) ? true : Array.Exists(lvControl.tagTextBox.Text.Split(delimeter), element => item.Tag.Equals(element))) &&
                (string.IsNullOrWhiteSpace(lvControl.msgTextBox.Text) ? true : item.Msg.Contains(lvControl.msgTextBox.Text));

            return result;
        }
    }
}
