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
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using NetCore.Profiler.Analytics.Model;
using NetCore.Profiler.Common.Helpers;
using NetCore.Profiler.Extension.VSPackage;

namespace NetCore.Profiler.Extension.UI.Adaptor
{
    public class ProfiledObjectStatisticsAdaptor : IItemAdaptor<IProfiledObjectStatistics>
    {
        public StatisticsType StatisticsType { get; set; }

        public bool Inclusive { get; set; }

        public IProfilingStatisticsTotals Totals { get; set; }

        public virtual object GetProperty(IProfiledObjectStatistics item, string name)
        {
            switch (name)
            {
                case "Value":
                    return GetValue(item);
                case "Percent":
                    return GetPercentage(item);
                case "PercentText":
                    return GetPercentageText(item);
                case "Color":
                    return GetColor(item);
                default:
                    throw new ArgumentException();
            }
        }

        protected object GetValue(IProfiledObjectStatistics item)
        {
            switch (StatisticsType)
            {
                case StatisticsType.Sample:
                    return $"{GetRawValue(item):0.##}";
                case StatisticsType.Memory:
                    return GetRawValue(item).SizeBytesToString();
                case StatisticsType.Time:
                    return GetRawValue(item).MillisecondsToString();
                default:
                    return "ERROR";
            }

        }

        protected object GetPercentage(IProfiledObjectStatistics item)
        {
            return $"{GetRawPercentage(item):0.##}";
        }

        protected object GetPercentageText(IProfiledObjectStatistics item)
        {
            return $"{GetRawPercentage(item):0.##} %";
        }


        protected object GetColor(IProfiledObjectStatistics item)
        {
            //return new SolidColorBrush(Color.FromArgb(255, 0, 0, (byte)(255 / 100.0 * GetRawPercentage(item))));
            var color = ProfilerPlugin.Instance.VsUiShell5.GetThemedWPFColor(EnvironmentColors.ToolWindowTextColorKey);
            if (color.B == 0)
            {
                color = Color.FromArgb(255, 0, 0, (byte)(255 / 100.0 * GetRawPercentage(item)));
            }
            else
            {
                //TODO Provide correct algorithm
                var r = GetRawPercentage(item) / 100.0;
                color = Color.FromArgb(255, (byte)(255 * (1 - r)), (byte)(255 * (1 - r)), 255);
            }

            return new SolidColorBrush(color);
        }

        protected ulong GetRawValue(IProfiledObjectStatistics item)
        {
            switch (StatisticsType)
            {
                case StatisticsType.Sample:
                    return Inclusive ? item.SamplesInclusive : item.SamplesExclusive;
                case StatisticsType.Memory:
                    return Inclusive ? item.AllocatedMemoryInclusive : item.AllocatedMemoryExclusive;
                case StatisticsType.Time:
                    return Inclusive ? item.TimeInclusive : item.TimeExclusive;
                default:
                    return 0;
            }
        }

        protected double GetRawPercentage(IProfiledObjectStatistics item)
        {
            return GetPercentage(GetRawValue(item));
        }

        protected double GetPercentage(ulong value)
        {
            ulong total = Totals.GetValue(StatisticsType);
            if (total == 0)
            {
                return 0;
            }

            return 100.0 * value / total;
        }

    }
}
