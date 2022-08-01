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
    [Description("Tizen.VisualStudio.TizenYamlParser ParseWebYaml test")]
    public class ParseWebYamlTest
    {
        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        [Category("P1")]    // Positive Test Case
        [Description("Test if project_type value is web_app.")]
        [Property("AUTHOR", "Kunal Sinha, kunal.sinha@samsung.com")]
        [TestCase(@"\tizen_web_project.yaml", "web_app", true)]
        public void ParseWebYaml_ProjectType_Return_True(string filePath, string projectType, bool expectedReturnValue)
        {
            var curDir = Directory.GetCurrentDirectory();
            var yamlFilePath = curDir + filePath;

            string yamlContent = File.ReadAllText(yamlFilePath);
            ParseWebYaml webYaml = ParseWebYaml.FromYaml(yamlContent);
            Assert.That(projectType == webYaml.ProjectType, Is.EqualTo(expectedReturnValue));
        }

        [Test]
        [Category("P2")]    // Negative Test Case
        [Description("Test if project_type value with native_app.")]
        [Property("AUTHOR", "Kunal Sinha, kunal.sinha@samsung.com")]
        [TestCase(@"\tizen_web_project.yaml", "native_app", false)]
        public void ParseWebYaml_ProjectType_Return_False(string filePath, string projectType, bool expectedReturnValue)
        {
            var curDir = Directory.GetCurrentDirectory();
            var yamlFilePath = curDir + filePath;

            string yamlContent = File.ReadAllText(yamlFilePath);
            ParseWebYaml webYaml = ParseWebYaml.FromYaml(yamlContent);
            Assert.That(projectType == webYaml.ProjectType, Is.EqualTo(expectedReturnValue));
        }

        [Test]
        [Category("P1")]    // Positive Test Case
        [Description("Test if project_type value written to the yaml is retrieved tested with native_app.")]
        [Property("AUTHOR", "Kunal Sinha, kunal.sinha@samsung.com")]
        [TestCase(@"\tizen_web_project.yaml", "web_app", "web_app", true)]
        public void ParseWebYaml_Write_ProjectType_Return_True(string filePath, string wProjectType, string rProjectType, bool expectedReturnValue)
        {
            var curDir = Directory.GetCurrentDirectory();
            var yamlFilePath = curDir + filePath;

            string yamlContent = File.ReadAllText(yamlFilePath);
            ParseWebYaml webYaml = ParseWebYaml.FromYaml(yamlContent);
            webYaml.ProjectType = wProjectType;
            string text = ParseWebYaml.ToYaml(webYaml);
            File.WriteAllText(yamlFilePath, text);

            string ryamlContent = File.ReadAllText(yamlFilePath);
            ParseWebYaml rWebYaml = ParseWebYaml.FromYaml(ryamlContent);
            Assert.That(rWebYaml.ProjectType == rProjectType, Is.EqualTo(expectedReturnValue));
        }

        [Test]
        [Category("P2")]    // Negative Test Case
        [Description("Test if project_type value written to the yaml is retrieved tested with web_app.")]
        [Property("AUTHOR", "Kunal Sinha, kunal.sinha@samsung.com")]
        [TestCase(@"\tizen_web_project.yaml", "web_app", "native_app", false)]
        public void ParseWebYaml_Write_ProjectType_Return_False(string filePath, string wProjectType, string rProjectType, bool expectedReturnValue)
        {
            var curDir = Directory.GetCurrentDirectory();
            var yamlFilePath = curDir + filePath;

            string yamlContent = File.ReadAllText(yamlFilePath);
            ParseWebYaml webYaml = ParseWebYaml.FromYaml(yamlContent);
            webYaml.ProjectType = wProjectType;
            string text = ParseWebYaml.ToYaml(webYaml);
            File.WriteAllText(yamlFilePath, text);

            string ryamlContent = File.ReadAllText(yamlFilePath);
            ParseWebYaml rWebYaml = ParseWebYaml.FromYaml(ryamlContent);
            Assert.That(rWebYaml.ProjectType == rProjectType, Is.EqualTo(expectedReturnValue));
        }
    }
}
