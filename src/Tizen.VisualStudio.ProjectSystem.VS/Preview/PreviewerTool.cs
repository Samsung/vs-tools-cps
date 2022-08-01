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
using System.IO;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using Tizen.VisualStudio.Tools.DebugBridge;
using Tizen.VisualStudio.Debug;
using Tizen.VisualStudio.Tools.Data;
using Tizen.VisualStudio.Tools.Utilities;
using Tizen.VisualStudio.Utilities;
using Tizen.VisualStudio.Tools.DebugBridge.SDBCommand;

namespace Tizen.VisualStudio.Preview
{
    public class PreviewerTool
    {
        public TimeSpan SdbCommandTimeout = TimeSpan.FromSeconds(10);

        enum EmulatorPlatformType
        {
            Mobile,
            TV,
            Wearable // TODO!! support wearable
        }

        private const string AppIdMobile = "org.tizen.example.XamlPreviewer.Tizen.Mobile";

        private const string AppIdTV = "org.tizen.example.XamlPreviewer.Tizen.TV";

        private SDBDeviceInfo _selectedDevice;

        public PreviewerTool()
        {
            _selectedDevice = DeviceManager.SelectedDevice;
            if (_selectedDevice == null)
            {
                throw new Exception("Target device not selected");
            }
        }

        public bool Preview(IServiceProvider serviceProvider)
        {
            DTE dte = (DTE)serviceProvider.GetService(typeof(SDTE));

            if (dte == null)
            {
                return false;
            }
            var doc = dte.ActiveDocument;
            if ((doc == null) || !doc.Name.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            {
                ShowWarning("Please open a XAML document to preview");
                return false;
            }

            EmulatorPlatformType platformType = EmulatorPlatformType.Mobile;

            string temp = "";
            try
            {
                try
                {
                    temp = Path.GetTempFileName();
                    FileHelper.CopyToUnixText(doc.FullName, temp);
                }
                catch (Exception ex)
                {
                    ShowError($"Cannot create temporary XAML file. {ex.Message}");
                    return false;
                }

                const string ErrMsg = "Cannot deploy XAML previewer utility";
                try
                {
                    platformType = GetPlatform();

                    if (!IsPreviewerInstalled(platformType))
                    {
                        if (!InstallTpk(platformType))
                        {
                            ShowError(ErrMsg);
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"{ErrMsg}. {ex.Message}");
                    return false;
                }

                try
                {
                    string targetXamlFile = String.Format("/opt/usr/home/owner/apps_rw/{0}/data/preview_data.txt",
                        (platformType == EmulatorPlatformType.TV) ? AppIdTV : AppIdMobile);

                    if (!PushXaml(temp, targetXamlFile))
                    {
                        return false; // PushXaml shows error messages
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Cannot push XAML file to the target system. {ex.Message}");
                    return false;
                }
            }
            finally
            {
                if (temp != "")
                {
                    try { File.Delete(temp); } catch { }
                }
            }

            try
            {
                if (!RunTpk(platformType))
                {
                    return false; // RunTpk shows error messages
                }
            }
            catch (Exception ex)
            {
                ShowError($"Cannot run XAML previewer utility on the target system. {ex.Message}");
                return false;
            }

            return true;
        }

        private bool IsPreviewerInstalled(EmulatorPlatformType platformType)
        {
            bool isPreviewerInstalled = false;
            int exitCode;
            SDBLib.SdbRunResult sdbResult = SDBLib.RunSdbCommand(_selectedDevice,
                String.Format(
                    "shell [ -f /opt/usr/home/owner/apps_rw/{0}/tizen-manifest.xml ] && echo 1 || echo 0",
                    (platformType == EmulatorPlatformType.TV) ? AppIdTV : AppIdMobile),
                (bool isStdOut, string line) =>
                {
                    if (line.StartsWith("1"))
                    {
                        isPreviewerInstalled = true;
                    }
                    return true; // only one line is needed
                },
                out exitCode, SdbCommandTimeout);

            if (sdbResult != SDBLib.SdbRunResult.Success)
            {
                ShowError("Failed to detect the XAML previewer application. " + SDBLib.FormatSdbRunResult(sdbResult, exitCode));
            }
            return isPreviewerInstalled;
        }

        private bool RunTpk(EmulatorPlatformType platformType)
        {
            string errorMessage;
            bool result = SDBLib.RunSdbShellCommandAndCheckExitStatus(_selectedDevice,
                $"launch_app {((platformType == EmulatorPlatformType.TV) ? AppIdTV : AppIdMobile)} " +
                 "__AUL_SDK__ dotnet-launcher", null, out errorMessage);
            if (!result)
            {
                ShowError(errorMessage);
            }
            return result;
        }

        private EmulatorPlatformType GetPlatform()
        {
            EmulatorPlatformType result = EmulatorPlatformType.Mobile;
            int exitCode;
            SDBLib.SdbRunResult sdbResult = SDBLib.RunSdbCommand(_selectedDevice,
                "shell grep TZ_BUILD_PROFILE /etc/tizen-build.conf",
                (bool isStdOut, string line) =>
                {
                    if (line.StartsWith("TZ_BUILD_PROFILE"))
                    {
                        if (line.EndsWith("=tv"))
                        {
                            result = EmulatorPlatformType.TV;
                        }
                        return true;
                    }
                    return false;
                },
                out exitCode, SdbCommandTimeout);

            if (sdbResult != SDBLib.SdbRunResult.Success)
            {
                ShowWarning("Failed to determine the target platform type. " + SDBLib.FormatSdbRunResult(sdbResult, exitCode));
            }
            return result;
        }

        private bool InstallTpk(EmulatorPlatformType platformType)
        {
            string packagePath = (platformType == EmulatorPlatformType.TV)
                ? ToolsPathInfo.XamlPreviewerTVPath
                : ToolsPathInfo.XamlPreviewerMobilePath;

            string lastErrorMessage;
            SDBCapability cap;
            if (DeviceManager.SdbCapsMap.ContainsKey(_selectedDevice.Serial))
            {
                cap = DeviceManager.SdbCapsMap[_selectedDevice.Serial];
            }
            else
            {
                cap = new SDBCapability(_selectedDevice);
                DeviceManager.SdbCapsMap.Add(_selectedDevice.Serial, cap);
            }
            InstallResult installResult = Launcher.Create().InstallTizenPackage(_selectedDevice, cap, packagePath, null,
                VsPackage.dialogFactory, false, out lastErrorMessage);

            return String.IsNullOrEmpty(lastErrorMessage);
        }

        private bool PushXaml(string source, string dest)
        {
            string errorMessage;
            if (DeployHelper.PushFile(_selectedDevice, source, dest, null, out errorMessage))
            {
                return true;
            }
            ShowError(errorMessage);
            return false;
        }

        private void ShowError(string message)
        {
            VsPackage.ShowMessage(MessageDialogType.Error, null, message);
        }

        private void ShowWarning(string message)
        {
            VsPackage.ShowMessage(MessageDialogType.Warning, null, message);
        }
    }
}
