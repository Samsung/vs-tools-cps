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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Tizen.VisualStudio.Tools.Data
{
    // This Class for decrypt password of profiles.xml
    public class CertificateProfileEncDec
    {
        static private readonly string encDecKey = "KYANINYLhijklmnopqrstuvwx";

        public static string Encrypt<T>(string value)
                               where T : SymmetricAlgorithm, new()
        {
            SymmetricAlgorithm algorithm = new T();
            byte[] rgbKey = Encoding.UTF8.GetBytes(encDecKey.ToCharArray(),
                                                    0, algorithm.KeySize / 8);
            byte[] rgbIV = new byte[algorithm.BlockSize / 8];
            algorithm.Mode = CipherMode.ECB;
            algorithm.Padding = PaddingMode.PKCS7;
            ICryptoTransform transform =
                algorithm.CreateEncryptor(rgbKey, rgbIV);

            try
            {
                using (MemoryStream buffer = new MemoryStream())
                {
                    using (CryptoStream stream =
                           new CryptoStream(buffer,
                                            transform,
                                            CryptoStreamMode.Write))
                    {
                        using (StreamWriter writer =
                               new StreamWriter(stream, Encoding.UTF8))
                        {
                            writer.Write(value);
                        }
                    }

                    return Convert.ToBase64String(buffer.ToArray());
                }
            }
            catch (Exception)
            {
            }

            return string.Empty;
        }

        public static string Decrypt<T>(string text)
                               where T : SymmetricAlgorithm, new()
        {
            SymmetricAlgorithm algorithm = new T();
            byte[] rgbKey = Encoding.UTF8.GetBytes(encDecKey.ToCharArray(),
                                                    0, algorithm.KeySize / 8);
            byte[] rgbIV = new byte[algorithm.BlockSize / 8];
            algorithm.Mode = CipherMode.ECB;
            algorithm.Padding = PaddingMode.PKCS7;
            ICryptoTransform transform =
                algorithm.CreateDecryptor(rgbKey, rgbIV);

            try
            {
                using (MemoryStream buffer =
                          new MemoryStream(Convert.FromBase64String(text)))
                {
                    using (CryptoStream stream =
                           new CryptoStream(buffer,
                                            transform,
                                            CryptoStreamMode.Read))
                    {
                        using (StreamReader reader =
                               new StreamReader(stream, Encoding.UTF8))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return string.Empty;
        }
    }
}
