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
using System.IO;
using System.Linq;
using Tizen.VisualStudio.Tools.Data;
using Tizen.VisualStudio.Tools.DebugBridge;
using Tizen.VisualStudio.Tools.DebugBridge.SDBCommand;
using Tizen.VisualStudio.Utilities;

namespace Tizen.VisualStudio.Tools.Utilities
{
    public class OnDemandInstaller
    {
        public const string LldbPackage = "lldb";
        public const string LldbTvPackage = "lldb-tv";

        public string ErrorMessage
        {
            get { return _errorMessage; }
            private set
            {
                _errorMessage = value;
                if (!String.IsNullOrEmpty(value))
                {
                    PrintError(value);
                }
            }
        }
        private string _errorMessage;

        SDBDeviceInfo _device;

        private bool _supportRpms;

        private bool _supportTarGz;

        private bool _isSecureProtocol;

        private string _sdkOnDemandFolder;

        private string _sdkToolPath;

        private string _cpuArch;

        private readonly SortedDictionary<string, Package> _packages = new SortedDictionary<string, Package>();

        private Action<string> _onMessage;

        public OnDemandInstaller(SDBDeviceInfo device, bool supportRpms, bool supportTarGz, Action<string> onMessage)
        {
            if (!(supportRpms || supportTarGz))
            {
                throw new ArgumentException($"At least one of {nameof(supportRpms)} or {nameof(supportTarGz)} parameters must be true");
            }
            _device = device;
            _supportRpms = supportRpms;
            _supportTarGz = supportTarGz;
            _onMessage = onMessage;
        }

        /// <summary>
        /// Install packages on the target.
        /// </summary>
        /// <param name="packageNames">E.g. "profctl", "heaptrack", "coreprofiler".</param>
        /// <returns>True iff installed successfully</returns>
        public bool Install(params string[] packageNames)
        {
            if ((packageNames == null) || (packageNames.Length == 0))
            {
                throw new ArgumentException(nameof(packageNames));
            }
            ErrorMessage = "";
            var cap = new SDBCapability(_device);
            string tizenVersion = cap.GetValueByKey("platform_version");
            if (!DeployHelper.IsTizenVersionSupported(tizenVersion))
            {
                ErrorMessage = $"The target system platform version {tizenVersion} is not supported";
                return false;
            }
            _sdkOnDemandFolder = ToolsPathInfo.GetOnDemandFolderPath(tizenVersion);
            if (string.IsNullOrEmpty(_sdkOnDemandFolder))
            {
                ErrorMessage = $"Can not find the folder with target packages for version \"{tizenVersion}\"";
                return false;
            }
            if (!Directory.Exists(_sdkOnDemandFolder))
            {
                ErrorMessage = $"Folder \"{_sdkOnDemandFolder}\" not found";
                return false;
            }
            _isSecureProtocol = cap.GetAvailabilityByKey("secure_protocol");
            if (_isSecureProtocol)
            {
                if (!_supportTarGz)
                {
                    ErrorMessage = "The target uses secure protocol. Only tar.gz packages are supported for secure targets";
                }
                _supportRpms = false;
            }
            _sdkToolPath = cap.GetValueByKey("sdk_toolpath");
            _cpuArch = DeployHelper.GetPackageSuffix(cap.GetValueByKey("cpu_arch"));
            var unavailablePackages = new List<string>();
            CheckAvailable(packageNames, unavailablePackages);
            bool result = true;
            try
            {
                if (unavailablePackages != null)
                {
                    foreach (string packageName in unavailablePackages)
                    {
                        Version installedVersion = GetInstalledPackageVersion(packageName);
                        if (installedVersion == null)
                        {
                            ErrorMessage = $"Package \"{packageName}\" not found both in \"{_sdkOnDemandFolder}\" and on the target system";
                            return false;
                        }
                    }
                }
                CheckInstalled();
                if (_packages.Any(p => p.Value.NeedToInstall))
                {
                    result = InstallPackages(_sdkToolPath + "/on-demand");
                }
            }
            finally
            {
                //
            }
            _packages.Clear();
            return result;
        }

        private void CheckAvailable(string[] packageNames, List<string> unavailablePackages)
        {
            _packages.Clear();
            foreach (var tuple in DeployHelper.GetLatestPackages(_sdkOnDemandFolder, packageNames, _cpuArch,
                _supportRpms, _supportTarGz, _isSecureProtocol))
            {
                _packages.Add(
                    key: tuple.Item1,
                    value: new Package(this, packageFile: tuple.Item2, version: tuple.Item3));
            }
            if (_packages.Count < packageNames.Length)
            {
                foreach (string packageName in packageNames)
                {
                    Package package;
                    if (!_packages.TryGetValue(packageName, out package))
                    {
                        Print($"Warning: package \"{packageName}\" for \"{_cpuArch}\" was not found in \"{_sdkOnDemandFolder}\"");
                        unavailablePackages.Add(packageName);
                        continue;
                    }
                    if (_isSecureProtocol)
                    {
                        string signatureFileName = Path.Combine(_sdkOnDemandFolder, package.PackageFile) +
                            DeployHelper.SignatureFileExtension;
                        if (!File.Exists(signatureFileName))
                        {
                            Print($"Warning: signature file \"{signatureFileName}\" was not found");
                            unavailablePackages.Add(packageName);
                            continue;
                        }
                    }
                }
            }
        }

        private void CheckInstalled()
        {
            foreach (var pair in _packages)
            {
                pair.Value.UpdateNeedToInstall(pair.Key);
            }
        }

        private bool PushPackages(string packagesPath)
        {
            bool result = true;
            foreach (var pair in _packages)
            {
                Package package = pair.Value;
                if (package.NeedToInstall)
                {
                    if (!package.Push(pair.Key, packagesPath))
                    {
                        result = false;
                        break;
                    }
                }
            }
            return result;
        }

        private bool InstallPackages(string packagesPath)
        {
            // push packages
            if (!PushPackages(packagesPath))
            {
                return false;
            }

            //string errorMessage;
            //if (!SDBLib.RemountRootFileSystem(_device, true, out errorMessage))
            //{
            //    Print(errorMessage);
            //    return false;
            //}
            //Print("Remounted root file system read-write");

            // install packages on target
            if (!TargetInstallPackages(packagesPath))
            {
                return false;
            }

            return true;
        }

        private bool TargetInstallPackages(string packagesPath)
        {
            bool result = true;
            foreach (var pair in _packages)
            {
                Package package = pair.Value;
                if (package.NeedToInstall)
                {
                    if (!package.Install(pair.Key, packagesPath))
                    {
                        result = false;
                    }
                }
            }
            return result;
        }

        private void PrintError(string msg)
        {
            Print($"Error. {msg}");
        }

        [Conditional("DEBUG")]
        private void DebugPrint(string msg)
        {
            Print($"[DBG] {msg}");
        }

        private void Print(string msg)
        {
            Debug.WriteLine($"{DateTime.Now} [OnDemand] {msg}");
            _onMessage?.Invoke(msg);
        }

        private string GetInstalledRpmPackage(string packageName, out Version installedVersion)
        {
            installedVersion = null;
            string installedPackageName, errorMessage;
            if (DeployHelper.IsRmpPackageInstalled(_device, packageName, out installedPackageName, out errorMessage))
            {
                // check version
                string rpmPackageName;
                installedVersion = DeployHelper.ParsePackageVersion(installedPackageName, out rpmPackageName);
                if ((installedVersion != null) && (rpmPackageName == packageName))
                {
                    Print($"Package found on the target system: \"{installedPackageName}\" (rpm)");
                    return installedPackageName;
                }
                errorMessage = $"Unexpected package name received: \"{installedPackageName}\" (rpm)";
            }
            else
            {
                if (String.IsNullOrEmpty(errorMessage))
                {
                    return "";
                }
            }
            Print($"Check package failed for \"{packageName}\" (rpm). {errorMessage}");
            return "";
        }

        private Version GetInstalledTarGzPackageVersion(string packageName)
        {
            string versionFileMask = $"{_sdkToolPath}/{packageName}/";
            // TODO!! try to remove special handling of "lldb"
            if (packageName == LldbPackage)
            {
                versionFileMask += "bin/lldb-server-*";
            }
            else
            {
                versionFileMask += "version-*";
            }
            string errorMessage;
            Version result = DeployHelper.GetInstalledPackageVersion(_device, versionFileMask, out errorMessage);
            if (result != null)
            {
                Print($"Package found on the target system: \"{packageName}-{result}-{_cpuArch}\" (tar.gz)");
            }
            else
            {
                if (!(String.IsNullOrEmpty(errorMessage) || errorMessage.Contains("No such file")))
                {
                    Print($"Check package failed for \"{packageName}\" (tar.gz). {errorMessage}");
                }
            }
            return result;
        }

        private Version GetInstalledPackageVersion(string packageName)
        {
            Version result = null;
            Version installedVersion;
            if (_supportRpms)
            {
                GetInstalledRpmPackage(packageName, out installedVersion);
                result = installedVersion;
            }
            if (_supportTarGz)
            {
                if (_isSecureProtocol)
                {
                    string errorMessage;
                    // TODO!! try to remove special handling of "lldb-tv"
                    if (packageName == LldbTvPackage)
                    {
                        installedVersion = DeployHelperSecure.RunGetVersionCommand(_device, "shell 0 vs_lldbversion", out errorMessage);
                    }
                    else
                    {
                        installedVersion = DeployHelperSecure.GetInstalledPackageVersion(_device, packageName, out errorMessage);
                    }
                    if (!String.IsNullOrEmpty(errorMessage))
                    {
                        Print($"Check package failed for \"{packageName}\" (secure). {errorMessage}");
                    }
                }
                else
                {
                    installedVersion = GetInstalledTarGzPackageVersion(packageName);
                }
                if (installedVersion != null)
                {
                    if ((result == null) || (installedVersion > result))
                    {
                        result = installedVersion;
                    }
                }
            }
            return result;
        }

        private bool Push(string source, string destination)
        {
            string errorMessage;
            bool success = DeployHelper.PushFile(_device, source, destination, null, out errorMessage);
            if (success)
            {
                Print($"Pushed \"{source}\" to \"{destination}\"");
            }
            else
            {
                Print(errorMessage);
            }
            return success;
        }

        private bool InstallPackage(string packageName, string packageFileName)
        {
            string errorMessage;
            bool success;
            if (packageFileName.EndsWith(".rpm"))
            {
                success = DeployHelper.InstallRpmPackage(_device, packageFileName, out errorMessage);
            }
            else
            {
                if (_isSecureProtocol)
                {
                    // TODO!! try to remove special handling of "lldb-tv"
                    if (packageName == LldbTvPackage)
                    {
                        // TODO!! do need to uninstall?
                        string outputLine;
                        success = DeployHelperSecure.RunCommand(_device, "shell 0 vs_lldbinstall", out outputLine, out errorMessage);
                    }
                    else
                    {
                        // TODO!! do need to uninstall?
                        if (DeployHelperSecure.GetInstalledPackageVersion(_device, packageName, out errorMessage) != null)
                        {
                            DeployHelperSecure.UninstallPackage(_device, packageName, out errorMessage);
                        }
                        success = DeployHelperSecure.InstallPackage(_device, packageName, out errorMessage);
                    }
                    if (!success)
                    {
                        errorMessage = StringHelper.CombineMessages($"Cannot install package \"{packageName}\"", errorMessage);
                    }
                }
                else
                {
                    // remove old files (if any)
                    if (!SDBLib.RunSdbShellCommandAndCheckExitStatus(_device, $"rm -rf {_sdkToolPath}/{packageName}", null,
                        out errorMessage))
                    {
                        DebugPrint(StringHelper.CombineMessages("Cannot remove old files", errorMessage));
                    }
                    success = DeployHelper.ExtractTarGzip(_device, packageFileName, _sdkToolPath, out errorMessage);
                }
            }
            if (success)
            {
                Print($"Successfully installed \"{packageFileName}\"");
            }
            else
            {
                Print(errorMessage);
            }
            return success;
        }

        private class Package
        {
            public string PackageFile { get; private set; }

            public Version Version { get; private set; }

            public bool NeedToInstall { get; private set; }

            private OnDemandInstaller _installer;

            public Package(OnDemandInstaller installer, string packageFile, Version version)
            {
                _installer = installer;
                PackageFile = packageFile;
                Version = version;
            }

            public void UpdateNeedToInstall(string name)
            {
                NeedToInstall = NeedToInstallPackage(name);
            }

            private string GetTargetFileName(string packageName)
            {
                if (PackageFile.EndsWith(".rpm"))
                    return PackageFile;
                else if (packageName == LldbTvPackage)
                    return "lldb.tar.gz";
                return packageName + ".tar.gz";
            }

            public bool Push(string packageName, string packagesPath)
            {
                string source = Path.Combine(_installer._sdkOnDemandFolder, PackageFile);
                bool success = true;
                if (_installer._isSecureProtocol)
                {
                    // TODO!! try to remove special handling of "lldb-tv"
                    if (packageName == LldbTvPackage)
                    {
                        success = _installer.Push(
                            Path.Combine(_installer._sdkOnDemandFolder,
                                $"signature-{Version}-{_installer._cpuArch}"),
                            packagesPath + '/' + "signature");
                    }
                    else
                    {
                        success = _installer.Push(
                            source + DeployHelper.SignatureFileExtension,
                            packagesPath + '/' + packageName + ".tar.gz" + DeployHelper.SignatureFileExtension);
                    }
                }
                if (success)
                {
                    success = _installer.Push(source, packagesPath + '/' + GetTargetFileName(packageName));
                }
                if (!success)
                {
                    NeedToInstall = false; // no need to install if failed to push
                }
                return success;
            }

            public bool Install(string packageName, string packagesPath)
            {
                return _installer.InstallPackage(packageName, packagesPath + '/' + GetTargetFileName(packageName));
            }

            private bool NeedToInstallPackage(string packageName)
            {
                Version installedVersion = _installer.GetInstalledPackageVersion(packageName);
                return (installedVersion == null) || (installedVersion < Version);
            }
        } // Package
    }
}
