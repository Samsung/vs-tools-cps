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

using System;
using System.IO;

namespace Tizen.VisualStudio.Tools.Data
{
    public static class EnvironmentStore
    {
        private static string programFilePath = string.Empty;
        private const string builderPath = @"MSBuild\Tizen\bin";
        private const string xmlSec = @"libxmlsec-1.2.18\bin\xmlsec.exe";

        public static string BuilderPath
        {
            get
            {
                if (programFilePath == string.Empty)
                {
                    SetProgramFilesPath();
                }

                return Path.Combine(programFilePath, builderPath);
            }
        }

        public static string XmlSec
        {
            get
            {
                if (programFilePath == string.Empty)
                {
                    SetProgramFilesPath();
                }

                return Path.Combine(programFilePath, builderPath, xmlSec);
            }
        }

        private static void SetProgramFilesPath()
        {
            if (Environment.Is64BitOperatingSystem)
            {
                programFilePath = Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFilesX86);
            }
            else
            {
                programFilePath = Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFiles);
            }

        }
    }
}
