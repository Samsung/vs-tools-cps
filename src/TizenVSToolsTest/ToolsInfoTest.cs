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
using System.IO;

namespace Tizen.VisualStudio.Tools.UnitTests
{
    [TestFixture]
    [Description("Tizen.VisualStudio.Tools.Data Tools Info tests")]
    public class ToolsInfoTests
    {
        private ToolsInfo info;

        [SetUp]
        public void Setup()
        {
            info = ToolsInfo.Instance();
            if (info != null)
                info.ToolsFolderPath = Directory.GetCurrentDirectory();
        }

        [Test]
        [Category("P1")]
        [Description("Test if ToolsFolderPath is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase(true)]
        public void GetToolsFolderPath_CorrectValue_ReturnTrue(bool expectedValue)
        {
            Assert.That(info.ToolsFolderPath.Equals(Directory.GetCurrentDirectory()), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]
        [Description("Test if ToolsFolderPath is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase("C:",false)]
        public void GetToolsFolderPath_IncorrectValue_ReturnFalse(string path, bool expectedValue)
        {
            Assert.That(info.ToolsFolderPath.Equals(path), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P1")]
        [Description("Test if OndemandFolderPath is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase(true)]
        public void OndemandFolderPath_CorrectValue_ReturnTrue(bool expectedValue)
        {
            var relativeOndemandFolderPath = @"platforms\tizen-4.0\common\on-demand";
            var expectedOndemandFolderPath = Path.Combine(Directory.GetCurrentDirectory(), relativeOndemandFolderPath);

            Assert.That(info.OndemandFolderPath.Equals(expectedOndemandFolderPath), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]
        [Description("Test if OndemandFolderPath is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase("C:", false)]
        public void OndemandFolderPath_IncorrectValue_ReturnFalse(string path, bool expectedValue)
        {
            Assert.That(info.OndemandFolderPath.Equals(path), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P1")]
        [Description("Test if PlatformPath is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase(true)]
        public void PlatformPath_CorrectValue_ReturnTrue(bool expectedValue)
        {
            var relativePlatformPath = @"platforms\tizen-4.0\";
            var expectedPlatformPath = Path.Combine(Directory.GetCurrentDirectory(), relativePlatformPath);

            Assert.That(info.PlatformPath.Equals(expectedPlatformPath), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]
        [Description("Test if PlatformPath is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase("C:", false)]
        public void PlatformPath_IncorrectValue_ReturnFalse(string path, bool expectedValue)
        {
            Assert.That(info.PlatformPath.Equals(path), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P1")]
        [Description("Test if PkgMgrPath is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase(true)]
        public void PkgMgrPath_CorrectValue_ReturnTrue(bool expectedValue)
        {
            var relativePkgMgrPath = Path.Combine(@"package-manager", "package-manager.exe");
            var expectedPkgMgrPath = Path.Combine(Directory.GetCurrentDirectory(), relativePkgMgrPath);

            Assert.That(info.PkgMgrPath.Equals(expectedPkgMgrPath), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]
        [Description("Test if PkgMgrPath is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase("C:", false)]
        public void PkgMgrPath_IncorrectValue_ReturnFalse(string path, bool expectedValue)
        {
            Assert.That(info.PkgMgrPath.Equals(path), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P1")]
        [Description("Test if EmulatorMgrPath is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase(true)]
        public void EmulatorMgrPath_CorrectValue_ReturnTrue(bool expectedValue)
        {
            var relativeEmulatorMgrPath = Path.Combine(@"tools\emulator\bin", "emulator-manager.exe");
            var expectedEmulatorMgrPath = Path.Combine(Directory.GetCurrentDirectory(), relativeEmulatorMgrPath);

            Assert.That(info.EmulatorMgrPath.Equals(expectedEmulatorMgrPath), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]
        [Description("Test if EmulatorMgrPath is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase("C:", false)]
        public void EmulatorMgrPath_IncorrectValue_ReturnFalse(string path, bool expectedValue)
        {
            Assert.That(info.EmulatorMgrPath.Equals(path), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P1")]
        [Description("Test if CertificateMgrPath is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase(true)]
        public void CertificateMgrPath_CorrectValue_ReturnTrue(bool expectedValue)
        {
            var relativeCertificateMgrPath = Path.Combine(@"tools\certificate-manager\", "certificate-manager.exe");
            var expectedCertificateMgrPath = Path.Combine(Directory.GetCurrentDirectory(), relativeCertificateMgrPath);

            Assert.That(info.CertificateMgrPath.Equals(expectedCertificateMgrPath), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]
        [Description("Test if CertificateMgrPath is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase("C:", false)]
        public void CertificateMgrPath_IncorrectValue_ReturnFalse(string path, bool expectedValue)
        {
            Assert.That(info.CertificateMgrPath.Equals(path), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P1")]
        [Description("Test if DeviceMgrPath is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase(true)]
        public void DeviceMgrPath_CorrectValue_ReturnTrue(bool expectedValue)
        {
            var relativeDeviceMgrPath = Path.Combine(@"tools\device-manager\bin", "device-manager.exe");
            var expectedDeviceMgrPath = Path.Combine(Directory.GetCurrentDirectory(), relativeDeviceMgrPath);

            Assert.That(info.DeviceMgrPath.Equals(expectedDeviceMgrPath), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]
        [Description("Test if DeviceMgrPath is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase("C:", false)]
        public void DeviceMgrPath_IncorrectValue_ReturnFalse(string path, bool expectedValue)
        {
            Assert.That(info.DeviceMgrPath.Equals(path), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P1")]
        [Description("Test if SDBPath is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase(true)]
        public void SDBPath_CorrectValue_ReturnTrue(bool expectedValue)
        {
            var relativeSDBPath = Path.Combine(@"tools\", "sdb.exe");
            var expectedSDBPath = Path.Combine(Directory.GetCurrentDirectory(), relativeSDBPath);

            Assert.That(info.SDBPath.Equals(expectedSDBPath), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P2")]
        [Description("Test if SDBPath is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase("C:", false)]
        public void SDBPath_IncorrectValue_ReturnFalse(string path, bool expectedValue)
        {
            Assert.That(info.SDBPath.Equals(path), Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P1")]
        [Description("Test if DefaultCertPath is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase(true)]
        public void DefaultCertPath_CorrectValue_ReturnTrue(bool expectedValue)
        {
            var relativeSDBPath = Path.Combine(@"profile", "profiles.xml");
            var expectedSDBPath = Path.Combine("C:\\Temp", relativeSDBPath);

            //Creating dummy file in the expectedSDBPath
            FileStream fileStream = null;

            try
            {
                Directory.CreateDirectory(Path.Combine("C:\\Temp", @"profile"));
                fileStream = File.Create(expectedSDBPath);
            }
            finally
            {
                if (fileStream != null)
                    fileStream.Dispose();
            }

            bool success = File.Exists(expectedSDBPath);

            Assert.That(info.DefaultCertPath.Equals(expectedSDBPath), Is.EqualTo(expectedValue));

            //Cleaning the testing file
            if (success) {
                File.Delete(expectedSDBPath);
                Directory.Delete(Path.Combine("C:\\Temp", @"profile"));
            }
        }

        [Test]
        [Category("P2")]
        [Description("Test if DefaultCertPath is set correctly")]
        [Property("AUTHOR", "Alka Sethi, alka.sethi@samsung.com")]
        [TestCase("C:", false)]
        public void DefaultCertPath_IncorrectValue_ReturnFalse(string path, bool expectedValue)
        {
            Assert.That(info.DefaultCertPath.Equals(path), Is.EqualTo(expectedValue));
        }
    }
}
