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

using System.Diagnostics;
using Tizen.VisualStudio.Tools.Data;
using Tizen.VisualStudio.Tools.ExternalTool;

namespace Tizen.VisualStudio.Tools.ExternalTool
{
    /// <summary>
    /// Launch %Tizen Certificate Manager
    /// </summary>
    public sealed class CertificateManagerLauncher : ExternalToolLauncher
    {
        private const string CertificateMgrDesc = "Tizen Certificate Manager";
        private string certificateMgrArguments = "";

        //private ToolsInfo toolInfo = ToolsInfo.Instance();
        private static ProcessStartInfo pInfo = new ProcessStartInfo();

        public CertificateManagerLauncher() : base(CertificateMgrDesc, pInfo, false)
        {
            /// Disable Passing argument to Certificate Manager (profiles.xml path & keystore path)
            /// if manual profiles.xml feature enable then this block should be enabled
#if false
            string userdatapath = this.toolInfo.UserDataFolderPath;

            /// Certificate Manager Argument
            if (!string.IsNullOrEmpty(userdatapath))
            {
                this.certificateMgrArguments += @" ""/profiles:" + Path.Combine(userdatapath, CertificateMgrDefaultProfilePath) + @"""";

                /// Certificate Manager does not recognize keystore path when last char is '\'
                if (userdatapath.EndsWith("\\"))
                {
                    userdatapath = userdatapath.Substring(0, userdatapath.Length - 1);
                }

                this.certificateMgrArguments += @" ""/keystore:" + userdatapath + @"""";
            }
#endif
            pInfo.FileName = ToolsPathInfo.CertificateMgrPath;
            pInfo.UseShellExecute = true;
            pInfo.Arguments = this.certificateMgrArguments;
            pInfo.Verb = "runas";
        }
    }
}
