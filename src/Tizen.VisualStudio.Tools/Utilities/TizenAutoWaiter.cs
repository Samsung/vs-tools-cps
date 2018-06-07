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

using System.Threading;

namespace Tizen.VisualStudio.Tools.Utilities
{
    public abstract class TizenAutoWaiter
    {
        public AutoResetEvent Waiter { get; set; }
        public abstract bool IsWaiterSet(string value);
        public abstract void OnExit();

        public TizenAutoWaiter()
        {
            this.Waiter = new AutoResetEvent(false);
        }
    }
}
