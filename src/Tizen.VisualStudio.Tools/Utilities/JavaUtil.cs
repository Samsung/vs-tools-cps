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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Tizen.VisualStudio.Utilities
{
    class JavaProcessUtil
    {
        public delegate void WaitJavaWorkDelegate();

        public static Process GetLastProcessByTitleAwait(Process parent, string title)
        {
            Process installerWindow = null;

            while (installerWindow == null)
            {
                installerWindow = GetLastProcessByTitle(parent, title);
                Thread.Sleep(100);
            }

            return installerWindow;
        }

        public static Process[] GetProcessesByTitle(string title)
        {
            return Array.FindAll(Process.GetProcesses(), proc => proc.MainWindowTitle.Equals(title));
        }

        public static Process GetLastProcessByTitle(Process parent, string title)
        {
            Process[] procCandidates = Array.FindAll(GetProcessesByTitle(title), proc => IsYoungerProcess(parent, proc));

            if (procCandidates == null || procCandidates.Length == 0)
            {
                return null;
            }

            return procCandidates[0];
        }

        public static void AttachExitEvent(Process proc, WaitJavaWorkDelegate OnExit)
        {
            new Thread(() =>
            {
                proc.WaitForExit();
                OnExit?.Invoke();
            }).Start();
        }

        private static bool IsYoungerProcess(Process parent, Process child)
        {
            if (DateTime.Compare(parent.StartTime, child.StartTime) <= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
