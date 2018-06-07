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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tizen.VisualStudio.Utilities
{
    class WaitDialogDescription
    {
        public string WaitCaption
        {
            get;
        }

        public string WaitMessage
        {
            get;
        }

        public string ProgressText
        {
            get;
        }

        public string StatusBarText
        {
            get;
        }

        public WaitDialogDescription(string waitCaption, string waitMessage, string progressText, string statusBarText)
        {
            WaitCaption = waitCaption;
            WaitMessage = waitMessage;
            ProgressText = progressText;
            StatusBarText = statusBarText;
        }
    }

}
