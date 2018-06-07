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

using System.Linq;
using NetCore.Profiler.Analytics.Model;
using NetCore.Profiler.Extension.UI.Adaptor;

namespace NetCore.Profiler.Extension.UI.CallTree
{
    public class CallTreeNodeAdaptor : ProfiledObjectStatisticsAdaptor, IItemAdaptor<ICallStatisticsTreeNode>
    {

        public object GetProperty(ICallStatisticsTreeNode item, string name)
        {
            switch (name)
            {
                case "Name":
                    return GetName(item);
                case "Percent":
                    return $"{base.GetPercentage(item)} %";
                case "Children":
                    return GetChildren(item);
                default:
                    return base.GetProperty(item, name);
            }
        }

        private object GetChildren(ICallStatisticsTreeNode item)
        {
            return item?.Children.Where(node => GetRawValue(node) > 0).OrderByDescending(GetRawValue).ToList();
        }

        private object GetName(ICallStatisticsTreeNode item)
        {
            return item.Name;
        }

    }
}
