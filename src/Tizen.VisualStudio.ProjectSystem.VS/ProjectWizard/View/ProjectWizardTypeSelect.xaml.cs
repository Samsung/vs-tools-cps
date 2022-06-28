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
    public partial class ProjectWizardTypeSelect : Window
    {
        public ProjectWizardTypeSelect()
        {
            InitializeComponent();
        }

        private void Button_cancel_click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_type_click(object sender, RoutedEventArgs e)
        {
            string btn_name = (string)(sender as Button).Name;
            string prjtype = "dotnet";
            if (btn_name == "button_dotnet")
            {
                prjtype = "dotnet";
            } else if(btn_name == "button_web")
            {
                prjtype = "web";
            }
            var typeWindow = new ProjectWizardPlatformList(prjtype) { Owner = this };
            this.Hide();
            typeWindow.ShowDialog();
        }

    }
}
