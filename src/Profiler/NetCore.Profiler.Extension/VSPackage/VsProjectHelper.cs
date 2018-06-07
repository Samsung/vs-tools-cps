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

namespace NetCore.Profiler.Extension.VSPackage
{
    public class VsProjectHelper
    {
        private static VsProjectHelper instance = null;
        private DTE2 dte2 = null;
        private Solution2 sol2 = null;
        private ServiceProvider serviceProvider = null;
        private IVsSolution solutionService = null;

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

    }
}
