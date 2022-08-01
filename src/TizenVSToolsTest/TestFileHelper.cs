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
using Tizen.VisualStudio.Utilities;

namespace Tizen.VisualStudio.Tools.UnitTests
{
    class TestFileHelper
    {
        private string readFilePath;
        private string writeFilePath;

        [SetUp]
        public void Setup()
        {
            var curDir = Directory.GetCurrentDirectory();
            readFilePath = curDir + @"\TestFile.txt";
            writeFilePath = curDir + @"\tempFile.txt";
        }

        [Test]
        public void TestCopyLines_WithoutSkipEmptyLines()
        {
            using (StreamReader sr = new StreamReader(readFilePath))
            {
                using (StreamWriter writer = File.CreateText(writeFilePath))
                {
                    var lineCount = FileHelper.CopyLines(sr, writer);
                    var expectedLineCount = File.ReadAllLines(readFilePath).Length;

                    Assert.That(lineCount, Is.EqualTo(expectedLineCount));
                }
            }
        }

        [Test]
        public void TestCopyLines_WithSkipEmptyLines()
        {
            using (StreamReader sr = new StreamReader(readFilePath))
            {
                using (StreamWriter writer = File.CreateText(writeFilePath))
                {
                    var lineCount = FileHelper.CopyLines(sr, writer, true);
                    var expectedLineCount = File.ReadAllLines(readFilePath).Length - 1; // TestFile has One empty line.

                    Assert.That(lineCount, Is.EqualTo(expectedLineCount));
                }
            }
        }

        [Test]
        public void TestCopyToUnixText()
        {
            var copiedLines = FileHelper.CopyToUnixText(readFilePath, writeFilePath);
            var expectedLineCount = File.ReadAllLines(readFilePath).Length;

            Assert.That(copiedLines, Is.EqualTo(expectedLineCount));
        }

        [Test]
        public void TestCopyToWindowsText()
        {
            var copiedLines = FileHelper.CopyToUnixText(readFilePath, writeFilePath);
            var expectedLineCount = File.ReadAllLines(readFilePath).Length;

            Assert.That(copiedLines, Is.EqualTo(expectedLineCount));
        }
    }
}
