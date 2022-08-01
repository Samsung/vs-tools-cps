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

using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Tizen.VisualStudio.APIChecker
{
    class Analyzer
    {
        private string apiversion;
        private string manifestFile;
        private List<string> privilegeList = new List<string>();
        private static Dictionary<string, bool> privilegeMap;
        private static Dictionary<string, bool> featureMap;
        private static string PRIVILEGE_TAG = "privilege";
        private static string FEATURE_TAG = "feature";
        //private static string API_VERSION = "apiversion";
        private IServiceProvider ServiceProvider = null;

        private Analyzer()
        {
        }

        public Analyzer(string apiversion, List<string> privileges, string manifestPath, IServiceProvider parent, List<string> features)
        {
            privilegeMap = new Dictionary<string, bool>();
            featureMap = new Dictionary<string, bool>();
            this.apiversion = apiversion;
            foreach (var s in privileges)
            {
                string res = s.Trim();
                if (privilegeMap.ContainsKey(res) == false)
                {
                    privilegeMap.Add(res, false);
                }
            }

            foreach (var s in features)
            {
                string res = s.Trim();
                if (featureMap.ContainsKey(res) == false)
                {
                    featureMap.Add(res, false);
                }
            }

            manifestFile = manifestPath;
            ServiceProvider = parent;
        }

        public void AnalyzeAPI(string apiname, string apiComment, string[] lineInfo, string filename)
        {
            string xmlTag = "<?xml version = \"1.0\" ?> <root>";
            apiComment = xmlTag + apiComment + "</root>";

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(apiComment);
            var nodes = xmlDoc.GetElementsByTagName(PRIVILEGE_TAG);
            var nodeFeature = xmlDoc.GetElementsByTagName(FEATURE_TAG);

            //Check Privilege Violations.
            CheckPrivilegeViolations(apiname, nodes, lineInfo[0], lineInfo[1], filename);

            //Check missing features
            CheckMissingFeatures(apiname, nodeFeature, lineInfo[0], lineInfo[1], filename);

            // TODO: Check API Violations.
            //checkAPIViolations(nodes);
        }

        public void ReportUnusedPrivilegesAndFeature()
        {
            ReportUnusedPrivileges();
            ReportUnusedFeatures();
        }

        public void ReportUnusedPrivileges()
        {
            ReportUnusedPrivilegesAndFeatures("The privilege {0} is unused", privilegeMap);
        }

        public void ReportUnusedPrivilegesAndFeatures(string msg, Dictionary<string, bool> map)
        {
            APICheckerWindowTaskProvider taskWindow = APICheckerWindowTaskProvider.CreateProvider(this.ServiceProvider);
            string[] lines = System.IO.File.ReadAllLines(manifestFile);
            foreach (KeyValuePair<string, bool> entry in map)
            {
                if (entry.Value == false)
                {
                    string warnMsg = string.Format(msg, entry.Key);
                    int lineNum = 0;
                    int columnNum = 0;
                    foreach (string line in lines)
                    {
                        columnNum = line.IndexOf(entry.Key);
                        if (columnNum > -1)
                        {
                            break;
                        }

                        lineNum++;
                    }

                    taskWindow.ReportUnusedPrivilegesAndFeatures(warnMsg, lineNum, columnNum, manifestFile);
                }
            }
        }

        public void ReportUnusedFeatures()
        {
            ReportUnusedPrivilegesAndFeatures("The feature {0} is unused", featureMap);
        }

        private void CheckAPIViolations(XmlNodeList nodes)
        {
            //throw new NotImplementedException();
        }

        private void CheckPrivilegeViolations(string apiname, XmlNodeList nodes, string lineStr, string columnStr, string filename)
        {
            string msg = "The API {0} needs these additions privilege {1}";
            CheckMissingPrivilegesAndFeatures(apiname, nodes, lineStr, columnStr, filename, privilegeMap, msg, TaskPriority.High);
        }

        private void CheckMissingPrivilegesAndFeatures(string apiname, XmlNodeList nodes, string lineStr, string columnStr, string filename, Dictionary<string, bool> map, string msg, TaskPriority priority)
        {
            List<string> RequiredList = new List<string>();
            foreach (XmlNode node in nodes)
            {
                string text = node.InnerText;
                text = text.Trim();
                if (map.ContainsKey(text) == false)
                {
                    RequiredList.Add(text);
                }
                else
                {
                    // Indicates the feature is used.
                    map[text] = true;
                }
            }

            int count = RequiredList.Count;
            if (count != 0)
            {
                int line, column;
                bool isValidLine = Int32.TryParse(lineStr, out line);
                bool isValidColum = Int32.TryParse(columnStr, out column);
                if (!isValidLine || !isValidColum)
                {
                    line = column = 0;
                }

                string message = string.Join(",", RequiredList.ToArray());
                string errorMsg = string.Format(msg, apiname, message);

                APICheckerWindowTaskProvider taskWindow = APICheckerWindowTaskProvider.CreateProvider(this.ServiceProvider);
                taskWindow.ReportMissingPrivilegesAndFeatures(RequiredList, apiname, line, column, filename, manifestFile, errorMsg, priority);
            }
        }

        private void CheckMissingFeatures(string apiname, XmlNodeList nodes, string lineStr, string columnStr, string filename)
        {
            string msg = "The API {0} needs these additions feature {1}";
            CheckMissingPrivilegesAndFeatures(apiname, nodes, lineStr, columnStr, filename, featureMap, msg, TaskPriority.Normal);
        }

    }
}
