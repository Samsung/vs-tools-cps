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
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using EnvDTE80;
using System.Xml;
using System.Xml.Linq;

namespace NetCore.Profiler.Extension.VSPackage
{
    public class VsProjectHelper
    {
        private static VsProjectHelper instance = null;
        private DTE2 dte2 = null;
        private Solution2 sol2 = null;
        private ServiceProvider serviceProvider = null;
        private IVsSolution solutionService = null;
        private readonly string TizenWebProjGuid = "{8E00536E-BD0D-4447-B307-F2C80A762AD0}";
        public readonly string CPPBasedProject = "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}";

        public static VsProjectHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new VsProjectHelper();
                }

                return instance;
            }
        }

        private VsProjectHelper()
        {
            dte2 = Package.GetGlobalService(typeof(SDTE)) as DTE2;
            sol2 = (Solution2)dte2.Solution;
            serviceProvider = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)this.dte2);
            solutionService = (IVsSolution)serviceProvider.GetService(typeof(IVsSolution));
        }

        public string GetManifestPackage(Project project)
        {
            return GetManifestNodeAttributeFromProject(project, "manifest", "package");
        }

        public string GetManifestVersion(Project project)
        {
            return GetManifestNodeAttributeFromProject(project, "manifest", "version");
        }

        public string GetManifestAppExec(Project project)
        {
            return GetManifestNodeAttributeFromProject(project, "ui-application", "exec", true);
        }

        public string GetManifestApplicationId(Project project)
        {
            return this.GetManifestNodeAttributeFromProject(project, "ui-application", "appid", true);
        }

        public string GetManifestNodeAttributeFromProject(Project project,
                                                string nodeName,
                                                string attributeName, bool IsUnknownNode = false)
        {
            string projectFolder = Path.GetDirectoryName(project.FullName);
            string manifestName = "tizen-manifest.xml";
            string manifestPath = Path.Combine(projectFolder, manifestName);

            if (IsUnknownNode)
            {
                return GetManifestUnknownNodeAttribute(manifestPath, attributeName);
            }
            else
            {
                return GetManifestNodeAttribute(manifestPath, nodeName, attributeName);
            }

        }

        private string GetManifestUnknownNodeAttribute(string manifestFilePath, string attributeName)
        {
            if (!File.Exists(manifestFilePath))
            {
                return String.Empty;
            }

            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(manifestFilePath);
            }
            catch (Exception)
            {
                return String.Empty;
            }

            string ret = string.Empty;
            XmlNodeList manifestNode = doc.GetElementsByTagName("manifest");
            if (manifestNode.Count == 1)
            {
                var elemList = manifestNode[0].ChildNodes;
                for (int i = 0; i < elemList.Count; i++)
                {
                    ret = elemList[i].Attributes?[attributeName]?.Value;
                    if (ret != null)
                    {
                        break;
                    }

                }
            }

            return ret;
        }

        private string GetManifestNodeAttribute(string manifestFilePath, string nodeName, string attributeName)
        {
            if (!File.Exists(manifestFilePath))
            {
                return String.Empty;
            }

            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.Load(manifestFilePath);
            }
            catch (Exception)
            {
                return String.Empty;
            }

            XmlNodeList nodeList = xmlDoc.GetElementsByTagName(nodeName);
            if (nodeList == null || nodeList.Count < 1)
            {
                return String.Empty;
            }

            foreach (XmlNode node in nodeList)
            {
                string appid = node.Attributes[attributeName].Value;
                if (!(String.IsNullOrEmpty(appid)) &&
                    !(String.IsNullOrWhiteSpace(appid)))
                {
                    return appid;
                }
            }

            return String.Empty;
        }

        public Projects GetProjects()
        {
            return this.dte2.Solution.Projects;
        }

        public bool IsHaveTizenNativeYaml(Project prj)
        {
            bool result = false;
            try
            {
                string projectFolder = Path.GetDirectoryName(prj.FullName);
                if (File.Exists(Path.Combine(projectFolder, "tizen_native_project.yaml")))
                {
                    result = true;
                }
            }
            catch
            {
            }

            return result;

        }

        public bool IsHaveTizenManifest(Project prj)
        {
            bool result = false;
            try
            {
                string projectFolder = Path.GetDirectoryName(prj.FullName);
                if (File.Exists(Path.Combine(projectFolder, "tizen-manifest.xml")))
                {
                    result = true;
                }
            }
            catch
            {
            }

            return result;

        }

        public bool IsTizenNativeProject()
        {
            if (this.sol2 == null || !this.sol2.IsOpen)
                return false;
            Projects ListOfProjectsInSolution = this.GetProjects();
            foreach (Project project in ListOfProjectsInSolution)
            {
                try
                {
                    if (IsHaveTizenManifest(project) || IsHaveTizenNativeYaml(project))
                    {
                        if (project.Kind == CPPBasedProject)
                        {
                            return true;
                        }
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("Tizen: " + e.Message);
                }
            }

            return false;
        }

        public bool IsTizenWebProject()
        {
            if (this.sol2 == null)
                return false;

            //Check for Tizen Web Project GUID
            Projects ListOfProjectsInSolution = this.GetProjects();

            foreach (Project project in ListOfProjectsInSolution)
            {
                try
                {
                    string projectFolder = Path.GetDirectoryName(project.FullName);
                    if (File.Exists(Path.Combine(projectFolder, "config.xml")))
                    {
                        if (project.Kind == "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}")
                        {
                            XDocument xmldoc = XDocument.Load(project.FullName);
                            XDocument xd = xmldoc.Document;

                            foreach (XElement element in xd.Descendants("ProjectTypeGuids"))
                            {
                                var val = element.Value;

                                if (val != null && val.ToUpper().Contains(TizenWebProjGuid))
                                {
                                    return true;
                                }
                            }
                        }
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("Tizen: " + e.Message);
                }
            }

            return false;
        }

        public String getSolutionFolderPath()
        {
            String result = null;
            Solution solution = dte2.Application.Solution;
            if (solution == null)
                return result;

            result = Path.GetDirectoryName(solution.FullName);

            return result;
        }

        public String getConfigXML()
        {
            String emptyPath = null;
            Solution solution = dte2.Application.Solution;
            if (solution == null)
                return emptyPath;
            Projects ListOfProjectsInSolution = solution.Projects;

            foreach (Project project in ListOfProjectsInSolution)
            {
                try
                {
                    String projectFolder = Path.GetDirectoryName(project.FullName);
                    String path = Path.Combine(projectFolder, "config.xml");
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Tizen: " + e.Message);
                }
            }
            return emptyPath;
        }
    }
}
