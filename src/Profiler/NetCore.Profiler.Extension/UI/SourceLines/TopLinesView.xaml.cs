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

using System.Windows.Data;
using NetCore.Profiler.Analytics.Model;
using NetCore.Profiler.Extension.Session;
using NetCore.Profiler.Extension.UI.Adaptor;

namespace NetCore.Profiler.Extension.UI.SourceLines
{
    /// <summary>
    /// Workaround for the lack of generics support in XAML 2006.
    /// Should be removed later
    /// </summary>
    public class SourceLineConverter : ItemConverter<SourceLineAdaptor, ISourceLineStatistics>, IMultiValueConverter
    {
    }

    /// <summary>
    /// Top Lines View
    /// </summary>
    public partial class TopLinesView
    {

        public string Header
        {
            set => LinesGrid.Columns[0].Header = value;
        }

        public StatisticsType FilterType
        {
            get => ItemAdaptor.StatisticsType;
            set => ItemAdaptor.StatisticsType = value;
        }

        public bool Inclusive
        {
            get => ItemAdaptor.Inclusive;
            set => ItemAdaptor.Inclusive = value;
        }

        public SourceLineAdaptor ItemAdaptor
        {
            get;
            private set;
        }


        public TopLinesView()
        {
            InitializeComponent();
            ItemAdaptor = new SourceLineAdaptor();
            DataContext = this;
        }

        public void SetItemsSource(ISourceLinesQueryResult queryResult)
        {
            ItemAdaptor.StatisticsType = queryResult.StatisticsType;
            ItemAdaptor.Totals = queryResult.Totals;
            LinesGrid.ItemsSource = queryResult.Lines;
        }

    }
}
