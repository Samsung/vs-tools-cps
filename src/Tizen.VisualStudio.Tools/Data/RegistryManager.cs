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

using Win32 = Microsoft.Win32;
using System;

namespace Tizen.VisualStudio.Tools.Data
{
    public class RegistryManager
    {
        const string TizenVSKey = @"Software\Tizen\VSIX\14.0";

        private static Win32.RegistryKey GetTizenKeyPage()
        {

            Win32.RegistryKey rkey = null;

            try
            {
                rkey = Win32.Registry.CurrentUser.OpenSubKey(TizenVSKey,
                        Win32.RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (rkey == null)
                {
                    rkey = Win32.Registry.CurrentUser.CreateSubKey(TizenVSKey,
                        Win32.RegistryKeyPermissionCheck.ReadWriteSubTree);
                }
            }
            catch
            {
            }

            return rkey;
        }

        public static void DeleteRegistryKey()
        {
            try
            {
                var rkey = Win32.Registry.CurrentUser.OpenSubKey(TizenVSKey,
                       Win32.RegistryKeyPermissionCheck.ReadWriteSubTree);
                if (rkey != null)
                {
                    Win32.Registry.CurrentUser.DeleteSubKeyTree(TizenVSKey);
                }
            }
            catch
            {

            }
        }

        public static string GetRegistryKey(string regPath)
        {
            Win32.RegistryKey rkey = GetTizenKeyPage();

            string returnValue = string.Empty;

            try
            {
                if (regPath != null)
                {
                    returnValue = rkey?.GetValue(regPath)?.ToString();
                }
            }
            catch
            {
            }

            rkey?.Close();

            return returnValue;
        }

        public static bool SetRegistryKey(string regPath, string value)
        {

            Win32.RegistryKey rkey = GetTizenKeyPage();

            try
            {
                rkey?.SetValue(regPath, value, Win32.RegistryValueKind.String);
                rkey?.Close();
            }
            catch
            {
                rkey?.Close();
                return false;
            }

            return true;
        }
    }
}
