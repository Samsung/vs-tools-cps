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
using System.Xml;
using System.IO;
using EnvDTE;

namespace Tizen.VisualStudio.ResourceManager
{
    public class XmlWriter
    {
        public static readonly string STR_res = "res";
        public static readonly string STR_contents = "contents";
        public static readonly string STR_folder = "folder";


        public static void updateResourceXML(Project project)
        {

            string projFullName = project.FullName;
            string projPath = projFullName.Substring(0, projFullName.LastIndexOf("\\") + 1);
            string resFolderPath = projPath + "res\\";
            XmlDocument doc = new XmlDocument();
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", "yes");
            doc.AppendChild(docNode);

            XmlNode rootNode = doc.CreateElement(STR_res, "http://tizen.org/ns/rm");
            doc.AppendChild(rootNode);

            XmlElement groupImageNode = doc.CreateElement("group-image", "http://tizen.org/ns/rm");
            groupImageNode.SetAttribute(STR_folder, STR_contents);
            rootNode.AppendChild(groupImageNode);

            XmlElement groupLayoutNode = doc.CreateElement("group-layout", "http://tizen.org/ns/rm");
            groupLayoutNode.SetAttribute(STR_folder, STR_contents);
            rootNode.AppendChild(groupLayoutNode);

            XmlElement groupSoundNode = doc.CreateElement("group-sound", "http://tizen.org/ns/rm");
            groupSoundNode.SetAttribute(STR_folder, STR_contents);
            rootNode.AppendChild(groupSoundNode);

            XmlElement groupBinNode = doc.CreateElement("group-bin", "http://tizen.org/ns/rm");
            groupBinNode.SetAttribute(STR_folder, STR_contents);
            rootNode.AppendChild(groupBinNode);

            DirectoryInfo di = new DirectoryInfo(@resFolderPath + STR_contents);
            if (!di.Exists)
            {
                return;
            }
            foreach (XmlNode groupNode in doc.DocumentElement.ChildNodes)
            {
                foreach (var fi in di.GetDirectories())
                {
                    String languageID = null;
                    String resolutionRange = null;
                    String folderPath = null;

                    String fileName = fi.Name;
                    folderPath = "contents/" + fileName;
                    if (fileName.Contains("-"))
                    {
                        String[] names = fileName.Split('-');
                        names[0] = names[0];
                        if (ResourceManagerUtil.isValidLanguageID(names[0]))
                        {
                            languageID = names[0].Equals("default_All") ? "All" : names[0];
                        }
                        if (ResourceManagerUtil.isValidResolution(names[1]))
                        {
                            resolutionRange = ResourceManagerUtil.getResolution(names[1]);
                        }
                    }
                    if (languageID == null || resolutionRange == null)
                    {
                        continue;
                    }
                    else
                    {
                        XmlElement node = doc.CreateElement("node", "http://tizen.org/ns/rm");
                        XmlAttribute folder = doc.CreateAttribute(STR_folder);
                        folder.Value = folderPath;
                        node.Attributes.Append(folder);
                        if (resolutionRange.Length != 0)
                        {
                            XmlAttribute screen_dpi_range = doc.CreateAttribute("screen-dpi-range");
                            screen_dpi_range.Value = resolutionRange;
                            node.Attributes.Append(screen_dpi_range);
                        }
                        // Language attribute is not emitted when ALL language is selected
                        if (!languageID.Equals("All"))
                        {
                            XmlAttribute language = doc.CreateAttribute("language");
                            language.Value = languageID;
                            node.Attributes.Append(language);
                        }

                        groupNode.AppendChild(node);
                    }
                }
            }

            doc.Save(@resFolderPath + "res.xml");
        }
    }
}
