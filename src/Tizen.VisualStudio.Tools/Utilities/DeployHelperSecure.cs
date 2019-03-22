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
using Tizen.VisualStudio.Tools.DebugBridge;

namespace Tizen.VisualStudio.Tools.Utilities
{
    public static class DeployHelperSecure
    {
        public static Version GetInstalledPackageVersion(SDBDeviceInfo device, string packageName, out string errorMessage)
        {
            return RunGetVersionCommand(device, $"shell 0 vs_sdkversion {packageName}", out errorMessage);
        }

        public static Version RunGetVersionCommand(SDBDeviceInfo device, string command, out string errorMessage)
        {
            string outputLine = "";
            if (!RunCommand(device, command, out outputLine, out errorMessage))
            {
                return null;
            }
            Version result = null;
            if ((outputLine != "") && !Version.TryParse(outputLine, out result))
            {
                errorMessage = $"Cannot parse package version \"{outputLine}\"";
            }
            return result;
        }

        public static bool InstallPackage(SDBDeviceInfo device, string packageName, out string errorMessage)
        {
            string outputLine;
            return RunCommand(device, $"shell 0 vs_sdkinstall {packageName}", out outputLine, out errorMessage); // TODO!! check outputLine
        }

        public static bool UninstallPackage(SDBDeviceInfo device, string packageName, out string errorMessage)
        {
            string outputLine;
            return RunCommand(device, $"shell 0 vs_sdkremove {packageName}", out outputLine, out errorMessage); // TODO!! check outputLine
        }

        public static bool RunCommand(SDBDeviceInfo device, string command, out string outputLine, out string errorMessage)
        {
            string s = "";
            int exitResult = 0;
            SDBLib.SdbRunResult sdbResult = SDBLib.RunSdbCommand(device, command,
                (bool isStdOut, string line) =>
                {
                    if (line != "")
                    {
                        s = line;
                        return true; // TODO!! check if it is valid to return 'true' here
                    }
                    return false;
                },
                out exitResult,
                TimeSpan.FromSeconds(60));
            outputLine = s;
            if (sdbResult != SDBLib.SdbRunResult.Success)
            {
                errorMessage = $"Cannot run \"{command}\". {SDBLib.FormatSdbRunResult(sdbResult)}";
                return false;
            }
            // TODO!! shell command might fail even if sdbResult is Success - check the output
            // (support different commands - vs_sdkinstall, vs_sdkremove, etc.!)
            if (outputLine.StartsWith("/bin/sh:")) // error
            {
                errorMessage = outputLine;
                return false;
            }
            else if (outputLine.EndsWith("is not installed")) // vs_sdkinstall error
            {
                errorMessage = outputLine;
                return false;
            }
            if (exitResult != 0)
            {
                errorMessage = outputLine;
                return false;
            }
            errorMessage = "";
            return true;
        }
    }
}
