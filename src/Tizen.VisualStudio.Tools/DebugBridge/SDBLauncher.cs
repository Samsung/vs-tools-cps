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
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Tizen.VisualStudio.Tools.Utilities;
using Tizen.VisualStudio.Tools.DebugBridge.SDBCommand;

namespace Tizen.VisualStudio.Tools.DebugBridge
{
    public class SDBLauncher
    {
        private SDBConnection sdbconnection = null;
        private IVsOutputWindowPane outputPane = null;

        private SDBLauncher()
        {

        }

        public static SDBLauncher Create(IVsOutputWindowPane outputPane)
        {
            SDBLauncher launcher = new SDBLauncher();
            launcher.outputPane = outputPane;
            return launcher;
        }

        public bool ConnectBridge()
        {
            this.sdbconnection = SDBConnection.Create();
            if (this.sdbconnection == null)
            {
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            if (this.sdbconnection != null)
            {
                this.sdbconnection.Dispose();
            }
        }

        /*
        private string TransportCheckVersion(string AppId)
        {
            int length;
            string requestcmd = "host:version";
            SDBRequest request = SDBConnection.MakeRequest(requestcmd);
            SDBResponse response = this.sdbconnection.Send(request);
            if (!response.IOSuccess || !response.Okay)
            {
                OutputDeviceErrorMsg("Cannot check transport version(1): " + AppId);
                return String.Empty;
            }

            length = this.sdbconnection.ReadLength();
            if (length <= 0)
            {
                OutputDeviceErrorMsg("Cannot check transport version(2): " + AppId);
                return String.Empty;
            }

            byte[] buffer = new byte[length];
            return this.sdbconnection.ReadData(buffer);
        }

        private bool TransportStart(string AppId)
        {
            string requestcmd = "host:transport-any";
            SDBRequest request = SDBConnection.MakeRequest(requestcmd);
            SDBResponse response = this.sdbconnection.Send(request);
            if (!response.IOSuccess || !response.Okay)
            {
                OutputDeviceErrorMsg("Cannot initiate transport: " + AppId);
                return false;
            }

            return true;
        }
        */

        public bool LaunchApplication(string AppId)
        {
            OutputResponseMsg("Try to launch application: " + AppId);

            SDBAppCmd appCommand = new SDBAppCmd(SDBProtocol.runapp, AppId);

            if (!appCommand.IsTargetFound)
            {
                OutputDeviceErrorMsg("Failed to get connection.");
                return false;
            }

            bool isLaunched = (appCommand.ExitCode == SDBReqExitCode.EXIT_SUCCESS);
            string msg = isLaunched ? "Application launched." : "Application launch failed: " + appCommand.ExitCode;

            OutputResponseMsg(msg);

            return isLaunched;
        }

        public void TerminateApplication(string appid)
        {
            OutputResponseMsg("Try to terminate running application: " + appid);

            SDBAppCmd appCommand = new SDBAppCmd(SDBProtocol.killapp, appid);

            if (!appCommand.IsTargetFound)
            {
                OutputDeviceErrorMsg("Failed to get connection.");
                return;
            }

            string msg = "Failed to terminate application";

            switch (appCommand?.ExitCode)
            {
                case 0:
                    msg = "Old application has been terminated to launch updated app";
                    break;
                case 1:
                    msg = "Application is not running";
                    break;
                case 255:
                    msg = "No application to be terminated";
                    break;
            }

            OutputResponseMsg(string.Format("{0}: {1}", msg, appCommand?.ExitCode));
        }

        public bool IsPackageDetected(string packageName)
        {
            OutputResponseMsg("Try to find installed package: " + packageName);

            SDBAppCmd appCommand = new SDBAppCmd(SDBProtocol.appinfo, packageName); 

            if (!appCommand.IsTargetFound)
            {
                OutputDeviceErrorMsg("Failed to get connection.");
                return false;
            }

            bool isDetected = (appCommand.ExitCode == SDBReqExitCode.EXIT_SUCCESS);
            string msg = isDetected ? "Package is already installed." : "Package is not installed.";

            OutputResponseMsg(msg);

            return isDetected;
        }

        private void OutputResponseMsg(string msg)
        {
            DateTime localDate = DateTime.Now;
            string message =
                String.Format("{0} : {1}\n",
                              localDate.ToString(),
                              msg);
            this.outputPane?.Activate();
            this.outputPane?.OutputString(message);
        }

        private void OutputDeviceErrorMsg(string msg)
        {
            DateTime localDate = DateTime.Now;
            string message =
                String.Format("{0} : {1}\n",
                              localDate.ToString(),
                              msg);
            this.outputPane?.Activate();
            this.outputPane?.OutputString(message);
        }
    }
}
