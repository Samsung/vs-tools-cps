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
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Tizen.VisualStudio.ResourceManager
{
    internal class ResourceManagerContextMenu
    {

        private List<string> subMenu = new List<string>();
        private string parentSelected = "";
        private string subMenuOriginNode;
        private Project currProj;
        private ContextMenu ctxMenu;

        /// <summary>
        /// Context Menu Constructor
        /// </summary>
        public ResourceManagerContextMenu(Project project)
        {
            currProj = project;
        }
        /// <summary>
        /// Context menu creator.
        /// </summary>
        public ContextMenu createContextMenu()
        {
            ctxMenu = new ContextMenu();
            MenuItem deleteMenu = new MenuItem();
            deleteMenu.Header = "Delete";
            deleteMenu.Click += Delete_Ctx_Click;
            ctxMenu.Items.Add(deleteMenu);

            MenuItem openMenu = new MenuItem();
            openMenu.Header = "Open";
            openMenu.Click += Open_Ctx_Click;
            ctxMenu.Items.Add(openMenu);

            MenuItem copyToMenu = new MenuItem();
            copyToMenu.Header = "Copy To";
            copyToMenu.SubmenuOpened += Create_SubMenu;
            copyToMenu.SubmenuClosed += Clear_Constants;
            copyToMenu.Items.Add("");
            ctxMenu.Items.Add(copyToMenu);

            MenuItem moveToMenu = new MenuItem();
            moveToMenu.Header = "Move To";
            moveToMenu.SubmenuOpened += Create_SubMenu;
            moveToMenu.SubmenuClosed += Clear_Constants;
            moveToMenu.Items.Add("");
            ctxMenu.Items.Add(moveToMenu);

            ctxMenu.Opened += ContextMenu_OpenTriggered;
            return ctxMenu;
        }

        private void ContextMenu_OpenTriggered(object sender, RoutedEventArgs e)
        {
            if (subMenu.Count() <= 1)
            {
                (ctxMenu.Items[2] as MenuItem).Visibility = Visibility.Collapsed;
                (ctxMenu.Items[3] as MenuItem).Visibility = Visibility.Collapsed;
            }
            else
            {
                (ctxMenu.Items[2] as MenuItem).Visibility = Visibility.Visible;
                (ctxMenu.Items[3] as MenuItem).Visibility = Visibility.Visible;
            }
        }

        private void Clear_Constants(object sender, RoutedEventArgs e)
        {
            parentSelected = "";
        }

        private void Create_SubMenu(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            setParentSelected(mi);
            if (mi != null)
            {
                mi.Items.Clear();
                foreach (string item in subMenu)
                {
                    if (!item.Equals(subMenuOriginNode))
                    {
                        MenuItem subMenu = new MenuItem();
                        subMenu.Header = item.ToString();
                        subMenu.Click += subMenu_Click;
                        mi.Items.Add(subMenu);
                    }
                }
            }
        }

        private void setParentSelected(MenuItem mi)
        {
            parentSelected = "";
            if (mi != null)
            {
                ContextMenu cm = mi.Parent as ContextMenu;
                if (cm != null)
                {
                    TreeViewItem node = cm.PlacementTarget as TreeViewItem;
                    if (node != null)
                    {
                        while (!(node.Parent is TreeView))
                        {
                            if (node.Header is StackPanel)
                            {
                                StackPanel sp = node.Header as StackPanel;
                                parentSelected = (sp.Children[0] as TextBlock).Text + "/" + parentSelected;
                            }
                            else
                                parentSelected = node.Header.ToString() + "/" + parentSelected;

                            node = node.Parent as TreeViewItem;
                        }
                        subMenuOriginNode = ((node.Parent as TreeView).Parent as Expander).Header.ToString();
                        parentSelected = ((node.Parent as TreeView).Parent as Expander).Tag.ToString() + "/" + node.Header.ToString() + "/" + parentSelected;
                    }
                }
            }
        }

        /// <summary>
        /// Sub menu click Handler for CopyTo/MoveTo
        /// </summary>
        private void subMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            if (mi != null)
            {
                string[] destNameArray = mi.Header.ToString().Split(',');
                ExpandoObject expObj = ResourceManagerUtil.getExpandoObj();
                //destNameArray[0].Equals("default_All") ? destNameArray[0] : GetProperty(expando, (ResourceManagerUtil.GetProperty(expObj, destNameArray[0]) as string));
                string expStoredName = (destNameArray[0].Equals("default_All") ? destNameArray[0] : (ResourceManagerUtil.GetProperty(expObj, destNameArray[0]) as string));
                string[] expStoredNameArray = expStoredName.Split('_');
                string destName = (expStoredNameArray[0] + "_" + (expStoredNameArray[1].Equals("All") ? expStoredNameArray[1] : expStoredNameArray[1].ToUpper()) + "-" + destNameArray[1].Trim());
                MenuItem cm = mi.Parent as MenuItem;
                string opt = cm.Header.ToString();//Either copy to or move to selected
                ProjectItem contentsDirItems = ResourceManagerUtil.getContentFolder(currProj);
                string[] srcHierarchy = parentSelected.Split('/');
                Boolean isAllConf = false;//since All Configuration points to directory res/contents folder, we have to change the path.
                ProjectItem destProjItem = ResourceManagerUtil.getProjectItem(destName, contentsDirItems);
                string projPath = Path.GetDirectoryName(currProj.FullName);
                srcHierarchy = srcHierarchy.Take(srcHierarchy.Count() - 1).ToArray();
                if (srcHierarchy.Count() == 0)
                    return;
                if (srcHierarchy[0].Equals("ALL"))
                {
                    isAllConf = true;
                    srcHierarchy = srcHierarchy.Skip(1).ToArray();
                }
                string srcPath = projPath.ToString() + "\\res\\contents\\" + String.Join("\\", srcHierarchy);

                string extResult = Path.GetExtension(String.Join("/", srcHierarchy));
                string[] srcPathArray = Path.GetDirectoryName(srcPath).Split(new string[] { srcHierarchy[0] }, StringSplitOptions.None);

                ProjectItem srcParentPrjItem = contentsDirItems;

                string srcLastItem = srcHierarchy.Last();
                foreach (string folderName in srcHierarchy)
                {
                    if (!object.ReferenceEquals(folderName, srcLastItem))
                    {
                        try
                        {
                            srcParentPrjItem = ResourceManagerUtil.getProjectItem(folderName, srcParentPrjItem);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

                //If path is Folder
                if (extResult.Equals(""))
                {
                    if (isAllConf)
                    {
                        List<string> srcHierList = new List<string>(srcHierarchy);
                        srcHierList.Insert(0, "ALL");
                        srcHierarchy = srcHierList.ToArray();
                    }

                    for (int i = 1; i < srcHierarchy.Length - 1; i++)
                    {
                        if (i < 0) break;
                        if (!srcHierarchy[i].Equals(""))
                        {
                            destProjItem = ResourceManagerUtil.addProjectFolder(srcHierarchy[i], destProjItem);
                        }
                    }
                    ResourceManagerUtil.copyDir(srcPath, destProjItem);
                }
                //If path is file
                else
                {
                    if (isAllConf)
                    {
                        srcPathArray = Path.GetDirectoryName(srcPath).Split(new string[] { "\\res\\contents\\" }, StringSplitOptions.None);
                    }
                    if (srcPathArray.Count() > 1)
                    {
                        foreach (string folderName in srcPathArray[1].Split('\\'))
                        {
                            if (!folderName.Equals(""))
                            {
                                destProjItem = ResourceManagerUtil.addProjectFolder(folderName, destProjItem);
                            }
                        }
                    }
                    ResourceManagerUtil.copyFile(srcPath, destProjItem);
                }

                if (opt.Equals("Move To"))
                {
                    try
                    {
                        ResourceManagerUtil.removeProjectItem(srcLastItem, srcParentPrjItem);
                    }
                    catch (Exception)
                    {
                        FileAttributes attr = File.GetAttributes(@srcPath);

                        //detect whether its a directory or file
                        if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                            Directory.Delete(srcPath);
                        else
                            File.Delete(srcPath);
                    }
                }
            }
        }

        /// <summary>
        /// Context menu Open click Handler
        /// </summary>
        private void Open_Ctx_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            if (mi != null)
            {
                ContextMenu cm = mi.Parent as ContextMenu;
                if (cm != null)
                {
                    TreeViewItem node = cm.PlacementTarget as TreeViewItem;
                    if (node != null)
                    {
                        System.Diagnostics.Process.Start(node.Tag.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Context menu Delete click Handler
        /// </summary>
        private void Delete_Ctx_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            if (mi != null)
            {
                ContextMenu cm = mi.Parent as ContextMenu;
                if (cm != null)
                {
                    TreeViewItem node = cm.PlacementTarget as TreeViewItem;
                    if (node != null)
                    {
                        string fullPath = node.Tag.ToString();
                        string nodePath = fullPath;
                        ProjectItem prjItem = ResourceManagerUtil.getContentFolder(currProj);
                        string contents = "res\\contents\\";
                        nodePath = nodePath.Substring(nodePath.IndexOf(contents) + contents.Length);
                        string[] srcSplit = nodePath.Split('\\');
                        for (int i = 0; i < srcSplit.Length - 1; ++i)
                        {
                            try
                            {
                                prjItem = ResourceManagerUtil.getProjectItem(srcSplit[i], prjItem);
                            }
                            catch (Exception)
                            {
                            }
                        }

                        try
                        {
                            ResourceManagerUtil.removeProjectItem(srcSplit.Last(), prjItem);
                        }
                        catch (Exception)
                        {
                            // If not in project delete from the filesystem.
                            FileAttributes attr = File.GetAttributes(@fullPath);

                            //detect whether its a directory or file
                            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                                Directory.Delete(fullPath);
                            else
                                File.Delete(fullPath);
                        }
                    }
                }
            }
        }

        public void setContextSubMenuItem(string menuItem)
        {
            if (!subMenu.Contains(menuItem))
                subMenu.Add(menuItem);
        }

        public void removeSubMenuItem(string menuItem)
        {
            if (subMenu.Contains(menuItem))
                subMenu.Remove(menuItem);
        }
    }
}
