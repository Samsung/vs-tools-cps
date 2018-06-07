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

namespace Tizen.VisualStudio.ResourceManager
{
    using EnvDTE;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Dynamic;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Xml;
    using System.Xml.Linq;

    public enum ResolutionDPI
    {
        All,
        LDPI,
        MDPI,
        HDPI,
        XHDPI,
        XXHDPI
    }

    /// <summary>
    /// Interaction logic for ResourceManagerControl.
    /// </summary>
    public partial class ResourceManagerControl : UserControl
    {
        public int currentResCounter = 0;
        private dynamic expando = new ExpandoObject();
        private ContextMenu ctxMenu;
        private List<string> viewLangComboList = new List<string>() { "" };
        private List<string> viewDpiComboList = new List<string>() { "" };
        private Project currentProj;
        string projPath;
        private Dictionary<string, string> langMap;
        private List<string> viewHeaderList = new List<string>();
        ResourceManagerContextMenu viewContextMenu;
        private TreeView defaultExpanderTreeHeader = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceManagerControl"/> class.
        /// </summary>
        public ResourceManagerControl()
        {
            this.InitializeComponent();
            InitializeDropDown();

            currentProj = ResourceManagerUtil.getCurrentProject();
            if (currentProj == null)
                return;
            projPath = currentProj.FullName;
            string projFilePath = projPath.Substring(0, projPath.LastIndexOf("\\") + 1);

            //Check if this is a Tizen project.
            if (!ResourceManagerUtil.isTizenProject(projFilePath)) return;

            var resFolderPath = projFilePath + "res\\";

            ContentsWatcher watcher = new ContentsWatcher(this, currentProj);
            watcher.watch(projFilePath);

            viewContextMenu = new ResourceManagerContextMenu(currentProj);
            ctxMenu = viewContextMenu.createContextMenu();
            updateResourceView();
            viewComboLang.ItemsSource = viewLangComboList;
            resolutionComboView.ItemsSource = viewDpiComboList;
            viewComboLang.DropDownOpened += fetchViewLangComboData;
            resolutionComboView.DropDownOpened += fetchViewDpiComboData;
            btnAdd.IsEnabled = false;
        }

        private void fetchViewDpiComboData(object sender, EventArgs e)
        {
            viewDpiComboList.Clear();
            viewDpiComboList.Add("");
            foreach (StackPanel sp in viewStack.Children)
            {
                string[] splitSP = (sp.Children[0] as Expander).Header.ToString().Split(',');
                if (splitSP.Count() > 1 && !viewDpiComboList.Contains(splitSP[1].Trim()))
                    viewDpiComboList.Add(splitSP[1].Trim());
            }
            resolutionComboView.ItemsSource = "";
            resolutionComboView.ItemsSource = viewDpiComboList;
        }

        private void fetchViewLangComboData(object sender, EventArgs e)
        {
            viewLangComboList.Clear();
            viewLangComboList.Add("");
            foreach (StackPanel sp in viewStack.Children)
            {
                string[] splitSP = (sp.Children[0] as Expander).Header.ToString().Split(',');
                if (splitSP.Count() > 1 && !viewLangComboList.Contains(splitSP[0]))
                    viewLangComboList.Add(splitSP[0]);
            }
            viewComboLang.ItemsSource = "";
            viewComboLang.ItemsSource = viewLangComboList;
        }

        internal void ClearAllViewsAndFile()
        {
            this.Dispatcher.Invoke(() =>
            {
                ConfigurationDataGrid.Items.Clear();
                ((StackPanel)(StackPanelGrid.Children[0])).Children.Clear();
            });
        }

        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void Add_Button_Click(object sender, RoutedEventArgs e)
        {
            XmlNode element = configLangCombo.SelectedItem as XmlNode;
            string langSelected = element.Attributes.GetNamedItem("name").Value.ToString();
            string resolutionSelected = resolutionCombo.Text.ToString();
            string id = element.Attributes.GetNamedItem("id").Value.ToString();
            id = id.Replace('-', '_');
            bool canAdd = validateAddParams(langSelected, resolutionSelected);
            if (!canAdd)
            {
                System.Windows.MessageBox.Show("Language-DPI is already present in the list");
                return;
            }
            string folderName = id + "-" + resolutionSelected;
            string directoryPath = ("/res/contents/" + folderName);

            if (canAdd && currentProj != null)
            {
                ProjectItem contents = ResourceManagerUtil.getContentFolder(currentProj);
                ResourceManagerUtil.addProjectFolder(folderName, contents);
            }
        }

        private ResourceManagerUtil.ResourceItem CreateResourceItem(string directoryPath, string langSelected, string resolutionSelected)
        {
            return new ResourceManagerUtil.ResourceItem() { Directory = directoryPath, Language = langSelected, Resolution = resolutionSelected };
        }

        /// <summary>
        /// Handles click on the delete button.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void Delete_Row_Button_Click(object sender, RoutedEventArgs e)
        {
            object item = ConfigurationDataGrid.SelectedItem;
            string path = (ConfigurationDataGrid.SelectedCells[0].Column.GetCellContent(item) as TextBlock).Text;
            MessageBoxResult result = System.Windows.MessageBox.Show("Are you sure you want to delete the configuration of " + path + "?", "Confirmation", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (result == MessageBoxResult.OK && currentProj != null)
            {
                try
                {
                    ProjectItem contents = ResourceManagerUtil.getContentFolder(currentProj);
                    string name = path.Substring(path.LastIndexOf('/') + 1);
                    ResourceManagerUtil.removeProjectItem(name, contents);
                }
                catch (ArgumentException exe)
                {
                    System.Windows.MessageBox.Show("Folder cannot be deleted..." + exe);
                }
            }
        }

        private void InitializeDropDown()
        {
            XmlDocument doc = new XmlDocument();
            var dirPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            doc.Load(dirPath + @"\ViewAndUI\ResourceManager\Resource\lang_country_lists.xml");
            XmlNode nodeEle = doc.SelectSingleNode("//languages");


            XDocument document = XDocument.Load(dirPath + @".\ViewAndUI\ResourceManager\Resource\lang_country_lists.xml");
            langMap = document.Descendants("languages").Descendants("lang")
                  .ToDictionary(d => (string)d.Attribute("id"),
                                d => (string)d.Attribute("name"));

            int c = langMap.Count;
            ExpandoObject utilExpObj = ResourceManagerUtil.getExpandoObj();
            foreach (XmlElement nodeE in nodeEle.ChildNodes)
            {
                AddProperty(expando, nodeE.GetAttribute("id"), nodeE.GetAttribute("name"));
                ResourceManagerUtil.AddProperty(utilExpObj, nodeE.GetAttribute("name"), nodeE.GetAttribute("id"));
            }

            XmlElement node = doc.CreateElement("lang");
            XmlAttribute idAttribute = doc.CreateAttribute("id");
            idAttribute.Value = "default_All";

            XmlAttribute nameAttribute = doc.CreateAttribute("name");
            nameAttribute.Value = "All";

            node.Attributes.Append(idAttribute);
            node.Attributes.Append(nameAttribute);

            nodeEle.InsertBefore(node, nodeEle.ChildNodes.Item(0));
            configLangCombo.ItemsSource = nodeEle;
            //configLangComboView.ItemsSource = nodeEle;
        }

        private static void AddProperty(ExpandoObject expando, string propertyName, object propertyValue)
        {
            // ExpandoObject supports IDictionary so we can extend it like this
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                expandoDict[propertyName] = propertyValue;
            else
                expandoDict.Add(propertyName, propertyValue);
        }

        private static object GetProperty(ExpandoObject expando, string propertyName)
        {
            // ExpandoObject supports IDictionary so we can extend it like this
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                return expandoDict[propertyName];
            else
                return null;
        }

        private void updateResourceView()
        {
            if (currentProj == null)
                return;

            var projFilePath = projPath;
            projFilePath = projFilePath.Substring(0, projFilePath.LastIndexOf("\\") + 1);

            //Check if this is a Tizen project.
            if (!ResourceManagerUtil.isTizenProject(projFilePath)) return;

            var resFolderPath = projFilePath + "res\\";
            if (!Directory.Exists(resFolderPath)) return;
            var contentsFolderPath = resFolderPath + "contents";
            if (!Directory.Exists(contentsFolderPath)) return;

            createDefaultPanel();
            // Add all folders to config and view
            DirectoryInfo di = new DirectoryInfo(contentsFolderPath);
            foreach (DirectoryInfo fi in di.GetDirectories())
            {
                populateConfigurationTab(fi.FullName.Substring(fi.FullName.IndexOf("res\\") + 4));
                populateViewTab(fi.FullName);
            }
            // Add all files to view
            FileInfo[] Files = di.GetFiles();
            foreach (FileInfo file in Files)
            {
                TreeViewItem item = createTreeItem(contentsFolderPath, file.Name, false);
                defaultExpanderTreeHeader.Items.Add(item);
            }
        }


        internal void UpdateConfigurationTab(object source, WatcherChangeTypes type, string folderName)
        {
            if (type == WatcherChangeTypes.Created)
                addToConfigurationTab(folderName);
            if (type == WatcherChangeTypes.Deleted)
                removeFromConfigurationTab(folderName);
        }

        private void removeFromConfigurationTab(string folderPath)
        {
            folderPath = folderPath.Replace("\\", "/");
            folderPath = folderPath.Replace("res/", "");
            string directoryPath = folderPath;
            foreach (ResourceManagerUtil.ResourceItem item in ConfigurationDataGrid.Items)
            {
                if (item.Directory.Equals(directoryPath))
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        ConfigurationDataGrid.Items.Remove(item);
                    });
                    return;
                }
            }
        }

        private void populateConfigurationTab(string folderPath)
        {
            addToConfigurationTab(folderPath);
        }

        private void addToConfigurationTab(string folderPath)
        {
            folderPath = folderPath.Replace("\\", "/");
            folderPath = folderPath.Replace("res/", "");
            string directoryPath = folderPath;
            string langString = folderPath.Substring(folderPath.IndexOf("/") + 1);
            string langId = "";
            string language = "";
            string resolution = "";

            string[] split = langString.Split('-');
            if (split.Length != 2) return;
            langId = split[0];
            if (langId.Equals("default_All"))
                language = "All";
            else
            {
                if (!langMap.TryGetValue(langId, out language))
                {
                    // the key isn't in the dictionary.
                    return;
                }
            }
            resolution = split[1];

            if (!split[1].Equals("All"))
                resolution = (ResourceManagerUtil.getResolution(split[1]).Length == 0) ? "" : split[1];


            if (isValidConfig(directoryPath, language, resolution))
                this.Dispatcher.Invoke(() =>
                  ConfigurationDataGrid.Items.Add(CreateResourceItem(directoryPath, language, resolution))
                );
        }

        private bool isValidConfig(string directoryPath, string lang, string resolution)
        {
            if (directoryPath.Length == 0 || lang.Length == 0 || resolution.Length == 0)
                return false;
            return true;
        }

        private void populateViewTab(string expanderHeader)
        {
            Expander expander = null;
            StackPanel sp = new StackPanel();
            sp.Margin = new Thickness(10, 5, 10, 5);
            var rowDefinition = new RowDefinition();
            rowDefinition.Height = GridLength.Auto;
            StackPanelGrid.RowDefinitions.Add(rowDefinition);
            StackPanelGrid.SetValue(Grid.RowProperty, StackPanelGrid.RowDefinitions.Count());
            StackPanelGrid.SetValue(Grid.ColumnProperty, 0);
            Grid.SetRow(sp, StackPanelGrid.RowDefinitions.Count() - 1);
            Grid.SetColumn(sp, 0);
            expander = makeExpander(sp, expanderHeader);
            if (expander != null)
            {
                viewStack.Children.Add(sp);
            }
        }

        private void createDefaultPanel()
        {
            StackPanel sp = new StackPanel();
            sp.Margin = new Thickness(10, 5, 10, 5);
            var rowDefinition = new RowDefinition();
            rowDefinition.Height = GridLength.Auto;
            StackPanelGrid.RowDefinitions.Add(rowDefinition);
            StackPanelGrid.SetValue(Grid.RowProperty, StackPanelGrid.RowDefinitions.Count());
            StackPanelGrid.SetValue(Grid.ColumnProperty, 0);
            Grid.SetRow(sp, StackPanelGrid.RowDefinitions.Count() - 1);
            Grid.SetColumn(sp, 0);
            Expander defaultExpander = makeDefaultExpander(sp, "ALL");
            sp.Children.Add(defaultExpander);
            viewStack.Children.Insert(0, sp);
            //viewStack.Children.Add(sp);
        }

        private Expander makeDefaultExpander(StackPanel sp, string expanderHeader)
        {
            Expander exp = new Expander();
            exp.Header = expanderHeader;
            exp.Tag = expanderHeader;
            exp.FontWeight = FontWeights.Bold;
            exp.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x80, 0xFF));
            exp.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            exp.BorderThickness = new Thickness(1);
            exp.BorderBrush = Brushes.Gray;

            // Add tree inside each of the expander
            defaultExpanderTreeHeader = new TreeView();
            defaultExpanderTreeHeader.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            defaultExpanderTreeHeader.VerticalContentAlignment = VerticalAlignment.Stretch;
            defaultExpanderTreeHeader.HorizontalAlignment = HorizontalAlignment.Stretch;
            defaultExpanderTreeHeader.VerticalAlignment = VerticalAlignment.Stretch;
            exp.Content = defaultExpanderTreeHeader;
            return exp;
        }

        private Expander makeExpander(StackPanel sp, string expanderHeader)
        {
            Expander exp = new Expander();
            DirectoryInfo dir = new DirectoryInfo(expanderHeader);
            string[] headerComponent = dir.Name.Split('-');
            string langComponent = headerComponent[0];
            string dpiComponent = "";

            if (headerComponent.Length == 2 && (langComponent.Equals("default_All") || GetProperty(expando, langComponent) != null) && isValidDpi(headerComponent[1]))
            {
                dpiComponent = headerComponent[1];
                exp.Header = (((headerComponent[0].Equals("default_All")) ? headerComponent[0] : GetProperty(expando, langComponent)) + ", " + dpiComponent);

            }
            else
            {
                // invalid name, put it to ALL
                TreeViewItem item = createTreeItem(expanderHeader.Substring(0, expanderHeader.LastIndexOf('\\')), dir.Name, false);
                defaultExpanderTreeHeader.Items.Add(item);
                createResourceViewTree(expanderHeader, item);
                return null;
            }

            exp.Tag = dir.Name;

            //Store Language and DPI values to display in view combobox
            if (!viewLangComboList.Contains(exp.Header.ToString().Split(',')[0]))
                viewLangComboList.Add(exp.Header.ToString().Split(',')[0]);

            if (!viewDpiComboList.Contains(dpiComponent))
                viewDpiComboList.Add(dpiComponent);

            //Store Header text in context menu for sub menu items
            viewContextMenu.setContextSubMenuItem(exp.Header.ToString());

            exp.FontWeight = FontWeights.Bold;
            exp.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x80, 0xFF));
            exp.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            exp.BorderThickness = new Thickness(1);
            exp.BorderBrush = Brushes.Gray;
            sp.Children.Add(exp);
            // Add tree inside each of the expander
            TreeView headerItem = new TreeView();
            headerItem.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            headerItem.VerticalContentAlignment = VerticalAlignment.Stretch;
            headerItem.HorizontalAlignment = HorizontalAlignment.Stretch;
            headerItem.VerticalAlignment = VerticalAlignment.Stretch;

            createResourceViewTreeHeader(expanderHeader, headerItem);
            exp.Content = headerItem;
            return exp;
        }

        private Boolean isValidDpi(string dpi)
        {
            var values = Enum.GetNames(typeof(ResolutionDPI));
            foreach (string value in values)
            {
                if (dpi.Equals(value))
                    return true;
            }

            return false;
        }

        private void createResourceViewTreeHeader(string projPath, TreeView parent)
        {
            DirectoryInfo d = new DirectoryInfo(projPath);

            FileAttributes attr = File.GetAttributes(projPath);
            if (!attr.HasFlag(FileAttributes.Directory))
            {
                return;
            }
            DirectoryInfo[] directories = d.GetDirectories();

            foreach (DirectoryInfo dir in directories)
            {
                String[] names = dir.Name.Split('-');
                if (parent.Equals(defaultExpanderTreeHeader) && names.Count() > 1 && ResourceManagerUtil.isValidLanguageID(names[0]) && ResourceManagerUtil.isValidResolution(names[1].Trim()))
                {
                    continue;
                }
                TreeViewItem item = createTreeItem(projPath, dir.Name, false);
                parent.Items.Add(item);
                createResourceViewTree(projPath + "\\" + dir.Name, item);
            }
            FileInfo[] Files = d.GetFiles();
            foreach (FileInfo file in Files)
            {
                TreeViewItem item = createTreeItem(projPath, file.Name, true);
                parent.Items.Add(item);
            }
        }

        private void createResourceViewTree(string projPath, TreeViewItem parent)
        {
            DirectoryInfo d = new DirectoryInfo(projPath);
            FileAttributes attr = File.GetAttributes(projPath);
            if (!attr.HasFlag(FileAttributes.Directory))
            {
                return;
            }
            DirectoryInfo[] directories = d.GetDirectories();

            foreach (DirectoryInfo dir in directories)
            {
                TreeViewItem item = createTreeItem(projPath, dir.Name, false);
                parent.Items.Add(item);
                createResourceViewTree(projPath + "\\" + dir.Name, item);
            }
            FileInfo[] Files = d.GetFiles();
            foreach (FileInfo file in Files)
            {
                TreeViewItem item = createTreeItem(projPath, file.Name, true);
                parent.Items.Add(item);
            }
        }

        private TreeViewItem createTreeItem(string itemPath, string itemName, Boolean isFile)
        {
            TreeViewItem item = null;
            if (isFile)
            {
                StackPanel sp = new StackPanel();
                sp.Orientation = Orientation.Horizontal;
                TextBlock tb = new TextBlock();
                tb.Text = itemName;
                sp.Children.Add(tb);
                item = new TreeViewItem();
                item.Header = itemName;
                item.Tag = itemPath + "\\" + itemName;
                item.MouseDoubleClick += ResourceManagerUtil.openFile;
            }
            else
            {
                item = new TreeViewItem() { Header = itemName.ToString() };
                item.Tag = itemPath + "\\" + itemName;
            }
            item.FontWeight = FontWeights.Medium;
            item.ContextMenu = ctxMenu;
            return item;
        }

        private bool validateAddParams(string langSelected, string resolutionSelected)
        {
            bool retStatus = true;
            if (ConfigurationDataGrid.Items.IsEmpty)
            {
                retStatus = true;
            }
            else
            {
                foreach (ResourceManagerUtil.ResourceItem data in ConfigurationDataGrid.Items)
                {
                    if (data.Language.ToString().Equals(langSelected) && data.Resolution.ToString().Equals(resolutionSelected))
                    {
                        retStatus = false;
                        break;
                    }
                }
            }
            return retStatus;
        }

        private void Filter_Button_Click(object sender, RoutedEventArgs e)
        {
            // filter functionality
            string viewLangSelected = viewComboLang.Text.ToString();
            string viewDpiSelected = resolutionComboView.Text.ToString();
            if (viewLangSelected.Equals("") && viewDpiSelected.Equals(""))
            {
                foreach (StackPanel sp in ((StackPanel)(StackPanelGrid.Children[0])).Children)
                {
                    (sp).Visibility = Visibility.Visible;
                }
            }

            if (!(viewLangSelected.Equals("")) && viewDpiSelected.Equals(""))
            {
                foreach (StackPanel sp in ((StackPanel)(StackPanelGrid.Children[0])).Children)
                {
                    if ((sp.Children[0] as Expander).Header.ToString().Split(',')[0].Equals(viewLangSelected))
                    {
                        (sp).Visibility = Visibility.Visible;
                    }
                    else
                        (sp).Visibility = Visibility.Collapsed;
                }
            }
            else if (viewLangSelected.Equals("") && !(viewDpiSelected.Equals("")))
            {
                foreach (StackPanel sp in ((StackPanel)(StackPanelGrid.Children[0])).Children)
                {
                    if ((sp.Children[0] as Expander).Header.ToString().Equals("ALL"))
                    {
                        (sp).Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        if ((sp.Children[0] as Expander).Header.ToString().Split(',')[1].Trim().Equals(viewDpiSelected))
                        {
                            (sp).Visibility = Visibility.Visible;
                        }
                        else
                            (sp).Visibility = Visibility.Collapsed;
                    }
                }
            }
            else if (!(viewLangSelected.Equals("")) && !(viewDpiSelected.Equals("")))
            {
                foreach (StackPanel sp in ((StackPanel)(StackPanelGrid.Children[0])).Children)
                {
                    string[] spSplitted = (sp.Children[0] as Expander).Header.ToString().Split(',');
                    if (spSplitted[0].Equals(viewLangSelected) && spSplitted[1].Trim().Equals(viewDpiSelected))
                    {
                        (sp).Visibility = Visibility.Visible;
                    }
                    else
                        (sp).Visibility = Visibility.Collapsed;
                }
            }
        }

        public void UpdateViewTab(object source, WatcherChangeTypes type, String name, String fullPath)
        {
            Boolean isFile = Path.GetExtension(fullPath).Equals("") ? false : true;
            if (type == WatcherChangeTypes.Created)
            {
                this.Dispatcher.Invoke(() =>
                {
                    addToViewTab(name, fullPath, isFile);
                });
            }

            if (type == WatcherChangeTypes.Deleted)
            {
                this.Dispatcher.Invoke(() =>
                {
                    removeFromViewTab(name, fullPath, isFile);
                });
            }
        }

        private void addToViewTab(string name, string fullPath, Boolean isFile)
        {
            object viewChildElements = ((StackPanel)(StackPanelGrid.Children[0])).Children;
            string[] createdHierarchy = name.Split('\\');
            int contentNotFoundIdx = -1;
            Boolean isDefaultConf = false;
            if (createdHierarchy.Count() == 2 && createdHierarchy[1].Equals("contents"))
            {
                createDefaultPanel();
                return;
            }
            for (int idx = 2; idx < createdHierarchy.Count(); idx++)
            {
                Boolean isContentFound = false;
                var convViewElem = (dynamic)viewChildElements;
                try
                {
                    convViewElem = convViewElem.Items;
                }
                catch (Exception)
                {
                    convViewElem = (dynamic)viewChildElements;
                }
                foreach (var ele in convViewElem)
                {
                    if (ele is StackPanel)
                    {
                        StackPanel sp = ele as StackPanel;
                        var exp = sp.Children[0];
                        string[] hieItem = createdHierarchy[idx].Split('-');
                        if (hieItem.Length != 2)
                            return;
                        string[] splittedLang = hieItem[0].Split('_');
                        if (splittedLang.Count() < 2)
                        {
                            viewChildElements = defaultExpanderTreeHeader;
                            isContentFound = false;
                            isDefaultConf = true;
                            break;
                        }

                        string hieLang = hieItem[0].Equals("default_All") ? hieItem[0] : GetProperty(expando, (splittedLang[0] + "_" + splittedLang[1]));
                        if (!(hieLang != null && ResourceManagerUtil.isValidLanguageID(hieItem[0]) && isValidDpi(hieItem[1]) && defaultExpanderTreeHeader != null))
                        {
                            viewChildElements = defaultExpanderTreeHeader;
                            isContentFound = false;
                            isDefaultConf = true;
                            break;
                        }
                        string[] expItem = ((string)((Expander)exp).Header).Split(',');
                        if (hieLang.Equals(expItem[0]) && hieItem[1].Equals(expItem[1].Trim()))
                        {
                            viewChildElements = (((Expander)exp).Content as TreeView);
                            isContentFound = true;
                            isDefaultConf = false;
                            break;
                        }
                    }
                    else if (ele is TreeViewItem)
                    {
                        TreeViewItem tvi = ele as TreeViewItem;
                        if (tvi.Header is StackPanel)
                        {
                            StackPanel sp = tvi.Header as StackPanel;
                            if ((sp.Children[0] as TextBlock).Text.ToString().Equals(createdHierarchy[idx]))
                            {
                                isContentFound = true;
                                isDefaultConf = false;
                                break;
                            }
                        }
                        if (tvi.Header.ToString().Equals(createdHierarchy[idx]))
                        {
                            viewChildElements = tvi;
                            isContentFound = true;
                            isDefaultConf = false;
                            break;
                        }
                    }
                }
                if (!isContentFound)
                {
                    contentNotFoundIdx = idx;
                    break;
                }
            }

            if (contentNotFoundIdx == 2 && !isDefaultConf)
            {
                createdHierarchy = createdHierarchy.Take(3).ToArray();
                var projFilePath = projPath.Substring(0, projPath.LastIndexOf("\\") + 1) + (string.Join("\\", createdHierarchy));
                populateViewTab(projFilePath);
            }
            else if (contentNotFoundIdx != -1)
            {
                var foundTVI = (dynamic)viewChildElements;
                string[] projPathArr = createdHierarchy.Take(contentNotFoundIdx).ToArray();
                var projFilePath = projPath.Substring(0, projPath.LastIndexOf("\\") + 1) + (string.Join("\\", projPathArr));
                if (isFile && (contentNotFoundIdx == createdHierarchy.Count() - 1))
                {
                    projFilePath = Path.GetDirectoryName(fullPath);
                    TreeViewItem item = createTreeItem(projFilePath, createdHierarchy[createdHierarchy.Count() - 1], true);
                    foundTVI.Items.Remove(item);
                    foundTVI.Items.Add(item);
                }
                else
                {
                    if (foundTVI is TreeView)
                    {
                        TreeView tv = foundTVI as TreeView;
                        tv.Items.Clear();
                        createResourceViewTreeHeader(projFilePath, tv);
                    }
                    else
                    {
                        TreeViewItem tvi = foundTVI;
                        tvi.Items.Clear();
                        createResourceViewTree(projFilePath, tvi);
                    }
                }
            }
        }

        private void removeFromViewTab(string name, string fullPath, Boolean isFile)
        {
            object viewChildElements = ((StackPanel)(StackPanelGrid.Children[0])).Children;
            ItemCollection treeViewItems = null;
            string[] removedHierarchy = name.Split('\\');
            string seleExt = Path.GetExtension(fullPath);
            if (removedHierarchy.Count() == 3)
            {
                var convViewElem = (dynamic)viewChildElements;
                string[] hieItem = removedHierarchy[2].Split('-');
                if (hieItem.Length != 2)
                    return;
                string[] splittedLang = hieItem[0].Split('_');
                string hieLang = "";
                if (!(splittedLang.Count() < 2))
                    hieLang = (hieItem[0].Equals("default_All") ? hieItem[0] : GetProperty(expando, (splittedLang[0] + "_" + splittedLang[1])));

                if (!(hieLang != null && ResourceManagerUtil.isValidLanguageID(hieItem[0]) && isValidDpi(hieItem[1]) && defaultExpanderTreeHeader != null))
                {
                    return;
                }
                foreach (StackPanel sp in convViewElem)
                {
                    var exp = sp.Children[0];
                    string[] expItem = ((string)((Expander)exp).Header).Split(',');

                    if (hieLang.Equals(expItem[0]) && hieItem[1].Equals(expItem[1].Trim()))
                    {
                        viewContextMenu.removeSubMenuItem(((Expander)exp).Header.ToString());
                        ((StackPanel)(StackPanelGrid.Children[0])).Children.Remove(sp);
                        break;
                    }
                }
                return;
            }
            else if (removedHierarchy.Count() < 2)
            {
                ((StackPanel)(StackPanelGrid.Children[0])).Children.Clear();
                return;
            }

            for (int idx = 2; idx < removedHierarchy.Count() - 1; idx++)
            {
                var convViewElem = (dynamic)viewChildElements;
                foreach (var ele in convViewElem)
                {
                    if (ele is StackPanel)
                    {
                        StackPanel sp = ele as StackPanel;
                        var exp = sp.Children[0];
                        string[] hieItem = removedHierarchy[idx].Split('-');
                        string[] splittedLang = hieItem[0].Split('_');
                        string hieLang = "";
                        if (!(splittedLang.Count() < 2))
                            hieLang = (hieItem[0].Equals("default_All") ? hieItem[0] : GetProperty(expando, (splittedLang[0] + "_" + splittedLang[1])));

                        if (!(hieLang != null && ResourceManagerUtil.isValidLanguageID(hieItem[0]) && isValidDpi(hieItem[1]) && defaultExpanderTreeHeader != null))
                        {
                            return;
                        }
                        string[] expItem = ((string)((Expander)exp).Header).Split(',');
                        if (hieLang.Equals(expItem[0]) && hieItem[1].Equals(expItem[1].Trim()))
                        {
                            treeViewItems = (((Expander)exp).Content as TreeView).Items;
                            break;
                        }
                    }
                    else if (ele is TreeViewItem)
                    {
                        TreeViewItem tvi = ele as TreeViewItem;
                        if (tvi.Header.ToString().Equals(removedHierarchy[idx]))
                        {
                            treeViewItems = tvi.Items;
                            break;
                        }
                    }
                }
            }
            if (treeViewItems == null)
                return;

            if (isFile)
            {
                foreach (TreeViewItem tvcItem in treeViewItems)
                {
                    string sp = tvcItem.Header.ToString();
                    if (seleExt.Equals(Path.GetExtension(tvcItem.Tag.ToString())) && (sp.Equals(removedHierarchy[removedHierarchy.Count() - 1])))
                    {
                        treeViewItems.Remove(tvcItem);
                        break;
                    }
                }
            }
            else
            {
                foreach (TreeViewItem tvcItem in treeViewItems)
                {
                    if (tvcItem.Header.ToString().Equals(removedHierarchy[removedHierarchy.Count() - 1]))
                    {
                        treeViewItems.Remove(tvcItem);
                        break;
                    }
                }
            }
        }

        private void Combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (btnAdd != null)
            {
                if (configLangCombo.SelectedValue.ToString() == "All" && resolutionCombo.SelectedValue.ToString() == "All")
                    btnAdd.IsEnabled = false;
                else
                    btnAdd.IsEnabled = true;
            }
        }
    }
}
