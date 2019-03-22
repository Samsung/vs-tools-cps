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
using Microsoft.VisualStudio.Shell.Interop;

namespace Tizen.VisualStudio.Utilities
{
    public enum MessageDialogType
    {
        Debug,
        Info,
        Warning,
        Error,
        Question
    }

    public static class ShellHelper
    {
        public static int ShowMessage(IServiceProvider serviceProvider, MessageDialogType messageType,
            string title, string message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            bool isDefaultTitle = false;
            if (String.IsNullOrEmpty(title))
            {
                title = "Tizen Plugin";
                isDefaultTitle = true;
            }
            OLEMSGICON icon;
            OLEMSGBUTTON button;
            switch (messageType)
            {
                case MessageDialogType.Debug:
                    icon = OLEMSGICON.OLEMSGICON_NOICON;
                    button = OLEMSGBUTTON.OLEMSGBUTTON_OK;
                    break;
                case MessageDialogType.Info:
                    icon = OLEMSGICON.OLEMSGICON_INFO;
                    button = OLEMSGBUTTON.OLEMSGBUTTON_OK;
                    break;
                case MessageDialogType.Warning:
                    if (isDefaultTitle)
                    {
                        title += " Warning";
                    }
                    icon = OLEMSGICON.OLEMSGICON_WARNING;
                    button = OLEMSGBUTTON.OLEMSGBUTTON_OK;
                    break;
                case MessageDialogType.Error:
                    if (isDefaultTitle)
                    {
                        title += " Error";
                    }
                    icon = OLEMSGICON.OLEMSGICON_CRITICAL;
                    button = OLEMSGBUTTON.OLEMSGBUTTON_OK;
                    break;
                case MessageDialogType.Question:
                    icon = OLEMSGICON.OLEMSGICON_QUERY;
                    button = OLEMSGBUTTON.OLEMSGBUTTON_YESNO;
                    break;
                default:
                    icon = OLEMSGICON.OLEMSGICON_NOICON;
                    button = OLEMSGBUTTON.OLEMSGBUTTON_OK;
                    break;
            }
            return VsShellUtilities.ShowMessageBox(serviceProvider, message, title, icon, button,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
