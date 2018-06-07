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

using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NetCore.Profiler.Extension.Session;

namespace NetCore.Profiler.Extension.UI.SessionWindow
{
    [Guid("547af14d-c17c-4063-a386-76064dda3781")]
    public class SessionWindow : ToolWindowPane
    {
        private IActiveSession _activeSession;

        public SessionWindow() : base(null)
        {
            Content = new SessionWindowContent()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
        }

        private SessionWindowContent _content => Content as SessionWindowContent;

        protected override void OnClose()
        {
            Close();
        }

        public void SetActiveSession(IActiveSession session)
        {
            _activeSession = session;
            Caption = session.Label;
            _content.SetActiveSession(session);
        }

        public void Show()
        {
            (Frame as IVsWindowFrame)?.Show();
        }

        private void Close()
        {
            (Frame as IVsWindowFrame)?.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
        }

    }
}
