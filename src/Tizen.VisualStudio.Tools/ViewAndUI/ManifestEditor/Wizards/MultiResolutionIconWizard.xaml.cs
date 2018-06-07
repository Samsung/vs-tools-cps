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
using System.Drawing;
using System.IO;
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

namespace Tizen.VisualStudio.ManifestEditor
{
    /// <summary>
    /// Interaction logic for MultiResolutionIconWizard.xaml
    /// </summary>
    public partial class MultiResolutionIconWizard : Window
    {
        public List<string> fList = new List<string>();
        static Dictionary<string, string> languageDic = new Dictionary<string, string>();
        private EnvDTE.DTE dte;
        private string resPath;
        private string projectPath;
        private string NowResolution = "default";
        private CollectionView view;
        private List<icon> ExistList = new List<icon>();
        private string[] ResolutionArray = { "default", "ldpi", "mdpi", "hdpi", "xhdpi", "xxhdpi" };
        private MultiObservableCollection<string> Resolution = new MultiObservableCollection<string>();
        private icon Modi = new icon();

        public MultiResolutionIconWizard(EnvDTE.DTE dte, List<icon> ExistList = null, icon Modi = null)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;
            InitializeComponent();
            this.projectPath = System.IO.Path.GetDirectoryName(dte.ActiveDocument.Path);
            this.resPath = this.projectPath + "\\shared\\res\\";
            this.dte = dte;
            this.Modi = Modi;
            Initialize();
            InItLocalSet();
            this.ExistList = ExistList;
            AddComboBoxChild();
            Resolution.AddArray(ResolutionArray);
            comboBox_resolution.ItemsSource = Resolution;
            comboBox_language.SelectedIndex = 0;
            comboBox_resolution.SelectedIndex = 0;
            if (Modi != null)
            {
                if (Modi.resolution == null)
                {
                    this.comboBox_resolution.Text = "default";
                }
                else
                {
                    this.comboBox_resolution.Text = Modi.resolution;
                }

                if (Modi.lang == null)
                {
                    this.comboBox_language.Text = "default";
                }
                else
                {
                    this.comboBox_language.Text = Modi.lang;
                }
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
                    languageDic.Add("default", "default");
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

        private void AddComboBoxChild()
        {
            foreach (var item in languageDic)
            {
                this.comboBox_language.Items.Add(item.Key);
            }
        }

        private void Initialize()
        {
            DirectoryInfo dInfo = new DirectoryInfo(resPath);
            FileInfo[] Files = dInfo.GetFiles("*.png");
            DirectoryInfo[] Directories = dInfo.GetDirectories();
            foreach (FileInfo file in Files)
            {
                fList.Add(file.Name);
            }

            foreach (DirectoryInfo subDInfo in Directories)
            {
                FileInfo[] subFiles = subDInfo.GetFiles("*.png");
                foreach (FileInfo subFileInfo in subFiles)
                {
                    fList.Add(subDInfo.Name + "/" + subFileInfo.Name);
                }
            }

            this.listView.ItemsSource = fList;
            this.view = (CollectionView)CollectionViewSource.GetDefaultView(listView.ItemsSource);
            this.view.Filter = UserFilter;
            ButtonEnableCheck();
        }

        private bool UserFilter(object item)
        {
            string ViewItems = item as string;
            if (NowResolution == "default")
            {
                return true;
            }
            else
            {
                return ViewItems != null && ViewItems.StartsWith(NowResolution);
            }
        }

        private void button_new_Click(object sender, RoutedEventArgs e)
        {
            string FinalFilePath;
            NewIconWizard nWizard = new NewIconWizard(this.resPath, fList);
            if (nWizard.ShowDialog() == true)
            {
                if (this.comboBox_resolution.Text != "default")
                {
                    FinalFilePath = this.resPath + this.comboBox_resolution.Text + "\\" + nWizard.fileName;
                    if (Directory.Exists(this.resPath + this.comboBox_resolution.Text) == false)
                    {
                        Directory.CreateDirectory(this.resPath + this.comboBox_resolution.Text);
                    }
                }
                else
                {
                    FinalFilePath = nWizard.destFilePath;
                }

                if (File.Exists(FinalFilePath) == false)
                {
                    File.Copy(nWizard.filePath, FinalFilePath);
                    if (this.comboBox_resolution.Text != "default")
                    {
                        fList.Add(this.comboBox_resolution.Text + "/" + new FileInfo(nWizard.filePath).Name);
                    }
                    else
                    {
                        fList.Add(new FileInfo(nWizard.filePath).Name);
                    }

                    Bitmap toResize = new Bitmap(FinalFilePath);
                    Bitmap resultImage = new Bitmap(toResize, new System.Drawing.Size(nWizard.imgSize, nWizard.imgSize));
                    toResize.Dispose();
                    resultImage.Save(FinalFilePath + ".temp");
                    File.SetAttributes(FinalFilePath, FileAttributes.Normal);
                    File.Delete(FinalFilePath);
                    File.Move(FinalFilePath + ".temp", FinalFilePath);
                    ButtonEnableCheck();
                    resultImage.Dispose();
                    //IncludeInSolutionExplorer(FinalFilePath, false);
                }
                else
                {
                    MessageBox.Show("Same file name exists!");
                }
            }
        }

        //private void IncludeInSolutionExplorer(string filePath, bool isNeedCopy = false)
        //{
        //    VsProjectHelper ProjectHelper = VsProjectHelper.GetInstance;
        //    var Projects = ProjectHelper.GetProjects();
        //    var CurrentProject = Projects.DTE.ActiveDocument.ProjectItem.ContainingProject;
        //    if (isNeedCopy)
        //    {
        //        CurrentProject?.ProjectItems?.AddFromFileCopy(filePath);
        //    }
        //    else
        //    {
        //        CurrentProject?.ProjectItems?.AddFromFile(filePath);
        //    }
        //    CurrentProject?.Save();
        //}

        private void button_ok_Click(object sender, RoutedEventArgs e)
        {
            icon newIcon = new icon();
            newIcon.lang = this.comboBox_language.Text;
            newIcon.resolution = this.comboBox_resolution.Text;
            newIcon.Text = this.listView.SelectedItem.ToString().Split('\n');
            if (newIcon.resolution != "default")
            {
                newIcon.resolutionSpecified = true;
            }

            foreach (icon input in ExistList)
            {
                if (newIcon.resolution == input.resolution && newIcon.lang == input.lang && newIcon.Text[0] == input.Text[0])
                {
                    if (Modi != null)
                    {
                        if (newIcon.resolution != Modi.resolution || newIcon.lang != Modi.lang || newIcon.Text[0] != Modi.Text[0])
                        {
                            MessageBox.Show("Duplicate data is existed.");
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Duplicate data is existed.");
                        return;
                    }
                }
            }

            this.DialogResult = true;
            this.Close();
        }

        private void button_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void comboBox_language_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ButtonEnableCheck();
        }

        private void comboBox_resolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(listView.ItemsSource).Refresh();
            NowResolution = (sender as ComboBox).SelectedItem as string;
            ButtonEnableCheck();
        }

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ButtonEnableCheck();
        }

        private void ButtonEnableCheck()
        {
            this.view.Refresh();
            if (this.comboBox_resolution.Text != null && this.comboBox_language.Text != null && this.listView.SelectedItem != null)
            {
                button_ok.IsEnabled = true;
            }
            else
            {
                button_ok.IsEnabled = false;
            }
        }
    }

    public class MultiObservableCollection<T> : ObservableCollection<T>
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
