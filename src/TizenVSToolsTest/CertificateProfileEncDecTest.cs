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
using System.Security.Cryptography;
using Tizen.VisualStudio.Tools.Data;

namespace Tizen.VisualStudio.Tools.UnitTests
{
    [TestFixture]
    [Description("Tizen.VisualStudio.Tools.Data Certificate profile encrypt decrypt tests")]
    public class CertificateProfileEncDecTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        [Category("P1")]
        [Description("Test if Encrypt and Decrypt are working fine")]
        [Property("AUTHOR", "Rahul Dadhich, r.dadhich@samsung.com")]
        [TestCase ("teststring", "teststring")]
        public void Encrypt_Decrypt_PassString_ReturnString(string input, string expectedValue)
        {
            string encryptString = CertificateProfileEncDec.Encrypt<TripleDESCryptoServiceProvider>(input);
            string decryptString = CertificateProfileEncDec.Decrypt<TripleDESCryptoServiceProvider>(encryptString);
            Assert.That(decryptString, Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]
        [Description("Test if  Decrypt with pwd file and without tool works")]
        [Property("AUTHOR", "Rahul Dadhich, r.dadhich@samsung.com")]
        public void Decrypt_PWDFile_ReturnString()
        {
            var curDir = System.IO.Directory.GetCurrentDirectory();
            var pwdFilePath = curDir + @"\pass.pwd";
            string decryptString = CertificateProfileEncDec.Decrypt<TripleDESCryptoServiceProvider>(pwdFilePath);
            Assert.That(decryptString, Is.EqualTo(string.Empty));
        }


    }
}
