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

using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using NetCore.Profiler.Analytics.Model;
using NetCore.Profiler.Extension.Session;
using NetCore.Profiler.Extension.UI.Adaptor;

namespace NetCore.Profiler.Extension.UI.CallTree
{
    /// <summary>
    /// Workaround for the lack of generics support in XAML 2006.
    /// Should be removed later
    /// </summary>
    public class CallTreeNodeConverter : ItemConverter<CallTreeNodeAdaptor, ICallStatisticsTreeNode>, IMultiValueConverter
    {
    }

    /// <summary>
    /// Call Tree View
    /// </summary>
    public partial class CallTreeView : UserControl, INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        public string Header
        {
            set { TreeViewHeader.Text = value; }
        }

        public StatisticsType StatisticsType
        {
            get { return ItemAdaptor.StatisticsType; }
            set
            {
                ItemAdaptor.StatisticsType = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ItemAdaptor"));
            }
        }

        public bool Inclusive
        {
            get { return ItemAdaptor.Inclusive; }
            set { ItemAdaptor.Inclusive = value; }
        }

        public CallTreeNodeAdaptor ItemAdaptor
        {
            get;
            private set;
        }

        public CallTreeView()
        {
            ItemAdaptor = new CallTreeNodeAdaptor();
            InitializeComponent();
            DataContext = this;
        }

        public void SetItemsSource(ICallTreeQueryResult inputSource, StatisticsType statisticsType)
        {
            ItemAdaptor.StatisticsType = statisticsType;
            ItemAdaptor.Totals = inputSource.Totals;
            TreeView.ItemsSource = inputSource.CallTree;

            foreach (var item in inputSource.CallTree)
            {
                ((CallStatisticsTreeNode)item).IsExpanded = true;
            }
        }
    }
}
