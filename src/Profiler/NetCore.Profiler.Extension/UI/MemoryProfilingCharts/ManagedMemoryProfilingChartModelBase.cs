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

namespace NetCore.Profiler.Extension.UI.MemoryProfilingCharts
{
    public abstract class ManagedMemoryProfilingChartModelBase
    {

        private readonly object _lock = new object();

        public ulong RangeMinValue { get; protected set; }

        public ulong RangeMaxValue { get; protected set; }

        public ulong ViewPortMinValue { get; protected set; }

        public ulong ViewPortMaxValue { get; protected set; }

        public ulong Offset
        {
            get => ViewPortMinValue;
            set
            {
                var delta = value - ViewPortMinValue;
                ViewPortMinValue = value;
                ViewPortMaxValue += delta;
                UpdateViewPort();
            }
        }

        public void ZoomTo(ulong start, ulong end)
        {
            lock (_lock)
            {

                //Don't dislpay less then 100 ms
                //TODO check number of points in the interval
                if (end - start < 100)
                {
                    end = start + 100;
                }

                ViewPortMinValue = Math.Max(start, RangeMinValue);
                ViewPortMaxValue = Math.Min(end, RangeMaxValue);
            }

            UpdateViewPort();
        }

        public void ResetZoom()
        {
            lock (_lock)
            {
                if (ViewPortMinValue == RangeMinValue && ViewPortMaxValue == RangeMaxValue)
                {
                    return;
                }

                ViewPortMinValue = RangeMinValue;
                ViewPortMaxValue = RangeMaxValue;
            }

            UpdateViewPort();
        }

        public void ZoomIn(double itemUnderCursor, double speed)
        {
            lock (_lock)
            {
                var l = ViewPortMaxValue - ViewPortMinValue;
                var target = l * speed;

                var rMin = (itemUnderCursor - ViewPortMinValue) / l;
                var rMax = 1 - rMin;

                var mint = (ulong)Math.Max(itemUnderCursor - target * rMin, RangeMinValue);
                var maxt = (ulong)Math.Min(itemUnderCursor + target * rMax, RangeMaxValue);

                //Don't zoom too much
                if (!AcceptableVewPort(mint, maxt))
                {
                    return;
                }

                ViewPortMinValue = mint;
                ViewPortMaxValue = maxt;
            }

            UpdateViewPort();
        }

        public void ZoomOut(double itemUnderCursor, double speed)
        {
            lock (_lock)
            {
                if (ViewPortMinValue == RangeMinValue && ViewPortMaxValue == RangeMaxValue)
                {
                    return;
                }

                var l = ViewPortMaxValue - ViewPortMinValue;
                var target = l / speed;
                if (target >= RangeMaxValue - RangeMinValue)
                {
                    ViewPortMinValue = RangeMinValue;
                    ViewPortMaxValue = RangeMaxValue;
                }
                else
                {
                    var rMin = (itemUnderCursor - ViewPortMinValue) / l;
                    var rMax = 1 - rMin;

                    var mint = itemUnderCursor - target * rMin;
                    var maxt = itemUnderCursor + target * rMax;

                    ViewPortMinValue = (ulong)Math.Max(mint, RangeMinValue);
                    ViewPortMaxValue = (ulong)Math.Min(maxt, RangeMaxValue);
                }
            }

            UpdateViewPort();
        }

        public void RevealSelection(ulong start, ulong end)
        {
            lock (_lock)
            {

                var lp = ViewPortMaxValue - ViewPortMinValue;
                var ls = end - start;

                if (start >= ViewPortMinValue && end <= ViewPortMaxValue)
                {
                    return;
                }

                if (ls >= lp)
                {
                    ViewPortMinValue = start;
                }
                else
                {
                    ViewPortMinValue = (lp - ls) / 2 > start ? RangeMinValue : start - (lp - ls) / 2;
                }

                ViewPortMaxValue = ViewPortMinValue + lp;
                ViewPortMinValue = Math.Max(ViewPortMinValue, RangeMinValue);
                ViewPortMaxValue = Math.Min(ViewPortMaxValue, RangeMaxValue);
            }

            UpdateViewPort();
        }

        protected abstract bool AcceptableVewPort(ulong min, ulong max);

        protected abstract void UpdateViewPort();
    }
}
