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

using System.Windows.Controls;
using System.Windows.Data;
using NetCore.Profiler.Analytics.Model;
using NetCore.Profiler.Extension.Session;
using NetCore.Profiler.Extension.UI.Adaptor;

namespace NetCore.Profiler.Extension.UI.HotPath
{

    /// <summary>
    /// Workaround for the lack of generics support in XAML 2006.
    /// Should be removed later
    /// </summary>
    public class HotPathConverter : ItemConverter<HotPathAdaptor, IHotPath>, IMultiValueConverter
    {
    }

    /// <summary>
    /// Hot Path View
    /// </summary>
    public partial class HotPathView : UserControl
    {
        public string Header
        {
            set { LinesGrid.Columns[0].Header = value; }
        }

        public StatisticsType StatisticsType
        {
            get { return ItemAdaptor.StatisticsType; }
            set
            {
                ItemAdaptor.StatisticsType = value;
            }
        }

        public bool Inclusive
        {
            get { return ItemAdaptor.Inclusive; }
            set { ItemAdaptor.Inclusive = value; }
        }

        public HotPathAdaptor ItemAdaptor
        {
            get;
            private set;
        }

        public HotPathView()
        {
            ItemAdaptor = new HotPathAdaptor();
            InitializeComponent();
            DataContext = this;
        }

        public void SetItemsSource(IHotPathsQueryResult inputSource)
        {
            ItemAdaptor.StatisticsType = inputSource.StatisticsType;
            ItemAdaptor.Totals = inputSource.Totals;
            LinesGrid.ItemsSource = inputSource.Methods;
        }

    }
}
