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
using System.IO;
using System.Linq;
using Tizen.VisualStudio.Tools.Data;
using System.Collections.Generic;

namespace Tizen.VisualStudio.Tools.UnitTests
{
    [TestFixture]
    [Description("Tizen.VisualStudio.Tools.Data CertificateProfile UTCs")]
    class CertificateProfileTests
    {
        private CertificateProfile certProfile;

        [SetUp]
        public void Setup()
        {
            var curDir = Directory.GetCurrentDirectory();
            var certProfilePath = curDir + @"\profiles.xml";
            certProfile = new CertificateProfile(certProfilePath);
        }

        [Test]
        [Category("P1")]    // Positive Test Case
        [Description("Test if LoadProfileXml() method returns True for the correct Profile path.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase (@"\profiles.xml", true)]
        public void LoadProfileXml_ProfilePathIsCorrect_ReturnTrue(string profileFileName, bool expectedRetVal)
        {
            var curDir = Directory.GetCurrentDirectory();
            var certProfilePath = curDir + profileFileName;

            Assert.That(certProfile.LoadProfileXml(certProfilePath), Is.EqualTo(expectedRetVal));
        }

        [Test]
        [Category("P2")]    // Negative Test Case
        [Description("Test if LoadProfileXml() method returns False for the Incorrect Profile path.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase(@"\negativeProfiles.xml", false)]
        public void LoadProfileXml_ProfilePathIsIncorrect_ReturnFalse(string profileFileName, bool expectedRetVal)
        {
            var curDir = Directory.GetCurrentDirectory();
            var certProfilePath = curDir + profileFileName;

            Assert.That(certProfile.LoadProfileXml(certProfilePath), Is.EqualTo(expectedRetVal));
        }

        [Test]
        [Category("P1")]    // Positive Test Case
        [Description("Test GetActiveProfileName() returns the Correct ProfileName.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase ("TestProfile", true)]
        public void GetActiveProfileName_CompareWithCorrectProfileName_ReturnTrue(string expectedProfileName, bool expectedValue)
        {
            Assert.That(certProfile.GetActiveProfileName().Equals(expectedProfileName), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]    // Negative Test Case
        [Description("Test  GetActiveProfileName() returns the Correct ProfileName.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase("NegativeTestProfile", false)]
        public void GetActiveProfileName_CompareWithIncorrectProfileName_ReturnFalse(string expectedProfileName, bool expectedValue)
        {
            Assert.That(certProfile.GetActiveProfileName().Equals(expectedProfileName), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P1")]    // Positive Test Case
        [Description("Test GetProfileNameList() returns the Correct ProfileNameList.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase ("TestProfile", true)]
        public void GetProfileNameList_CompareWithCorrectProfileNameList_ReturnTrue(string expectedProfileName, bool expectedValue)
        {
            var expectedProfileNameList = new List<string>();
            expectedProfileNameList.Add(expectedProfileName);

            Assert.That(certProfile.GetProfileNameList().SequenceEqual(expectedProfileNameList), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]    // Negative Test Case
        [Description("Test GetProfileNameList() returns the Correct ProfileNameList.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase("NegativeTestProfile", false)]
        public void GetProfileNameList_CompareWithIncorrectProfileNameList_ReturnFalse(string expectedProfileName, bool expectedValue)
        {
            var expectedProfileNameList = new List<string>();
            expectedProfileNameList.Add(expectedProfileName);

            Assert.That(certProfile.GetProfileNameList().SequenceEqual(expectedProfileNameList), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P1")]    // Positive Test Case
        [Description("Test GetProfileInfo() returns correct CertificateProfileInfo for the given profileName.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase ("TestProfile")]
        public void GetProfileInfo_PassCorrectProfileName_ReturnCertificateProfileInfo(string profileName)
        {
            CertificateProfileInfo expectedProfileInfo = null;
            certProfile.profileDic.TryGetValue(profileName, out expectedProfileInfo);

            Assert.That(certProfile.GetProfileInfo(profileName), Is.EqualTo(expectedProfileInfo));
        }

        [Test]
        [Category("P2")]    // Negative Test Case
        [Description("Test GetProfileInfo() returns NULL CertificateProfileInfo for the wrong profileName.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase("NegativeTestProfile")]
        public void GetProfileInfo_PassIncorrectProfileName_ReturnNull(string profileName)
        {
            CertificateProfileInfo expectedProfileInfo = null;

            Assert.That(certProfile.GetProfileInfo(profileName), Is.EqualTo(expectedProfileInfo));
        }

        [Test]
        [Category("P1")]    // Positive Test Case
        [Description("Test if GetHashCode() returns correct HashCode.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase ("TestProfile", true)]
        public void GetHashCode_CompareWithCorrectHashCode_ReturnTrue(string activeProfileName, bool expectedValue)
        {
            int expectedHashCode = activeProfileName.GetHashCode() ^ certProfile.profileDic.GetHashCode();

            Assert.That(certProfile.GetHashCode().Equals(expectedHashCode), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]    // Negative Test Case
        [Description("Test if GetHashCode() returns correct HashCode.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase("NegativeTestProfile", false)]
        public void GetHashCode_CompareWithIncorrectHashCode_ReturnFalse(string activeProfileName, bool expectedValue)
        {
            int expectedHashCode = activeProfileName.GetHashCode() ^ certProfile.profileDic.GetHashCode();

            Assert.That(certProfile.GetHashCode().Equals(expectedHashCode), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P1")]    // Positive Test Case
        [Description("Test if Equals() method returns True for two CertificateProfile Instance having same Profile.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase(@"\profiles.xml", true)]
        public void Equals_SameProfileName_ReturnTrue(string profileFileName, bool expectedValue)
        {
            var curDir = Directory.GetCurrentDirectory();
            var certProfilePath = curDir + profileFileName;
            var testCertProfile = new CertificateProfile(certProfilePath);

            Assert.That(certProfile.Equals(testCertProfile), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]    // Negative Test Case
        [Description("Test if Equals() method returns False for two CertificateProfile Instance having Different Profile.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase(@"\wrongProfiles.xml", false)]
        public void Equals_DifferentProfileName_ReturnFalse(string profileFileName, bool expectedValue)
        {
            var curDir = Directory.GetCurrentDirectory();
            var certProfilePath = curDir + profileFileName;
            var testCertProfile = new CertificateProfile(certProfilePath);

            Assert.That(certProfile.Equals(testCertProfile), Is.EqualTo(expectedValue));
        }
    }
}
