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

namespace NetCore.Profiler.Analytics.DataProvider
{
    public class SelectedTimeFrame : ISelectedTimeFrame
    {
        public ulong Start { get; set; } = 0;

        public ulong End { get; set; } = ulong.MaxValue;

        public override bool Equals(object other)
        {
            if (other == null)
            {
                return false;
            }

            if (other == this)
            {
                return true;
            }

            var tf = (other as SelectedTimeFrame);
            if (tf != null)
            {
                return Start == tf.Start && End == tf.End;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }

        protected bool Equals(SelectedTimeFrame other)
        {
            if (other == null)
            {
                return false;
            }

            return Start == other.Start && End == other.End;
        }
    }
}
