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

namespace Tizen.VisualStudio.Utilities
{
    public class CheckCycle
    {
        private Dictionary<string, List<string>> adjList = new Dictionary<string, List<string>>();
        private HashSet<string> visited = new HashSet<string>();

        public void addEdge(string source, string dest)
        {
            if (!adjList.ContainsKey(source))
            {
                adjList.Add(source, new List<string>());
            }
            List<string> list = adjList[source];
            if (list.Contains(dest) == false)
            {
                list.Add(dest);
            }
        }

        private bool search(string pkg, string source)
        {
            bool result = false;
            if (visited.Contains(pkg))
            {
                return result;
            }
            visited.Add(pkg);
            List<string> children;
            if (!adjList.TryGetValue(pkg, out children))
            {
                return result;
            }
            foreach(string child in children)
            {
                if (result == true)
                    break;
                if (child.Equals(source))
                {
                    result = true;
                    break;
                }
                result |= search(child, source);
            }
            return result;
        }

        
        public bool checkCycle(string src, string dest)
        {
            visited.Clear();
            return search(src, dest);
        }

        public bool checkPath(string src, string dest)
        {
            if (adjList.ContainsKey(src))
            {
                foreach (string child in adjList[src])
                    if (child.Equals(dest))
                    {
                        return true;
                    }
            }
            return false;
        }
    }
}
