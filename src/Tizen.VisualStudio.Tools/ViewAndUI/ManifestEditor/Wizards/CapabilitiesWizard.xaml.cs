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

namespace Tizen.VisualStudio.ManifestEditor
{
    /// <summary>
    /// Interaction logic for BackgroundCategoryWizard.xaml
    /// </summary>
    public partial class CapabilitiesWizard : Window
    {
        private CollectionView View;
        private List<string> CapabilitiesList = new List<string>();
        private List<string> CapabilitiesCategoryList = new List<string>
        {
            "http://tizen.org/account/capability/contact" ,
            "http://tizen.org/account/capability/calendar" ,
            "http://tizen.org/account/capability/email",
            "http://tizen.org/account/capability/photo",
            "http://tizen.org/account/capability/video",
            "http://tizen.org/account/capability/music",
            "http://tizen.org/account/capability/document",
            "http://tizen.org/account/capability/message",
            "http://tizen.org/account/capability/game"
        };

        public string SelectedItem;

        public CapabilitiesWizard(string option = null, List<string> ExistList = null) // If option is null, add new category. If it isn't, modify the option category.
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;
            InitializeComponent();
            CapabilitiesList = ExistList;
            this.FilterCapabilitiesList();
            this.listView_capabilities.ItemsSource = CapabilitiesCategoryList;
            this.View = (CollectionView)CollectionViewSource.GetDefaultView(listView_capabilities.ItemsSource);
            this.View.Filter = UserFilter;
            this.listView_capabilities.SelectedIndex = 0;
            Okbutton_CheckEnable();
        }

        private void FilterCapabilitiesList()
        {
            foreach (string input in CapabilitiesList)
            {
                if (CapabilitiesCategoryList.Contains(input))
                {
                    CapabilitiesCategoryList.Remove(input);
                }
            }
        }

        private bool UserFilter(object item)
        {
            if (string.IsNullOrEmpty(textBox_capabilities.Text))
            {
                return true;
            }
            else
            {
                return ((item as string).IndexOf(textBox_capabilities.Text, StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }

        private void button_ok_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.SelectedItem = this.listView_capabilities.SelectedItem.ToString();
                this.DialogResult = true;
            }
            catch
            {
                this.DialogResult = false;
            }
        }

        private void button_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void textBox_capabilities_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(listView_capabilities.ItemsSource).Refresh();
            Okbutton_CheckEnable();
        }

        private void listView_capabilities_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Okbutton_CheckEnable();
        }

        private void Okbutton_CheckEnable()
        {
            if (listView_capabilities.SelectedItem == null)
            {
                button_ok.IsEnabled = false;
            }
            else
            {
                button_ok.IsEnabled = true;
            }
        }
    }
}
