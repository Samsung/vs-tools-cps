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
    /// Interaction logic for LocalizationWizard.xaml
    /// </summary>
    public partial class LocalizationWizard : Window
    {
        static Dictionary<string, string> languageDic
            = new Dictionary<string, string>();
        List<string> existList = new List<string>();

        public LocalizationWizard(string elementName,
                                  string languageName = null,
                                  string elementNameValue = null, List<string> ExistList = null)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;
            this.existList = ExistList;
            InitializeComponent();
            InItLocalSet();
            this.Title = elementName;
            this.elementTypeLabel.Content = elementName;
            if (languageName != null)
            {
                this.LangComboBox.Items.Add(languageName);
                if (this.LangComboBox.Items.Contains(languageName))
                {
                    this.LangComboBox.SelectedItem = languageName;
                }
            }

            if (elementNameValue != null)
            {
                this.ElementNameTextBox.Text = elementNameValue;
            }

            AddComboBoxChild();
            this.Loaded += LocalizationWizard_Loaded;
        }

        private void LocalizationWizard_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.ElementNameTextBox.Text == string.Empty)
            {
                this.OkBtn.IsEnabled = false;
            }
        }

        private void AddComboBoxChild()
        {
            foreach (var item in languageDic)
            {
                if (existList != null)
                {
                    if (existList.Contains(item.Key) == true)
                    {
                        continue;
                    }
                }

                this.LangComboBox.Items.Add(item.Key);
            }
        }

        private void InItLocalSet()
        {
            if (languageDic.Count == 0)
            {
                try
                {
                    XmlDocument xml = new XmlDocument();
                    var dirPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    xml.Load(dirPath + @"\ViewAndUI\Resources\lang_country_list.xml");
                    XmlNodeList xnList = xml.GetElementsByTagName("lang");
                    foreach (XmlNode xn in xnList)
                    {
                        var id = xn.Attributes.GetNamedItem("id");
                        var conturyName = xn.Attributes.GetNamedItem("name");
                        languageDic.Add(id.Value, conturyName.Value);
                    }
                }
                catch
                {
                }
            }
        }

        private void EnableCheckOKbtn()
        {
            if (this.ElementNameTextBox.Text != string.Empty && this.LangComboBox.SelectedItem != null)
            {
                this.OkBtn.IsEnabled = true;
            }
            else
            {
                this.OkBtn.IsEnabled = false;
            }
        }

        private void ElementNameTextBox_TextChanged(object sender,
                                                    TextChangedEventArgs e)
        {
            EnableCheckOKbtn();
        }

        private void LangComboBox_SelectionChanged(object sender,
                                                   SelectionChangedEventArgs e)
        {
            if (this.LangComboBox.SelectedItem != null)
            {
                string countryName = this.LangComboBox.SelectedItem as string;
                this.Countrylabel.Content = languageDic[countryName];
            }
            else
            {
                this.Countrylabel.Content = string.Empty;
            }

            EnableCheckOKbtn();
        }

        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
