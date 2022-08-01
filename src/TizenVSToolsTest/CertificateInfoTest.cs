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
    [Description("Tizen.VisualStudio.Tools.Data CertificateInfo UTCs")]
    class CertificateInfoTests
    {
        private CertificateInfo certInfo;

        [SetUp]
        public void Setup()
        {
            certInfo = new CertificateInfo();
        }

        [Test]
        [Category("P1")]
        [Description("Test if SetCertificateInfo() method sets the correct value for the Certificate properties.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase ("authCert", "authCert", "authPwd", "authPwd", "distCert", "distCert", "distPwd", "distPwd", true)]
        public void SetCertificateInfo_CompareWithCorrectInfo_ShouldMatchWithCertInfo(string authCert, string expectedAuthcert, string authPasswd, string expectedAuthPasswd,
            string distCert, string expectedDisCert, string distPasswd, string expectedDistPasswd, bool expectedValue)
        {
            certInfo.SetCertificateInfo(authCert, authPasswd, distCert, distPasswd);

            Assert.That(certInfo.AuthorCertificateFile.Equals(expectedAuthcert), Is.EqualTo(expectedValue));
            Assert.That(certInfo.AuthorPassword.Equals(expectedAuthPasswd), Is.EqualTo(expectedValue));
            Assert.That(certInfo.DistributorCertificateFile.Equals(expectedDisCert), Is.EqualTo(expectedValue));
            Assert.That(certInfo.DistributorPassword.Equals(expectedDistPasswd), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]
        [Description("Test if SetCertificateInfo() method sets the correct value for the Certificate properties.")]
        [Property("AUTHOR", "Om Prakash, om90.prakash@samsung.com")]
        [TestCase("authCert", "Cert", "authPwd", "Pwd", "distCert", "Cert", "distPwd", "Pwd", false)]
        public void SetCertificateInfo_CompareWithWrongInfo_ShouldNotMatchWithCertInfo(string authCert, string expectedAuthcert, string authPasswd, string expectedAuthPasswd,
            string distCert, string expectedDisCert, string distPasswd, string expectedDistPasswd, bool expectedValue)
        {
            certInfo.SetCertificateInfo(authCert, authPasswd, distCert, distPasswd);

            Assert.That(certInfo.AuthorCertificateFile.Equals(expectedAuthcert), Is.EqualTo(expectedValue));
            Assert.That(certInfo.AuthorPassword.Equals(expectedAuthPasswd), Is.EqualTo(expectedValue));
            Assert.That(certInfo.DistributorCertificateFile.Equals(expectedDisCert), Is.EqualTo(expectedValue));
            Assert.That(certInfo.DistributorPassword.Equals(expectedDistPasswd), Is.EqualTo(expectedValue));
        }
    }
}
