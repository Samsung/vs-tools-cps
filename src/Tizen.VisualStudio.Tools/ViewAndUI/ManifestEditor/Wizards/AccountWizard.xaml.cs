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

namespace Tizen.VisualStudio.ManifestEditor
{
    /// <summary>
    /// Interaction logic for AccountWizard.xaml
    /// </summary>
    public partial class AccountWizard : Window
    {
        private EnvDTE.DTE dte;
        public ObservableCollection<label> LanguageList { get; private set; }
        public ObservableCollection<string> CapabilitiesList { get; private set; }

        public AccountWizard(EnvDTE.DTE dte, account Modi = null, string Appid = null)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;
            InitializeComponent();
            this.dte = dte;
            LanguageList = new ObservableCollection<label>();
            CapabilitiesList = new ObservableCollection<string>();
            DataContext = this;
            LangListBtnEnable(false);
            CapaListBtnEnable(false);
            EnableCheckOKbtn();

            if (Modi != null)
            {
                this.comboBox_multipleaccount.Text = Modi.accountprovider.multipleaccounts;
                this.textBox_providerid.Text = Modi.accountprovider.providerid;
                
                this.textBox_icon.Text = Modi.accountprovider.icon;
                this.textBox_iconsmall.Text = Modi.accountprovider.iconsmall;

                foreach (label LabelList in Modi.accountprovider.Items)
                {
                    if (LabelList.lang != null)
                    {
                        LanguageList.Add(LabelList);
                    }
                    else
                    {
                        this.textBox_defaultlabel.Text = LabelList.Text[0];
                    }
                }

                foreach (string Capability in Modi.accountprovider.capability)
                {
                    CapabilitiesList.Add(Capability);
                }
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

        private void EnableCheckOKbtn()
        {
            if (string.IsNullOrEmpty(textBox_providerid.Text) || string.IsNullOrEmpty(textBox_defaultlabel.Text) || string.IsNullOrEmpty(textBox_icon.Text) || string.IsNullOrEmpty(textBox_iconsmall.Text) || string.IsNullOrEmpty(comboBox_multipleaccount.Text))
            {
                this.button_ok.IsEnabled = false;
            }
            else
            {
                this.button_ok.IsEnabled = true;
            }
        }

        private void button_browse_Click(object sender, RoutedEventArgs e)
        {
            IconChooserWizard iWizard = new IconChooserWizard(this.dte);
            if (iWizard.ShowDialog() == true)
            {
                this.textBox_icon.Text = iWizard.selectImageValue;
                EnableCheckOKbtn();
            }
        }

        private void button_browsesmall_Click(object sender, RoutedEventArgs e)
        {
            IconChooserWizard iWizard = new IconChooserWizard(this.dte);
            if (iWizard.ShowDialog() == true)
            {
                this.textBox_iconsmall.Text = iWizard.selectImageValue;
                EnableCheckOKbtn();
            }
        }

        private void button_add_Click(object sender, RoutedEventArgs e)
        {
            List<string> ExistList = new List<string>();
            foreach (label Item in LanguageList)
            {
                ExistList.Add(Item.lang);
            }

            LocalizationWizard LWizard = new LocalizationWizard("Name", ExistList: ExistList);
            if (LWizard.ShowDialog() == true)
            {
                var label = new label();
                label.lang = LWizard.LangComboBox.Text;
                label.Text = LWizard.ElementNameTextBox.Text.Trim().Split('\n');
                LanguageList.Add(label);
            }
        }

        private void button_modify_Click(object sender, RoutedEventArgs e)
        {
            string LanguageName = (listview_locallabel.SelectedItem as label).lang;
            string ElementNameValue = (listview_locallabel.SelectedItem as label).Text[0];
            List<string> ExistList = new List<string>();
            foreach (label Item in LanguageList)
            {
                ExistList.Add(Item.lang);
            }

            LocalizationWizard LWizard = new LocalizationWizard("Name", LanguageName, ElementNameValue, ExistList: ExistList);

            if (LWizard.ShowDialog() == true)
            {
                if (LanguageList.Contains((listview_locallabel.SelectedItem as label)))
                {
                    LanguageList.Remove((listview_locallabel.SelectedItem as label));
                }

                var label = new label();
                label.lang = LWizard.LangComboBox.Text;
                label.Text = LWizard.ElementNameTextBox.Text.Trim().Split('\n');
                LanguageList.Add(label);
            }

            if (listview_locallabel.Items.Count == 0 || listview_locallabel.SelectedItem == null)
            {
                LangListBtnEnable(false);
            }
        }

        private void button_delete_Click(object sender, RoutedEventArgs e)
        {
            if (LanguageList.Contains((listview_locallabel.SelectedItem as label)))
            {
                LanguageList.Remove((listview_locallabel.SelectedItem as label));
            }

            if (listview_locallabel.Items.Count == 0 || listview_locallabel.SelectedItem == null)
            {
                LangListBtnEnable(false);
            }
        }

        private void listview_locallabel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LangListBtnEnable(true);
        }

        private void LangListBtnEnable(bool status)
        {
            button_modify.IsEnabled = status;
            button_delete.IsEnabled = status;
        }

        private void comboBox_multipleaccount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EnableCheckOKbtn();
        }

        private void textBox_providerid_TextChanged(object sender, TextChangedEventArgs e)
        {
            EnableCheckOKbtn();
        }

        private void textBox_defaultlabel_TextChanged(object sender, TextChangedEventArgs e)
        {
            EnableCheckOKbtn();
        }

        private void button_add_capa_Click(object sender, RoutedEventArgs e)
        {
            var list = CapabilitiesList;
            List<string> ExistList = new List<string>();
            foreach (string Item in list)
            {
                ExistList.Add(Item);
            }

            CapabilitiesWizard CWizard = new CapabilitiesWizard(ExistList: ExistList);
            if (CWizard.ShowDialog() == true)
            {
                string input = CWizard.SelectedItem;
                CapabilitiesList.Add(input);
            }
        }

        private void button_modify_capa_Click(object sender, RoutedEventArgs e)
        {
            string capability = (listview_capabilities.SelectedItem as string);
            var list = CapabilitiesList;
            List<string> ExistList = new List<string>();
            foreach (string Item in list)
            {
                ExistList.Add(Item);
            }

            CapabilitiesWizard CWizard = new CapabilitiesWizard(ExistList: ExistList);
            if (CWizard.ShowDialog() == true)
            {
                if (CapabilitiesList.Contains((listview_capabilities.SelectedItem as string)))
                {
                    CapabilitiesList.Remove((listview_capabilities.SelectedItem as string));
                }

                string input = CWizard.SelectedItem;
                CapabilitiesList.Add(input);
            }

            if (listview_capabilities.Items.Count == 0 || listview_capabilities.SelectedItem == null)
            {
                CapaListBtnEnable(false);
            }
        }

        private void button_delete_capa_Click(object sender, RoutedEventArgs e)
        {
            if (CapabilitiesList.Contains((listview_capabilities.SelectedItem as string)))
            {
                CapabilitiesList.Remove((listview_capabilities.SelectedItem as string));
            }

            if (listview_capabilities.Items.Count == 0 || listview_capabilities.SelectedItem == null)
            {
                CapaListBtnEnable(false);
            }
        }

        private void CapaListBtnEnable(bool status)
        {
            button_modify_capa.IsEnabled = status;
            button_delete_capa.IsEnabled = status;
        }

        private void listview_capabilities_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CapaListBtnEnable(true);
        }
    }
}
