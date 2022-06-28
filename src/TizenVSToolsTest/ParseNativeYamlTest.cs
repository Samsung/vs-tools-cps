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
    [Description("Tizen.VisualStudio.TizenYamlParser ParseNativeYaml tests")]
    public class ParseNativeYamlTest
    {
        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        [Category("P1")]    // Positive Test Case
        [Description("Test if project_type value is native_app.")]
        [Property("AUTHOR", "Kunal Sinha, kunal.sinha@samsung.com")]
        [TestCase(@"\tizen_native_project.yaml", "native_app", true)]
        public void ParseNativeYaml_ProjectType_Return_True(string filePath, string projectType, bool expectedReturnValue)
        {
            var curDir = Directory.GetCurrentDirectory();
            var yamlFilePath = curDir + filePath;

            string yamlContent = File.ReadAllText(yamlFilePath);
            ParseNativeYaml nativeYaml = ParseNativeYaml.FromYaml(yamlContent);
            Assert.That(projectType == nativeYaml.ProjectType, Is.EqualTo(expectedReturnValue));
        }

        [Test]
        [Category("P2")]    // Negative Test Case
        [Description("Test if project_type value with web_app.")]
        [Property("AUTHOR", "Kunal Sinha, kunal.sinha@samsung.com")]
        [TestCase(@"\tizen_native_project.yaml", "web_app", false)]
        public void ParseNativeYaml_ProjectType_Return_False(string filePath, string projectType, bool expectedReturnValue)
        {
            var curDir = Directory.GetCurrentDirectory();
            var yamlFilePath = curDir + filePath;

            string yamlContent = File.ReadAllText(yamlFilePath);
            ParseNativeYaml nativeYaml = ParseNativeYaml.FromYaml(yamlContent);
            Assert.That(projectType == nativeYaml.ProjectType, Is.EqualTo(expectedReturnValue));
        }

        [Test]
        [Category("P1")]    // Positive Test Case
        [Description("Test if project_type value written to the yaml is retrieved tested with native_app.")]
        [Property("AUTHOR", "Kunal Sinha, kunal.sinha@samsung.com")]
        [TestCase(@"\tizen_native_project.yaml", "native_app", "native_app", true)]
        public void ParseNativeYaml_Write_ProjectType_Return_True(string filePath, string wProjectType, string rProjectType, bool expectedReturnValue)
        {
            var curDir = Directory.GetCurrentDirectory();
            var yamlFilePath = curDir + filePath;

            string yamlContent = File.ReadAllText(yamlFilePath);
            ParseNativeYaml nativeYaml = ParseNativeYaml.FromYaml(yamlContent);
            nativeYaml.ProjectType = wProjectType;
            string text = ParseNativeYaml.ToYaml(nativeYaml);
            File.WriteAllText(yamlFilePath, text);

            string ryamlContent = File.ReadAllText(yamlFilePath);
            ParseNativeYaml rNativeYaml = ParseNativeYaml.FromYaml(ryamlContent);
            Assert.That(rNativeYaml.ProjectType == rProjectType, Is.EqualTo(expectedReturnValue));
        }

        [Test]
        [Category("P2")]    // Negative Test Case
        [Description("Test if project_type value written to the yaml is retrieved tested with web_app.")]
        [Property("AUTHOR", "Kunal Sinha, kunal.sinha@samsung.com")]
        [TestCase(@"\tizen_native_project.yaml", "native_app", "web_app", false)]
        public void ParseNativeYaml_Write_ProjectType_Return_False(string filePath, string wProjectType, string rProjectType, bool expectedReturnValue)
        {
            var curDir = Directory.GetCurrentDirectory();
            var yamlFilePath = curDir + filePath;

            string yamlContent = File.ReadAllText(yamlFilePath);
            ParseNativeYaml nativeYaml = ParseNativeYaml.FromYaml(yamlContent);
            nativeYaml.ProjectType = wProjectType;
            string text = ParseNativeYaml.ToYaml(nativeYaml);
            File.WriteAllText(yamlFilePath, text);

            string ryamlContent = File.ReadAllText(yamlFilePath);
            ParseNativeYaml rNativeYaml = ParseNativeYaml.FromYaml(ryamlContent);
            Assert.That(rNativeYaml.ProjectType == rProjectType, Is.EqualTo(expectedReturnValue));
        }
    }
}
