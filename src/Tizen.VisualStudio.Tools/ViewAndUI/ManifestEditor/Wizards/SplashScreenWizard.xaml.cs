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
    /// Interaction logic for SplashScreenWizard.xaml
    /// </summary>
    public partial class SplashScreenWizard : Window
    {
        private EnvDTE.DTE dte;
        private string[] ResourceTypeArray = { "img", "edj" };
        private string[] ResolutionArray = { "ldpi", "mdpi", "hdpi", "xhdpi", "xxhdpi" };
        private string[] OrientationArray = { "landscape", "portrait" };
        private string[] IndicatorDisplayArray = { "true", "false" };
        private SplashObservableCollection<string> ResourceType = new SplashObservableCollection<string>();
        private SplashObservableCollection<string> Resolution = new SplashObservableCollection<string>();
        private SplashObservableCollection<string> Orientation = new SplashObservableCollection<string>();
        private SplashObservableCollection<string> IndicatorDisplay = new SplashObservableCollection<string>();

        public SplashScreenWizard(EnvDTE.DTE dte, splashscreen Modi = null)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;
            InitializeComponent();
            InitializeComboboxItem();
            this.dte = dte;
            EnableCheckOKbtn();

            if (Modi != null)
            {
                this.comboBox_ResourceType.Text = Modi.type;
                this.comboBox_Resolution.Text = Modi.dpi;
                this.comboBox_Orientation.Text = Modi.orientation;
                this.comboBox_IndicatorDisplay.Text = Modi.indicatordisplay;
                this.textBox_source.Text = Modi.src;
                this.textBox_AppcontrolOp.Text = Modi.appcontroloperation;
            }
        }

        private void InitializeComboboxItem()
        {
            ResourceType.AddArray(ResourceTypeArray);
            Resolution.AddArray(ResolutionArray);
            Orientation.AddArray(OrientationArray);
            IndicatorDisplay.AddArray(IndicatorDisplayArray);
            comboBox_ResourceType.ItemsSource = ResourceType;
            comboBox_Resolution.ItemsSource = Resolution;
            comboBox_Orientation.ItemsSource = Orientation;
            comboBox_IndicatorDisplay.ItemsSource = IndicatorDisplay;
            comboBox_IndicatorDisplay.SelectedIndex = 0;
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

        private void button_browse_Click(object sender, RoutedEventArgs e)
        {
            IconChooserWizard iWizard = new IconChooserWizard(this.dte);
            if (iWizard.ShowDialog() == true)
            {
                this.textBox_source.Text = iWizard.selectImageValue;
                EnableCheckOKbtn();
            }
        }

        private void comboBox_ResourceType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EnableCheckOKbtn();
        }

        private void comboBox_Resolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EnableCheckOKbtn();
        }

        private void comboBox_Orientation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EnableCheckOKbtn();
        }

        private void comboBox_IndicatorDisplay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EnableCheckOKbtn();
        }

        private void textBox_source_TextChanged(object sender, TextChangedEventArgs e)
        {
            EnableCheckOKbtn();
        }

        private void textBox_AppcontrolOp_TextChanged(object sender, TextChangedEventArgs e)
        {
            EnableCheckOKbtn();
        }

        private void EnableCheckOKbtn()
        {
            if (string.IsNullOrEmpty(comboBox_ResourceType.Text) ||
                string.IsNullOrEmpty(comboBox_Resolution.Text) ||
                string.IsNullOrEmpty(comboBox_Orientation.Text) ||
                string.IsNullOrEmpty(comboBox_IndicatorDisplay.Text) ||
                string.IsNullOrEmpty(textBox_source.Text) ||
                string.IsNullOrEmpty(textBox_AppcontrolOp.Text))
            {
                this.button_ok.IsEnabled = false;
            }
            else
            {
                this.button_ok.IsEnabled = true;
            }
        }
    }

    public class SplashObservableCollection<T> : ObservableCollection<T>
    {
        public void AddArray(IEnumerable<T> InputArray)
        {
            foreach (var input in InputArray)
            {
                this.Items.Add(input);
            }
        }
    }
}
