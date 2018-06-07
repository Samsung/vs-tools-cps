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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using NetCore.Profiler.Cperf.LogAdaptor.Core;
using Process = System.Diagnostics.Process;
using Thread = System.Threading.Thread;
using NetCore.Profiler.Extension.Session;
using NetCore.Profiler.Extension.VSPackage;
using NetCore.Profiler.Session.Core;
using Tizen.VisualStudio.Tools.DebugBridge;
using Tizen.VisualStudio.Tools.Utilities;
using Tizen.VisualStudio.Tools.DebugBridge.SDBCommand;
using Tizen.VisualStudio.Tools.Data;

namespace NetCore.Profiler.Extension.Launcher.Model
{
    internal class OnDemandInstaller
    {
        private class ProfilerPackage
        {
            public string AvailableVersion;
            public string AvailableRelease;
            public string InstalledVersion;
            public string InstalledRelease;
            public bool NeedToInstall;
        }

        private Dictionary<string, ProfilerPackage> _packages = new Dictionary<string, OnDemandInstaller.ProfilerPackage>
        {
            {"babeltrace", new ProfilerPackage() }, {"liburcu0", new ProfilerPackage() },
            {"userspace-rcu", new ProfilerPackage() }, {"lttng-ust", new ProfilerPackage() },
            {"lttng-tools", new ProfilerPackage() }, {"coreprofiler", new ProfilerPackage() }
        };

        public OnDemandInstaller()
        {
        }

        public bool Install()
        {
            CheckAvailable();
            CheckInstalled();
            bool needToInstall = false;
            bool retval = true;
            foreach (var p in _packages.Values)
            {
                if (p.NeedToInstall)
                {
                    needToInstall = true;
                    break;
                }
            }

            if (needToInstall)
            {
                retval = InstallProfiler();
            }

            return retval;
        }

        private string[] ParsePackageFilename(string filename)
        {
            string pattern = @"(.*)-(.*)-(.*?)\.(.*)(\.rpm)?";

            Regex r = new Regex(pattern);
            Match m = r.Match(filename);

            List<string> str = new List<string>();
            if (m.Success)
            {
                foreach (Group g in m.Groups)
                {
                    CaptureCollection cc = g.Captures;
                    foreach (Capture c in cc)
                    {
                        str.Add(c.Value);
                    }
                }
            }

            return str.ToArray();
        }

        private void CheckAvailable()
        {
            DirectoryInfo dir = new DirectoryInfo(ToolsPathInfo.OndemandFolderPath);
            FileInfo[] rpms = dir.GetFiles("*." + ArchToSuffix(GetArch()) + ".rpm");
            foreach (var rpm in rpms)
            {
                string[] components = ParsePackageFilename(rpm.Name);
                if (components.Length < 3)
                {
                    continue;
                }

                if (_packages.ContainsKey(components[1]))
                {
                    _packages[components[1]].AvailableVersion = components[2];
                    if (components.Length > 3)
                    {
                        _packages[components[1]].AvailableRelease = components[3];
                    }
                }
            }
        }


        private void CheckInstalled()
        {
            foreach (KeyValuePair<string, ProfilerPackage> package in _packages)
            {
                CheckPackage(package.Key, package.Value);
                if (!package.Value.NeedToInstall)
                {
                    Version av = null, iv = null;
                    double ar = 0, ir = 0;
                    if (!string.IsNullOrEmpty(package.Value.AvailableVersion) &&
                        !string.IsNullOrEmpty(package.Value.InstalledVersion))
                    {
                        av = new Version(package.Value.AvailableVersion);
                        iv = new Version(package.Value.InstalledVersion);
                    }

                    if (!string.IsNullOrEmpty(package.Value.AvailableRelease) &&
                        !string.IsNullOrEmpty(package.Value.InstalledRelease))
                    {
                        ar = double.Parse(package.Value.AvailableRelease);
                        ir = double.Parse(package.Value.InstalledRelease);
                    }

                    if (av != null && iv != null)
                    {
                        if (av > iv)
                        {
                            package.Value.NeedToInstall = true;
                        }
                        else if (av == iv)
                        {
                            if (ar > ir)
                            {
                                package.Value.NeedToInstall = true;
                            }
                        }
                    }
                }
            }


            ProfilerPackage liburcu1 = _packages["liburcu0"], liburcu2 = _packages["userspace-rcu"];
            if (liburcu1 != null && liburcu2 != null)
            {
                if (liburcu1.NeedToInstall && liburcu2.NeedToInstall)
                {
                    if (!string.IsNullOrEmpty(liburcu2.AvailableVersion))
                    {
                        liburcu1.NeedToInstall = false;
                    }
                    else if (!string.IsNullOrEmpty(liburcu1.AvailableVersion))
                    {
                        liburcu2.NeedToInstall = false;
                    }
                }
                else if (liburcu2.NeedToInstall && !liburcu1.NeedToInstall)
                {
                    liburcu2.NeedToInstall = false;
                }
                else if (liburcu1.NeedToInstall && !liburcu2.NeedToInstall)
                {
                    liburcu1.NeedToInstall = false;
                }
            }
        }

        private void CheckPackage(string name, ProfilerPackage package)
        {
            InstalledWaiter waiter = new InstalledWaiter();
            var proc = SDBLib.CreateSdbProcess(true, true);
            string cmdline = DeviceManager.AdjustSdbArgument("shell \"rpm -q " + name + "\"");
            var result = SDBLib.RunSdbProcessAsync(proc, cmdline, true, waiter);
            if (!waiter.Waiter.WaitOne(30000))
            {
                Print($"CheckPackage fails for {name}");
                return;
            }

            bool installed = !string.IsNullOrEmpty(waiter.InstalledStatus) 
                            && !waiter.InstalledStatus.EndsWith("not installed");
            Print($"Package {name} installed: {installed}");
            if (installed)
            {
                string[] components = ParsePackageFilename(waiter.InstalledStatus);
                if (components.Length > 2)
                {
                    package.InstalledVersion = components[2];
                }

                if (components.Length > 3)
                {
                    package.InstalledRelease = components[3];
                }
            }

            package.NeedToInstall = !installed;
        }

        private string GetRpmsPath()
        {
            SDBCapability cap = new SDBCapability();
            return cap.GetValueByKey("sdk_toolpath") + @"/on-demand";
        }

        private string GetArch()
        {
            SDBCapability cap = new SDBCapability();
            return cap.GetValueByKey("cpu_arch");
        }

        private string ArchToSuffix(string arch)
        {
            switch (arch)
            {
                case "x86_64":
                    return "x86_64";
                case "x86":
                    return "i686";
                default:
                    return "armv7l";
            }
        }

        private bool PushPackage(string name, ProfilerPackage p)
        {
            string rpm = name + "-" + p.AvailableVersion + "-" + p.AvailableRelease 
                + "." + ArchToSuffix(GetArch()) + ".rpm";
            string src = ToolsPathInfo.OndemandFolderPath + @"\" + rpm;
            string dst = GetRpmsPath() + "/" + rpm;

            Print($"Push {src} -> {dst}");
            var proc = SDBLib.CreateSdbProcess(true, true);
            string cmdline = DeviceManager.AdjustSdbArgument($"push \"{src}\" {dst}");
            SDBLib.RunSdbProcess(proc, cmdline, true);
            int rc = proc.ExitCode;
            proc.Close();
            return rc == 0;
        }

        private bool PushPackages()
        {
            foreach (var p in _packages)
            {
                if (p.Value.NeedToInstall)
                {
                    PushPackage(p.Key, p.Value);
                }
            }

            return true;
        }

        private bool SwitchToRoot(bool on)
        {
            Print($"Switch to root: {on}");

            var process = SDBLib.CreateSdbProcess();
            string cmdline = DeviceManager.AdjustSdbArgument($"root {((on) ? "on" : "off")}");
            SDBLib.RunSdbProcess(process, cmdline);
            var rc = process.ExitCode;
            process.Close();
            return rc == 0;
        }

        private bool RemountRW()
        {
            Print("Remount rootfs RW");
            var process = SDBLib.CreateSdbProcess();
            string cmdline = DeviceManager.AdjustSdbArgument("shell \"mount / -o remount,rw\"");
            SDBLib.RunSdbProcess(process, cmdline);
            var rc = process.ExitCode;
            process.Close();
            return rc == 0;
        }

        private bool InstallPackage(string name, ProfilerPackage p)
        {
            string rpm = name + "-" + p.AvailableVersion + "-" + p.AvailableRelease
                + "." + ArchToSuffix(GetArch()) + ".rpm";
            string package_path = GetRpmsPath() + "/" + rpm;

            Print($"Installing {package_path}");
            var process = SDBLib.CreateSdbProcess();
            string cmdline = DeviceManager.AdjustSdbArgument($"shell \"rpm -U --force {package_path}\"");
            SDBLib.RunSdbProcess(process, cmdline, true);
            int rc = process.ExitCode;
            process.Close();
            return rc == 0;
        }

        private bool InstallPackages()
        {
            foreach (var p in _packages)
            {
                if (p.Value.NeedToInstall)
                {
                    InstallPackage(p.Key, p.Value);
                }
            }

            return true;
        }

        private bool InstallProfiler()
        {
            // push packages
            if (!PushPackages())
            {
                return false;
            }

            // switch to root
            if (!SwitchToRoot(true))
            {
                return false;
            }

            // mount rw
            if (!RemountRW())
            {
                return false;
            }

            // install packages
            if (!InstallPackages())
            {
                return false;
            }

            SwitchToRoot(false);
            return true;
        }

        private void Print(string msg)
        {
            ProfilerPlugin.Instance.WriteToOutput(msg);
        }

        class InstalledWaiter : TizenAutoWaiter
        {
            public string InstalledStatus { set; get; }

            public InstalledWaiter()
            {
                this.InstalledStatus = null;
            }

            public override bool IsWaiterSet(string value)
            {
                if (!String.IsNullOrEmpty(value))
                {
                    InstalledStatus = value;
                    return true;
                }

                return false;
            }

            public override void OnExit()
            {
            }
        }
    }
}