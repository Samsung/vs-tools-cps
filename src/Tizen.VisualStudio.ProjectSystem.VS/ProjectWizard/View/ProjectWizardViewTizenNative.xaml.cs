/*
 * Copyright 2018 (c) Samsung Electronics Co., Ltd  All Rights Reserved.
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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Tizen.VisualStudio.ProjectWizard.View
{
    public class TizenNativeSelector
    {
        public int tizenmode;
        public string profile;
        public string toolset;
        public string projectType;
        public string apiVersion;
        public string tizenApi;
    }

    /// <summary>
    /// Interaction logic for ProjectWizardViewTizenNative.xaml
    /// </summary>
    public partial class ProjectWizardViewTizenNative : Window
    {
        internal TizenNativeSelector data = new TizenNativeSelector();
        List<TizenNativeTemplate> nativeTemplates;

        public ProjectWizardViewTizenNative(string project_name, string project_path, List<TizenNativeTemplate> nativeTemplates)
        {
            this.nativeTemplates = nativeTemplates;

            InitializeComponent();

            FixLists(nativeTemplates);

            /* Default select value */

            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;
            label_get_projectname.Content = project_name;
            label_get_projectlocation.Content = project_path;
            PreviewKeyDown += new KeyEventHandler(KeyPressEvent);
        }

        private void FindOrAdd(List<string> list, string value)
        {
            foreach(string s in list)
            {
                if (s == value)
                    return;
            }
            list.Add(value);
        }

        private void FixLists(List<TizenNativeTemplate> nativeTemplates)
        {
            List<string> profiles = new List<string>();
            List<string> revisions = new List<string>();
            List<string> names = new List<string>();
            string library = null;

            foreach (TizenNativeTemplate t in nativeTemplates)
            {
                FindOrAdd(profiles, t.profile);
                FindOrAdd(revisions, t.version);
                FindOrAdd(names, t.name);
                if (t.name.ToLower().Contains("library"))
                    library = t.name;
            }
            profile_combobox.ItemsSource = new ObservableCollection<string>(profiles);
            revision_combobox.ItemsSource = new ObservableCollection<string>(revisions);
            project_type_combobox.ItemsSource = new ObservableCollection<string>(names);

            // Fix selection
            profile_combobox.Text = data.profile = profiles[0];
            revision_combobox.Text = data.tizenApi = revisions[0];
            project_type_combobox.Text = data.projectType = (library == null) ? names[0] : library;
        }

        private void KeyPressEvent(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Escape))
            {
                DialogResult = false;
            }
            else if (e.Key.Equals(Key.Enter))
            {
                ValidationChecker();
            }
        }

        private void Button_cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void Button_ok_Click(object sender, RoutedEventArgs e) => ValidationChecker();

        private void ValidationChecker()
        {
            // Check that data set is available
            foreach(TizenNativeTemplate t in nativeTemplates)
            {
                if (t.name == data.projectType && t.profile == data.profile && t.version == data.tizenApi)
                {
                    DialogResult = true;
                    return;
                }
            }
            MessageBox.Show("This combination of parameters is unsupported");
        }

        private void Profile_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string value = profile_combobox.SelectedValue as string;
            if (!string.IsNullOrEmpty(value))
                data.profile = value;
        }

        private void Toolset_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem cbi = toolset_combobox.SelectedValue as ComboBoxItem;
            data.toolset = cbi?.Name.ToString();
        }

        private void ProjectType_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string value = project_type_combobox.SelectedValue as string;
            if (!string.IsNullOrEmpty(value))
                data.projectType = value;
        }

        private void Revision_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string value = revision_combobox.SelectedValue as string;
            if (!string.IsNullOrEmpty(value))
                data.tizenApi = value;
        }

    }

}
