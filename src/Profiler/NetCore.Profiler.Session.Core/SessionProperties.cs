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
using System.Xml;

namespace NetCore.Profiler.Session.Core
{
    public class SessionProperties : ISessionProperties
    {
        private readonly Dictionary<string, Dictionary<string, string>> _sessionProps = new Dictionary<string, Dictionary<string, string>>();

        public string FileName { get; set; }

        public SessionProperties(string filename)
        {
            FileName = filename;
        }

        public void Load()
        {
            if (string.IsNullOrEmpty(FileName))
            {
                throw new InvalidOperationException("FileName is not set");
            }

            _sessionProps.Clear();
            LoadPropertiesContainer(_sessionProps, FileName);
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(FileName))
            {
                throw new InvalidOperationException("FileName is not set");
            }

            SavePropertiesContainer(_sessionProps, FileName);
        }

        public string GetProperty(string mainKey, string key)
        {
            return GetContainerProperty(_sessionProps, mainKey, key);
        }

        public bool GetBoolProperty(string mainKey, string key, bool defaultValue)
        {
            var s = GetContainerProperty(_sessionProps, mainKey, key);
            if (string.IsNullOrEmpty(s))
            {
                return defaultValue;
            }

            switch (s)
            {
                case "1":
                    return true;
                case "0":
                    return false;
                default:
                    return defaultValue;
            }
        }

        public int GetIntProperty(string mainKey, string key, int defaultValue)
        {
            var s = GetContainerProperty(_sessionProps, mainKey, key);
            return string.IsNullOrEmpty(s) ? defaultValue : Convert.ToInt32(s);
        }

        public void SetProperty(string mainKey, string key, string value)
        {
            SetContainerProperty(_sessionProps, mainKey, key, value);
            Save();
        }

        public void SetProperty(string mainKey, string key, bool value)
        {
            SetContainerProperty(_sessionProps, mainKey, key, value ? "1" : "0");
            Save();
        }

        public void SetProperty(string mainKey, string key, int value)
        {
            SetContainerProperty(_sessionProps, mainKey, key, value.ToString());
            Save();
        }

        public bool PropertyExists(string mainKey)
        {
            return ContainerPropertyExists(_sessionProps, mainKey);
        }

        public bool PropertyExists(string mainKey, string key)
        {
            return ContainerPropertyExists(_sessionProps, mainKey, key);
        }

        private static void LoadPropertiesContainer(IDictionary<string, Dictionary<string, string>> container, string path)
        {
            var document = new XmlDocument();
            document.Load(Path.GetFullPath(path));
            foreach (XmlNode node in document.ChildNodes)
            {
                if (node.Name == "Session")
                {
                    foreach (XmlNode prop in node.ChildNodes)
                    {
                        container.Add(prop.Name, new Dictionary<string, string>());
                        if (prop.Attributes != null)
                        {
                            foreach (XmlNode attr in prop.Attributes)
                            {
                                container[prop.Name].Add(attr.Name, attr.Value);
                            }
                        }
                    }
                }
            }
        }

        private static void SavePropertiesContainer(IDictionary<string, Dictionary<string, string>> container, string path)
        {
            using (var fs = new StreamWriter(Path.GetFullPath(path)))
            {
                fs.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                fs.WriteLine("<Session>");
                foreach (var i in container)
                {
                    fs.Write($"<{i.Key}");
                    foreach (var j in i.Value)
                    {
                        fs.Write($" {j.Key}=\"{j.Value}\"");
                    }

                    fs.WriteLine(" />");
                }

                fs.WriteLine("</Session>");
            }
        }

        private static string GetContainerProperty(IReadOnlyDictionary<string, Dictionary<string, string>> container,
            string mainKey, string key)
        {
            if (container.ContainsKey(mainKey) && container[mainKey].ContainsKey(key))
            {
                return container[mainKey][key];
            }

            return string.Empty;
        }

        private static bool ContainerPropertyExists(IReadOnlyDictionary<string, Dictionary<string, string>> container, string mainKey)
        {
            return container.ContainsKey(mainKey);
        }

        private static bool ContainerPropertyExists(IReadOnlyDictionary<string, Dictionary<string, string>> container, string mainKey, string key)
        {
            return (container.ContainsKey(mainKey) && container[mainKey].ContainsKey(key));
        }

        private void SetContainerProperty(IDictionary<string, Dictionary<string, string>> container, string mainKey, string key, string value)
        {
            Dictionary<string, string> propContainer;
            if (container.ContainsKey(mainKey))
            {
                propContainer = container[mainKey];
            }
            else
            {
                container.Add(mainKey, propContainer = new Dictionary<string, string>());
            }

            if (propContainer.ContainsKey(key))
            {
                propContainer.Remove(key);
            }

            propContainer.Add(key, value);
        }
    }
}
