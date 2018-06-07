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

namespace NetCore.Profiler.Extension.UI.TimelineCharts
{
    public abstract class ManagedTimelineChartModelBase
    {

        private readonly object _lock = new object();

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

                ViewPortMinValue = Math.Max(start, 0);
                ViewPortMaxValue = Math.Min(end, RangeMaxValue);
            }

            UpdateViewPort();
        }

        public void ResetZoom()
        {
            lock (_lock)
            {
                if (ViewPortMinValue == 0 && ViewPortMaxValue == RangeMaxValue)
                {
                    return;
                }

                ViewPortMinValue = 0;
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

                //Don't dislpay less then 100 ms
                //TODO check number of points in the interval
                if (target < 100)
                {
                    return;
                }

                var rMin = (itemUnderCursor - ViewPortMinValue) / l;
                var rMax = 1 - rMin;

                var mint = itemUnderCursor - target * rMin;
                var maxt = itemUnderCursor + target * rMax;

                ViewPortMinValue = (ulong)Math.Max(mint, 0);
                ViewPortMaxValue = (ulong)Math.Min(maxt, RangeMaxValue);
            }

            UpdateViewPort();
        }

        public void ZoomOut(double itemUnderCursor, double speed)
        {
            lock (_lock)
            {
                if (ViewPortMinValue == 0 && ViewPortMaxValue == RangeMaxValue)
                {
                    return;
                }

                var l = ViewPortMaxValue - ViewPortMinValue;
                var target = l / speed;
                if (target >= RangeMaxValue)
                {
                    ViewPortMinValue = 0;
                    ViewPortMaxValue = RangeMaxValue;
                }
                else
                {
                    var rMin = (itemUnderCursor - ViewPortMinValue) / l;
                    var rMax = 1 - rMin;

                    var mint = itemUnderCursor - target * rMin;
                    var maxt = itemUnderCursor + target * rMax;

                    ViewPortMinValue = (ulong)Math.Max(mint, 0);
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
                    ViewPortMinValue = (lp - ls) / 2 > start ? 0 : start - (lp - ls) / 2;
                }

                ViewPortMaxValue = ViewPortMinValue + lp;
                ViewPortMinValue = Math.Max(ViewPortMinValue, 0);
                ViewPortMaxValue = Math.Min(ViewPortMaxValue, RangeMaxValue);
            }

            UpdateViewPort();
        }

        protected abstract void UpdateViewPort();
    }
}
