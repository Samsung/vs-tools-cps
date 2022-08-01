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
using System;
using System.IO;
using Tizen.VisualStudio.Utilities;

namespace Tizen.VisualStudio.ProjectWizard.View
{
    public partial class ProjectWizardAddProjTempList : System.Windows.Window
    {
        string workspacePath;
        public ProjectWizardAddProjTempList(string dir)
        {
            InitializeComponent();
            workspacePath = dir;
            PopulateList();
        }


        void ButtonOkClick(object sender, RoutedEventArgs e)
        {
            string tname = (string)(sender as Button).Content;
            var nextWindow = new ProjectWizardAddProjectName(tname, workspacePath) { Owner = this };
            this.Hide();
            nextWindow.ShowDialog();
        }

        public void PopulateList()
        {
            var executor = new TzCmdExec();
            string message;
            message = executor.RunTzCmnd(string.Format("/c tz templates -w {0}", workspacePath));
            if (message == null)
            {
                this.Close();
                MessageBox.Show("Error occurred while running Tz command");
                return;
            }
            
            if (message.Length != 0)
            {
                int i = 0, j = 0;
                char[] delims = new[] { '\r', '\n' };
                string[] strings = message.ToString().Split(delims, StringSplitOptions.RemoveEmptyEntries);
                bool skip = true;

                foreach (string str in strings)
                {
                    if (skip)
                    {
                        skip = false;
                        continue;
                    }
                    Button btn = new Button();
                    Grid.SetColumn(btn, j);
                    Grid.SetRow(btn, i);
                    if (j < 2)
                    {
                        j++;
                    }
                    else
                    {
                        j = 0;
                        i++;
                    }
                    this.template_list.Children.Add(btn);
                    string[] val = str.Split(' ');
                    btn.Content = val[2];
                    btn.Height = 80;
                    btn.Width = 120;
                    btn.Click += new RoutedEventHandler(ButtonOkClick);
                }
            }
        }

        private void ButtonCancelClick(object sender, RoutedEventArgs e) => this.Close();
    }
}