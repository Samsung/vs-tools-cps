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
using Win32 = Microsoft.Win32;

namespace Tizen.VisualStudio.Tools.UnitTests
{
    [TestFixture]
    [Description("Tizen.VisualStudio.Tools.Data RegistryManagerTest tests")]
    public class RegistryManagerTest
    {
        [SetUp]
        public void Setup()
        {
            RegistryManager.DeleteRegistryKey();
        }

        [Test]
        [Category("P2")]
        [Description("Test if GetRegistryKey is working fine")]
        [Property("AUTHOR", "Rahul Dadhich, r.dadhich@samsung.com")]
        [TestCase("test", null)]
        public void GetRegistryKey_PassString_ReturnString(string input, string expectedValue)
        {
            string registryValue = RegistryManager.GetRegistryKey(input);
            Assert.That(registryValue, Is.EqualTo(expectedValue));
        }

        [Test]
        [Category("P1")]
        [Description("Test if SetRegistryKey is working fine")]
        [Property("AUTHOR", "Rahul Dadhich, r.dadhich@samsung.com")]
        [TestCase("tmp", "teststring", true)]
        public void SetRegistryKey_PassString_ReturnBool(string path, string inputKey, bool expectedValue)
        {
            bool registrySetValue = RegistryManager.SetRegistryKey(path, inputKey);
            Assert.That(registrySetValue, Is.EqualTo(expectedValue));
        }
    }
}
