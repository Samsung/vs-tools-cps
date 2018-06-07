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
using System.Runtime.InteropServices;
using System.Xml.Linq;
using System.Dynamic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Tizen.VisualStudio.ResourceManager
{
    class ResourceManagerUtil
    {
        private static Dictionary<string, string> langMap;
        private static Dictionary<string, bool> dpiMap;
        private static dynamic expando = new ExpandoObject();

        private static EnvDTE80.DTE2 GetDTE2()
        {
            return Package.GetGlobalService(typeof(DTE)) as EnvDTE80.DTE2;
        }

        public static Project getCurrentProject()
        {
            IntPtr hierarchyPointer, selectionContainerPointer;
            Object selectedObject = null;
            IVsMultiItemSelect multiItemSelect;
            uint projectItemId;

            IVsMonitorSelection monitorSelection =
                    (IVsMonitorSelection)Package.GetGlobalService(
                    typeof(SVsShellMonitorSelection));

            monitorSelection.GetCurrentSelection(out hierarchyPointer,
                                                 out projectItemId,
                                                 out multiItemSelect,
                                                 out selectionContainerPointer);
            if (hierarchyPointer == IntPtr.Zero)
                return null;
            IVsHierarchy selectedHierarchy = Marshal.GetTypedObjectForIUnknown(
                                                 hierarchyPointer,
                                                 typeof(IVsHierarchy)) as IVsHierarchy;

            if (selectedHierarchy != null)
            {
                selectedHierarchy.GetProperty(projectItemId,
                                              (int)__VSHPROPID.VSHPROPID_ExtObject,
                                              out selectedObject);
            }

            Project selectedProject = selectedObject as Project;
            if (selectedProject != null) return selectedProject;

            ProjectItem projItem = selectedObject as ProjectItem;
            if (projItem != null) return projItem.ContainingProject;

            return null;

        }

        public static ProjectItem getContentFolder(Project currentProj)
        {
            ProjectItem resFolder = getResourceFolder(currentProj);
            ProjectItem contents = addProjectFolder("contents", resFolder);

            return contents;
        }

        private static ProjectItem getResourceFolder(Project currentProj)
        {

            foreach(ProjectItem item in currentProj.ProjectItems)
            {
                if (item.Name.Equals("res")) return item;
            }
            ProjectItem ret = currentProj.ProjectItems.AddFolder("res");
            currentProj.Save();
            return ret;
        }

        public static ProjectItem addProjectFolder(string name, ProjectItem prjItem)
        {   
            foreach(ProjectItem item in prjItem.ProjectItems)
            { 
                if (item.Name.Equals(name)) return item;
            }
            ProjectItem ret = prjItem.ProjectItems.AddFolder(name);
            prjItem.ContainingProject.Save();
            return ret;
        }

        public static void removeProjectItem(string name, ProjectItem prjItem)
        {
            try
            {
                prjItem.ProjectItems.Item(name).Delete();
                prjItem.ContainingProject.Save();
            }
            catch (Exception)
            {

            }
        }

        internal static bool isValidLanguageID(string langId)
        {
            if (langMap == null)
            {
                var dirPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                XDocument document = XDocument.Load(dirPath + @"\ViewAndUI\ResourceManager\Resource\lang_country_lists.xml");
                langMap = document.Descendants("languages").Descendants("lang")
                      .ToDictionary(d => (string)d.Attribute("id"),
                                    d => (string)d.Attribute("name"));
            }
            if (langId.Equals("default_All")) return true;

            return langMap.ContainsKey(langId);
        }

        internal static string getResolution(string dpi)
        {
            switch (dpi)
            {
                case "All": return "";
                case "LDPI": return "from 0 to 240";
                case "MDPI": return "from 241 to 300";
                case "HDPI": return "from 301 to 380";
                case "XHDPI": return "from 381 to 480";
                case "XXHDPI": return "from 481 to 600";
                default: return "";
            }
        }

        internal static bool isValidResolution(string dpi)
        {
            if (dpiMap == null)
            {
                dpiMap = new Dictionary<string, bool>();
                foreach (string item in Enum.GetNames(typeof(ResolutionDPI)))
                {
                    dpiMap.Add(item, true);
                }
            }
            return dpiMap.ContainsKey(dpi);
        }

        public static bool isTizenProject(string projFilePath)
        {
            var manifestFile = projFilePath + "tizen-manifest.xml";
            return File.Exists(manifestFile);
        }

        public static ProjectItem getProjectItem(string name, ProjectItem prjItem)
        {
            ProjectItem retVal;
            try
            {
                retVal = prjItem.ProjectItems.Item(name);
            }
            catch (Exception exe)
            {
                throw exe;
            }
            return retVal;
        }

        public static void AddProperty(ExpandoObject expando, string propertyName, object propertyValue)
        {
            // ExpandoObject supports IDictionary so we can extend it like this
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                expandoDict[propertyName] = propertyValue;
            else
                expandoDict.Add(propertyName, propertyValue);
        }

        public static object GetProperty(ExpandoObject expando, string propertyName)
        {
            // ExpandoObject supports IDictionary so we can extend it like this
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                return expandoDict[propertyName];
            else
                return null;
        }

        public static ExpandoObject getExpandoObj()
        {
            return expando;
        }

        public static void copyDir(string srcPath, ProjectItem destProjItem)
        {
            try
            {
                destProjItem.ProjectItems.AddFromDirectory(srcPath);
                destProjItem.ContainingProject.Save();
            }
            catch (Exception)
            {
                //throw exe;
            }

        }

        public static void copyFile(string srcPath, ProjectItem destProjItem)
        {
            try
            {
                destProjItem.ProjectItems.AddFromFileCopy(srcPath);
                destProjItem.ContainingProject.Save();
            }
            catch (Exception)
            {
                //throw exe;
            }
        }

        public static void openFile(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem)
            {
                if (!((TreeViewItem)sender).IsSelected)
                {
                    return;
                }
            }

            var treeViewItem = sender as TreeViewItem;
            System.Diagnostics.Process.Start(treeViewItem.Tag.ToString());
        }

        public static void deleteProjectItem(Project project, string relativePath) {
            string[] pathArray = relativePath.Split(new char[] { '\\' });
            foreach (ProjectItem item in project.ProjectItems)
            {
                if (item.Name.Equals(pathArray[pathArray.Count() - 1]))
                {
                    project.ProjectItems.Item(item.Name).Delete();
                    project.Save();
                    return;
                }
                if (item.Name.Equals(pathArray[0]))
                {
                    delete(item, pathArray, 1);
                    return;
                }
            }
        }

        private static void delete(ProjectItem parent, string[] pathArray, int index) {
            
            foreach (ProjectItem item in parent.ProjectItems)
            {
                if (item.Name.Equals(pathArray[pathArray.Count() - 1]))
                {
                    removeProjectItem(item.Name, parent);
                    return;
                }
                if (item.Name.Equals(pathArray[index]))
                {
                    delete(item, pathArray, index + 1);
                    return;
                }
            }
        }

        public class ResourceItem
        {
            public string Directory { get; set; }
            public string Language { get; set; }
            public string Resolution { get; set; }
        }
    }
}
