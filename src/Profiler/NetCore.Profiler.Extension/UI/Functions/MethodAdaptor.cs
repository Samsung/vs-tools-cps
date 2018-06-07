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

namespace NetCore.Profiler.Extension.UI.Functions
{
    public class MethodAdaptor : ProfiledObjectStatisticsAdaptor, IItemAdaptor<IMethodStatistics>
    {
        public object GetProperty(IMethodStatistics item, string name)
        {
            switch (name)
            {
                case "Name":
                    return GetName(item);
                case "Signature":
                    return GetSignature(item);
                default:
                    return base.GetProperty(item, name);
            }
        }

        private object GetName(IMethodStatistics item)
        {
            return item.Name;
        }

        private object GetSignature(IMethodStatistics item)
        {
            return item.Signature;
        }

    }
}
