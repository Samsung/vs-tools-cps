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
using System.Text;

namespace Tizen.VisualStudio.Utilities
{
    public static class FileHelper
    {
        public static int CopyToUnixText(string sourceName, string destinationName)
        {
            using (var source = new StreamReader(sourceName, true))
            {
                using (var destination = new StreamWriter(destinationName, false, source.CurrentEncoding))
                {
                    destination.NewLine = "\n";
                    return CopyLines(source, destination, false);
                }
            }
        }

        public static int CopyToWindowsText(string sourceName, string destinationName)
        {
            using (var source = new StreamReader(sourceName, false))
            {
                using (var destination = new StreamWriter(destinationName, false, source.CurrentEncoding))
                {
                    destination.NewLine = "\r\n";
                    return CopyLines(source, destination, false);
                }
            }
        }

        public static int CopyLines(TextReader source, TextWriter destination, bool skipEmptyLines = false)
        {
            int result = 0;
            string line;
            while ((line = source.ReadLine()) != null)
            {
                if (skipEmptyLines && (line == ""))
                {
                    continue;
                }
                destination.WriteLine(line);
                ++result;
            }
            return result;
        }

        /// <summary>
        /// Read all lines from <paramref name="source"/> and copy them to <paramref name="destination"/>.
        /// </summary>
        /// <param name="source">The source stream</param>
        /// <param name="destination">The destination stream</param>
        /// <param name="newLine">The line terminator string (optional, default is CR LF)</param>
        /// <param name="encoding">The stream encoding (optional, default is UTF8)</param>
        /// <param name="skipEmptyLines">If true then only non-empty lines are copied</param>
        /// <returns>The number or lines copied</returns>
        public static int CopyLines(TextReader source, Stream destination, string newLine = "\r\n",
            Encoding encoding = null, bool skipEmptyLines = false)
        {
            int result = 0;
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }
            string line;
            while ((line = source.ReadLine()) != null)
            {
                if (skipEmptyLines && (line == ""))
                {
                    continue;
                }
                byte[] data = encoding.GetBytes(line + newLine);
                destination.Write(data, 0, data.Length);
                ++result;
            }
            return result;
        }
    }
}
