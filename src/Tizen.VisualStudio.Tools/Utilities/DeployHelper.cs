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
using System.Runtime.InteropServices;
using Tizen.VisualStudio.Tools.DebugBridge;
using Tizen.VisualStudio.Utilities;

namespace Tizen.VisualStudio.Tools.Utilities
{
    public static class DeployHelper
    {
        public static TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

        public const string SignatureFileExtension = ".signature";

        public static bool IsTizenVersionSupported(string tizenVersion)
        {
            Version version  = Version.Parse(tizenVersion);

            if (version >= new Version("4.0"))
                return true;
            return false;
        }

        public static string GetPackageSuffix(string architecture)
        {
            switch (architecture)
            {
                case "x86_64":
                    return "x86_64";
                case "x86":
                    return "i686";
                default:
                    return "armv7l";
            }
        }

        // key: RPM or Tar.Gz package name (without version); value: full package file name and version
        private class Packages : Dictionary<string, Tuple<string, Version>> { }

        // return enumerable tuples where
        // tuple item 1: package name (without version), e.g. "coreprofiler"
        // tuple item 2: package file name (with version), e.g. "coreprofiler-1.0.1-1.i686.rpm"
        // tuple item 3: package file version object
        public static IEnumerable<Tuple<string, string, Version>> GetLatestPackages(string folder,
            IEnumerable<string> requiredPackages, string architecture, bool getRpm, bool getTarGz, bool isSecure)
        {
            if (!(getRpm || getTarGz))
            {
                return null;
            }
            var packages = new Packages();
            var dir = new DirectoryInfo(folder);
            if (getRpm && !isSecure)
            {
                AddPackages(packages, requiredPackages, dir, $"*.{architecture}.rpm");
            }
            if (getTarGz)
            {
                AddPackages(packages, requiredPackages, dir, $"*-{architecture}.tar.gz");
            }
            return packages.Select(pair => new Tuple<string, string, Version>(
                pair.Key, pair.Value.Item1, pair.Value.Item2));
        }

        private static void AddPackages(Packages packages, IEnumerable<string> requiredPackages,
            DirectoryInfo directoryInfo, string fileMask)
        {
            FileInfo[] files = directoryInfo.GetFiles(fileMask);
            foreach (FileInfo file in files)
            {
                string packageName;
                Version packageVersion = ParsePackageVersion(file.Name, out packageName);
                if (packageVersion == null)
                {
                    Debug.WriteLine($"Cannot parse version of \"{file.Name}\"");
                    continue;
                }
                string requiredPackage = requiredPackages.FirstOrDefault((string name) => (name == packageName));
                if (requiredPackage != null)
                {
                    Tuple<string, Version> foundPackage;
                    if (packages.TryGetValue(requiredPackage, out foundPackage))
                    {
                        if (packageVersion <= foundPackage.Item2)
                        {
                            continue;
                        }
                    }
                    packages[requiredPackage] = new Tuple<string, Version>(file.Name, packageVersion);
                }
            } // foreach
        }

        public static bool PushFile(SDBDeviceInfo device, string sourceFileName, string destinationFileName,
            Func<bool, string, bool> onLineRead, out string errorMessage, TimeSpan? timeout = null)
        {
            if (!File.Exists(sourceFileName))
            {
                errorMessage = $"File \"{sourceFileName}\" not found";
                return false;
            }
            string sdbErr = null;
            string sdbOutput = null;
            int exitResult = 0;
            SDBLib.SdbRunResult sdbResult = SDBLib.RunSdbCommand(device,
                $"push \"{sourceFileName}\" \"{destinationFileName}\"",
                (bool isStdOut, string line) =>
                {
                    bool stop = false;
                    if (onLineRead != null)
                    {
                        stop = onLineRead(isStdOut, line);
                    }
                    if (line.Contains("1 file(s) pushed"))
                    {
                        stop = true;
                    }
                    if (line.StartsWith("error:"))
                    {
                        sdbErr = line;
                        stop = true;
                    }
                    sdbOutput += line;
                    return stop;
                },
                out exitResult,
                timeout ?? DefaultTimeout);
            if (sdbResult == SDBLib.SdbRunResult.Success && exitResult == 0)
            {
                if (sdbErr == null)
                {
                    errorMessage = "";
                    return true;
                }
            }
            errorMessage = $"Cannot push \"{sourceFileName}\" to \"{destinationFileName}\"";
            if (sdbResult != SDBLib.SdbRunResult.Success)
            {
                errorMessage = StringHelper.CombineMessages(errorMessage, SDBLib.FormatSdbRunResult(sdbResult));
            }
            if (sdbOutput != null)
            {
                errorMessage = StringHelper.CombineMessages(errorMessage, sdbOutput);
            }
            return false;
            //if (sdbErr != null)
            //{
            //    errorMessage = StringHelper.CombineMessages(errorMessage, sdbErr);
            //}
            //return false;
        }

        public static bool InstallTpk(SDBDeviceInfo device, string tpkFileName,
            Func<bool, string, bool> onLineRead, out string errorMessage, TimeSpan? timeout = null)
        {
            if (!File.Exists(tpkFileName))
            {
                errorMessage = $"File \"{tpkFileName}\" not found";
                return false;
            }
            string sdbErr = null;
            int exitCode;
            SDBLib.SdbRunResult sdbResult = SDBLib.RunSdbCommand(device,
                $"install \"{tpkFileName}\"",
                (bool isStdOut, string line) =>
                {
                    bool stop = false;
                    if (onLineRead != null)
                    {
                        stop = onLineRead(isStdOut, line);
                    }
                    if (line.StartsWith("spend time for pkgcmd is"))
                    {
                        stop = true;
                    }
                    if (line.StartsWith("error:"))
                    {
                        sdbErr = line;
                        stop = true;
                    }
                    return stop;
                },
                out exitCode, timeout ?? DefaultTimeout);
            if (sdbResult == SDBLib.SdbRunResult.Success)
            {
                if (sdbErr == null)
                {
                    errorMessage = "";
                    return true;
                }
            }
            errorMessage = StringHelper.CombineMessages($"Cannot install TPK", SDBLib.FormatSdbRunResult(sdbResult, exitCode));
            if (sdbErr != null)
            {
                if (!sdbErr.Contains(tpkFileName))
                {
                    errorMessage = StringHelper.CombineMessages(errorMessage, $"Package: \"{tpkFileName}\"");
                }
                errorMessage = StringHelper.CombineMessages(errorMessage, sdbErr);
            }
            return false;
        }

        public static bool IsRmpPackageInstalled(SDBDeviceInfo device, string packageName,
            out string installedPackageName, out string errorMessage, TimeSpan? timeout = null)
        {
            string command = $"rpm -q {packageName}";
            installedPackageName = "";
            string lastNonEmptyLine = "";
            bool success = SDBLib.RunSdbShellCommandAndCheckExitStatus(device, command,
                (bool isStdOut, string line) =>
                {
                    if (line != "")
                    {
                        lastNonEmptyLine = line;
                    }
                    return false; // continue processing
                },
                out errorMessage, timeout);
            if (success)
            {
                if (!String.IsNullOrEmpty(lastNonEmptyLine))
                {
                    installedPackageName = lastNonEmptyLine;
                }
                else
                {
                    errorMessage = $"Cannot check RPM package \"{packageName}\"";
                    success = false;
                }
            }
            else
            {
                if (lastNonEmptyLine.EndsWith("is not installed"))
                {
                    errorMessage = ""; // no error, package just not installed
                }
                else
                {
                    if (!(errorMessage.Contains(packageName) || lastNonEmptyLine.Contains(packageName)))
                    {
                        errorMessage = StringHelper.CombineMessages(errorMessage, $"Package: \"{packageName}\"");
                    }
                    errorMessage = StringHelper.CombineMessages(errorMessage, lastNonEmptyLine);
                }
            }
            return success;
        }

        public static bool InstallRpmPackage(SDBDeviceInfo device, string rpmFileName, out string errorMessage,
            TimeSpan? timeout = null)
        {
            string command = $"rpm -U --force {rpmFileName}";
            string lastNonEmptyLine = "";
            bool success = SDBLib.RunSdbShellCommandAndCheckExitStatus(device, command,
                (bool isStdOut, string line) =>
                {
                    if (line != "")
                    {
                        lastNonEmptyLine = line;
                    }
                    return false; // continue processing
                },
                out errorMessage, timeout);
            if (!success)
            {
                if (!(errorMessage.Contains(rpmFileName) || lastNonEmptyLine.Contains(rpmFileName)))
                {
                    errorMessage = StringHelper.CombineMessages(errorMessage, $"Package: \"{rpmFileName}\"");
                }
                errorMessage = StringHelper.CombineMessages(errorMessage, lastNonEmptyLine);
            }
            return success;
        }

        public static bool ExtractTarGzip(SDBDeviceInfo device, string tarGzipFileName, string destinationPath,
            out string errorMessage, TimeSpan? timeout = null)
        {
            string command = $"mkdir -p {destinationPath} && tar -xvf \"{tarGzipFileName}\" -C {destinationPath}";
            string tarError = null;
            string lastNonEmptyLine = "";
            bool success = SDBLib.RunSdbShellCommandAndCheckExitStatus(device, command,
                (bool isStdOut, string line) =>
                {
                    if ((tarError == null) && line.StartsWith("tar: ") && line.Contains("Cannot"))
                    {
                        tarError = line;
                    }
                    if (line != "")
                    {
                        lastNonEmptyLine = line;
                    }
                    return false; // continue processing
                },
                out errorMessage, timeout);
            if (!success)
            {
                if (String.IsNullOrEmpty(tarError))
                {
                    tarError = lastNonEmptyLine;
                }
                if (!String.IsNullOrEmpty(tarError))
                {
                    if (!(errorMessage.Contains(tarGzipFileName) || tarError.Contains(tarGzipFileName)))
                    {
                        errorMessage = StringHelper.CombineMessages(errorMessage, $"Package: \"{tarGzipFileName}\"");
                    }
                    errorMessage = StringHelper.CombineMessages(errorMessage, tarError);
                }
                if (String.IsNullOrEmpty(errorMessage))
                {
                    errorMessage = $"Cannot extract \"{tarGzipFileName}\" to {destinationPath}";
                }
            }
            return success;
        }

        public static bool ListAndGetFirstLine(SDBDeviceInfo device, string pathName, out string firstLine, out string errorMessage)
        {
            string s = null;
            bool result = (SDBLib.RunSdbShellCommandAndCheckExitStatus(device, $"ls -U {pathName}", // "-U" is for speed
                (bool isStdOut, string line) =>
                {
                    if ((s == null) && isStdOut && (line != ""))
                    {
                        s = line;
                    }
                    return false; // we want to get the exit status so nether return true
                },
                out errorMessage));
            firstLine = s;
            return result && (firstLine != null);
        }

        public static bool FileExists(SDBDeviceInfo device, string fullFileName, out string errorMessage)
        {
            string firstLine;
            bool result = ListAndGetFirstLine(device, fullFileName, out firstLine, out errorMessage);
            if (result)
            {
                if (fullFileName != firstLine)
                {
                    errorMessage = $"Unexpected line received: \"{firstLine}\"";
                    result = false;
                }
            }
            return result;
        }

        public static Version GetInstalledPackageVersion(SDBDeviceInfo device, string versionFileMask, out string errorMessage)
        {
            Version result = null;
            string firstLine;
            if (ListAndGetFirstLine(device, versionFileMask, out firstLine, out errorMessage))
            {
                if (firstLine != null)
                {
                    int i = firstLine.LastIndexOf('/');
                    if (i > 0)
                    {
                        result = ParsePackageVersion(firstLine.Substring(i + 1));
                    }
                }
            }
            else if (firstLine != null)
            {
                errorMessage = firstLine;
            }
            return result;
        }

        // Examples:
        // coreprofiler-1.2.3-4.i686.rpm -> Version == "1.2.3.4"
        // lldb-tv-5.6.7-armv7l.tar.gz -> Version == "5.6.7"
        public static Version ParsePackageVersion(string packageFileName)
        {
            string packageName;
            return ParsePackageVersion(packageFileName, out packageName);
        }

        /// <summary>
        /// Parse package file name which is expected to use the following format:
        /// <package_name>-<version>-<architecture>.<extension> or
        /// <package_name>-<version>.<architecture>.<extension>
        /// where version part consists from 1 to 4 numerical parts separated by '.' or
        /// from 1 to 3 numerical parts separated by '.' and one more numerical part after '-',
        /// e.g. "1" or "1.2" or "1.2.3" or "1.2.3.4" or "1-2" or "1.2-3" or "1.2.3-4".
        /// The architecture part must start from an alpha character.
        /// Examples:
        /// coreprofiler-1.2.3-4.i686.rpm -> Version == "1.2.3.4", packageName = "coreprofiler"
        /// lldb-tv-5.6.7-armv7l.tar.gz -> Version == "5.6.7", packageName = "lldb-tv"
        /// </summary>
        /// <param name="packageFileName">The source file name</param>
        /// <param name="packageName">Package name (part before version) or ""</param>
        /// <returns>Package version or null</returns>
        public static Version ParsePackageVersion(string packageFileName, out string packageName)
        {
            packageName = "";
            string[] parts = packageFileName.Split('-');
            int charsPassed = 0;
            for (int i = 0, partsLength = parts.Length; i < partsLength; ++i)
            {
                bool noMinor = false;
                Version result;
                if (!Version.TryParse(parts[i], out result))
                {
                    int n;
                    if (int.TryParse(parts[i], out n))
                    {
                        result = new Version(n, 0);
                        noMinor = true;
                    }
                }
                if (result != null)
                {
                    if (result.Revision < 0)
                    {
                        int iNext = i + 1;
                        if (iNext < partsLength)
                        {
                            string nextPart = parts[iNext];
                            int j = nextPart.IndexOfAny(new[] { '.', '-' });
                            if (j > 0)
                            {
                                nextPart = nextPart.Remove(j);
                            }
                            int number;
                            if (int.TryParse(nextPart, out number))
                            {
                                if (noMinor)
                                {
                                    result = new Version(result.Major, number);
                                }
                                else if (result.Build < 0)
                                {
                                    result = new Version(result.Major, result.Minor, number);
                                }
                                else
                                {
                                    result = new Version(result.Major, result.Minor, result.Build, number);
                                }
                            }
                        }
                    }
                    if (charsPassed > 0)
                    {
                        packageName = packageFileName.Substring(0, charsPassed - 1);
                    }
                    return result;
                }
                charsPassed += parts[i].Length + 1;
            }
            return null;
        }

#region Unit tests
        [Conditional("DEBUG")]
        static void TestParsePackageVersion()
        {
            Version v;
            string packageName;
            v = ParsePackageVersion("", out packageName);
            Debug.Assert((v == null) && (packageName == ""));
            v = ParsePackageVersion("a", out packageName);
            Debug.Assert((v == null) && (packageName == ""));
            v = ParsePackageVersion("n-1", out packageName);
            Debug.Assert((v == new Version(1, 0)) && (packageName == "n"));
            v = ParsePackageVersion("n-1.2", out packageName);
            Debug.Assert((v == new Version(1, 2)) && (packageName == "n"));
            v = ParsePackageVersion("n0-1.2.3", out packageName);
            Debug.Assert((v == new Version(1, 2, 3)) && (packageName == "n0"));
            v = ParsePackageVersion("n-1.2.3.4", out packageName);
            Debug.Assert((v == new Version(1, 2, 3, 4)) && (packageName == "n"));
            v = ParsePackageVersion("n0-1-2", out packageName);
            Debug.Assert((v == new Version(1, 2)) && (packageName == "n0"));
            v = ParsePackageVersion("n55-1.2-3", out packageName);
            Debug.Assert((v == new Version(1, 2, 3)) && (packageName == "n55"));
            v = ParsePackageVersion("n-1.2.3-4", out packageName);
            Debug.Assert((v == new Version(1, 2, 3, 4)) && (packageName == "n"));
            v = ParsePackageVersion("coreprofiler-1.2.3-4.i686.rpm", out packageName);
            Debug.Assert((v == new Version(1, 2, 3, 4)) && (packageName == "coreprofiler"));
            v = ParsePackageVersion("lldb-3.8.1-armv7l.tar.gz", out packageName);
            Debug.Assert((v == new Version(3, 8, 1)) && (packageName == "lldb"));
            v = ParsePackageVersion("lldb-tv-3.8.1-i686.tar.gz", out packageName);
            Debug.Assert((v == new Version(3, 8, 1)) && (packageName == "lldb-tv"));
            v = ParsePackageVersion("lttng-tools-2.9.3-4.x86_64.rpm", out packageName);
            Debug.Assert((v == new Version(2, 9, 3, 4)) && (packageName == "lttng-tools"));
        }
#if DEBUG
        static DeployHelper()
        {
            TestParsePackageVersion();
        }
#endif
#endregion

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int memcmp(byte[] b1, byte[] b2, IntPtr count);

        public static bool AreFilesEqual(string file1, string file2)
        {
            var fi1 = new FileInfo(file1);
            var fi2 = new FileInfo(file2);
            bool equal = (fi1.Length == fi2.Length);
            if (equal)
            {
                const int BufferSize = 8192;
                var buffer1 = new byte[BufferSize];
                var buffer2 = new byte[BufferSize];
                using (FileStream stream1 = fi1.OpenRead())
                using (FileStream stream2 = fi2.OpenRead())
                {
                    while (true)
                    {
                        int read1 = stream1.Read(buffer1, 0, BufferSize);
                        int read2 = stream2.Read(buffer2, 0, BufferSize);
                        if (read1 != read2)
                        {
                            equal = false;
                            break;
                        }
                        if (read1 == 0)
                        {
                            break;
                        }
                        if (memcmp(buffer1, buffer2, new IntPtr(read1)) != 0)
                        {
                            equal = false;
                            break;
                        }
                    }
                }
            }
            return equal;
        }
    }
}
