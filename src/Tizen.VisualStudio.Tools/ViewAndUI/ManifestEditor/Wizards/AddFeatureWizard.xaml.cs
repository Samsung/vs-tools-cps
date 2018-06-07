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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;

namespace Tizen.VisualStudio.ManifestEditor
{
    /// <summary>
    /// Interaction logic for AddFeatureWizard.xaml
    /// </summary>
    public partial class AddFeatureWizard : Window
    {
        private string toolPath;
        public string SelectedFeature;
        public string SelectedOption;
        private static List<FeatureSupporters> FeatureItems = new List<FeatureSupporters>();

        public AddFeatureWizard(string toolPath, List<feature> ExistList)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;
            InitializeComponent();
            this.toolPath = toolPath;
            FeatureItems.RemoveAll(x => x.featureName != null);
            LoadFeatureXml(toolPath);

            foreach (var f in FeatureItems)
            {
                if (!ExistList.Any(x => x.name == f.featureName))
                {
                    this.listView.Items.Add(f.featureName);
                }
            }

            OkBtnEnable(false);
        }

        public static List<FeatureSupporters> GetFeatureList(string path)
        {
            if (FeatureItems.Count == 0)
            {
                LoadFeatureXml(path);
            }

            return FeatureItems;
        }

        private static void LoadFeatureXml(string path)
        {
            if (System.IO.File.Exists(path) == false)
            {
                MessageBox.Show("Can not find [" + path + "]");
                return;
            }

            try
            {
                XDocument Features = XDocument.Load(path);
                foreach (XElement feature in Features.Descendants("Feature"))
                {
                    FeatureSupporters newItem = new FeatureSupporters();
                    newItem.featureName = feature.Attribute("Key").Value;
                    newItem.featureType = feature.Attribute("Type").Value;

                    if (newItem.featureType == "FT_BOOL")
                    {
                        newItem.featureDesc = (feature.Element("Option")).Attribute("Desc").Value;
                    }
                    else if (newItem.featureType == "FT_COMBO")
                    {
                        newItem.featureDesc = feature.Element("Description").Value;
                        int count = feature.Elements("Option").Count();
                        newItem.optionList = new List<string>();

                        var optionList = feature.Elements("Option");
                        foreach (var option in optionList)
                        {
                            if (option.Attribute("Default") != null)
                            {
                                newItem.defaultOption = option.Value;
                            }

                            newItem.optionList.Add(option.Value);
                        }
                    }

                    FeatureItems.Add(newItem);
                }
            }
            catch
            {
                MessageBox.Show("The format of xml file is not supported");
            }
        }

        private void button_ok_Click(object sender, RoutedEventArgs e)
        {
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
            this.textBlock.Text = "";
            this.listView.Items.Clear();

            IEnumerable<FeatureSupporters> items =
                        from feature in FeatureItems
                        where feature.featureName.Contains(this.textBox_search.Text)
                        select feature;

            foreach (var f in items)
            {
                this.listView.Items.Add(f.featureName);
            }
        }

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.SelectedFeature = this.listView.SelectedItem as string;
            var obj = FeatureItems.FirstOrDefault(x => x.featureName == SelectedFeature);
            if (obj != null && obj.featureDesc != null)
            {
                this.textBlock.Text = obj.featureDesc;
            }

            if (obj != null && obj.featureType == "FT_COMBO")
            {
                foreach (string content in obj.optionList)
                {
                    Combobox_Option.Items.Add(content);
                }

                Combobox_Option.SelectedItem = obj.defaultOption;
                Option_Panel.Visibility = Visibility.Visible;
            }
            else
            {
                SelectedOption = "true";
                Option_Panel.Visibility = Visibility.Hidden;
            }


            if (listView.SelectedItem == null)
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

        private void Combobox_Option_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.SelectedOption = Combobox_Option.SelectedItem.ToString();
        }
    }

    public class FeatureSupporters
    {
        public string featureName;
        public string featureDesc;
        public string featureType;
        public List<string> optionList = null;
        public string defaultOption;
    }
}
