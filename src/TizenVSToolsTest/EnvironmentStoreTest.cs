/*
 * Copyright 2020 (c) Samsung Electronics Co., Ltd  All Rights Reserved.
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
*/

using NUnit.Framework;
using Tizen.VisualStudio.Tools.Data;
using System;
using System.IO;

namespace Tizen.VisualStudio.Tools.UnitTests
{
    [TestFixture]
    [Description("Tizen.VisualStudio.Tools.Data Environment Store tests")]
    public class EnvironmentStoreTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        [Category("P1")]
        [Description("Test if BuilderPath is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase(true)]
        public void GetBuilderPath_CorrectValue_ReturnTrue(bool expectedValue)
        {
            string programFilePath = Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFilesX86);
            string builderPath = @"MSBuild\Tizen\bin";
            string expectedPath = Path.Combine(programFilePath, builderPath);
            Assert.That(EnvironmentStore.BuilderPath.Equals(expectedPath), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]
        [Description("Test if BuilderPath is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase(false)]
        public void GetBuilderPath_IncorrectValue_ReturnFalse(bool expectedValue)
        {
            Assert.That(EnvironmentStore.BuilderPath.Equals(Directory.GetCurrentDirectory()), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P1")]
        [Description("Test if XmlSec is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase(true)]
        public void GetXmlSec_CorrectValue_ReturnTrue(bool expectedValue)
        {
            string programFilePath = Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFilesX86);
            string builderPath = @"MSBuild\Tizen\bin";
            string xmlSec = @"libxmlsec-1.2.18\bin\xmlsec.exe";
            string expectedPath = Path.Combine(programFilePath, builderPath, xmlSec);
            Assert.That(EnvironmentStore.XmlSec.Equals(expectedPath), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]
        [Description("Test if XmlSec is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase(false)]
        public void GetXmlSec_IncorrectValue_ReturnFalse(bool expectedValue)
        {
            Assert.That(EnvironmentStore.XmlSec.Equals(Directory.GetCurrentDirectory()), Is.EqualTo(expectedValue));
        }
    }
}
