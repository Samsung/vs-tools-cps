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

        public ulong RangeMaxValueMilliseconds { get; protected set; }

        public ulong ViewPortMinValueMilliseconds { get; protected set; }

        public ulong ViewPortMaxValueMilliseconds { get; protected set; }

        public ulong OffsetMilliseconds
        {
            get => ViewPortMinValueMilliseconds;
            set
            {
                ulong delta = value - ViewPortMinValueMilliseconds;
                ViewPortMinValueMilliseconds = value;
                ViewPortMaxValueMilliseconds += delta;
                UpdateViewPort();
            }
        }

        public void ZoomTo(ulong startMilliseconds, ulong endMilliseconds)
        {
            //Don't dislpay less then 100 ms
            //TODO check number of points in the interval
            if (endMilliseconds - startMilliseconds < 100)
            {
                endMilliseconds = startMilliseconds + 100;
            }

            lock (_lock)
            {
                ViewPortMinValueMilliseconds = Math.Max(startMilliseconds, 0);
                ViewPortMaxValueMilliseconds = Math.Min(endMilliseconds, RangeMaxValueMilliseconds);
            }

            UpdateViewPort();
        }

        public void ResetZoom()
        {
            lock (_lock)
            {
                if (ViewPortMinValueMilliseconds == 0 && ViewPortMaxValueMilliseconds == RangeMaxValueMilliseconds)
                {
                    return;
                }

                ViewPortMinValueMilliseconds = 0;
                ViewPortMaxValueMilliseconds = RangeMaxValueMilliseconds;
            }

            UpdateViewPort();
        }

        public void ZoomIn(double itemUnderCursor, double speed)
        {
            lock (_lock)
            {
                var l = ViewPortMaxValueMilliseconds - ViewPortMinValueMilliseconds;
                var target = l * speed;

                //Don't dislpay less then 100 ms
                //TODO check number of points in the interval
                if (target < 100)
                {
                    return;
                }

                var rMin = (itemUnderCursor - ViewPortMinValueMilliseconds) / l;
                var rMax = 1 - rMin;

                var mint = itemUnderCursor - target * rMin;
                var maxt = itemUnderCursor + target * rMax;

                ViewPortMinValueMilliseconds = (ulong)Math.Max(mint, 0);
                ViewPortMaxValueMilliseconds = (ulong)Math.Min(maxt, RangeMaxValueMilliseconds);
            }

            UpdateViewPort();
        }

        public void ZoomOut(double itemUnderCursor, double speed)
        {
            lock (_lock)
            {
                if (ViewPortMinValueMilliseconds == 0 && ViewPortMaxValueMilliseconds == RangeMaxValueMilliseconds)
                {
                    return;
                }

                var l = ViewPortMaxValueMilliseconds - ViewPortMinValueMilliseconds;
                var target = l / speed;
                if (target >= RangeMaxValueMilliseconds)
                {
                    ViewPortMinValueMilliseconds = 0;
                    ViewPortMaxValueMilliseconds = RangeMaxValueMilliseconds;
                }
                else
                {
                    var rMin = (itemUnderCursor - ViewPortMinValueMilliseconds) / l;
                    var rMax = 1 - rMin;

                    var mint = itemUnderCursor - target * rMin;
                    var maxt = itemUnderCursor + target * rMax;

                    ViewPortMinValueMilliseconds = (ulong)Math.Max(mint, 0);
                    ViewPortMaxValueMilliseconds = (ulong)Math.Min(maxt, RangeMaxValueMilliseconds);
                }
            }

            UpdateViewPort();
        }

        public void RevealSelection(ulong startMilliseconds, ulong endMilliseconds)
        {
            lock (_lock)
            {
                var lp = ViewPortMaxValueMilliseconds - ViewPortMinValueMilliseconds;
                var ls = endMilliseconds - startMilliseconds;

                if (startMilliseconds >= ViewPortMinValueMilliseconds && endMilliseconds <= ViewPortMaxValueMilliseconds)
                {
                    return;
                }

                if (ls >= lp)
                {
                    ViewPortMinValueMilliseconds = startMilliseconds;
                }
                else
                {
                    ViewPortMinValueMilliseconds = (lp - ls) / 2 > startMilliseconds ? 0 : startMilliseconds - (lp - ls) / 2;
                }

                ViewPortMaxValueMilliseconds = ViewPortMinValueMilliseconds + lp;
                ViewPortMinValueMilliseconds = Math.Max(ViewPortMinValueMilliseconds, 0);
                ViewPortMaxValueMilliseconds = Math.Min(ViewPortMaxValueMilliseconds, RangeMaxValueMilliseconds);
            }

            UpdateViewPort();
        }

        protected abstract void UpdateViewPort();
    }
}
