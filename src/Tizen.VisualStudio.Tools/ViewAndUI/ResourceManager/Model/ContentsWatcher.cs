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
using System;
using System.IO;

namespace Tizen.VisualStudio.ResourceManager
{
    class ContentsWatcher
    {
        public string projPath = null;
        private FileSystemWatcher watcher = null;
        private ResourceManagerControl resourceManagerControl;
        private Project currentProject;

        public ContentsWatcher(ResourceManagerControl resourceManagerControl, Project currentProject)
        {
            this.resourceManagerControl = resourceManagerControl;
            this.currentProject = currentProject;
        }

        public string Path { get => projPath; set => projPath = value; }

        public void watch(string projectPath)
        {
            if(watcher != null)
            {
                watcher.Dispose();
            }
            projPath = projectPath;
            watcher = new FileSystemWatcher();
            watcher.Path = projPath;
            watcher.IncludeSubdirectories = true;
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);
            watcher.EnableRaisingEvents = true;

        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            if (!e.Name.StartsWith("res")) return;
            if (e.ChangeType == WatcherChangeTypes.Deleted)
            {
                if (e.Name.Equals("res") || e.Name.Equals("res\\contents"))
                {
                    resourceManagerControl.ClearAllViewsAndFile();
                    return;
                }
                if (currentProject != null && !e.Name.StartsWith("res\\contents"))
                {
                    ResourceManagerUtil.deleteProjectItem(currentProject, e.Name);
                }
            }

            //If a new folder is added inside content then update the res.xml and configuration Tab.
            if ((e.ChangeType != WatcherChangeTypes.Created) || isFolderCreated(e))
            {
                resourceManagerControl.UpdateConfigurationTab(source, e.ChangeType,e.Name);
            }
            resourceManagerControl.UpdateViewTab(source, e.ChangeType, e.Name, e.FullPath);
        }

        private static bool isFolderCreated(FileSystemEventArgs e)
        {
            string filePath = e.Name.Replace("\\", "/");
            string[] files = filePath.Split('/');
            FileAttributes attr = File.GetAttributes(e.FullPath);
            return (files.Length == 3 && ((attr & FileAttributes.Directory) == FileAttributes.Directory));
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            if (e.OldName.StartsWith("res") && (e.OldName.Contains("-"))) {
                //Remove old value of renamed folder from configuration
                resourceManagerControl.UpdateConfigurationTab(source, WatcherChangeTypes.Deleted, e.OldName);  
            }
            if (!e.Name.StartsWith("res\\contents")) return;
            resourceManagerControl.UpdateViewTab(source, WatcherChangeTypes.Deleted, e.OldName, e.FullPath);
            resourceManagerControl.UpdateViewTab(source, WatcherChangeTypes.Created, e.Name, e.FullPath);
            if (e.Name.StartsWith("res") && e.Name.Contains("-")) {
                FileAttributes attr = File.GetAttributes(e.FullPath);
                if (!((attr & FileAttributes.Directory) == FileAttributes.Directory))
                    return;
                resourceManagerControl.UpdateConfigurationTab(source, WatcherChangeTypes.Created, e.Name);
            }
        }
    }
}
