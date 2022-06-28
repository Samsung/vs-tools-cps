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
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using EnvDTE;
using System.Text.RegularExpressions;
using Tizen.VisualStudio.Tools.Data;
using System.Xml;

namespace Tizen.VisualStudio.ManifestEditor
{
    /// <summary>
    /// Interaction logic for TizenManifestDesignerControl.xaml
    /// </summary>
    public partial class TizenManifestDesignerControl : UserControl
    {
        public feature latestSelect = null;
        private DTE dte;
        public string PreviewIconPath;
        private string FeaturePath;
        private string PrivilegePath;
        private string apiVersion;
        private List<TextboxLabelSet> Overview = new List<TextboxLabelSet>();
        private readonly List<string> appNodeNameCandidates = new List<string>() { "ui-application", "service-application", "widget-application", "watch-application", "component-based-application" };

        public string GetAppType()
        {
            Project project = (dte.ActiveSolutionProjects as Array).GetValue(0) as Project;

            string manifestPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(project.FullName), "tizen-manifest.xml");
            if (!System.IO.File.Exists(manifestPath))
            {
                Projects ListOfProjectsInSolution = dte.Solution.Projects;
                foreach (Project proj in ListOfProjectsInSolution)
                {
                    if (proj != null)
                    {
                        manifestPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(proj.FullName), "tizen-manifest.xml");
                        if (System.IO.File.Exists(manifestPath))
                        {
                            break;
                        }
                    }
                }
            }
            XmlDocument doc = new XmlDocument();

            try
            {
                doc.Load(manifestPath);
                var appNodes = doc.GetElementsByTagName("manifest")[0].ChildNodes.Cast<XmlNode>().Where(_ => appNodeNameCandidates.Contains(_.Name.ToLower()));//new List<XmlNode>();

                return appNodes?.First(_ => true)?.Name.ToLower();
            }
            catch (Exception)
            {
            }

            return null;
        }

        public TizenManifestDesignerControl(IViewModelTizen viewModel, DTE dte)
        {
            DataContext = viewModel;
            InitializeComponent();
            viewModel.ViewModelChanged += new EventHandler(ViewModelChanged);
            this.dte = dte;

            string appType = GetAppType();
            UpdateModelAppType(viewModel, appType);
            UpdateAppControlGridColumns();

            InitializeEnv();
            OverviewSetInit();
        }

        private void UpdateModelAppType(IViewModelTizen viewModel, string appType)
        {
            if (appType != null)
            {
                if (appType.Equals("service-application"))
                {
                    viewModel.AppType = ItemsChoiceType.serviceapplication;
                }
                else if (appType.Equals("ui-application"))
                {
                    viewModel.AppType = ItemsChoiceType.uiapplication;
                }
                else if (appType.Equals("widget-application"))
                {
                    viewModel.AppType = ItemsChoiceType.widgetapplication;
                }
                else if (appType.Equals("watch-application"))
                {
                    viewModel.AppType = ItemsChoiceType.watchapplication;
                }
                else if (appType.Equals("component-based-application"))
                {
                    viewModel.AppType = ItemsChoiceType.componentbasedapplication;
                }
                else
                {
                    viewModel.AppType = ItemsChoiceType.uiapplication;
                }
            }
            this.apiVersion = viewModel.ApiVersion;
        }

        private void UpdateAppControlGridColumns()
        {
            GridView appControlGrid = this.appContorlListBox.View as GridView;
            List<GridViewColumn> toRemove = new List<GridViewColumn>();
            foreach (GridViewColumn col in appControlGrid.Columns)
            {
                if (!ApiVersionGreaterThanFive)
                {
                    if ((col.Header.Equals("Visibility") || col.Header.Equals("id")))
                    {
                        toRemove.Add(col);
                    }
                    else
                    {
                        col.Width = 200;
                    }
                }

            }

            foreach (GridViewColumn gc in toRemove)
            {
                appControlGrid.Columns.Remove(gc);
            }
        }

        private void UpdatPrivilegesGridColumns()
        {
            GridView gridView = this.PrivilegeListView.View as GridView;
            if (gridView != null)
            {
                foreach (var column in gridView.Columns)
                {
                    if (double.IsNaN(column.Width))
                        column.Width = column.ActualWidth;
                    column.Width = double.NaN;
                }
            }

        }

        public bool ApiVersionGreaterThanFive
        {
            get
            {
                float val = -1;
                float.TryParse(this.apiVersion, out val);
                if ((val != -1 && val >= 5.5))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            set
            {
            }
        }

        private void OverviewSetInit()
        {
            Overview.Add(new TextboxLabelSet(this.TBApplicationID, this.Label_Application_ID, "Application ID"));
            Overview.Add(new TextboxLabelSet(this.TBPackage, this.Label_Package, "Package"));
            Overview.Add(new TextboxLabelSet(this.TBVersion, this.Label_Version, "Version"));
            Overview.Add(new TextboxLabelSet(this.TBLabel, this.Label_label, "Label"));
            Overview.Add(new TextboxLabelSet(this.TBExec, this.Label_Exec, "Exec"));
            Overview.Add(new TextboxLabelSet(this.TBEmail, this.Label_Email, "Email"));
            Overview.Add(new TextboxLabelSet(this.TBWebsite, this.Label_Website, "Website"));
        }

        private void OverViewText_TextChanged(object sender, TextChangedEventArgs e)
        {
            var SenderTextbox = sender as TextBox;
            if (SenderTextbox != null)
            {
                ChangePropertyValue(SenderTextbox);
            }

            TextboxLabelSet input = Overview.FirstOrDefault(x => x.PairTextbox.Equals(SenderTextbox));
            if (input != null)
            {
                if (input.PairName == "Email" || input.PairName == "Website")
                {
                    TextboxRuleChecker(input.PairTextbox, input.PairLabel, TargetName: input.PairName, AllowEmpty:true, AllowNameruleException:true, AllowEndconditionException:true);
                }
                else if (input.PairName == "Label")
                {
                    TextboxRuleChecker(input.PairTextbox, input.PairLabel, TargetName: input.PairName, AllowSpace: true, AllowNameruleException: true);
                }
                else
                {
                    TextboxRuleChecker(input.PairTextbox, input.PairLabel, TargetName: input.PairName);
                }
            }
        }

        //private void TBUpdatePeriod_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    IViewModelTizen viewModel = DataContext as IViewModelTizen;

        //   // viewModel.UpdatePeriod = TBUpdatePeriod.Text.ToString();

        //    if (viewModel != null && this.IsKeyboardFocusWithin)
        //    {
        //        try
        //        {
        //            var propertyInfo = viewModel.GetType()?.GetProperty("TBUpdatePeriod");
        //            propertyInfo.SetValue(viewModel, (sender as TextBox).Text);
        //        }
        //        catch
        //        {
        //        }
        //    }
        //}

        private class TextboxLabelSet
        {
            public TextBox PairTextbox;
            public Label PairLabel;
            public string PairName;
            public TextboxLabelSet(TextBox PairTextbox, Label PairLabel, string PairName)
            {
                this.PairTextbox = PairTextbox;
                this.PairLabel = PairLabel;
                this.PairName = PairName;
            }
        }

        private void TextboxRuleChecker(TextBox TargetTextbox, Label TargetLabel, string TargetName,
            bool AllowEmpty = false, bool AllowSpace = false, bool AllowEndconditionException = false, bool AllowNameruleException = false, bool VersionRulecheck = false)
        {
            if (TargetLabel != null)
            {
                if (string.IsNullOrEmpty(TargetTextbox.Text) && !AllowEmpty)
                {
                    TargetLabel.Content = TargetName + " must not empty";
                    return;
                }

                if (TargetTextbox.Text.Contains(" ") && !AllowSpace)
                {
                    TargetLabel.Content = TargetName + " must not have space";
                    return;
                }

                if ((new Regex(@"^.*[a-zA-Z0-9]$").IsMatch(TargetTextbox.Text) == false) && !AllowEndconditionException)
                {
                    TargetLabel.Content = TargetName + " must end with alphabetic or numeric character.";
                    return;
                }

                if ((new Regex(@"^[a-zA-Z0-9\._-]*$").IsMatch(TargetTextbox.Text) == false) && !AllowNameruleException)
                {
                    TargetLabel.Content = TargetName + " must consist with alphabetic, numeric, ., - or _ character.";
                    return;
                }

                if ((new Regex(@"^(\d+\.)?(\d+\.)?(\*|\d+)$").IsMatch(TargetTextbox.Text) == false) && TargetName == "Version")
                {
                    TargetLabel.Content = "Invalid version format.";
                    return;
                }

                if ((new Regex(@"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$").IsMatch(TargetTextbox.Text) == false) && TargetName == "Email")
                {
                    if (!string.IsNullOrEmpty(TargetTextbox.Text))
                    {
                        TargetLabel.Content = "Invalid email format.";
                        return;
                    }
                }

                if ((new Regex(@"^http(s)?://([\w-]+.)+[\w-]+(/[\w- ./?%&=])?$").IsMatch(TargetTextbox.Text) == false) && TargetName == "Website")
                {
                    if (!string.IsNullOrEmpty(TargetTextbox.Text))
                    {
                        TargetLabel.Content = "Invalid URL format.";
                        return;
                    }
                }

                TargetLabel.Content = "";
            }
        }

        private void InitializeEnv()
        {
            FeatureListBtnEnable(false);
            PrevilegeListBtnEnable(false);
            LabelListBtnEnable(false);
            DescriptionListBtnEnable(false);
            IconListBtnEnable(false);
            MetaListBtnEnable(false);
            PkgListBtnEnable(false);
            ControlListBtnEnable(false);
            AppControlListBtnEnable(false);
            ShortcutListBtnEnable(false);
            BgCategoryListBtnEnable(false);
            AccountListBtnEnable(false);
            SplashScreenListBtnEnable(false);
            advIconListBtnEnable(false);
            PlatformPathFactory();
        }

        private void PlatformPathFactory()
        {
            IViewModelTizen viewModel = DataContext as IViewModelTizen;
            ProfileType profile_type = viewModel.Profile.name;
            string platform_path = ToolsPathInfo.PlatformPath;
            platform_path += @"common\";
            this.FeaturePath = platform_path + "features\\feature-dotnet.properties";
            this.PrivilegePath = platform_path + "privileges\\privilege-dotnet.properties";
        }

        public void Refresh(IViewModelTizen viewModel)
        {
            DataContext = viewModel;
            string appType = GetAppType();
            UpdateModelAppType(viewModel, appType);
            UpdatPrivilegesGridColumns();
        }

        internal void DoIdle()
        {
            // only call the view model DoIdle if this control has focus
            // otherwise, we should skip and this will be called again
            // once focus is regained
            IViewModelTizen viewModel = DataContext as IViewModelTizen;
            if (viewModel != null && this.IsKeyboardFocusWithin)
            {
                viewModel.DoIdle();
            }
        }

        private void ViewModelChanged(object sender, EventArgs e)
        {
            // this gets called when the view model is updated because the Xml Document was updated
            // since we don't get individual PropertyChanged events, just re-set the DataContext
        }

        private void FeatureAddBtn_Click(object sender, RoutedEventArgs e)
        {
            PlatformPathFactory();
            if (System.IO.File.Exists(FeaturePath) == false)
            {
                MessageBox.Show("Please install baseline SDK to use feature list");
                return;
            }

            IViewModelTizen viewModel = DataContext as IViewModelTizen;
            var list = viewModel.FeatureField;
            AddFeatureWizard fWizard = new AddFeatureWizard(FeaturePath, list);
            if (fWizard.ShowDialog() == true)
            {
                string featureName = fWizard.SelectedFeature;
                var fList = AddFeatureWizard.GetFeatureList(FeaturePath);
                var originFeature = fList.FirstOrDefault
                                        (x => x.featureName.Equals(featureName));
                if (originFeature != null)
                {
                    feature newFeature = new feature();
                    newFeature.name = featureName;


                        newFeature.Value = fWizard.SelectedOption;


                    if (!list.Any(f => f.name == newFeature.name))
                    {
                        list.Add(newFeature);
                    }

                    viewModel.FeatureField = list;
                    DataContext = null;
                    DataContext = viewModel;
                }

                if (FeatureListView.SelectedItem == null)
                {
                    FeatureListBtnEnable(false);
                }
            }
        }

        private void FeatureRemoveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.FeatureListView.SelectedItems != null)
            {
                IViewModelTizen viewModel = DataContext as IViewModelTizen;
                var list = viewModel.FeatureField;

                string name = (this.FeatureListView.SelectedItem as feature).name;

                foreach (var selectItem in this.FeatureListView.SelectedItems)
                {
                    foreach(var item in list)
                    {
                        if (item.name.Equals((selectItem as feature).name))
                        {
                            list.Remove(item);
                            break;
                        }
                    }
                }

                viewModel.FeatureField = list;
                DataContext = null;
                DataContext = viewModel;
            }

            if (FeatureListView.Items.Count == 0 || FeatureListView.SelectedItem == null)
            {
                FeatureListBtnEnable(false);
            }
        }

        private void FeatureListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.FeatureListView.SelectedItem != null)
            {
                PlatformPathFactory();
                latestSelect = this.FeatureListView.SelectedItem as feature;
                string selectItemString =
                    (this.FeatureListView.SelectedItem as feature).name;

                var fList = AddFeatureWizard.GetFeatureList(FeaturePath);
                if (fList == null)
                {
                    this.featureDescBlock.Text = string.Empty;
                    return;
                }

                var selectFeature = fList.FirstOrDefault
                                        (x => x.featureName == selectItemString);

                if (selectFeature != null
                    && string.IsNullOrEmpty(selectFeature.featureDesc) == false)
                {
                    this.featureDescBlock.Text = selectFeature.featureDesc;

                    if ((selectFeature.optionList == null
                        || selectFeature.optionList.Count == 0) == false)
                    {
                        this.FeatureListView.SelectionChanged
                            -= FeatureListView_SelectionChanged;
                        this.FeatureListView.SelectedItem = latestSelect;
                        this.FeatureListView.SelectionChanged
                            += FeatureListView_SelectionChanged;
                    }
                }
                else
                {
                    this.featureDescBlock.Text = string.Empty;
                }
            }
            else
            {
                this.featureDescBlock.Text = string.Empty;
            }

            FeatureListBtnEnable(true);
        }

        private void FeatureListBtnEnable(bool status)
        {
            this.FeatureRemoveBtn.IsEnabled = status;
        }

        private void PrivilegeAddBtn_Click(object sender, RoutedEventArgs e)
        {
            PlatformPathFactory();
            if (System.IO.File.Exists(PrivilegePath) == false)
            {
                MessageBox.Show("Please install baseline SDK to use privilege list");
                return;
            }

            IViewModelTizen viewModel = DataContext as IViewModelTizen;
            var providerPrivList = viewModel.AppdefprivilegeList;
            var platformPrivList = viewModel.DefaultprivilegeList;
            var consumerPrivList = viewModel.ConsumerappdefprivilegeList;

            AddPrivilegeWizard privilegeWindow = new AddPrivilegeWizard(PrivilegePath, platformPrivList, viewModel.Package, this.dte);
            if (privilegeWindow.ShowDialog() == true)
            {
                if (privilegeWindow.isAppDefined == true)
                {
                    ComboBoxItem selectedCombobox = (ComboBoxItem)privilegeWindow.combobox_appdef.SelectedItem;
                    if (selectedCombobox.Content.ToString() == "Provider")
                    {
                        appdefprivilege appdef_provider = new appdefprivilege();
                        if (privilegeWindow.combobox_license.SelectedValue != null && privilegeWindow.checkbox_appdef.IsChecked == true)
                        {
                            appdef_provider.License = privilegeWindow.combobox_license.SelectedValue.ToString();
                        }

                        if (!string.IsNullOrEmpty(privilegeWindow.textbox_appdef_privileges.Text))
                        {
                            appdef_provider.Value = privilegeWindow.textbox_appdef_privileges.Text;
                        }

                        if (providerPrivList.Find(x => x.Value == appdef_provider.Value && x.License == appdef_provider.License) == null)
                        {
                            providerPrivList.Add(appdef_provider);
                        }

                        viewModel.AppdefprivilegeList = providerPrivList;
                        DataContext = null;
                        DataContext = viewModel;
                    }
                    else if (selectedCombobox.Content.ToString() == "Consumer")
                    {
                        appdefprivilege appdef_consumer = new appdefprivilege();
                        if (privilegeWindow.combobox_license.SelectedValue != null && privilegeWindow.checkbox_appdef.IsChecked == true)
                        {
                            appdef_consumer.License = privilegeWindow.combobox_license.SelectedValue.ToString();
                        }

                        if (!string.IsNullOrEmpty(privilegeWindow.textbox_appdef_privileges.Text))
                        {
                            appdef_consumer.Value = privilegeWindow.textbox_appdef_privileges.Text;
                        }

                        if (consumerPrivList.Find(x => x.Value == appdef_consumer.Value && x.License == appdef_consumer.License) == null)
                        {
                            consumerPrivList.Add(appdef_consumer);
                        }

                        viewModel.ConsumerappdefprivilegeList = consumerPrivList;
                        DataContext = null;
                        DataContext = viewModel;
                    }
                }
                else
                {
                    if (privilegeWindow.selectPrivilegeList.Count > 0)
                    {
                        var arr = privilegeWindow.selectPrivilegeList.ToArray();
                        foreach (var item in arr)
                        {
                            if (platformPrivList.Contains(item) == false)
                            {
                                platformPrivList.Add(item);
                            }
                        }

                        viewModel.DefaultprivilegeList = platformPrivList;
                        DataContext = null;
                        DataContext = viewModel;
                    }
                }

                foreach (GridViewColumn Column in Priv_Gridview.Columns)
                {
                    Column.Width = 0;
                    Column.Width = double.NaN;
                }

                if (PrivilegeListView.SelectedItem == null)
                {
                    PrevilegeListBtnEnable(false);
                }
            }
        }

        private void PrevilegeRemoveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.PrivilegeListView.SelectedItems == null)
            {
                return;
            }
            var list = this.PrivilegeListView.SelectedItems.Cast<string>().ToList();
            IViewModelTizen viewModel = DataContext as IViewModelTizen;

            var providerPrivList = viewModel.AppdefprivilegeList;
            var platformPrivList = viewModel.DefaultprivilegeList;
            var consumerPrivList = viewModel.ConsumerappdefprivilegeList;

            foreach (var item in list)
            {
                if (platformPrivList.Contains(item))
                {
                    platformPrivList.Remove(item);
                }
                else
                {
                    string[] parse = item.Split(' ');
                    if (parse.Length == 2)
                    {
                        if (providerPrivList.Find(x => x.Value == parse[1]) != null && parse[0] == "[App-defined-provider]")
                        {
                            providerPrivList.Remove(providerPrivList.Find(x => x.Value == parse[1]));
                        }
                        else if (consumerPrivList.Find(x => x.Value == parse[1]) != null && parse[0] == "[App-defined-consumer]")
                        {
                            consumerPrivList.Remove(consumerPrivList.Find(x => x.Value == parse[1]));
                        }
                    }
                    else if (parse.Length == 4 && parse[2].Equals("License:"))
                    {
                        if (providerPrivList.Find(x => x.Value == parse[1] && x.License == parse[3]) != null && parse[0] == "[App-defined-provider]")
                        {
                            providerPrivList.Remove(providerPrivList.Find(x => x.Value == parse[1] && x.License == parse[3]));
                        }
                        else if (consumerPrivList.Find(x => x.Value == parse[1] && x.License == parse[3]) != null && parse[0] == "[App-defined-consumer]")
                        {
                            consumerPrivList.Remove(consumerPrivList.Find(x => x.Value == parse[1] && x.License == parse[3]));
                        }
                    }
                }
            }

            viewModel.ConsumerappdefprivilegeList = consumerPrivList;
            viewModel.DefaultprivilegeList = platformPrivList;
            viewModel.AppdefprivilegeList = providerPrivList;
            DataContext = null;
            DataContext = viewModel;

            foreach (GridViewColumn Column in Priv_Gridview.Columns)
            {
                Column.Width = 0;
                Column.Width = double.NaN;
            }

            if (PrivilegeListView.Items.Count == 0 || PrivilegeListView.SelectedItem == null)
            {
                PrevilegeListBtnEnable(false);
            }
        }

        private void PrivilegeListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.PrivilegeListView.SelectedItem != null)
            {
                PlatformPathFactory();
                string selectedItemString = this.PrivilegeListView.SelectedItem as string;

                var pList = AddPrivilegeWizard.GetPrivilegeList(PrivilegePath);
                if (pList == null)
                {
                    this.featureDescBlock.Text = string.Empty;
                    return;
                }

                var selectPrivilege = pList.FirstOrDefault(x => x.privilegeName == selectedItemString);

                if (selectPrivilege != null && string.IsNullOrEmpty(selectPrivilege.privilegeDesc) == false)
                {
                    this.privilegeDescBlock.Text = selectPrivilege.privilegeDesc;
                }
                else
                {
                    this.privilegeDescBlock.Text = string.Empty;
                }
            }
            else
            {
                this.privilegeDescBlock.Text = string.Empty;
            }

            PrevilegeListBtnEnable(true);
        }

        private void PrevilegeListBtnEnable(bool status)
        {
            this.PrevilegeRemoveBtn.IsEnabled = status;
        }

        private void AddLabelBtn_Click(object sender, RoutedEventArgs e)
        {
            IViewModelTizen viewModel = DataContext as IViewModelTizen;
            var list = viewModel.LocalizationLabels;
            List<string> ExistList = new List<string>();
            foreach (label Item in list)
            {
                ExistList.Add(Item.lang);
            }

            LocalizationWizard lWizard = new LocalizationWizard("Name", ExistList: ExistList);
            if (lWizard.ShowDialog() == true)
            {
                var label = new label();
                label.lang = lWizard.LangComboBox.Text;
                label.Text = lWizard.ElementNameTextBox.Text.Trim().Split('\n');
                list.Add(label);
                viewModel.LocalizationLabels = list;
                DataContext = null;
                DataContext = viewModel;
            }
        }

        private void RemoveLabelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.localLabel.SelectedItem != null)
            {
                IViewModelTizen viewModel = DataContext as IViewModelTizen;
                var list = viewModel.LocalizationLabels;
                list.RemoveAt(this.localLabel.SelectedIndex);
                viewModel.LocalizationLabels = list;
                DataContext = null;
                DataContext = viewModel;
            }

            if (localLabel.Items.Count == 0 || localLabel.SelectedItem == null)
            {
                LabelListBtnEnable(false);
            }
        }

        private void EditLabelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.localLabel.SelectedItem != null)
            {
                var item = this.localLabel.SelectedItem as label;
                IViewModelTizen viewModel = DataContext as IViewModelTizen;
                var list = viewModel.LocalizationLabels;
                List<string> ExistList = new List<string>();
                foreach (label Item in list)
                {
                    ExistList.Add(Item.lang);
                }

                LocalizationWizard lWizard
                    = new LocalizationWizard("Label", item.lang, item.Text[0], ExistList: ExistList);
                if (lWizard.ShowDialog() == true)
                {
                    list.RemoveAt(localLabel.SelectedIndex);
                    item.lang = lWizard.LangComboBox.Text;
                    item.Text = lWizard.ElementNameTextBox.Text.Trim().Split('\n');
                    list.Add(item);
                    viewModel.LocalizationLabels = list;
                    DataContext = null;
                    DataContext = viewModel;
                }
            }

            if (localLabel.Items.Count == 0 || localLabel.SelectedItem == null)
            {
                LabelListBtnEnable(false);
            }

            this.AddLabelBtn.Focus();
        }

        private void localLabel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LabelListBtnEnable(true);
        }

        private void LabelListBtnEnable(bool status)
        {
            this.RemoveLabelBtn.IsEnabled = status;
            this.EditLabelBtn.IsEnabled = status;
        }

        private void AddDescriptionBtn_Click(object sender, RoutedEventArgs e)
        {
            IViewModelTizen viewModel = DataContext as IViewModelTizen;
            var list = viewModel.LocalizationDescriptions;
            List<string> ExistList = new List<string>();
            foreach (description Item in list)
            {
                ExistList.Add(Item.lang);
            }

            LocalizationWizard lWizard = new LocalizationWizard("Description", ExistList: ExistList);
            if (lWizard.ShowDialog() == true)
            {
                var descrption = new description();
                descrption.lang = lWizard.LangComboBox.Text;
                descrption.Text = lWizard.ElementNameTextBox.Text.Trim();
                list.Add(descrption);
                viewModel.LocalizationDescriptions = list;
                DataContext = null;
                DataContext = viewModel;
            }
        }

        private void RemoveDescriptionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.localDescription.SelectedItem != null)
            {
                IViewModelTizen viewModel = DataContext as IViewModelTizen;
                var list = viewModel.LocalizationDescriptions;
                list.RemoveAt(this.localDescription.SelectedIndex);
                viewModel.LocalizationDescriptions = list;
                DataContext = null;
                DataContext = viewModel;
            }

            if (localDescription.Items.Count == 0 || localDescription.SelectedItem == null)
            {
                DescriptionListBtnEnable(false);
            }
        }

        private void EditDescriptionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.localDescription.SelectedItem != null)
            {
                var item = this.localDescription.SelectedItem as description;
                IViewModelTizen viewModel = DataContext as IViewModelTizen;
                var list = viewModel.LocalizationDescriptions;
                List<string> ExistList = new List<string>();
                foreach (description Item in list)
                {
                    ExistList.Add(Item.lang);
                }

                LocalizationWizard lWizard = new LocalizationWizard("Description", item.lang, item.Text, ExistList: ExistList);
                if (lWizard.ShowDialog() == true)
                {
                    list.RemoveAt(this.localDescription.SelectedIndex);
                    item.lang = lWizard.LangComboBox.Text;
                    item.Text = lWizard.ElementNameTextBox.Text.Trim();
                    list.Add(item);
                    viewModel.LocalizationDescriptions = list;
                    DataContext = null;
                    DataContext = viewModel;
                }
            }

            if (localDescription.Items.Count == 0 || localDescription.SelectedItem == null)
            {
                DescriptionListBtnEnable(false);
            }

            this.AddDescriptionBtn.Focus();
        }

        private void localDescription_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DescriptionListBtnEnable(true);
        }

        private void DescriptionListBtnEnable(bool status)
        {
            this.RemoveDescriptionBtn.IsEnabled = status;
            this.EditDescriptionBtn.IsEnabled = status;
        }

        private void AddIconBtn_Click(object sender, RoutedEventArgs e)
        {
            IViewModelTizen viewModel = DataContext as IViewModelTizen;
            var list = viewModel.LocalizationIcons;
            List<string> ExistList = new List<string>();
            foreach (icon Item in list)
            {
                ExistList.Add(Item.lang);
            }

            LocalizationWizard lWizard = new LocalizationWizard("Icon", ExistList: ExistList);
            if (lWizard.ShowDialog() == true)
            {
                var icon = new icon();
                icon.lang = lWizard.LangComboBox.Text;
                icon.Text = lWizard.ElementNameTextBox.Text.Trim().Split('\n');
                list.Add(icon);
                viewModel.LocalizationIcons = list;
                DataContext = null;
                DataContext = viewModel;
            }
        }

        private void RemoveIconBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.localIcon.SelectedItem != null)
            {
                IViewModelTizen viewModel = DataContext as IViewModelTizen;
                var list = viewModel.LocalizationIcons;
                list.RemoveAt(this.localIcon.SelectedIndex);
                viewModel.LocalizationIcons = list;
                DataContext = null;
                DataContext = viewModel;
            }

            if (localIcon.Items.Count == 0 || localIcon.SelectedItem == null)
            {
                IconListBtnEnable(false);
            }
        }

        private void EditIconBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.localIcon.SelectedItem != null)
            {
                var item = this.localIcon.SelectedItem as icon;
                IViewModelTizen viewModel = DataContext as IViewModelTizen;
                var list = viewModel.LocalizationIcons;
                List<string> ExistList = new List<string>();
                foreach (icon Item in list)
                {
                    ExistList.Add(Item.lang);
                }

                LocalizationWizard lWizard = new LocalizationWizard("Icon", item.lang, item.Text[0], ExistList: ExistList);
                if (lWizard.ShowDialog() == true)
                {
                    list.RemoveAt(this.localIcon.SelectedIndex);
                    item.lang = lWizard.LangComboBox.Text;
                    item.Text = lWizard.ElementNameTextBox.Text.Trim().Split('\n');
                    list.Add(item);
                    viewModel.LocalizationIcons = list;
                    DataContext = null;
                    DataContext = viewModel;
                }
            }

            if (localIcon.Items.Count == 0 || localIcon.SelectedItem == null)
            {
                IconListBtnEnable(false);
            }

            this.AddIconBtn.Focus();
        }

        private void localIcon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IconListBtnEnable(true);
        }

        private void IconListBtnEnable(bool status)
        {
            if (this.RemoveIconBtn != null)
            {
                this.RemoveIconBtn.IsEnabled = status;
            }
            if (this.EditIconBtn != null)
            {
                this.EditIconBtn.IsEnabled = status;
            }
        }

        private void AddMetaBtn_Click(object sender, RoutedEventArgs e)
        {
            IViewModelTizen viewModel = DataContext as IViewModelTizen;
            var list = viewModel.AdvanceMetaList;
            AddMetaDataWizard addMetaWizard = new AddMetaDataWizard(ExistList: list);
            if (addMetaWizard.ShowDialog() == true)
            {
                metadata input = new metadata();
                input.key = addMetaWizard.keyTextBox.Text.Trim();
                input.value = addMetaWizard.valueTextBox.Text.Trim();
                list.Add(input);
                viewModel.AdvanceMetaList = list;
                DataContext = null;
                DataContext = viewModel;
            }
        }

        private void RemoveMetaBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.metaListBox.SelectedItem != null)
            {
                IViewModelTizen viewModel = DataContext as IViewModelTizen;
                var list = viewModel.AdvanceMetaList;
                list.RemoveAt(this.metaListBox.SelectedIndex);
                viewModel.AdvanceMetaList = list;
                DataContext = null;
                DataContext = viewModel;
            }

            if (metaListBox.Items.Count == 0 || metaListBox.SelectedItem == null)
            {
                MetaListBtnEnable(false);
            }
        }

        private void EditMetaBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.metaListBox.SelectedItem != null)
            {
                IViewModelTizen viewModel = DataContext as IViewModelTizen;
                var list = viewModel.AdvanceMetaList;
                var item = this.metaListBox.SelectedItem as metadata;
                AddMetaDataWizard addMetaWizard = new AddMetaDataWizard(item.key, item.value, ExistList: list);
                if (addMetaWizard.ShowDialog() == true)
                {
                    list.RemoveAt(this.metaListBox.SelectedIndex);
                    item.key = addMetaWizard.keyTextBox.Text.Trim();
                    item.value = addMetaWizard.valueTextBox.Text.Trim();
                    list.Add(item);
                    viewModel.AdvanceMetaList = list;
                    DataContext = null;
                    DataContext = viewModel;
                }
            }

            if (metaListBox.Items.Count == 0 || metaListBox.SelectedItem == null)
            {
                MetaListBtnEnable(false);
            }

            this.AddMetaBtn.Focus();
        }

        private void metaListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MetaListBtnEnable(true);
        }

        private void MetaListBtnEnable(bool status)
        {
            this.EditMetaBtn.IsEnabled = status;
            this.RemoveMetaBtn.IsEnabled = status;
        }

        private void AddControlBtn_Click(object sender, RoutedEventArgs e)
        {
            IViewModelTizen viewModel = DataContext as IViewModelTizen;
            if (string.IsNullOrEmpty(viewModel.ApplicationID))
            {
                MessageBox.Show("Can not find app id value.");
                return;
            }

            AddDataControlWizard addDataControlWizard
                = new AddDataControlWizard(PrivilegePath, appid:viewModel.ApplicationID, 
                privilegeList:viewModel.DefaultprivilegeList, appdefprivList:viewModel.AppdefprivilegeList);

            if (addDataControlWizard.ShowDialog() == true)
            {
                List<string> privList = new List<string>();
                if (addDataControlWizard.AddedPrivilegeList != null)
                {
                    foreach (var item in addDataControlWizard.AddedPrivilegeList)
                    {
                        privList.Add(item);
                    }
                }

                datacontrol input = new datacontrol
                {
                    providerid = addDataControlWizard.providerIDTxtBox.Text.Trim(),
                    type = addDataControlWizard.GetTypeString(),
                    access = addDataControlWizard.GetAccessString(),
                    trusted = addDataControlWizard.GetTrustedcheck(),
                    privilegeList = privList
                };
                var list = viewModel.AdvanceDataControlList;
                list.Add(input);
                viewModel.AdvanceDataControlList = list;
                DataContext = null;
                DataContext = viewModel;
            }
        }

        private void RemoveControlBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.dataControlListBox.SelectedItem != null)
            {
                IViewModelTizen viewModel = DataContext as IViewModelTizen;
                var list = viewModel.AdvanceDataControlList;
                list.RemoveAt(this.dataControlListBox.SelectedIndex);
                viewModel.AdvanceDataControlList = list;
                DataContext = null;
                DataContext = viewModel;
            }

            if (dataControlListBox.Items.Count == 0 || dataControlListBox.SelectedItem == null)
            {
                ControlListBtnEnable(false);
            }
        }

        private void EditControlBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.dataControlListBox.SelectedItem != null)
            {
                var item = this.dataControlListBox.SelectedItem as datacontrol;
                IViewModelTizen viewModel = DataContext as IViewModelTizen;
                AddDataControlWizard addDataControlWizard = new AddDataControlWizard(PrivilegePath, dataControl: item, 
                    privilegeList: viewModel.DefaultprivilegeList, appdefprivList: viewModel.AppdefprivilegeList);
                if (addDataControlWizard.ShowDialog() == true)
                {                    
                    var list = viewModel.AdvanceDataControlList;
                    list.RemoveAt(this.dataControlListBox.SelectedIndex);

                    List<string> privList = new List<string>();
                    if (addDataControlWizard.AddedPrivilegeList != null)
                    {
                        foreach (var privitem in addDataControlWizard.AddedPrivilegeList)
                        {
                            privList.Add(privitem);
                        }
                    }

                    datacontrol input = new datacontrol
                    {
                        providerid = addDataControlWizard.providerIDTxtBox.Text.Trim(),
                        type = addDataControlWizard.GetTypeString(),
                        access = addDataControlWizard.GetAccessString(),
                        trusted = addDataControlWizard.GetTrustedcheck(),
                        privilegeList = privList
                    };

                    list.Add(input);
                    viewModel.AdvanceDataControlList = list;
                    DataContext = null;
                    DataContext = viewModel;
                }
            }

            if (dataControlListBox.Items.Count == 0 || dataControlListBox.SelectedItem == null)
            {
                ControlListBtnEnable(false);
            }

            this.AddControlBtn.Focus();
        }

        private void dataControlListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ControlListBtnEnable(true);
        }

        private void ControlListBtnEnable(bool status)
        {
            this.EditControlBtn.IsEnabled = status;
            this.RemoveControlBtn.IsEnabled = status;
        }

        private void AddAppControlBtn_Click(object sender, RoutedEventArgs e)
        {
            IViewModelTizen viewModel = DataContext as IViewModelTizen;
            AddAppControlWizard addAppControlWizard = new AddAppControlWizard(privilegePath:PrivilegePath, privilegeList: viewModel.DefaultprivilegeList, appdefprivList: viewModel.AppdefprivilegeList, apiVersion : viewModel.ApiVersion);
            if (addAppControlWizard.ShowDialog() == true)
            {
                List<string> privList = new List<string>();
                if (addAppControlWizard.AddedPrivilegeList != null)
                {
                    foreach (var privitem in addAppControlWizard.AddedPrivilegeList)
                    {
                        privList.Add(privitem);
                    }
                }

                appcontrol input = new appcontrol();

                operation op = new operation();
                op.name = addAppControlWizard.operationTextBox.Text.Trim();

                uri ur = new uri();
                ur.name = addAppControlWizard.UriTextBox.Text.Trim();

                mime mi = new mime();
                mi.name = addAppControlWizard.mimeTextBox.Text.Trim();

                visibility vi = new visibility();
                vi.name = addAppControlWizard.visibilityValue;

                input.appControlId = addAppControlWizard.idTextBox.Text.Trim();
                if (string.IsNullOrEmpty(input.appControlId))
                {
                    input.appControlId = null;
                }

                input.Items = new object[4] { op, ur, mi, vi };
                input.privilegeList = privList;

                var list = viewModel.AdvanceAppControlList;
                list.Add(input);
                viewModel.AdvanceAppControlList = list;
                DataContext = null;
                DataContext = viewModel;
            }
        }

        private void RemoveAppControlBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.appContorlListBox.SelectedItem != null)
            {
                IViewModelTizen viewModel = DataContext as IViewModelTizen;
                var list = viewModel.AdvanceAppControlList;
                list.RemoveAt(this.appContorlListBox.SelectedIndex);
                viewModel.AdvanceAppControlList = list;
                DataContext = null;
                DataContext = viewModel;
            }

            if (appContorlListBox.Items.Count == 0 || appContorlListBox.SelectedItem == null)
            {
                AppControlListBtnEnable(false);
            }
        }

        private void EditAppControlBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.appContorlListBox.SelectedItem != null)
            {
                var item = this.appContorlListBox.SelectedItem as appcontrol;

                string op = string.Empty;
                string ur = string.Empty;
                string mi = string.Empty;
                string vi = string.Empty;
                string id = string.Empty;

                if (item.Items != null)
                {
                    foreach (var appItem in item.Items)
                    {
                        if (appItem is operation)
                        {
                            op = (appItem as operation).name;
                        }
                        else if (appItem is uri)
                        {
                            ur = (appItem as uri).name;
                        }
                        else if (appItem is mime)
                        {
                            mi = (appItem as mime).name;
                        } else if (appItem is visibility)
                        {
                            vi = (appItem as visibility).name;
                        }
                    }
                    id = item.appControlId;
                }
                
                IViewModelTizen viewModel = DataContext as IViewModelTizen;
                AddAppControlWizard wizard = new AddAppControlWizard(PrivilegePath, privilegeList: viewModel.DefaultprivilegeList, appdefprivList: viewModel.AppdefprivilegeList, ExistList: item.privilegeList, op:op, uri:ur, mime:mi, visibility:vi, id:id, apiVersion:viewModel.ApiVersion);

                if (wizard.ShowDialog() == true)
                {                    
                    var list = viewModel.AdvanceAppControlList;

                    list.RemoveAt(this.appContorlListBox.SelectedIndex);

                    List<string> privList = new List<string>();
                    if (wizard.AddedPrivilegeList != null)
                    {
                        foreach (var privitem in wizard.AddedPrivilegeList)
                        {
                            privList.Add(privitem);
                        }
                    }

                    operation op1 = new operation();
                    op1.name = wizard.operationTextBox.Text.Trim();

                    uri ur1 = new uri();
                    ur1.name = wizard.UriTextBox.Text.Trim();

                    mime mi1 = new mime();
                    mi1.name = wizard.mimeTextBox.Text.Trim();

                    visibility vi1 = new visibility();
                    vi1.name = wizard.visibilityValue;

                    item.appControlId = wizard.idTextBox.Text.Trim();

                    item.Items = new object[4] { op1, ur1, mi1, vi1 };
                    item.privilegeList = privList;
                    list.Add(item);

                    viewModel.AdvanceAppControlList = list;
                    DataContext = null;
                    DataContext = viewModel;
                }
            }

            if (appContorlListBox.Items.Count == 0 || appContorlListBox.SelectedItem == null)
            {
                AppControlListBtnEnable(false);
            }

            this.AddAppControlBtn.Focus();
        }

        private void appContorlListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AppControlListBtnEnable(true);
        }

        private void AppControlListBtnEnable(bool status)
        {
            this.EditAppControlBtn.IsEnabled = status;
            this.RemoveAppControlBtn.IsEnabled = status;
        }

        private void imgButton_Click(object sender, RoutedEventArgs e)
        {
            IconChooserWizard iWizard = new IconChooserWizard(this.dte);
            string IconPath = null;
            string PrevIconPath = this.TBIcon.Text;
            if (iWizard.ShowDialog() == true)
            {
                IconPath = SetIconPreview(iWizard.selectImageValue);
                if (IconPath != null)
                {
                    if (!IconPath.Equals(PrevIconPath))
                    {
                        foreach (string addPath in iWizard.fList)
                        {
                            //IncludeInSolutionExplorer(System.IO.Path.GetDirectoryName(dte.ActiveDocument.Path) + "\\shared\\res\\" + addPath, false);
                        }

                        IViewModelTizen viewModel = DataContext as IViewModelTizen;
                        if (viewModel != null)
                        {
                            viewModel.Icon = IconPath;
                            DataContext = null;
                            DataContext = viewModel;
                        }
                    }
                }
            }
        }

        //public void IncludeInSolutionExplorer(string filePath, bool isNeedCopy = false)
        //{
        //    VsProjectHelper ProjectHelper = VsProjectHelper.GetInstance;
        //    var Projects = ProjectHelper.GetProjects();
        //    var CurrentProject = Projects.DTE.ActiveDocument.ProjectItem.ContainingProject;
        //    if (isNeedCopy)
        //    {
        //        CurrentProject?.ProjectItems?.AddFromFileCopy(filePath);
        //    }
        //    else
        //    {
        //        CurrentProject?.ProjectItems?.AddFromFile(filePath);
        //    }
        //    CurrentProject?.Save();
        //}

        private string SetIconPreview(string FileName)
        {
            string IconPath = FileName;
            try
            {
                PreviewIconPath = System.IO.Path.GetDirectoryName(dte.ActiveDocument.Path) + "\\shared\\res\\" + FileName;
                BitmapImage previewImage = new BitmapImage();
                previewImage.BeginInit();
                previewImage.UriSource = new Uri(PreviewIconPath);
                previewImage.CacheOption = BitmapCacheOption.OnLoad;
                previewImage.EndInit();
                this.imagePreview.Source = previewImage;
                if (this.TBIcon.Text.Equals(FileName) == false)
                {
                    IconPath = FileName;
                    ChangePropertyValue(this.TBIcon, true);
                }
            }
            catch
            {
                this.imagePreview.Source = null;
                IconPath = "";
                ChangePropertyValue(this.TBIcon, true);
            }

            return IconPath;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            SetIconPreview(this.TBIcon.Text);
        }

        private void ChangePropertyValue(TextBox tb, bool IsForceChange = false)
        {
            if (tb == null)
            {
                return;
            }

            string propertyName = string.Empty;

            try
            {
                if (tb.Name?.StartsWith("TB") == true)
                {
                    propertyName = tb.Name?.Substring(2);
                }
            }
            catch
            {
            }

            if (propertyName == string.Empty)
            {
                return;
            }

            IViewModelTizen viewModel = DataContext as IViewModelTizen;
            if (viewModel != null && (this.IsKeyboardFocusWithin || IsForceChange))
            {
                try
                {
                    var propertyInfo = viewModel.GetType()?.GetProperty(propertyName);
                    propertyInfo.SetValue(viewModel, tb.Text);
                }
                catch
                {
                }
            }
        }

        private void AddShortcutBtn_Click(object sender, RoutedEventArgs e)
        {
            ShortcutListWizard sWizard = new ShortcutListWizard(this.dte, Appid: TBApplicationID.Text);
            if (sWizard.ShowDialog() == true)
            {
                shortcut input = new shortcut();
                input.appid = sWizard.textBox_appid.Text.Trim();
                input.extra_key = sWizard.textBox_key.Text.Trim();
                input.extra_data = sWizard.textBox_data.Text.Trim();

                if (!string.IsNullOrEmpty(sWizard.textBox_icon.Text))
                {
                    icon defaultIcon = new icon();
                    defaultIcon.Text = sWizard.textBox_icon.Text.Trim().Split('\n');
                    input.icon = defaultIcon;
                }

                if (!string.IsNullOrEmpty(sWizard.textBox_defaultlabel.Text))
                {
                    label defaultLable = new label();
                    defaultLable.Text = sWizard.textBox_defaultlabel.Text.Trim().Split('\n');
                    input.Items.Add(defaultLable);
                }

                foreach (label LanguageList in sWizard.LanguageList)
                {
                    input.Items.Add(LanguageList);
                }

                IViewModelTizen viewModel = DataContext as IViewModelTizen;
                var shourcutList = viewModel.ShortcutList;
                if (shourcutList == null)
                {
                    shourcutList = new List<shortcut>();
                }

                shourcutList.Add(input);

                viewModel.ShortcutList = shourcutList;
                DataContext = null;
                DataContext = viewModel;
            }
        }

        private void EditShortcutBtn_Click(object sender, RoutedEventArgs e)
        {
            var SelectedList = this.shortcutlListBox.SelectedItem as shortcut;
            ShortcutListWizard sWizard = new ShortcutListWizard(this.dte, Appid: TBApplicationID.Text, Modi: SelectedList);
            if (sWizard.ShowDialog() == true)
            {
                IViewModelTizen viewModel = DataContext as IViewModelTizen;
                var list = viewModel.ShortcutList;
                list.RemoveAt(this.shortcutlListBox.SelectedIndex);

                shortcut input = new shortcut();
                input.appid = sWizard.textBox_appid.Text.Trim();
                input.extra_key = sWizard.textBox_key.Text.Trim();
                input.extra_data = sWizard.textBox_data.Text.Trim();

                if (!string.IsNullOrEmpty(sWizard.textBox_icon.Text))
                {
                    icon defaultIcon = new icon();
                    defaultIcon.Text = sWizard.textBox_icon.Text.Trim().Split('\n');
                    input.icon = defaultIcon;
                }

                if (!string.IsNullOrEmpty(sWizard.textBox_defaultlabel.Text))
                {
                    label defaultLable = new label();
                    defaultLable.Text = sWizard.textBox_defaultlabel.Text.Trim().Split('\n');
                    input.Items.Add(defaultLable);
                }

                foreach (label LanguageList in sWizard.LanguageList)
                {
                    if (LanguageList.lang != null)
                    {
                        input.Items.Add(LanguageList);
                    }
                }

                list.Add(input);
                viewModel.ShortcutList = list;
                DataContext = null;
                DataContext = viewModel;

                if (shortcutlListBox.Items.Count == 0 || shortcutlListBox.SelectedItem == null)
                {
                    ShortcutListBtnEnable(false);
                }

                this.AddShortcutBtn.Focus();
            }
        }

        private void RemoveShortcutBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.shortcutlListBox.SelectedItem != null)
            {
                IViewModelTizen viewModel = DataContext as IViewModelTizen;
                var list = viewModel.ShortcutList;
                list.RemoveAt(this.shortcutlListBox.SelectedIndex);
                viewModel.ShortcutList = list;
                DataContext = null;
                DataContext = viewModel;
            }

            if (shortcutlListBox.Items.Count == 0 || shortcutlListBox.SelectedItem == null)
            {
                ShortcutListBtnEnable(false);
            }
        }

        private void shortcutlListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShortcutListBtnEnable(true);
        }

        private void ShortcutListBtnEnable(bool status)
        {
            this.EditShortcutBtn.IsEnabled = status;
            this.RemoveShortcutBtn.IsEnabled = status;
        }

        private void AddBgCategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            IViewModelTizen viewModel = DataContext as IViewModelTizen;
            var BgcategoryList = viewModel.BackgroundCategoryList;
            if (BgcategoryList == null)
            {
                BgcategoryList = new List<background>();
            }

            BackgroundCategoryWizard bWizard = new BackgroundCategoryWizard(bList: BgcategoryList);
            if (bWizard.ShowDialog() == true)
            {
                background input = new background();
                input.value = bWizard.SelectedItem;
                BgcategoryList.Add(input);
                viewModel.BackgroundCategoryList = BgcategoryList;
                DataContext = null;
                DataContext = viewModel;
            }
        }

        private void RemoveBgCategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.BgCategoryListBox.SelectedItem != null)
            {
                IViewModelTizen viewModel = DataContext as IViewModelTizen;
                var list = viewModel.BackgroundCategoryList;
                list.RemoveAt(this.BgCategoryListBox.SelectedIndex);
                viewModel.BackgroundCategoryList = list;
                DataContext = null;
                DataContext = viewModel;
            }

            if (BgCategoryListBox.Items.Count == 0 || BgCategoryListBox.SelectedItem == null)
            {
                BgCategoryListBtnEnable(false);
            }
        }

        private void EditBgCategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            IViewModelTizen viewModel = DataContext as IViewModelTizen;
            var BgcategoryList = viewModel.BackgroundCategoryList;
            var SelectedList = this.BgCategoryListBox.SelectedItem as string;
            BackgroundCategoryWizard bWizard = new BackgroundCategoryWizard(bList: BgcategoryList);
            if (bWizard.ShowDialog() == true)
            {
                background input = new background();
                BgcategoryList.RemoveAt(this.BgCategoryListBox.SelectedIndex);
                input.value = bWizard.SelectedItem;
                BgcategoryList.Add(input);
                viewModel.BackgroundCategoryList = BgcategoryList;
                DataContext = null;
                DataContext = viewModel;
            }

            if (BgCategoryListBox.Items.Count == 0 || BgCategoryListBox.SelectedItem == null)
            {
                BgCategoryListBtnEnable(false);
            }

            this.AddBgCategoryBtn.Focus();
        }

        private void BgCategoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BgCategoryListBtnEnable(true);
        }

        private void BgCategoryListBtnEnable(bool status)
        {
            this.EditBgCategoryBtn.IsEnabled = status;
            this.RemoveBgCategoryBtn.IsEnabled = status;
        }

        private void AddAccountBtn_Click(object sender, RoutedEventArgs e)
        {
            AccountWizard aWizard = new AccountWizard(this.dte, Appid: TBApplicationID.Text);
            if (aWizard.ShowDialog() == true)
            {
                account input = new account();
                input.accountprovider.appid = TBApplicationID.Text.Trim();
                input.accountprovider.multipleaccounts = aWizard.comboBox_multipleaccount.Text.Trim();
                input.accountprovider.providerid = aWizard.textBox_providerid.Text.Trim();

                if (!string.IsNullOrEmpty(aWizard.textBox_icon.Text))
                {
                    icon Icon = new icon();
                    Icon.Text = aWizard.textBox_icon.Text.Trim().Split('\n');
                    Icon.section = "account";
                    input.accountprovider.icon = Icon.Text[0];
                    input.accountprovider.iconlist.Add(Icon);
                }

                if (!string.IsNullOrEmpty(aWizard.textBox_iconsmall.Text))
                {
                    icon Icon = new icon();
                    Icon.Text = aWizard.textBox_iconsmall.Text.Trim().Split('\n');
                    Icon.section = "account-small";
                    input.accountprovider.iconsmall = Icon.Text[0];
                    input.accountprovider.iconlist.Add(Icon);
                }

                if (!string.IsNullOrEmpty(aWizard.textBox_defaultlabel.Text))
                {
                    label defaultLable = new label();
                    defaultLable.Text = aWizard.textBox_defaultlabel.Text.Trim().Split('\n');
                    input.accountprovider.Items.Add(defaultLable);
                }

                foreach (label LanguageList in aWizard.LanguageList)
                {
                    input.accountprovider.Items.Add(LanguageList);
                }

                foreach (string CapabilitiesList in aWizard.CapabilitiesList)
                {
                    input.accountprovider.capability.Add(CapabilitiesList);
                }

                IViewModelTizen viewModel = DataContext as IViewModelTizen;
                var AccountList = viewModel.AccountField;
                if (AccountList == null)
                {
                    AccountList = new List<account>();
                }

                AccountList.Add(input);
                viewModel.AccountField = AccountList;
                DataContext = null;
                DataContext = viewModel;
            }
        }

        private void EditAccountBtn_Click(object sender, RoutedEventArgs e)
        {
            var SelectedList = this.accountlListBox.SelectedItem as account;
            AccountWizard aWizard = new AccountWizard(this.dte, Appid: TBApplicationID.Text, Modi: SelectedList);
            if (aWizard.ShowDialog() == true)
            {
                IViewModelTizen viewModel = DataContext as IViewModelTizen;
                var list = viewModel.AccountField;
                list.RemoveAt(this.accountlListBox.SelectedIndex);

                account input = new account();
                input.accountprovider.appid = TBApplicationID.Text.Trim();
                input.accountprovider.multipleaccounts = aWizard.comboBox_multipleaccount.Text.Trim();
                input.accountprovider.providerid = aWizard.textBox_providerid.Text.Trim();
                input.accountprovider.iconlist.Clear();

                if (!string.IsNullOrEmpty(aWizard.textBox_icon.Text))
                {
                    icon Icon = new icon();
                    Icon.Text = aWizard.textBox_icon.Text.Trim().Split('\n');
                    Icon.section = "account";
                    input.accountprovider.icon = Icon.Text[0];
                    input.accountprovider.iconlist.Add(Icon);
                }

                if (!string.IsNullOrEmpty(aWizard.textBox_iconsmall.Text))
                {
                    icon Icon = new icon();
                    Icon.Text = aWizard.textBox_iconsmall.Text.Trim().Split('\n');
                    Icon.section = "account-small";
                    input.accountprovider.iconsmall = Icon.Text[0];
                    input.accountprovider.iconlist.Add(Icon);
                }

                if (!string.IsNullOrEmpty(aWizard.textBox_defaultlabel.Text))
                {
                    label defaultLable = new label();
                    defaultLable.Text = aWizard.textBox_defaultlabel.Text.Trim().Split('\n');
                    input.accountprovider.Items.Add(defaultLable);
                }

                foreach (label LanguageList in aWizard.LanguageList)
                {
                    input.accountprovider.Items.Add(LanguageList);
                }

                foreach (string CapabilitiesList in aWizard.CapabilitiesList)
                {
                    input.accountprovider.capability.Add(CapabilitiesList);
                }

                list.Add(input);
                viewModel.AccountField = list;
                DataContext = null;
                DataContext = viewModel;

                if (accountlListBox.Items.Count == 0 || accountlListBox.SelectedItem == null)
                {
                    AccountListBtnEnable(false);
                }

                this.AddAccountBtn.Focus();
            }
        }

        private void RemoveAccountBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.accountlListBox.SelectedItem != null)
            {
                IViewModelTizen viewModel = DataContext as IViewModelTizen;
                var list = viewModel.AccountField;
                list.RemoveAt(this.accountlListBox.SelectedIndex);
                viewModel.AccountField = list;
                DataContext = null;
                DataContext = viewModel;
            }

            if (accountlListBox.Items.Count == 0 || accountlListBox.SelectedItem == null)
            {
                AccountListBtnEnable(false);
            }
        }

        private void accountlListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AccountListBtnEnable(true);
        }

        private void AccountListBtnEnable(bool status)
        {
            this.EditAccountBtn.IsEnabled = status;
            this.RemoveAccountBtn.IsEnabled = status;
        }

        private void comboBox_apiVersion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            /*IViewModelTizen viewModel = DataContext as IViewModelTizen;
            var apiVersion = viewModel.ApiVersion;
            //this.comboBox_apiVersion.SelectedItem = apiVersion;
            viewModel.ApiVersion = apiVersion;
            DataContext = null;
            DataContext = viewModel;*/
        }

        private void AddSplashScreenBtn_Click(object sender, RoutedEventArgs e)
        {
            SplashScreenWizard sWizard = new SplashScreenWizard(this.dte);
            if (sWizard.ShowDialog() == true)
            {
                splashscreen input = new splashscreen();
                input.type = sWizard.comboBox_ResourceType.Text.Trim();
                input.dpi = sWizard.comboBox_Resolution.Text.Trim();
                input.orientation = sWizard.comboBox_Orientation.Text.Trim();
                input.indicatordisplay = sWizard.comboBox_IndicatorDisplay.Text.Trim();
                input.src = sWizard.textBox_source.Text.Trim();
                input.appcontroloperation = sWizard.textBox_AppcontrolOp.Text.Trim();

                IViewModelTizen viewModel = DataContext as IViewModelTizen;
                var splashScreenList = viewModel.SplashscreenList;
                if (splashScreenList == null)
                {
                    splashScreenList = new List<splashscreen>();
                }

                splashScreenList.Add(input);
                viewModel.SplashscreenList = splashScreenList;
                DataContext = null;
                DataContext = viewModel;
            }
        }

        private void RemoveSplashScreenBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.splashscreenListBox.SelectedItem != null)
            {
                IViewModelTizen viewModel = DataContext as IViewModelTizen;
                var splashScreenList = viewModel.SplashscreenList;
                splashScreenList.RemoveAt(this.splashscreenListBox.SelectedIndex);
                viewModel.SplashscreenList = splashScreenList;
                DataContext = null;
                DataContext = viewModel;
            }

            if (splashscreenListBox.Items.Count == 0 || splashscreenListBox.SelectedItem == null)
            {
                SplashScreenListBtnEnable(false);
            }
        }

        private void EditSplashScreenBtn_Click(object sender, RoutedEventArgs e)
        {
            var SelectedList = this.splashscreenListBox.SelectedItem as splashscreen;
            SplashScreenWizard sWizard = new SplashScreenWizard(this.dte, Modi: SelectedList);
            if (sWizard.ShowDialog() == true)
            {
                IViewModelTizen viewModel = DataContext as IViewModelTizen;
                var splashScreenList = viewModel.SplashscreenList;
                splashScreenList.RemoveAt(this.splashscreenListBox.SelectedIndex);

                splashscreen input = new splashscreen();
                input.type = sWizard.comboBox_ResourceType.Text.Trim();
                input.dpi = sWizard.comboBox_Resolution.Text.Trim();
                input.orientation = sWizard.comboBox_Orientation.Text.Trim();
                input.indicatordisplay = sWizard.comboBox_IndicatorDisplay.Text.Trim();
                input.src = sWizard.textBox_source.Text.Trim();
                input.appcontroloperation = sWizard.textBox_AppcontrolOp.Text.Trim();

                splashScreenList.Add(input);
                viewModel.SplashscreenList = splashScreenList;
                DataContext = null;
                DataContext = viewModel;

                if (splashscreenListBox.Items.Count == 0 || splashscreenListBox.SelectedItem == null)
                {
                    SplashScreenListBtnEnable(false);
                }

                this.AddSplashScreenBtn.Focus();
            }
        }

        private void splashscreenListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SplashScreenListBtnEnable(true);
        }

        private void SplashScreenListBtnEnable(bool status)
        {
            this.EditSplashScreenBtn.IsEnabled = status;
            this.RemoveSplashScreenBtn.IsEnabled = status;
        }

        private void AddadvIconBtn_Click(object sender, RoutedEventArgs e)
        {
            IViewModelTizen viewModel = DataContext as IViewModelTizen;
            var ExistList = viewModel.LocalizationIcons;
            MultiResolutionIconWizard mWizard = new MultiResolutionIconWizard(this.dte, ExistList: ExistList);
            if (mWizard.ShowDialog() == true)
            {
                icon newIcon = new icon();
                if (mWizard.comboBox_language.Text == "default")
                {
                    newIcon.lang = null;
                }
                else
                {
                    newIcon.lang = mWizard.comboBox_language.Text;
                }

                if (mWizard.comboBox_resolution.Text == "default")
                {
                    newIcon.resolutionSpecified = false;
                    newIcon.resolution = null;
                }
                else
                {
                    newIcon.resolutionSpecified = true;
                    newIcon.resolution = mWizard.comboBox_resolution.Text;
                }

                if (newIcon.resolution == null && newIcon.lang == null)
                {
                    MessageBox.Show("Default icon will be changed.");
                    viewModel.Icon = SetIconPreview(mWizard.listView.SelectedItem.ToString());
                    DataContext = null;
                    DataContext = viewModel;
                }
                else
                {
                    newIcon.Text = mWizard.listView.SelectedItem.ToString().Split('\n');
                    ExistList.Add(newIcon);
                    viewModel.LocalizationIcons = ExistList;
                    DataContext = null;
                    DataContext = viewModel;
                }
            }
        }

        private void RemoveadvIconBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.advIconListBox.SelectedItem != null)
            {
                IViewModelTizen viewModel = DataContext as IViewModelTizen;
                var LocalizationIconList = viewModel.LocalizationIcons;
                LocalizationIconList.RemoveAt(this.advIconListBox.SelectedIndex);
                viewModel.LocalizationIcons = LocalizationIconList;
                DataContext = null;
                DataContext = viewModel;
            }

            if (advIconListBox.Items.Count == 0 || advIconListBox.SelectedItem == null)
            {
                advIconListBtnEnable(false);
            }
        }

        private void EditadvIconBtn_Click(object sender, RoutedEventArgs e)
        {
            var SelectedList = this.advIconListBox.SelectedItem as icon;
            IViewModelTizen viewModel = DataContext as IViewModelTizen;
            var ExistList = viewModel.LocalizationIcons;
            MultiResolutionIconWizard mWizard = new MultiResolutionIconWizard(this.dte, ExistList: ExistList, Modi: SelectedList);
            if (mWizard.ShowDialog() == true)
            {
                ExistList.RemoveAt(this.advIconListBox.SelectedIndex);
                icon newIcon = new icon();
                if (mWizard.comboBox_language.Text == "default")
                {
                    newIcon.lang = null;
                }
                else
                {
                    newIcon.lang = mWizard.comboBox_language.Text;
                }

                if (mWizard.comboBox_resolution.Text == "default")
                {
                    newIcon.resolutionSpecified = false;
                    newIcon.resolution = null;
                }
                else
                {
                    newIcon.resolutionSpecified = true;
                    newIcon.resolution = mWizard.comboBox_resolution.Text;
                }

                if (newIcon.resolution == null && newIcon.lang == null)
                {
                    MessageBox.Show("Default icon will be changed.");
                    viewModel.Icon = SetIconPreview(mWizard.listView.SelectedItem.ToString());
                    DataContext = null;
                    DataContext = viewModel;
                }
                else
                {
                    newIcon.Text = mWizard.listView.SelectedItem.ToString().Split('\n');
                    ExistList.Add(newIcon);
                    viewModel.LocalizationIcons = ExistList;
                    DataContext = null;
                    DataContext = viewModel;
                }

                if (advIconListBox.Items.Count == 0 || advIconListBox.SelectedItem == null)
                {
                    advIconListBtnEnable(false);
                }

                this.AddadvIconBtn.Focus();
            }
        }

        private void advIconListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            advIconListBtnEnable(true);
        }

        private void advIconListBtnEnable(bool status)
        {
            this.EditadvIconBtn.IsEnabled = status;
            this.RemoveadvIconBtn.IsEnabled = status;
        }

        private void comboBox_profileType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IViewModelTizen viewModel = DataContext as IViewModelTizen;
            var profileType = viewModel.Profile;

            var input = new profile();
            input.name = (ProfileType)Enum.Parse(typeof(ProfileType), comboBox_profileType.SelectedItem.ToString());

            viewModel.Profile = input;
            DataContext = null;
            DataContext = viewModel;
            this.PlatformPathFactory();
        }

        private void comboBox_profileType_Loaded(object sender, RoutedEventArgs e)
        {
            IViewModelTizen viewModel = DataContext as IViewModelTizen;
            var profileType = viewModel.Profile;

            comboBox_profileType.Text = profileType.name.ToString();
            this.PlatformPathFactory();
        }

        private void AddPkgBtn_Click(object sender, RoutedEventArgs e)
        {
            IViewModelTizen viewModel = DataContext as IViewModelTizen;
            var pkgList = viewModel.AdvancePkgList;
            if (pkgList == null)
            {
                pkgList = new List<packages>();
            }
            
            DependencyWizard DWizard = new DependencyWizard("Add Dependency");
            if (DWizard.ShowDialog() == true)
            {
                var package = new packages();
                package.type = DWizard.TypeComboBox.Text;
                package.package = DWizard.packageIDTextBox.Text.Trim();
                package.requiredVersion = DWizard.requiredVersionTextBox.Text.Trim();
                pkgList.Add(package);
                viewModel.AdvancePkgList = pkgList;
                DataContext = null;
                DataContext = viewModel;
            }
        }

        private void RemovePkgBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.pkgListBox.SelectedItem != null)
            {
                IViewModelTizen viewModel = DataContext as IViewModelTizen;
                var list = viewModel.AdvancePkgList;
                list.RemoveAt(this.pkgListBox.SelectedIndex);
                viewModel.AdvancePkgList = list;
                DataContext = null;
                DataContext = viewModel;
            }

            if (pkgListBox.Items.Count == 0 || pkgListBox.SelectedItem == null)
            {
                PkgListBtnEnable(false);
            }
        }

        private void EditPkgBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.pkgListBox.SelectedItem != null)
            {
                IViewModelTizen viewModel = DataContext as IViewModelTizen;
                var list = viewModel.AdvancePkgList;
                var item = this.pkgListBox.SelectedItem as packages;
                DependencyWizard DWizard = new DependencyWizard("Edit Dependency", item.type, item.package, item.requiredVersion);
                if (DWizard.ShowDialog() == true)
                {
                    list.RemoveAt(this.pkgListBox.SelectedIndex);
                    item.type = DWizard.TypeComboBox.Text;
                    item.package = DWizard.packageIDTextBox.Text.Trim();
                    item.requiredVersion = DWizard.requiredVersionTextBox.Text.Trim();
                    list.Add(item);
                    viewModel.AdvancePkgList = list;
                    DataContext = null;
                    DataContext = viewModel;
                }
            }

            if (pkgListBox.Items.Count == 0 || pkgListBox.SelectedItem == null)
            {
                PkgListBtnEnable(false);
            }

            this.AddPkgBtn.Focus();
        }

        private void pkgListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PkgListBtnEnable(true);
        }

        private void PkgListBtnEnable(bool status)
        {
            this.EditPkgBtn.IsEnabled = status;
            this.RemovePkgBtn.IsEnabled = status;
        }
    }
}
