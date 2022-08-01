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
    /// Interaction logic for AddAppControlWizard.xaml
    /// </summary>
    public partial class AddAppControlWizard : Window, INotifyPropertyChanged
    {
        private ObservableCollection<string> AddedPrivilegeListField;
        private ObservableCollection<string> supportPrivilegeListField;
        static List<string> VisibilityList = new List<string>() { "local-only", "remote-only", "both"};
        private String visibilityInfo;
        private string privilegePath;
        public List<PrivilegeSupporters> PrivilegeItems = new List<PrivilegeSupporters>();
        private string ApiVersion;

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

        public string visibilityValue
        {
            get
            {
                return visibilityInfo;
            }

            set
            {
                if (visibilityInfo != value)
                {
                    visibilityInfo = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private void CreateSupportPrivList(List<string> privilegeList, List<appdefprivilege> appdefprivList, List<string> existList = null)
        {
            foreach (var item in PrivilegeItems)
            {
                SupportPrivilegeList.Add(item.privilegeName);
            }

            foreach (var item in appdefprivList)
            {
                SupportPrivilegeList.Add(item.Value);
            }

            if (existList != null)
            {
                List<string> ExistList = existList;
                foreach (var item in ExistList)
                {
                    SupportPrivilegeList.Remove(item);
                }
            }
        }

        private void CreateAddedPrivList(List<string> existList = null)
        {
            if (existList != null)
            {
                List<string> ExistList = existList;
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
        public System.Windows.Visibility ApiVersionGreaterThanFive
        {
            get
            {
                float val = -1;
                float.TryParse(ApiVersion, out val);
                if ((val != -1 && val >= 5.5))
                {
                    return System.Windows.Visibility.Visible;
                }
                else
                {
                    return System.Windows.Visibility.Hidden;
                }
            }

            set
            {
            }
        }

        public AddAppControlWizard(string privilegePath, List<string> privilegeList, List<appdefprivilege> appdefprivList, List<string> ExistList = null, string op = null, string uri = null, string mime = null, string visibility = null, string id = null, string apiVersion = null)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;
            InitializeComponent();
            this.privilegePath = privilegePath;
            PlatformPrivilegeListCreator();
            CreateSupportPrivList(privilegeList, appdefprivList, ExistList);
            CreateAddedPrivList(ExistList);

            if (op != null)
            {
                this.operationTextBox.Text = op;
            }

            if (uri != null)
            {
                this.UriTextBox.Text = uri;
            }

            if (mime != null)
            {
                this.mimeTextBox.Text = mime;
            }

            if (visibility != null)
            {
                this.visibilityValue = visibility;
            }

            if (id != null)
            {
                this.idTextBox.Text  = id;
            }

            if (apiVersion != null)
            {
                this.ApiVersion = apiVersion;
            }


            AddVisibilityComboBoxChild();
            this.EnableCheckOKbtn();
            DataContext = this;
        }

        private void EnableCheckOKbtn()
        {
            if (string.IsNullOrEmpty(this.operationTextBox.Text) || string.IsNullOrEmpty(this.UriTextBox.Text) || string.IsNullOrEmpty(this.mimeTextBox.Text))
            {
                this.OKbutton.IsEnabled = false;
            }
            else
            {
                this.OKbutton.IsEnabled = true;
            }
        }

        private void OKbutton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Cancelbutton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void operationTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.EnableCheckOKbtn();
        }

        private void UriTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.EnableCheckOKbtn();
        }

        private void mimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.EnableCheckOKbtn();
        }

        private void idTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.EnableCheckOKbtn();
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
        private void AddVisibilityComboBoxChild()
        {
            foreach (var item in VisibilityList)
            {
                this.visibilityComboBox.Items.Add(item);
            }
        }
    }
}
