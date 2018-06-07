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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;

namespace Tizen.VisualStudio.ManifestEditor
{
    /// <summary>
    /// Interaction logic for AddDataControlWizard.xaml
    /// </summary>
    public partial class AddDataControlWizard : Window, INotifyPropertyChanged
    {
        static string providerIDFormet = "http://{0}/datacontrol/provider/";
        private string privilegePath;

        private ObservableCollection<string> AddedPrivilegeListField;
        private ObservableCollection<string> supportPrivilegeListField;
        public List<PrivilegeSupporters> PrivilegeItems = new List<PrivilegeSupporters>();
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<string> SupportPrivilegeList
        {
            get
            {
                if (supportPrivilegeListField == null)
                {
                    supportPrivilegeListField = new ObservableCollection<string>();
                }

                return supportPrivilegeListField;
            }

            set
            {
                this.supportPrivilegeListField = value;
                NotifyPropertyChanged();
            }
        }

        
        public ObservableCollection<string> AddedPrivilegeList
        {
            get
            {
                if (AddedPrivilegeListField == null)
                {
                    AddedPrivilegeListField = new ObservableCollection<string>();
                }

                return AddedPrivilegeListField;
            }

            set
            {
                this.AddedPrivilegeListField = value;
                NotifyPropertyChanged();
            }
        }

        static string CreateProvideText(string appid)
        {
            return string.Format(providerIDFormet, appid);
        }

        private void CreateSupportPrivList(List<string> privilegeList, List<appdefprivilege> appdefprivList, datacontrol dataControl = null)
        {
            foreach (var item in PrivilegeItems)
            {
                SupportPrivilegeList.Add(item.privilegeName);
            }

            foreach (var item in appdefprivList)
            {
                SupportPrivilegeList.Add(item.Value);
            }

            if (dataControl != null)
            {
                List<string> ExistList = dataControl.privilegeList;
                foreach (var item in ExistList)
                {
                    SupportPrivilegeList.Remove(item);
                }
            }
        }

        private void CreateAddedPrivList(datacontrol dataControl)
        {
            if (dataControl != null)
            {
                List<string> ExistList = dataControl.privilegeList;
                foreach (var item in ExistList)
                {
                    AddedPrivilegeList.Add(item);
                }
            }
        }

        private void PlatformPrivilegeListCreator()
        {
            if (System.IO.File.Exists(privilegePath) == false)
            {
                return;
            }

            try
            {
                var doc = new System.Xml.XmlDocument();
                doc.Load(privilegePath);

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
            }
        }

        public AddDataControlWizard(string privilegePath, string appid, List<string> privilegeList, List<appdefprivilege> appdefprivList)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;
            InitializeComponent();
            this.privilegePath = privilegePath;
            PlatformPrivilegeListCreator();
            this.providerIDTxtBox.Text = CreateProvideText(appid);
            Button_Enable_Checker();
            CreateSupportPrivList(privilegeList, appdefprivList);
            DataContext = this;
        }

        public AddDataControlWizard(string privilegePath, datacontrol dataControl, List<string> privilegeList, List<appdefprivilege> appdefprivList)
        {
            InitializeComponent();
            this.privilegePath = privilegePath;
            PlatformPrivilegeListCreator();
            CreateSupportPrivList(privilegeList, appdefprivList, dataControl);
            CreateAddedPrivList(dataControl);

            this.providerIDTxtBox.Text = dataControl.providerid;

            if (dataControl.type.Equals("Sql"))
            {
                this.SQLRadio.IsChecked = true;
            }
            else if (dataControl.type.Equals("Map"))
            {
                this.MapRadio.IsChecked = true;
            }

            if (dataControl.access.Contains("Read"))
            {
                this.ReadCheckBox.IsChecked = true;
            }

            if (dataControl.access.Contains("Write"))
            {
                this.WriteCheckBox.IsChecked = true;
            }

            if (dataControl.trusted == null)
            {
                this.TrustedCheckbox.IsChecked = false;
            }
            else
            {
                if (dataControl.trusted.Contains("true"))
                {
                    this.TrustedCheckbox.IsChecked = true;
                }
            }

            Button_Enable_Checker();
            DataContext = this;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void providerIDTxtBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Button_Enable_Checker();
        }

        public string GetAccessString()
        {
            if ((bool)WriteCheckBox.IsChecked && (bool)ReadCheckBox.IsChecked)
            {
                return "ReadWrite";
            }
            else if ((bool)WriteCheckBox.IsChecked)
            {
                return "WriteOnly";
            }
            else if ((bool)ReadCheckBox.IsChecked)
            {
                return "ReadOnly";
            }

            return string.Empty;
        }

        public string GetTypeString()
        {
            if ((bool)SQLRadio.IsChecked)
            {
                return "Sql";
            }
            else
            {
                return "Map";
            }
        }

        public string GetTrustedcheck()
        {
            if ((bool)TrustedCheckbox.IsChecked == true)
            {
                return "true";
            }
            else
            {
                return "false";
            }
        }

        private void typeCheckBox_Click(object sender, RoutedEventArgs e)
        {
            Button_Enable_Checker();
        }

        private void Button_add_Click(object sender, RoutedEventArgs e)
        {
            if (SupportedPrivList.SelectedItem != null)
            {
                var item = SupportedPrivList.SelectedItem.ToString();
                SupportPrivilegeList.Remove(item);
                AddedPrivilegeList.Add(item);
            }
        }

        private void Button_remove_Click(object sender, RoutedEventArgs e)
        {
            if (AddedPrivList.SelectedItem != null)
            {
                var item = AddedPrivList.SelectedItem.ToString();
                AddedPrivilegeList.Remove(item);
                SupportPrivilegeList.Add(item);
            }
        }

        private void Button_Enable_Checker()
        {
            if ((((bool)this.ReadCheckBox.IsChecked || (bool)this.WriteCheckBox.IsChecked)) && !string.IsNullOrEmpty(this.providerIDTxtBox.Text))
            {
                this.OKButton.IsEnabled = true;
            }
            else
            {
                this.OKButton.IsEnabled = false;
            }
        }
    }
}
