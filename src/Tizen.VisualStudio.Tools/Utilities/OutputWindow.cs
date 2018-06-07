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
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using EnvDTE80;

namespace Tizen.VisualStudio.Tools.Utilities
{
    public class OutputWindow
    {
        private OutputWindowPane OutputPane { get; set; }

        public OutputWindow()
        {
            OutputPane = null;
        }

        public void CreatePane(string title)
        {
            this.OutputPane = GetPane(title);
        }

        private OutputWindowPane GetPane(string title)
        {
            DTE2 dte = (DTE2)Package.GetGlobalService(typeof(DTE));
            OutputWindowPanes panes = dte.ToolWindows.OutputWindow.OutputWindowPanes;

            try
            {
                return panes.Item(title);
            }
            catch (ArgumentException)
            {
                return panes.Add(title);
            }
        }

        public void PrintString(string s)
        {
            if (OutputPane == null)
            {
                return;
            }

            OutputPane.OutputString(s);
        }

        public void ActivatePane(string title)
        {
            OutputWindowPane outputwindowpane = GetPane(title);
            outputwindowpane.Activate();
        }

        public void ClearePane(string title)
        {
            OutputWindowPane outputwindowpane = GetPane(title);
            outputwindowpane.Clear();
        }
    }
}
