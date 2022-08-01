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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Tizen.VisualStudio.Tools.Data;

namespace Tizen.VisualStudio.Tools.UnitTests
{
    [TestFixture]
    [Description("Tizen.VisualStudio.Tools.Data CertificateProfileChangedEventArgs tests")]
    public class CertificateProfileChangedEventArgsTests
    {
        CertificateProfileChangedEventArgs eArgs;
       [SetUp]
        public void Setup()
        {
            eArgs = new CertificateProfileChangedEventArgs();

            var curDir = Directory.GetCurrentDirectory();
            eArgs.ProfileFilePath = curDir + @"\profiles.xml";
            eArgs.ActiveProfile = "TestProfile";
            eArgs.ProfileFileContents = true;
        }

        [Test]
        [Category("P1")]
        [Description("Test if ProfileFilePath is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase(true)]
        public void GetProfileFilePath_CorrectValue_ReturnTrue(bool expectedValue)
        {
            var curDir = Directory.GetCurrentDirectory();
            var expectedPath = curDir + @"\profiles.xml";

            Assert.That(eArgs.ProfileFilePath.Equals(expectedPath), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]
        [Description("Test if ProfileFilePath is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase(false)]
        public void GetProfileFilePath_IncorrectValue_ReturnFalse(bool expectedValue)
        {
            Assert.That(eArgs.ProfileFilePath.Equals(Directory.GetCurrentDirectory()), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P1")]
        [Description("Test if ActiveProfile is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase("TestProfile", true)]
        public void GetActiveProfile_CorrectValue_ReturnTrue(string profileName, bool expectedValue)
        {
            Assert.That(eArgs.ActiveProfile.Equals(profileName), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]
        [Description("Test if ActiveProfile is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase("NegativeTestCaseName", false)]
        public void GetActiveProfile_IncorrectValue_ReturnFalse(string profileName, bool expectedValue)
        {
            Assert.That(eArgs.ActiveProfile.Equals(profileName), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P1")]
        [Description("Test if ProfileFileContents is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase(true)]
        public void GetProfileFileContents_CorrectValue_ReturnTrue(bool expectedValue)
        {
            Assert.That(eArgs.ProfileFileContents.Equals(true), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]
        [Description("Test if ProfileFileContents is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase(false)]
        public void GetProfileFileContents_IncorrectValue_ReturnFalse(bool expectedValue)
        {
            Assert.That(eArgs.ProfileFileContents.Equals(false), Is.EqualTo(expectedValue));
        }
    }

    [TestFixture]
    [Description("Tizen.VisualStudio.Tools.Data CertificateProfilesManager UTCs")]
    class CertificateProfilesManagerTests
    {
        private string profileFilePath;
        private System.EventHandler<CertificateProfileChangedEventArgs> ProfileChangedEventHandler = null;

        [SetUp]
        public void Setup()
        {
            var curDir = Directory.GetCurrentDirectory();
            profileFilePath = curDir + @"\profiles.xml";
            ProfileChangedEventHandler = null;
            CertificateProfilesManager.RegisterProfileFile(profileFilePath);
        }

        [TearDown]
        public void Teardown()
        {
            if (ProfileChangedEventHandler != null)
                CertificateProfilesManager.ProfilesChanged -= ProfileChangedEventHandler;
        }

        [Test]
        [Category("P1")]
        [Description("Test if GetProfileFilePath() method returns the correct path.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase(true)]
        public void GetProfileFilePath_CompareWithCorrectPath_ReturnTrue(bool expectedValue)
        {
            var testPath = CertificateProfilesManager.GetProfileFilePath();

            Assert.That(profileFilePath.Equals(testPath), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]
        [Description("Test if GetProfileFilePath() method sets the correct path.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase(@"\dummyPath\profiles.xml", false)]
        public void GetProfileFilePath_CompareWithInCorrectPath_ReturnFalse(string testPath, bool expectedValue)
        {
            var profilePath = CertificateProfilesManager.GetProfileFilePath();

            Assert.That(profilePath.Equals(testPath), Is.EqualTo(false));
        }

        [Test]
        [Category("P1")]
        [Description("Test if GetActiveProfileName() method returns the correct Profile Name.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase("TestProfile", true)]
        public void GetActiveProfileName_CompareWithCorrectProfileName_ReturnTrue(string testProfileName, bool expectedValue)
        {
            CertificateProfilesManager.RegisterProfileFile(profileFilePath);
            Assert.That(CertificateProfilesManager.GetActiveProfileName().Equals(testProfileName), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]
        [Description("Test if GetActiveProfileName() method returns the correct Profile name.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase("wrongTestProfile", false)]
        public void GetActiveProfileName_CompareWithInCorrectProfileName_ReturnFalse(string testProfileName, bool expectedValue)
        {
            CertificateProfilesManager.RegisterProfileFile(profileFilePath);
            Assert.That(CertificateProfilesManager.GetActiveProfileName().Equals(testProfileName), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P1")]    // Positive Test Case
        [Description("Test if GetProfileInfo() method returns correct CertificateProfileInfo object.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase("TestProfile", "key", "password", "distributor", "ca", "rootca", true)]
        public void GetProfileInfo_SameProfileInfo_ReturnTrue(string testProfileName, string testKey, string testPassword, string testDist, string testCa,
                                                              string testRootCa, bool expectedValue)
        {
            var testCertProfileInfo = new CertificateProfileInfo();
            testCertProfileInfo.profileName = testProfileName;
            var certProfileItem = new CertificateProfileItem(testKey, testPassword, testDist, testCa, testRootCa);
            testCertProfileInfo.profileItemDic.Add(testDist, certProfileItem);

            Assert.That(CertificateProfilesManager.GetProfileInfo(testProfileName).Equals(testCertProfileInfo), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]    // Negative Test Case
        [Description("Test if GetProfileInfo() method returns correct CertificateProfileInfo object.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase("TestProfile", "wrongKey", "wrongPasswd", "distributor", "wrongCa", "wrongRootca", false)]
        public void GetProfileInfo_DifferentProfileInfo_ReturnFalse(string testProfileName, string testKey, string testPassword, string testDist, string testCa,
                                                            string testRootCa, bool expectedValue)
        {
            var testCertProfileInfo = new CertificateProfileInfo();
            testCertProfileInfo.profileName = testProfileName;
            var certProfileItem = new CertificateProfileItem(testKey, testPassword, testDist, testCa, testRootCa);
            testCertProfileInfo.profileItemDic.Add(testDist, certProfileItem);

            Assert.That(CertificateProfilesManager.GetProfileInfo(testProfileName).Equals(testCertProfileInfo), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]    // Negative Test Case
        [Description("Test if GetProfileInfo() method returns correct CertificateProfileInfo object.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase("wrongTestProfile", null)]
        public void GetProfileInfo_WrongProfileName_ReturnNull(string testProfileName, CertificateProfileInfo expectedCertProfileInfo)
        {
             Assert.That(CertificateProfilesManager.GetProfileInfo(testProfileName), Is.EqualTo(expectedCertProfileInfo));
        }

        [Test]
        [Category("P2")]    // Negative Test Case
        [Description("Test if GetProfileInfo() method returns correct CertificateProfileInfo object.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase("TestProfile", "key", "password", "wrongDistributor", "ca", "rootca")]
        public void GetProfileInfo_WrongDistributorName_ThrowException(string testProfileName, string testKey, string testPassword, string testDist, string testCa,
                                                            string testRootCa)
        {
            var testCertProfileInfo = new CertificateProfileInfo();
            testCertProfileInfo.profileName = testProfileName;
            var certProfileItem = new CertificateProfileItem(testKey, testPassword, testDist, testCa, testRootCa);
            testCertProfileInfo.profileItemDic.Add(testDist, certProfileItem);

            try
            {
                var val = CertificateProfilesManager.GetProfileInfo(testProfileName).Equals(testCertProfileInfo);
                Assert.That(val, Is.EqualTo(false));
            }
            catch(KeyNotFoundException)
            {
                Assert.Pass();
            }
        }

        [Test]
        [Category("P1")]    // Positive Test Case
        [Description("Test GetProfileNameList() returns the Correct ProfileNameList.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase("TestProfile", true)]
        public void GetProfileNameList_CompareWithCorrectProfileNameList_ReturnTrue(string expectedProfileName, bool expectedValue)
        {
            var expectedProfileNameList = new List<string>();
            expectedProfileNameList.Add(expectedProfileName);

            Assert.That(CertificateProfilesManager.GetProfileNameList().SequenceEqual(expectedProfileNameList), Is.EqualTo(expectedValue));
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

            Assert.That(CertificateProfilesManager.GetProfileNameList().SequenceEqual(expectedProfileNameList), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P1")]    // Positive Test Case
        [Description("Test if ProfilesChanged Events is raised when Profile File is modified.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        public void TestProfilesChanged_RaiseFileChangedEvent()
        {
            var  eventRaised = false;
            var curDir = Directory.GetCurrentDirectory();
            var eventTestProfileFilePath = curDir + @"\eventTestProfiles.xml";

            CertificateProfilesManager.RegisterProfileFile(eventTestProfileFilePath);
            CertificateProfilesManager.ProfilesChanged += (sender, eventArgs) =>
            {
                eventRaised = true;
                Assert.IsTrue(eventRaised);
            };

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(eventTestProfileFilePath);
            XmlElement profile = (XmlElement)xmlDoc.SelectSingleNode("//profiles/profile/profileitem");
            profile?.SetAttribute("password", "xPsswd123"); // Set to new value.
            xmlDoc.Save(eventTestProfileFilePath);
        }
    }
}
