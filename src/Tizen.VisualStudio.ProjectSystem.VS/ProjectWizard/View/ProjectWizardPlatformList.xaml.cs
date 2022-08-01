/*
 * Copyright 2021 (c) Samsung Electronics Co., Ltd  All Rights Reserved.
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

namespace Tizen.VisualStudio.ProjectWizard.View
{
    public partial class ProjectWizardPlatformList : Window
    {
        string prjtype;
        string[] webprofiles = { "mobile-6.0", "wearable-6.0", "mobile-5.5", "wearable-5.5", "tv-samsung-6.0" };
        string[] dotnetprofiles = { "tizen-6.0", "tizen-5.5", "tizen-5.0", "tizen-4.0" };
        public ProjectWizardPlatformList(string type)
        {
            InitializeComponent();
            prjtype = type;
            if(prjtype == "web")
            {

                PopulateList(webprofiles);
            }
            else
            {

                PopulateList(dotnetprofiles);
            }
        }

        void button_Click(object sender, RoutedEventArgs e)
        {
            string tname = (string)(sender as Button).Content;

            // Show waiting type cursor till next Wizard Page is loaded.
            this.Cursor = System.Windows.Input.Cursors.Wait;
            this.Cursor = System.Windows.Input.Cursors.IBeam;
            this.Hide();
        }

        public void PopulateList(string[] arr)
        {
            foreach (string str in arr)
            {
                Button btn = new Button();
                this.profile_list.Children.Add(btn);
                btn.Content = str;
                btn.Height = 50;
                btn.Width = 200;
                btn.Click += new RoutedEventHandler(button_Click);
            }
        }

        private void Button_cancel_click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
