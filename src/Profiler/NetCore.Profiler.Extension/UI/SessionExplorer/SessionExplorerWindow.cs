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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace NetCore.Profiler.Extension.UI.SessionExplorer
{
    [Guid("68c176b7-d965-4ddb-941f-3781fa70876f")]
    public class SessionExplorerWindow : ToolWindowPane, IVsWindowPane
    {
        public SessionExplorerWindow() : base(null)
        {
            Caption = "Session Explorer";
            // ReSharper disable once VirtualMemberCallInConstructor
            Content = new SessionExplorerWindowContent();
        }

        public int SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp)
        {
            if (Content is SessionExplorerWindowContent sessionExplorerWindowContent)
            {
                using (var provider = new ServiceProvider(psp))
                {
                    sessionExplorerWindowContent.TrackSelection = provider.GetService(typeof(STrackSelection)) as ITrackSelection;
                }
            }

            return VSConstants.S_OK;
        }


        public void Show()
        {
            (Frame as IVsWindowFrame)?.Show();
        }

    }
}
