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
    [Description("Tizen.VisualStudio.Tools.Data CertificateProfileInfo UTCs")]
    class CertificateProfileInfoTests
    {
        private CertificateProfileInfo certProfileInfo;
        [SetUp]
        public void Setup()
        {
            certProfileInfo = new CertificateProfileInfo();
            certProfileInfo.profileName = "TestProfile";
            var certProfileItem = new CertificateProfileItem("key", "password", "distributor", "ca", "rootca");
            certProfileInfo.profileItemDic.Add("distributor", certProfileItem);
        }

        [Test]
        [Category("P1")]    // Positive Test Case
        [Description("Test if Equals() method returns True for two CertificateProfileItem Instance having same Profile.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase("key", "password", "distributor", "ca", "rootca", true)]
        public void Equals_SameProfileInfo_ReturnTrue(string testKey, string testPassword, string testDist, string testCa,
                                                              string testRootCa, bool expectedValue)
        {
            var testCertProfileInfo = new CertificateProfileInfo();
            testCertProfileInfo.profileName = "TestProfile";
            var certProfileItem = new CertificateProfileItem(testKey, testPassword, testDist, testCa, testRootCa);
            testCertProfileInfo.profileItemDic.Add(testDist, certProfileItem);

            Assert.That(testCertProfileInfo.Equals(certProfileInfo), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]    // Negative Test Case
        [Description("Test if Equals() method returns False for two CertificateProfileItem Instance having Different Profile.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase("wrongKey", "wrongPasswd", "wrongDist", "wrongCa", "wrongRootca", false)]
        public void Equals_DifferentProfileInfo_ReturnFalse(string testKey, string testPassword, string testDist, string testCa,
                                                                    string testRootCa, bool expectedValue)
        {
            var testCertProfileInfo = new CertificateProfileInfo();
            testCertProfileInfo.profileName = "wrongProfile";
            var certProfileItem = new CertificateProfileItem(testKey, testPassword, testDist, testCa, testRootCa);
            testCertProfileInfo.profileItemDic.Add(testDist, certProfileItem);

            Assert.That(testCertProfileInfo.Equals(certProfileInfo), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P1")]    // Positive Test Case
        [Description("Test if GetHashCode() returns correct HashCode.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase("TestProfile", true)]
        public void GetHashCode_CorrectProfileName_ReturnTrue(string testProfileName, bool expectedValue)
        {
            int expectedHashCode = testProfileName.GetHashCode();

            Assert.That(certProfileInfo.GetHashCode().Equals(expectedHashCode), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]    // Negative Test Case
        [Description("Test if GetHashCode() returns correct HashCode.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase("wrongProfileName", false)]
        public void GetHashCode_InCorrectProfileName_ReturnFalse(string testProfileName, bool expectedValue)
        {
            int wrongHashCode = testProfileName.GetHashCode();

            Assert.That(certProfileInfo.GetHashCode().Equals(wrongHashCode), Is.EqualTo(expectedValue));

        }
    }
}
