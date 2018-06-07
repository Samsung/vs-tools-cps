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

using System.Collections.Generic;
using NetCore.Profiler.Analytics.Model;
using NetCore.Profiler.Extension.Common;

namespace NetCore.Profiler.Extension.UI.CallTree
{
    public class CallStatisticsTreeNode : NotifyPropertyChanged, ICallStatisticsTreeNode
    {
        public ICallStatisticsTreeNode Parent { get; set; }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                SetProperty(ref _isExpanded, value);

                // Expand all the way up to the root.
                if (_isExpanded && Parent != null)
                {
                    ((CallStatisticsTreeNode)Parent).IsExpanded = true;
                }
            }
        }

        private bool _isExpanded;

        public ulong SamplesExclusive { get; set; }
        public ulong SamplesInclusive { get; set; }
        public ulong TimeExclusive { get; set; }
        public ulong TimeInclusive { get; set; }
        public ulong AllocatedMemoryExclusive { get; set; }
        public ulong AllocatedMemoryInclusive { get; set; }
        public List<ICallStatisticsTreeNode> Children { get; set; } = new List<ICallStatisticsTreeNode>();

        public string Name { get; set; }
    }
}
