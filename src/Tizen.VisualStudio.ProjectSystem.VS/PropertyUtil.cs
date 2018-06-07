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

using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Tizen.VisualStudio
{
    public static class PropertyUtil
    {


        public static XmlDocument GetCSProject(string csprojPath)
        {
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(csprojPath);

            return xmldoc;
        }

        public static XmlNode GetProjectFlavorCfgNodeInCSProj(XmlDocument xmldoc,
                                                              XmlNamespaceManager ns,
                                                              string solutionConfig,
                                                              string guid,
                                                              string cfgClassName)
        {
            XmlNode node = xmldoc.SelectSingleNode("//*/ms:VisualStudio/ms:FlavorProperties[@Configuration=" + "'" + solutionConfig + "'" + " and @GUID='{" + guid + "}' ]", ns);
            if (node == null)
            {
                return null;
            }

            node = node.SelectSingleNode("ms:" + cfgClassName, ns);
            return node;
        }

        private static XmlNamespaceManager GetProjectNSManager(string csprojPath, out XmlDocument xmldoc)
        {
            xmldoc = GetCSProject(csprojPath);
            XmlNamespaceManager ns = new XmlNamespaceManager(xmldoc.NameTable);
            ns.AddNamespace("ms", "http://schemas.microsoft.com/developer/msbuild/2003");

            return ns;
        }

        public static string GetStringProperty(Project project, string guid, string cfgName, string propertyName)
        {
            string value = string.Empty;
            string solutionConfig = GetPrjSolutionConfig(project);
            if (project == null || string.IsNullOrEmpty(solutionConfig))
            {
                return value;
            }

            string csprojPath = project.FullName;

            try
            {
                XmlDocument xmldoc;
                XmlNamespaceManager ns = GetProjectNSManager(csprojPath, out xmldoc);
                XmlNode node = GetProjectFlavorCfgNodeInCSProj(xmldoc, ns, solutionConfig, guid, cfgName);

                if (node == null)
                {
                    return value;
                }

                if (ExistChildNode(node, propertyName))
                {
                    value = node.SelectSingleNode("ms:" + propertyName, ns).InnerText;
                }
            }
            catch
            {
                throw new Exception("Not Exist " + csprojPath + "File. Check the file.");
            }

            return value;
        }

        public static bool GetBoolProperty(Project project, string guid, string cfgName, string propertyName)
        {
            bool value = false;
            string solutionConfig = GetPrjSolutionConfig(project);
            if (project == null || string.IsNullOrEmpty(solutionConfig))
            {
                return value;
            }

            string csprojPath = project.FullName;

            try
            {
                XmlDocument xmldoc;
                XmlNamespaceManager ns = GetProjectNSManager(csprojPath, out xmldoc);
                XmlNode node = GetProjectFlavorCfgNodeInCSProj(xmldoc, ns, solutionConfig, guid, cfgName);

                if (node == null)
                {
                    return value;
                }

                if (ExistChildNode(node, propertyName))
                {
                    string valueText = node.SelectSingleNode("ms:" + propertyName, ns).InnerText;
                    value = Convert.ToBoolean(valueText);
                }
            }
            catch
            {
                throw new Exception("Not Exist " + csprojPath + "File. Check the file.");
            }

            return value;

        }

        public static bool ExistChildNode(XmlNode node, string childNodeName)
        {
            bool result = false;
            if (node == null)
            {
                return result;
            }

            XmlNodeList childNodeLists = node.ChildNodes;
            foreach (XmlNode child in childNodeLists)
            {
                if (child.Name == childNodeName)
                {
                    return true;
                }
            }

            return result;
        }

        public static string GetPrjSolutionConfig(Project project)
        {
            string solutionConfig = string.Empty;

            solutionConfig = project?.ConfigurationManager?.ActiveConfiguration?.ConfigurationName
                                + "|"
                                + project?.ConfigurationManager?.ActiveConfiguration?.PlatformName;

            return solutionConfig;
        }
    }
}
