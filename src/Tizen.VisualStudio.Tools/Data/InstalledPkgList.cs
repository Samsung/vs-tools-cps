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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tizen.VisualStudio.Tools.Data
{
    public class InstalledPkgList
    {
        private Dictionary<string, PkgList> Installed_PkgList;
        string RootPath;

        public InstalledPkgList()
        {
            this.RootPath = ToolsPathInfo.ToolsRootPath;
            Installed_PkgList = new Dictionary<string, PkgList>();
            if (!string.IsNullOrEmpty(this.RootPath))
            {
                Generate_InstalledPkgList();
            }
        }

        public InstalledPkgList(string RootPath)
        {
            this.RootPath = RootPath;
            Installed_PkgList = new Dictionary<string, PkgList>();
            if (!string.IsNullOrEmpty(this.RootPath))
            {
                Generate_InstalledPkgList();
            }
        }

        private void Generate_InstalledPkgList()
        {
            string Path_ListFile = Path.Combine(this.RootPath, @".info\installedpackage.list");
            string InstalledPkgFileStream;
            string[] PkgInfo;

            if (File.Exists(Path_ListFile))
            {
                InstalledPkgFileStream = File.ReadAllText(Path_ListFile, Encoding.UTF8);
            }
            else
            {
                return;
            }

            try
            {
                if (!string.IsNullOrEmpty(InstalledPkgFileStream))
                {
                    PkgInfo = InstalledPkgFileStream?.Split(new string[] { "\n\n" }, StringSplitOptions.None);
                    foreach (string PkgToken in PkgInfo)
                    {
                        string[] PkgComponent = PkgToken.Split('\n');
                        PkgList Package = new PkgList();
                        foreach (string Line in PkgComponent)
                        {
                            string[] Token = Line.Split(':');
                            Token[0] = Token[0]?.Trim();

                            if (Token.Length > 1)
                            {
                                Token[1] = Token[1]?.Trim();
                            }

                            if (Token[0] == "Package")
                            {
                                Package.Name = Token[1];
                            }
                            else if (Token[0] == "Label")
                            {
                                Package.Label = Token[1];
                            }
                            else if (Token[0] == "Description")
                            {
                                Package.Description = Token[1];
                            }
                            else if (Token[0] == "Version")
                            {
                                Version.TryParse(Token[1], out Version CurrentVersion);
                                Package.Version = CurrentVersion;
                            }
                        }

                        Installed_PkgList.Add(Package.Name, Package);
                    }
                }
                else
                {
                    return;
                }
            }
            catch
            {

            }
        }

        public PkgList GetPackage(string PackageName)
        {
            if (Installed_PkgList.TryGetValue(PackageName, out PkgList ResultPkg))
            {
                return ResultPkg;
            }
            else
            {
                return null;
            }
        }

        public string GetPkgInfo(string PackageName, string key)
        {
            if (Installed_PkgList.TryGetValue(PackageName, out PkgList ResultPkg))
            {
                if (key == "Label")
                {
                    return Installed_PkgList[PackageName].Label;
                }
                else if (key == "Package")
                {
                    return Installed_PkgList[PackageName].Name;
                }
                else if (key == "Description")
                {
                    return Installed_PkgList[PackageName].Description;
                }
                else
                {
                    return "";
                }
            }
            else
            {
                return "Package is not installed";
            }
        }

        public Version GetPkgVersion(string PackageName)
        {
            if (Installed_PkgList.TryGetValue(PackageName, out PkgList ResultPkg))
            {
                return ResultPkg.Version;
            }
            else
            {
                return new Version("0.0.0");
            }
        }
    }
}
