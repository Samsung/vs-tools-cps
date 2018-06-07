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

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using Microsoft.WindowsAPICodePack;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Tizen.VisualStudio.ManifestEditor
{
    /// <summary>
    /// Interaction logic for AddPrivilegeWizard.xaml
    /// </summary>
    public partial class AddPrivilegeWizard : Window, INotifyPropertyChanged
    {
        private string toolPath;
        public string SelectedFeature;
        public string PkgID;
        public bool isAppDefined;
        private string ProjectPath, LicensePath;
        private string NewLicenseString = "New License...";


        public static List<PrivilegeSupporters> PrivilegeItems = new List<PrivilegeSupporters>();
        public List<string> selectPrivilegeList = new List<string>();
        Regex http = new Regex(@"^http(s)?://([\w-]+.)+[\w-]+(/[\w- ./?%&=])?$");
        private EnvDTE.DTE dte;

        public AddPrivilegeWizard(string toolPath, List<string> ExistList, string PkgID, EnvDTE.DTE dte)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;
            InitializeComponent();
            this.PkgID = PkgID;
            this.toolPath = toolPath;
            isAppDefined = false;
            textbox_appdef_privileges.Text = "http://" + PkgID + "/appdefined/";
            textbox_appdef_privileges.TextChanged += radio_appdef_Checked;
            PrivilegeItems.RemoveAll(x => x.privilegeName != null);
            this.dte = dte;
            LoadPrivilegeXml(this.toolPath);

            if (ExistList == null)
            {
                ExistList = new List<string>();
            }

            foreach (var f in PrivilegeItems)
            {
                if (!ExistList.Contains(f.privilegeName))
                {
                    this.listView_internal.Items.Add(f.privilegeName);
                }
            }

            OkBtnEnable(false);
            radio_internal.IsChecked = true;
            radio_internal.Checked += new RoutedEventHandler(CheckInternalPrivilege);
            listView_internal.SelectionChanged += new SelectionChangedEventHandler(CheckInternalPrivilege);
            ProjectPath = System.IO.Path.GetDirectoryName(dte.ActiveDocument.Path);
            LicensePath = System.IO.Path.Combine(ProjectPath, "res\\.appdefined-license");
            GetLicenseFileList();
            this.DataContext = this;
        }

        private void GetLicenseFileList()
        {

            DirectoryInfo dInfo = new DirectoryInfo(LicensePath);
            if (!dInfo.Exists)
            {
                dInfo.Create();
            }

            if (!LicenseFileList.Contains(NewLicenseString))
            {
                LicenseFileList.Add(NewLicenseString);
            }
            
            FileInfo[] Files = dInfo.GetFiles();
            foreach (FileInfo file in Files)
            {
                if (!LicenseFileList.Contains(file.Name))
                {
                    LicenseFileList.Add(file.Name);
                }
            }

        }

        public static List<PrivilegeSupporters> GetPrivilegeList(string path)
        {
            if (PrivilegeItems.Count == 0)
            {
                LoadPrivilegeXml(path);
            }

            return PrivilegeItems;
        }

        private static void LoadPrivilegeXml(string path)
        {
            if (System.IO.File.Exists(path) == false)
            {
                MessageBox.Show("Can not find [" + path + "]");
                return;
            }

            try
            {
                var doc = new System.Xml.XmlDocument();
                doc.Load(path);

                var nodes = doc.SelectNodes("properties/entry");
                var arr = nodes.OfType<XmlNode>().ToArray();
                PrivilegeItems.Clear();

                foreach (var arrItem in arr)
                {
                    PrivilegeSupporters newItem = new PrivilegeSupporters();
                    newItem.privilegeName = arrItem.Attributes["key"].Value;
                    newItem.privilegeDesc = arrItem.Attributes["desc"].Value;
                    PrivilegeItems.Add(newItem);
                }
            }
            catch
            {
                MessageBox.Show("The format of xml file is not supported");
            }
        }

        private void button_ok_Click(object sender, RoutedEventArgs e)
        {
            if (radio_appdef.IsChecked == true)
            {
                isAppDefined = true;
            }
            else
            {
                isAppDefined = false;
            }

            this.DialogResult = true;
            this.Close();
        }

        private void button_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void textBox_search_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.listView_internal.Items.Clear();

            IEnumerable<PrivilegeSupporters> items =
                        from privileges in PrivilegeItems
                        where privileges.privilegeName.Contains(this.textBox_search.Text)
                        select privileges;

            foreach (var f in items)
            {
                this.listView_internal.Items.Add(f.privilegeName);
            }
        }

        private void CheckInternalPrivilege(object sender, EventArgs e)
        {
            this.selectPrivilegeList.Clear();
            var list = this.listView_internal.SelectedItems.Cast<string>().ToList();

            this.SelectedFeature = this.listView_internal.SelectedItem as string;
            var obj = PrivilegeItems.FirstOrDefault(x => x.privilegeName == SelectedFeature);

            if (list.Count > 0)
            {
                this.selectPrivilegeList.AddRange(list);
            }

            if (list.Count == 1 && obj != null && obj.privilegeDesc != null)
            {
                this.textBlock_description.Text = obj.privilegeDesc;
            }
            else
            {
                this.textBlock_description.Text = "";
            }

            if (listView_internal.SelectedItem == null)
            {
                OkBtnEnable(false);
            }
            else
            {
                OkBtnEnable(true);
            }
        }

        private void OkBtnEnable(bool status)
        {
            this.button_ok.IsEnabled = status;
        }

        private void button_browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fDialog = new OpenFileDialog();
            fDialog.DefaultExt = ".xml";
            fDialog.Filter = "XML FIles (*.xml)|*.xml";
            string XmlFilePath;
            if (fDialog.ShowDialog() == true)
            {
                XmlFilePath = fDialog.FileName;
                this.textbox_file.Text = XmlFilePath;
                if (System.IO.File.Exists(XmlFilePath) == false)
                {
                    MessageBox.Show("Can not found [" + XmlFilePath + "]");
                    return;
                }

                var doc = new System.Xml.XmlDocument();

                try
                {
                    doc.Load(XmlFilePath);

                    var nodes = doc.SelectNodes("properties/entry");

                    if(nodes.Count == 0)
                    {
                        OkBtnEnable(false);
                        this.textbox_file.Text = string.Empty;
                        MessageBox.Show("There is no privilege element");
                        return;
                    }

                    var arr = nodes.OfType<XmlNode>().ToArray();
                    this.selectPrivilegeList.Clear();

                    foreach (var arrItem in arr)
                    {
                        PrivilegeSupporters newItem = new PrivilegeSupporters();
                        newItem.privilegeName = arrItem.Attributes["key"].Value;
                        this.selectPrivilegeList.Add(newItem.privilegeName);
                    }
                    OkBtnEnable(true);
                }
                catch
                {
                    this.textbox_file.Text = string.Empty;
                    OkBtnEnable(false);
                    MessageBox.Show("The format of xml file is not supported");
                }
            }
        }

        private void radio_custom_Checked(object sender, RoutedEventArgs e)
        {
            if (http.IsMatch(textbox_custom_privileges.Text) == true)
            {
                this.selectPrivilegeList.Clear();
                this.selectPrivilegeList.Add(textbox_custom_privileges.Text);
                OkBtnEnable(true);
            }

            else
            {
                OkBtnEnable(false);
            }
        }

        private void radio_file_Checked(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(textbox_file.Text))
            {
                OkBtnEnable(false);
            }

            else
            {
                OkBtnEnable(true);
            }
        }

        private void radio_appdef_Checked(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(textbox_appdef_privileges.Text))
            {
                this.selectPrivilegeList.Clear();
                this.selectPrivilegeList.Add(textbox_appdef_privileges.Text);
                OkBtnEnable(true);
            }
            else
            {
                OkBtnEnable(false);
            }
        }

        private void checkbox_appdef_Unchecked(object sender, RoutedEventArgs e)
        {
            combobox_license.SelectedValue = "";
        }

        private ObservableCollection<string> LicenseFileListField;
        public ObservableCollection<string> LicenseFileList
        {
            get
            {
                if (LicenseFileListField == null)
                {
                    LicenseFileListField = new ObservableCollection<string>();
                }

                return LicenseFileListField;
            }

            set
            {
                this.LicenseFileListField = value;
                NotifyPropertyChanged();
            }
        }

        private string SelectedField;
        public string Selected
        {
            get
            {
                return SelectedField;
            }

            set
            {
                SelectedField = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void combobox_license_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string sourceFileName = "";
            if (NewLicenseString.Equals(Selected))
            {
                OpenFileDialog FileDialog = new OpenFileDialog();
                if (FileDialog.ShowDialog() == true)
                {
                    string sourceFilePath = FileDialog.FileName;
                    sourceFileName = System.IO.Path.GetFileName(sourceFilePath);
                    string destFilePath = System.IO.Path.Combine(LicensePath, sourceFileName);
                    if (File.Exists(destFilePath))
                    {
                        MessageBox.Show("Same file name exists, Please check your license file");
                    }
                    else
                    {
                        try
                        {
                            File.Copy(sourceFilePath, destFilePath);
                            LicenseFileList.Add(sourceFileName);
                            Selected = sourceFileName;
                        }
                        catch
                        {
                        }
                    }                    
                }
                else
                {
                    Selected = "";
                    combobox_license.SelectedValue = "";
                }
            }
        }

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
