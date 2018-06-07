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
using NetCore.Profiler.Extension.Session;

namespace NetCore.Profiler.Extension.UI.SessionWindow
{
    /// <summary>
    /// Interaction logic for SessionWindowContent.xaml
    /// </summary>
    public partial class SessionWindowContent
    {
        public SessionWindowContent()
        {
            InitializeComponent();
        }

        public void SetActiveSession(IActiveSession session)
        {

            var filters = new FilterPanel(session);
            Children.Add(filters);
            Grid.SetRow(filters, 0);
            Grid.SetColumn(filters, 0);

            var timeline = new TimelinePanel(session);
            RightGrid.Children.Add(timeline);
            Grid.SetRow(timeline, 0);
            Grid.SetColumn(timeline, 0);

            var callStack = new CallStackPanel(session);
            RightGrid.Children.Add(callStack);
            Grid.SetRow(callStack, 0);
            Grid.SetColumn(callStack, 2);
        }

    }
}
