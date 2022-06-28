/*
 * Copyright 2021(c) Samsung Electronics Co., Ltd  All Rights Reserved.
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
using Tizen.VisualStudio.Tools.Data;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Runtime.InteropServices;
using System.IO;
using NetCore.Profiler.Extension.VSPackage;
using System.Linq;

namespace Tizen.VisualStudio.APIChecker
{
    class WebPrivilegeCheckerWindowTaskProvider : TaskProvider
    {
        private static WebPrivilegeCheckerWindowTaskProvider taskProvider;
        private IServiceProvider parent;
        private WebPrivilegeCheckerWindowTaskProvider(IServiceProvider sp)
        : base(sp)
        {
            this.parent = sp;
        }

        public static WebPrivilegeCheckerWindowTaskProvider CreateProvider(IServiceProvider parent)
        {
            if (taskProvider == null)
            {
                taskProvider = new WebPrivilegeCheckerWindowTaskProvider(parent);
                taskProvider.ProviderName = "APIViolations";
            }

            return taskProvider;
        }

        public void ClearWarn()
        {
            taskProvider.Tasks.Clear();
        }

        public void ReportPrivilegeError(string err, int line, int column, string filename)
        {
            var warnTask = new Microsoft.VisualStudio.Shell.TaskListItem();
            warnTask.CanDelete = true;
            warnTask.Category = TaskCategory.BuildCompile;
            warnTask.Document = filename;
            warnTask.Line = line;
            warnTask.Column = column;
            warnTask.Navigate += new EventHandler(NavigateHandler);
            warnTask.Text = err;
            warnTask.Priority = TaskPriority.Normal;
            taskProvider.Tasks.Add(warnTask);
        }

        private void NavigateHandler(object sender, EventArgs arguments)
        {
            Microsoft.VisualStudio.Shell.TaskListItem task = sender as Microsoft.VisualStudio.Shell.TaskListItem;
            if (task == null)
                throw new ArgumentException("sender parm cannot be null");

            if (String.IsNullOrEmpty(task.Document))
                return;

            IVsUIShellOpenDocument openDoc = GetService(typeof(IVsUIShellOpenDocument)) as IVsUIShellOpenDocument;
            if (openDoc == null)
                return;

            IVsWindowFrame frame;
            Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider;
            IVsUIHierarchy hierarchy;
            uint itemId;
            Guid logicalView = VSConstants.LOGVIEWID_Code;
            if (ErrorHandler.Failed(openDoc.OpenDocumentViaProject(
                task.Document, ref logicalView, out serviceProvider, out hierarchy, out itemId, out frame))
                || frame == null)
            {
                return;
            }

            object docData;
            frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out docData);
            VsTextBuffer buffer = docData as VsTextBuffer;
            if (buffer == null)
            {
                IVsTextBufferProvider bufferProvider = docData as IVsTextBufferProvider;
                if (bufferProvider != null)
                {
                    IVsTextLines lines;
                    ErrorHandler.ThrowOnFailure(bufferProvider.GetTextBuffer(out lines));
                    buffer = lines as VsTextBuffer;

                    if (buffer == null)
                    {
                        return;
                    }
                }
            }
            IVsTextManager mgr = GetService(typeof(VsTextManagerClass)) as IVsTextManager;
            if (mgr == null)
                return;
            mgr.NavigateToLineAndColumn(buffer, ref logicalView, task.Line, task.Column, task.Line, task.Column);
        }
    }

    internal sealed class WebPrivilegeChecker
    {
        private static WebPrivilegeCheckerWindowTaskProvider taskProvider;
        private readonly Package package;

        private static IVsOutputWindowPane outpane;

        public static WebPrivilegeChecker Instance
        {
            get;
            private set;
        }

        public static void Initialize(Package package, IVsOutputWindowPane outWinPane)
        {
            Instance = new WebPrivilegeChecker(package);
            outpane = outWinPane;
        }

        private IServiceProvider ServiceProvider
        {
            get { return this.package as IServiceProvider; }
        }

        private void populatePrivilegeWarnings(string warn)
        {
            int indx = warn.IndexOf(".js:");
            if (indx == -1) return;
            string fileName = warn.Substring(0, indx + 3);
            string remain_str = warn.Substring(indx + 4);
            string[] err_info = remain_str.Split(':');
            int line = Int32.Parse(err_info[0]);
            int col = Int32.Parse(err_info[1]);
            taskProvider.ReportPrivilegeError(warn, line-1, col-1, fileName);
        }

        public void HandleMenuItemPrivilegeCheck(object sender, EventArgs e)
        {
            taskProvider = WebPrivilegeCheckerWindowTaskProvider.CreateProvider(this.ServiceProvider);
            taskProvider.ClearWarn();
            OutputWSLaunchMessage("Started Web Privilege Checker...");
           
            VsProjectHelper projHelp = VsProjectHelper.Instance;
            String configXMLPath = projHelp.getConfigXML();

            if (configXMLPath == null)
            {
                OutputWSLaunchMessage("unable to get config file, stopping Privilege Checker...");
                return;
            }

            String workspacePath = projHelp.getSolutionFolderPath();
            if (workspacePath == null)
            {
                OutputWSLaunchMessage("Unable to get workspace path...");
                return;
            }

            var process = new System.Diagnostics.Process();
            String command = "/C " + ToolsPathInfo.ToolsRootPath + "\\tools\\tizen-core\\web-privilege-checker" + " -p " + workspacePath + " -c " + configXMLPath + " -s " + ToolsPathInfo.ToolsRootPath;

            process.StartInfo.FileName = @"C:\Windows\System32\cmd.exe";
            process.StartInfo.Arguments = command;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            string err = process.StandardError.ReadToEnd();
            process.WaitForExit();

            string warn = null;
            StringReader strReader = new StringReader(err);
            while (true)
            {
                warn = strReader.ReadLine();
                if (warn != null)
                {
                    OutputWSLaunchMessage(warn);
                    populatePrivilegeWarnings(warn);
                }
                else
                    break;
            }
            OutputWSLaunchMessage("Web Privilege Completed.");
        }


        private static void OutputWSLaunchMessage(string rawMsg)
        {
            if (string.IsNullOrEmpty(rawMsg))
                return;
            string message = String.Format($"{DateTime.Now} : {rawMsg}\n");
            outpane?.Activate();
            outpane?.OutputStringThreadSafe(message);
        }

        private WebPrivilegeChecker(Package package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
        }
    }
}

