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
using System.Text;
using System.Security.Cryptography;

namespace Tizen.VisualStudio.Tools.Data
{
    public class CertificateEncDec
    {
        static private readonly string encDecKey = "tizencertipass";
        static private readonly string encDecAlt = "certisalt";

        public static string Encrypt<T>(string value)
                               where T : SymmetricAlgorithm, new()
        {
            DeriveBytes rgb = new Rfc2898DeriveBytes(encDecKey,
                                    Encoding.Unicode.GetBytes(encDecAlt));

            SymmetricAlgorithm algorithm = new T();
            byte[] rgbKey = rgb.GetBytes(algorithm.KeySize >> 3);
            byte[] rgbIV = rgb.GetBytes(algorithm.BlockSize >> 3);
            ICryptoTransform transform =
                algorithm.CreateEncryptor(rgbKey, rgbIV);
            rgb.Dispose();

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
                               new StreamWriter(stream, Encoding.Unicode))
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
            DeriveBytes rgb = new Rfc2898DeriveBytes(encDecKey,
                                        Encoding.Unicode.GetBytes(encDecAlt));
            SymmetricAlgorithm algorithm = new T();
            byte[] rgbKey = rgb.GetBytes(algorithm.KeySize >> 3);
            byte[] rgbIV = rgb.GetBytes(algorithm.BlockSize >> 3);
            ICryptoTransform transform =
                algorithm.CreateDecryptor(rgbKey, rgbIV);
            rgb.Dispose();

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
                               new StreamReader(stream, Encoding.Unicode))
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
