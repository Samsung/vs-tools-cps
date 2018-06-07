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

using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Tizen.VisualStudio.LogViewer
{
    static class Resource
    {

        public static string selectedDeviceNameInConnectionEx = null;// { get; set; }
        public static string outputPath = null;
        public static string projectName = null;

        public static Color bgColor;
        public static Color textColor;
        public static Color lightBorderColor;
        public static Color borderColor;
        public static Brush oddRowColorBrush;
        public static Brush evenRowColorBrush = new SolidColorBrush(Colors.Transparent);

        public const string levelLike = "Level LIKE '%{0}%'";
        public const string txtLike = "{0} LIKE '%{1}%'";

        public const int timeColWidth = 120;
        public const int levelColWidth = 70;
        public const int pidColWidth = 50;
        public const int tidColWidth = 50;
        public const int tagColWidth = 100;

        public const int timeColIdx = 0;
        public const int levelColIdx = 1;
        public const int pidColIdx = 2;
        public const int tidColIdx = 3;
        public const int tagColIdx = 4;
        public const int msgColIdx = 5;

        private static Dictionary<string, SolidColorBrush> levelColorDic = new Dictionary<string, SolidColorBrush>();

        public static string GetLevelName(char key)
        {
            switch (key)
            {
                case 'V': return "Verbose";
                case 'D': return "Debug";
                case 'I': return "Info";
                case 'E': return "Error";
                case 'F': return "Fatal";
                case 'W': return "Warning";
                default: return "???";
            }
        }

        public static void InitEnvColor()
        {
            bgColor = GetEnvColor(EnvironmentColors.GridHeadingBackgroundColorKey);
            textColor = GetEnvColor(EnvironmentColors.PanelTextColorKey);
            lightBorderColor = GetEnvColor(EnvironmentColors.ComboBoxBorderColorKey);
            borderColor = GetEnvColor(EnvironmentColors.ComboBoxBorderColorKey);//Color.FromArgb(255, 127, 127, 127);

            oddRowColorBrush = new SolidColorBrush(Color.FromArgb(32, 127, 127, 127));
        }

        public static SolidColorBrush GetLevelColor(char key)
        {
            switch (key)
            {
                case 'E':
                    return new SolidColorBrush(Colors.Red);
                case 'F':
                    return new SolidColorBrush(Colors.Purple);
                case 'W':
                    return new SolidColorBrush(Colors.DarkGoldenrod);
                default:
                    return new SolidColorBrush(textColor);
            }
        }

        public static string GetLevelType(ToggleButton cb)
        {
            return GetLevelName(cb.Content.ToString()[0]);
        }

        private static Color GetEnvColor(ThemeResourceKey key)
        {
            System.Drawing.Color dColor = VSColorTheme.GetThemedColor(key);
            Color mColor = Color.FromArgb(dColor.A, dColor.R, dColor.G, dColor.B);
            return mColor;
        }
    }
}
