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
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tizen.VisualStudio.ResourceManager
{
    public class ResourceManagerLauncher
    {
        static ResourceManagerLauncher instance = new ResourceManagerLauncher();
        private Dictionary<string, int> projectIdMap = new Dictionary<string, int>();
        static int id = 0;
        private ResourceManagerLauncher() { }
        public static ResourceManagerLauncher getInstance()
        {
            return instance;
        }

        public void launch(Package package)
        {
            int winID;
            Project proj = ResourceManagerUtil.getCurrentProject();
            if (proj == null) return;
            string projPath = proj.FullName;
            string projFolder = projPath.Substring(0, projPath.LastIndexOf("\\") + 1);

            //Check if this is a Tizen project.
            if (!ResourceManagerUtil.isTizenProject(projFolder)) return;

            if (projectIdMap.ContainsKey(projPath))
            {
                projectIdMap.TryGetValue(projPath, out winID);
            }else
            {
                winID = id++;
                projectIdMap[projPath] = winID;
            }
            // Create a new instance of Resource Manager when tool is invoked on project.
            ToolWindowPane window = package.FindToolWindow(typeof(ResourceManager), winID, true);
            if ((null == window))
            {
                return;
                //throw new NotSupportedException("Failed to open Resource Manager. Cannot create tool window");
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        public void CloseAllResourceManagerWindow(Package package)
        {
            foreach (KeyValuePair<string, int> entry in projectIdMap)
            {
                ToolWindowPane window =
                    package.FindToolWindow(typeof(ResourceManager), entry.Value, false);
                if ((null == window))
                    continue;
                IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
                windowFrame.CloseFrame((int)__FRAMECLOSE.FRAMECLOSE_SaveIfDirty);
            }
            projectIdMap.Clear();
        }

        public void CloseProjectResourceManagerWindow(Package package, Project project)
        {
            string projPath = project.FullName;
            if (!projectIdMap.ContainsKey(projPath)) return;
            ToolWindowPane window =
                    package.FindToolWindow(typeof(ResourceManager), projectIdMap[projPath], false);
            projectIdMap.Remove(projPath);
            if ((null == window))
                return;
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            windowFrame.CloseFrame((int)__FRAMECLOSE.FRAMECLOSE_SaveIfDirty);

        }
    }
}
