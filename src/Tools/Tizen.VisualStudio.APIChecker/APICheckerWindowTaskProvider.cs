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

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Tizen.VisualStudio.APIChecker.Model;

namespace Tizen.VisualStudio.APIChecker
{
	[Guid("72de1eAD-a00c-4f57-bff7-57edb162d0be")]
	class APICheckerWindowTaskProvider : TaskProvider
	{
		private static APICheckerWindowTaskProvider taskProvider;
		private IServiceProvider parent;
		private APICheckerWindowTaskProvider(IServiceProvider sp)
		: base(sp)
		{
			this.parent = sp;
		}

        public static APICheckerWindowTaskProvider CreateProvider(IServiceProvider parent)
        {
            if (taskProvider == null)
            {
                taskProvider = new APICheckerWindowTaskProvider(parent);
                taskProvider.ProviderName = "APIViolations";
            }

            return taskProvider;
        }

        public static APICheckerWindowTaskProvider GetTaskProvider()
        {
            return taskProvider;
        }

        public void ClearError()
		{
			taskProvider.Tasks.Clear();
        }

        private void QuickFixHandler(object sender, EventArgs e)
        {
            Microsoft.VisualStudio.Shell.Task task = sender as Microsoft.VisualStudio.Shell.Task;
            if (task == null)
            {
                throw new ArgumentException("sender parm cannot be null");
            }

            if (String.IsNullOrEmpty(task.Document))
            {
                return;
            }
        }

        public void ReportUnusedPrivileges(string p, int line, int column, string filename)
		{
            var warnTask = new Microsoft.VisualStudio.Shell.Task();
            warnTask.CanDelete = true;
            warnTask.Category = TaskCategory.BuildCompile;
            warnTask.Document = filename;
            warnTask.Line = line;
            warnTask.Column = column;
            warnTask.Navigate += new EventHandler(NavigateHandler);
            warnTask.Text = p;
            warnTask.Priority = TaskPriority.Normal;
            taskProvider.Tasks.Add(warnTask);
        }

		private void NavigateHandler(object sender, EventArgs arguments)
		{
			Microsoft.VisualStudio.Shell.Task task = sender as Microsoft.VisualStudio.Shell.Task;

			if (task == null)
			{
				throw new ArgumentException("sender parm cannot be null");
			}

			if (String.IsNullOrEmpty(task.Document))
			{
				return;
			}

			IVsUIShellOpenDocument openDoc = GetService(typeof(IVsUIShellOpenDocument)) as IVsUIShellOpenDocument;

			if (openDoc == null)
			{
				return;
			}

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
			{
				return;
			}

			mgr.NavigateToLineAndColumn(buffer, ref logicalView, task.Line, task.Column, task.Line, task.Column);
        }

        public void ReportMissingPrivileges(List<string> RequiredPrivileges, string apiname, int line, int column, string filename, string manifestFile)
        {
            string message = string.Join(",", RequiredPrivileges.ToArray());
            string errorMsg = string.Format("The API {0} needs these additions privileges {1}", apiname, message);

            // Report missing privilege violations
            var errTask = new NeedsPrivilegeTask(RequiredPrivileges, manifestFile);
            errTask.CanDelete = true;
            errTask.Category = TaskCategory.BuildCompile;
            errTask.Document = filename;
            errTask.Line = line;
            errTask.Column = column;
            errTask.Navigate += new EventHandler(NavigateHandler);
            errTask.Text = errorMsg;
            errTask.Priority = TaskPriority.High;
            errTask.ApiName = apiname;
            errTask.FileName = filename;
            taskProvider.Tasks.Add(errTask);
        }

    }
}
