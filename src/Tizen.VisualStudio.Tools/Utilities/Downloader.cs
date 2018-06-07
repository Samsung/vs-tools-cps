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

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tizen.VisualStudio.Utilities
{
    class Downloader
    {
        public bool IsCanceledByUser
        {
            get;
            private set;
        }

        public delegate void DownloadHandlingDelegate(string downloadedFilepath);
        private DownloadHandlingDelegate OnDownloadCompleted;
        private DownloadHandlingDelegate OnDownloadCanceled;

        private WaitDialogDescription dialogDesc;
        private WebClient webClient;
        private IVsThreadedWaitDialog2 waitDialog;

        private Uri srcUri;
        private string dest;

        public Downloader(string src, string destFolder, WaitDialogDescription dialogDesc, DownloadHandlingDelegate OnDownloadCompleted, DownloadHandlingDelegate OnDownloadCanceled)
        {
            srcUri = new Uri(src);
            dest = GetLocalPathByUri(destFolder, srcUri);//Path.Combine(destFolder, Path.GetFileName(srcUri.AbsolutePath));
            this.dialogDesc = dialogDesc;
            this.OnDownloadCompleted = OnDownloadCompleted;
            this.OnDownloadCanceled = OnDownloadCanceled;

            webClient = new WebClient();

            IVsThreadedWaitDialogFactory dlgFactory = Package.GetGlobalService(typeof(SVsThreadedWaitDialogFactory)) as IVsThreadedWaitDialogFactory;

            if (dlgFactory != null)
            {
                dlgFactory.CreateInstance(out waitDialog);
            }
        }

        public bool Start()
        {
            string msg = string.Empty;

            if (DeleteFileSync() && PopDownloadDialog())
            {
                try
                {
                    StartDownload();
                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.Show("Failed to download : " + e.Message, "Download Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            return false;
        }

        private bool DeleteFileSync()
        {
            try
            {
                if (!File.Exists(dest))
                {
                    return true;
                }

                FileInfo file = new FileInfo(dest);
                file.IsReadOnly = false;
                File.Delete(dest);

                while (File.Exists(dest))
                {
                    System.Threading.Thread.Sleep(100);
                }

                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to remove old file : " + e.Message, "Download Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private bool PopDownloadDialog()
        {
            return (waitDialog != null) &&
                waitDialog.StartWaitDialogWithPercentageProgress(
                    dialogDesc?.WaitCaption,
                    dialogDesc?.WaitMessage,
                    dialogDesc?.ProgressText,
                    null,
                    dialogDesc?.StatusBarText,
                    true, 0, 100, 0) == VSConstants.S_OK;
        }

        private bool StartDownload()
        {
            if (webClient != null)
            {
                webClient.DownloadFileAsync(srcUri, dest);
                webClient.DownloadProgressChanged += OnUpdateProgress;
                webClient.DownloadFileCompleted += RunPostDownloadWork;

                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsValidUri(out string msg)
        {
            WebRequest webRequest = WebRequest.Create(srcUri);
            WebResponse webResponse;

            try
            {
                webResponse = webRequest.GetResponse();
            }
            catch (Exception e)
            {
                msg = e.Message;
                return false;
            }

            msg = string.Empty;
            return true;
        }

        private void OnUpdateProgress(object sender, DownloadProgressChangedEventArgs ea)
        {
            bool isCanceled = true;

            waitDialog?.UpdateProgress(
                dialogDesc?.WaitMessage,
                dialogDesc?.ProgressText,
                dialogDesc?.StatusBarText,
                ea.ProgressPercentage,
                100,
                false,
                out isCanceled);

            if (isCanceled && !IsCanceledByUser)
            {
                IsCanceledByUser = isCanceled;
                webClient?.CancelAsync();
            }
        }

        private void RunPostDownloadWork(object sender, AsyncCompletedEventArgs e)
        {
            webClient.Dispose();
            waitDialog?.EndWaitDialog();

            if (!IsCanceledByUser)
            {
                OnDownloadCompleted?.Invoke(dest);
            }
            else
            {
                DeleteFileSync();
                OnDownloadCanceled?.Invoke(dest);
            }
        }

        public static string GetLocalPathByUri(string destFolder, Uri srcUri)
        {
            return Path.Combine(destFolder, Path.GetFileName(srcUri.AbsolutePath));
        }

    }
}
