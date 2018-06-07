/*
 * Copyright 2017 (c) Samsung Electronics Co., Ltd  All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * 	http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Tizen.VisualStudio.Tools.Utilities
{
    public static class ZipFile
    {
        // TODO: Temporary use
        static string[] tizenDllNames =
            {
            "Tizen.TV.Halo.dll",
            "Tizen.TV.HaloBinder.dll",
            "Tizen.TV.HaloStyle.dll"
        };

        public static void CreateFromDirectory(string directory, string targetZip)
        {
            using (var zip = System.IO.Compression.ZipFile.Open(targetZip, ZipArchiveMode.Create))
            {
                var files = Directory.GetFiles(directory).ToList<string>();

                if (files.Contains(targetZip))
                {
                    files.Remove(targetZip);
                }

                foreach (string fileName in files)
                {
                    zip.CreateEntryFromFile(fileName, Path.GetFileName(fileName));
                }
            }
        }

        public static void CreateFromDirectoryExcludeTizenDll(string directory, string targetZip)
        {
            using (var zip = System.IO.Compression.ZipFile.Open(targetZip, ZipArchiveMode.Create))
            {
                var files = Directory.GetFiles(directory).ToList<string>();

                if (files.Contains(targetZip))
                {
                    files.Remove(targetZip);
                }

                foreach (string fileName in files)
                {
                    if (tizenDllNames.Contains(Path.GetFileName(fileName)) == false)
                    {
                        zip.CreateEntryFromFile(fileName, Path.GetFileName(fileName));
                    }
                }
            }
        }

        public static void CreateResourcesDirectory(string directory, string targetZip)
        {
            System.IO.Compression.ZipFile.CreateFromDirectory(directory, targetZip);
        }

        public static void ExtractToDirectory(string ZipFileName, string targetDirectory)
        {
            System.IO.Compression.ZipFile.ExtractToDirectory(ZipFileName, targetDirectory);
        }

        public static void UnZip(string zipFile, string folderPath)
        {
            System.IO.Compression.ZipFile.ExtractToDirectory(zipFile, folderPath);
        }
    }
}
