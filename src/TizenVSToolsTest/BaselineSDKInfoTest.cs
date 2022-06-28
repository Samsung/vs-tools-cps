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

namespace Tizen.VisualStudio.Tools.UnitTests
{
    [TestFixture]
    [Description("Tizen.VisualStudio.Tools.Data BaseLine SDK Info tests")]
    public class BaselineSDKInfoTests
    {

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        [Category("P1")]
        [Description("Test if Get32InstallerURL() method returns True for the correct URL.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase ("http://download.tizen.org/sdk/Installer/Latest/Baseline_Tizen_Studio_windows-32.exe", true)]
        public void Get32InstallerURL_URLIsCorrect_ReturnTrue(string url, bool expectedValue)
        {
            Assert.That(url.Equals(BaselineSDKInfo.Get32InstallerURL()), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]
        [Description("Test if Get32InstallerURL() method returns false for the incorrect URL.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase("http://testurl.exe", false)]
        public void Get32InstallerURL_URLIsIncorrect_ReturnFalse(string url, bool expectedValue)
        {
            Assert.That(url.Equals(BaselineSDKInfo.Get32InstallerURL()), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P1")]
        [Description("Test if Get64InstallerURL() method returns True for the correct URL.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase("https://download.tizen.org/sdk/Installer/Latest/Baseline_Tizen_Studio_windows-64.exe", true)]
        public void Get64InstallerURL_URLIsCorrect_ReturnTrue(string url, bool expectedValue)
        {
            Assert.That(url.Equals(BaselineSDKInfo.Get64InstallerURL()), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]
        [Description("Test if Get64InstallerURL() method returns False for the incorrect URL.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase("http://testurl.exe", false)]
        public void Get64InstallerURL_URLIsIncorrect_ReturnFalse(string url, bool expectedValue)
        {
            Assert.That(url.Equals(BaselineSDKInfo.Get64InstallerURL()), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P1")]
        [Description("Test if GetBaselineSDKMinVersion() method returns True for the correct Version.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase("2.0.0", true)]
        public void TestBaseLineMinVersion_VersionIsCorrect_ReturnTrue(string version, bool expectedValue)
        {
            Assert.That(version.Equals(BaselineSDKInfo.GetBaselineSDKMinVersion().ToString()), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]
        [Description("Test if GetBaselineSDKMinVersion() method returns False for the incorrect Version.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase("1.1.1", false)]
        public void TestBaseLineMinVersion_VersionIsIncorrect_ReturnFalse(string version, bool expectedValue)
        {
            Assert.That(version.Equals(BaselineSDKInfo.GetBaselineSDKMinVersion().ToString()), Is.EqualTo(expectedValue));
        }

    }
}
