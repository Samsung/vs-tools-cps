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

using NetCore.Profiler.Analytics.Model;
using NetCore.Profiler.Extension.UI.Adaptor;

namespace NetCore.Profiler.Extension.UI.SourceLines
{
    public class SourceLineAdaptor : ProfiledObjectStatisticsAdaptor, IItemAdaptor<ISourceLineStatistics>
    {
        public object GetProperty(ISourceLineStatistics item, string name)
        {
            switch (name)
            {
                case "Position":
                    return GetPosition(item);
                case "PositionRows":
                    return GetPositionRows(item);
                case "PositionColumns":
                    return GetPositionColumns(item);
                case "Function":
                    return GetFunction(item);
                default:
                    return base.GetProperty(item,name);
            }
        }

        private object GetFunction(ISourceLineStatistics item)
        {
            return item.FunctionName;
        }

        private object GetPosition(ISourceLineStatistics item)
        {
            return $"{item.StartLine}:{item.StartColumn}-{item.EndLine}:{item.EndColumn}";
        }

        private object GetPositionRows(ISourceLineStatistics item)
        {
            return $"[{item.StartLine}-{item.EndLine}]";
        }

        private object GetPositionColumns(ISourceLineStatistics item)
        {
            return $"[{item.StartColumn}-{item.EndColumn}]";
        }
    }
}
