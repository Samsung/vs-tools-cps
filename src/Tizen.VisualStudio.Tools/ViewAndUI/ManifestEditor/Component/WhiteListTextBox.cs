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
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace Tizen.VisualStudio.ManifestEditor
{
    public abstract class WhiteListTextBox : TextBox
    {
        protected string whiteList = string.Empty;
        private string prevText;
        private int prevSelection;

        public WhiteListTextBox()
        {
            KeyDown += SavePrevText;
            MouseDown += SavePrevText;//For "right click + paste"

            TextChanged += BlockAbnormalInput;
        }

        private void SavePrevText(object sender, EventArgs e)
        {
            prevText = Text;
            prevSelection = SelectionStart;
        }

        private void BlockAbnormalInput(object sender, EventArgs e)
        {
            bool isValidInput = Regex.IsMatch(Text, whiteList);

            if (!isValidInput)
            {
                Text = prevText;
                SelectionStart = prevSelection;
            }
        }
    }
}