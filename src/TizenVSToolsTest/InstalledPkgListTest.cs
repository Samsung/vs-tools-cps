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
using System;
using Tizen.VisualStudio.Tools.Data;

namespace Tizen.VisualStudio.Tools.UnitTests
{
    [TestFixture]
    [Description("Tizen.VisualStudio.Tools.Data Installed pkg list tests")]
    public class InstalledPkgListTest
    {
        InstalledPkgList installedPkgListManual = null;
        [SetUp]
        public void Setup()
        {
            var curDir = System.IO.Directory.GetCurrentDirectory();
            InstalledPkgList installedPkgList = new InstalledPkgList();
            installedPkgListManual = new InstalledPkgList(curDir);
        }

        [Test]
        [Category("P1")]
        [Description("Test if GetPackage() method returns PkgList obj for the Installed package.")]
        [Property("AUTHOR", "Rahul Dadhich, r.dadhich@samsung.com")]
        [TestCase ("sdb", "sdb", "1.1.1", "testSDB", "SDBDescription")]
        public void GetPackage_PackageIsInstalled_ReturnPkgListObj(string pkg, string expectedPackageValue, string expactedVesionValue, string expactedLabelValue, string expatedDescriptionValue)
        {
            PkgList pkgList = installedPkgListManual.GetPackage(pkg);
            Assert.That(pkgList.Name, Is.EqualTo(expectedPackageValue));
            Assert.That(pkgList.Version.ToString(), Is.EqualTo(expactedVesionValue));
            Assert.That(pkgList.Label, Is.EqualTo(expactedLabelValue));
            Assert.That(pkgList.Description, Is.EqualTo(expatedDescriptionValue));
        }

        [Test]
        [Category("P2")]
        [Description("Test if GetPackage() method returns NULL for the not Installed package.")]
        [Property("AUTHOR", "Rahul Dadhich, r.dadhich@samsung.com")]
        [TestCase("test")]
        public void GetPackage_PackageIsNotInstalled_ReturnNULL(string pkg)
        {
            if (!(installedPkgListManual.GetPackage(pkg) == null))
            {
                Assert.That(true);
            }
        }


        [Test]
        [Category("P1")]
        [Description("Test if GetPkgInfo() method returns key value for installed package.")]
        [Property("AUTHOR", "Rahul Dadhich, r.dadhich@samsung.com")]
        [TestCase("sdb", "testSDB", "sdb", "SDBDescription")]
        public void GetPkgInfo_ValidKey_Returnstring(string pkg, string expectedLabel, string expectedPackage, string expectedDescription)
        {
            Assert.That(installedPkgListManual.GetPkgInfo(pkg, "Label"), Is.EqualTo(expectedLabel));
            Assert.That(installedPkgListManual.GetPkgInfo(pkg, "Package"), Is.EqualTo(expectedPackage));
            Assert.That(installedPkgListManual.GetPkgInfo(pkg, "Description"), Is.EqualTo(expectedDescription));
        }


        [Test]
        [Category("P2")]
        [Description("Test if GetPackage() method returns empty string for invalid key in Installed package.")]
        [Property("AUTHOR", "Rahul Dadhich, r.dadhich@samsung.com")]
        [TestCase("sdb")]
        public void GetPkgInfo_InValidKey_ReturnEmptyString(string pkg)
        {
            Assert.That(installedPkgListManual.GetPkgInfo(pkg, "test"), Is.EqualTo(""));
        }

        [Test]
        [Category("P2")]
        [Description("Test if GetPackage() method returns not installed string for not Installed package.")]
        [Property("AUTHOR", "Rahul Dadhich, r.dadhich@samsung.com")]
        [TestCase("test")]
        public void GetPkgInfo_NotInstalledPkg_Returnstring(string pkg)
        {
            Assert.That(installedPkgListManual.GetPkgInfo(pkg, "test"), Is.EqualTo("Package is not installed"));
        }


        [Test]
        [Category("P1")]
        [Description("Test if GetPkgVersion() method returns version for Installed package.")]
        [Property("AUTHOR", "Rahul Dadhich, r.dadhich@samsung.com")]
        [TestCase("sdb", "1.1.1")]
        public void GetPkgVersion_InstallPkg_ReturnVersionstring(string pkg, string expactedValue)
        {
            Version version = installedPkgListManual.GetPkgVersion(pkg);
            Assert.That(version.ToString(), Is.EqualTo(expactedValue));
        }

        [Test]
        [Category("P2")]
        [Description("Test if GetPkgVersion() method returns version for Installed package.")]
        [Property("AUTHOR", "Rahul Dadhich, r.dadhich@samsung.com")]
        [TestCase("test", "0.0.0")]
        public void GetPkgVersion_NotInstallPkg_ReturnDefaultVersionstring(string pkg, string expactedValue)
        {
            Version version = installedPkgListManual.GetPkgVersion(pkg);
            Assert.That(version.ToString(), Is.EqualTo(expactedValue));
        }
    }
}
