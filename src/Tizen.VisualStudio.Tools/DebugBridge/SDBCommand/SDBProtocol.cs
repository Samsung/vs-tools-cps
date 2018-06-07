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

namespace Tizen.VisualStudio.Tools.DebugBridge.SDBCommand
{
    public class SDBProtocol
    {
        private static SDBProtocol instance;

        public const char delemeter = ':';
        public const string newline = "\n";
        public const string terminator = "\0";

        #region App Command
        public const string appcmd = "appcmd";
        public const string runapp = "runapp";
        public const string appinfo = "appinfo";
        public const string killapp = "killapp";
        public const string uninstall = "uninstall";

        public const string appcmd_exitcode = "appcmd_exitcode";
        public const string appcmd_returnstr = "appcmd_returnstr";
        #endregion

        #region Capability
        public const int capBufferSize = 2;
        public const string capability = "capability";
        public const string enabled = "enabled";
        public const string disabled = "disabled";
        #endregion

        protected SDBProtocol()
        {
        }

        public static SDBProtocol Instance()
        {
            instance = new SDBProtocol();
            return instance;
        }
    }
}
