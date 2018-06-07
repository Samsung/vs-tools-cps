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

using System.Windows;

namespace NetCore.Profiler.Extension.UI.SessionExplorer
{
    /// <summary>
    /// Interaction logic for RunProfilerDialog.xaml
    /// </summary>
    public partial class EditAnnotationDialog
    {

        public string Annotation { get; set; }

        public EditAnnotationDialog(string annotation)
        {
            Owner = Application.Current.MainWindow;
            Annotation = annotation;
            DataContext = this;
            InitializeComponent();
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

    }
}
