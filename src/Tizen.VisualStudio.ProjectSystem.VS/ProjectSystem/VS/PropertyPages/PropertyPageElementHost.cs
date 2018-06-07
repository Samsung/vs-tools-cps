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
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Windows.Forms.Integration;
using Microsoft.VisualStudio;

namespace Tizen.VisualStudio.ProjectSystem.VS.PropertyPages
{
    internal class PropertyPageElementHost : ElementHost
    {
        private const int _wm_KEYFIRST = 0x0100;
        private const int _wm_KEYLAST = 0x0108;

        public override bool PreProcessMessage(ref Message msg)
        {
            if (msg.Msg >= _wm_KEYFIRST && msg.Msg <= _wm_KEYLAST)
            {
                IVsFilterKeys2 filterKeys = (IVsFilterKeys2)ServiceProvider.GlobalProvider.GetService(typeof(SVsFilterKeys));
                Microsoft.VisualStudio.OLE.Interop.MSG oleMSG = new Microsoft.VisualStudio.OLE.Interop.MSG() { hwnd = msg.HWnd, lParam = msg.LParam, wParam = msg.WParam, message = (uint)msg.Msg };

                //Ask the shell to do the command mapping for us and without firing off the command. We need to check if this command is one of the
                //supported commands first before actually firing the command.
                filterKeys.TranslateAcceleratorEx(new Microsoft.VisualStudio.OLE.Interop.MSG[] { oleMSG },
                                                  (uint)(__VSTRANSACCELEXFLAGS.VSTAEXF_NoFireCommand | __VSTRANSACCELEXFLAGS.VSTAEXF_UseGlobalKBScope | __VSTRANSACCELEXFLAGS.VSTAEXF_AllowModalState),
                                                  0 /*scope count*/,
                                                  new Guid[0] /*scopes*/,
                                                  out Guid cmdGuid,
                                                  out uint cmdId,
                                                  out int fTranslated,
                                                  out int fStartsMultiKeyChord);

                if (ShouldRouteCommandBackToVS(cmdGuid, cmdId, fTranslated == 1, fStartsMultiKeyChord == 1))
                {
                    return false;
                }
            }

            return base.PreProcessMessage(ref msg);
        }

        private bool ShouldRouteCommandBackToVS(Guid cmdGuid, uint cmdId, bool translated, bool startsMultiKeyChord)
        {
            //Any command that wasn't translated by TranslateAccelleratorEx or has no VS handler in global scope should be routed to WPF
            if (!translated || cmdGuid == Guid.Empty)
            {
                return false;
            }

            //Allow VS to take over for anything that would be a common shell command
            //  (CTRL+Tab, CTRL+Shift+TAB, Shift+ALT+Enter, etc.)
            if (cmdGuid == VSConstants.GUID_VSStandardCommandSet97)
            {
                //If there's a GUID_VSStandardCommandSet97 command that should be handled by WPF instead, check for them and return false

                //Otherwise indicate that the command should be handled by VS
                return true;
            }

            //If there are additional commands that VS should be handling instead of WPF, check for them and return true here

            //Otherwise indicate that the command should be handled by WPF
            return false;
        }
    }
}
