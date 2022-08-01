/*
 * Copyright 2021 (c) Samsung Electronics Co., Ltd  All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 * Contributors
 * - SRIB
*/

using NUnit.Framework;
using System.IO;
using Tizen.VisualStudio.TizenYamlParser;

namespace Tizen.VisualStudio.Tools.UnitTests
{
    [TestFixture]
    [Description("Tizen.VisualStudio.TizenYamlParser ParseWorkspaceYaml test")]
    public class ParseWorkspaceYamlTest
    {
        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        [Category("P1")]    // Positive Test Case
        [Description("Test if arch value with x86.")]
        [Property("AUTHOR", "Kunal Sinha, kunal.sinha@samsung.com")]
        [TestCase(@"\tizen_workspace.yaml", "x86", true)]
        public void ParseWorkspaceYaml_Arch_Return_True(string filePath, string arch, bool expectedReturnValue)
        {
            var curDir = Directory.GetCurrentDirectory();
            var yamlFilePath = curDir + filePath;

            string yamlContent = File.ReadAllText(yamlFilePath);
            ParseWorkspaceYaml wkspceYaml = ParseWorkspaceYaml.FromYaml(yamlContent);
            Assert.That(arch == wkspceYaml.Arch, Is.EqualTo(expectedReturnValue));
        }

        [Test]
        [Category("P2")]    // Negative Test Case
        [Description("Test if arch value with arm.")]
        [Property("AUTHOR", "Kunal Sinha, kunal.sinha@samsung.com")]
        [TestCase(@"\tizen_workspace.yaml", "arm", false)]
        public void ParseWorkspaceYaml_Arch_Return_False(string filePath, string arch, bool expectedReturnValue)
        {
            var curDir = Directory.GetCurrentDirectory();
            var yamlFilePath = curDir + filePath;

            string yamlContent = File.ReadAllText(yamlFilePath);
            ParseWorkspaceYaml wkspceYaml = ParseWorkspaceYaml.FromYaml(yamlContent);
            Assert.That(arch == wkspceYaml.Arch, Is.EqualTo(expectedReturnValue));
        }

        [Test]
        [Category("P1")]    // Positive Test Case
        [Description("Test if project_type value written to the yaml is retrieved tested with native_app.")]
        [Property("AUTHOR", "Kunal Sinha, kunal.sinha@samsung.com")]
        [TestCase(@"\tizen_workspace.yaml", "x86", "x86", true)]
        public void ParseWorkspaceYaml_Write_Arch_Return_True(string filePath, string wProjectType, string rProjectType, bool expectedReturnValue)
        {
            var curDir = Directory.GetCurrentDirectory();
            var yamlFilePath = curDir + filePath;

            string yamlContent = File.ReadAllText(yamlFilePath);
            ParseWorkspaceYaml wkspceYaml = ParseWorkspaceYaml.FromYaml(yamlContent);
            wkspceYaml.Arch = wProjectType;
            string text = ParseWorkspaceYaml.ToYaml(wkspceYaml);
            File.WriteAllText(yamlFilePath, text);

            string ryamlContent = File.ReadAllText(yamlFilePath);
            ParseWorkspaceYaml rWkspceYaml = ParseWorkspaceYaml.FromYaml(ryamlContent);
            Assert.That(rWkspceYaml.Arch == rProjectType, Is.EqualTo(expectedReturnValue));
        }

        [Test]
        [Category("P2")]    // Negative Test Case
        [Description("Test if project_type value written to the yaml is retrieved tested with web_app.")]
        [Property("AUTHOR", "Kunal Sinha, kunal.sinha@samsung.com")]
        [TestCase(@"\tizen_workspace.yaml", "x86", "arm", false)]
        public void ParseWorkspaceYaml_Write_Arch_Return_False(string filePath, string wProjectType, string rProjectType, bool expectedReturnValue)
        {
            var curDir = Directory.GetCurrentDirectory();
            var yamlFilePath = curDir + filePath;

            string yamlContent = File.ReadAllText(yamlFilePath);
            ParseWorkspaceYaml wkspceYaml = ParseWorkspaceYaml.FromYaml(yamlContent);
            wkspceYaml.Arch = wProjectType;
            string text = ParseWorkspaceYaml.ToYaml(wkspceYaml);
            File.WriteAllText(yamlFilePath, text);

            string ryamlContent = File.ReadAllText(yamlFilePath);
            ParseWorkspaceYaml rWkspceYaml = ParseWorkspaceYaml.FromYaml(ryamlContent);
            Assert.That(rWkspceYaml.Arch == rProjectType, Is.EqualTo(expectedReturnValue));
        }
    }
}
