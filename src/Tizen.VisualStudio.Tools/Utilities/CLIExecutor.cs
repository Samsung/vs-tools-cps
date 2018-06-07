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
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using System.Windows.Threading;

namespace Tizen.VisualStudio.Utilities
{
    class CLIExecutor
    {
        public delegate void ProcessDataReceiverDelegate(object sender, DataReceivedEventArgs e);
        public delegate void ProcessTerminatorDelegate(object sender, EventArgs e);

        private ProcessDataReceiverDelegate OnProcessUpdated;
        private ProcessTerminatorDelegate OnProcessCanceled;
        private ProcessTerminatorDelegate OnProcessExited;

        private IVsThreadedWaitDialog2 waitDialog;
        private WaitDialogDescription dialogDesc;
        private Dispatcher uiThreadDispatcher;

        private Process executable;

        private bool isCanceled = false;

        public CLIExecutor(Process executable, WaitDialogDescription dialogDesc, ProcessDataReceiverDelegate OnProcessUpdated, ProcessTerminatorDelegate OnProcessCanceled, ProcessTerminatorDelegate OnProcessExited)
        {
            this.executable = executable;
            this.dialogDesc = dialogDesc;
            this.OnProcessExited = OnProcessExited;
            this.OnProcessUpdated = OnProcessUpdated;
            this.OnProcessCanceled = OnProcessCanceled;

            IVsThreadedWaitDialogFactory dlgFactory = Package.GetGlobalService(typeof(SVsThreadedWaitDialogFactory)) as IVsThreadedWaitDialogFactory;

            if (dlgFactory != null)
            {
                dlgFactory.CreateInstance(out waitDialog);
            }
        }

        public bool Execute()
        {
            try
            {
                if (!isCanceled && PopWaitDialog())
                {
                    executable.StartInfo.RedirectStandardOutput = true;
                    executable.StartInfo.UseShellExecute = false;
                    executable.StartInfo.CreateNoWindow = true;

                    executable.EnableRaisingEvents = true;
                    executable.Exited += RunPostProcessWork;
                    executable.OutputDataReceived += OnUpdateProgress;

                    executable.Start();
                    executable.BeginOutputReadLine();

                    return true;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to execute `" + executable.StartInfo.FileName + "` : " + e.Message, "Execution Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return false;
        }

        public void Kill()
        {
            isCanceled = true;
            executable.CancelOutputRead();
            executable.Kill();
        }

        private bool PopWaitDialog()
        {
            uiThreadDispatcher = Dispatcher.CurrentDispatcher;

            return (waitDialog != null && executable != null) &&
                waitDialog.StartWaitDialog(
                    dialogDesc?.WaitCaption,
                    dialogDesc?.WaitMessage,
                    dialogDesc?.ProgressText,
                    null,
                    dialogDesc?.StatusBarText,
                    0, true, false) == VSConstants.S_OK;
        }

        private void OnUpdateProgress(object sender, DataReceivedEventArgs e)
        {
            bool isButtonClicked = false;

            waitDialog?.UpdateProgress(string.Empty,
                    e.Data,
                    e.Data,
                    0,
                    100,
                    false,
                    out isButtonClicked);

            isCanceled |= isButtonClicked;

            if (isCanceled)
            {
                Kill();
            }
            else
            {
                OnProcessUpdated?.Invoke(sender, e);
            }
        }

        private void RunPostProcessWork(object sender, EventArgs e)
        {
            CloseDialog();

            if (isCanceled)
            {
                OnProcessCanceled?.Invoke(sender, e);
            }
            else
            {
                OnProcessExited?.Invoke(sender, e);
            }
        }

        private void CloseDialog()
        {
            try
            {
                uiThreadDispatcher.Invoke(() =>
                {
                    waitDialog?.EndWaitDialog();
                });
            }
            catch
            {

            }
        }
    }
}
