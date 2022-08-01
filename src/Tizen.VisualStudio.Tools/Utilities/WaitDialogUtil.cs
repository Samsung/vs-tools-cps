﻿/*
 * Copyright 2021 (c) Samsung Electronics Co., Ltd  All Rights Reserved.
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

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Tizen.VisualStudio.Utilities
{
    public class WaitDialogUtil
    {
        IVsThreadedWaitDialogFactory dlgFactory = Package.GetGlobalService(typeof(SVsThreadedWaitDialogFactory)) as IVsThreadedWaitDialogFactory;
        IVsThreadedWaitDialog2 waitDialog = null;
        public void ShowPopup(string msg1, string msg2, string msg3, string msg4)
        {
            if (dlgFactory != null)
            {
                dlgFactory.CreateInstance(out waitDialog);
                waitDialog?.StartWaitDialog(msg1, msg2, msg3, null, msg4, 0, false, true);
            }
        }

        public void ClosePopup()
        {
            waitDialog.EndWaitDialog(out int userCancel);
        }
    }
}
