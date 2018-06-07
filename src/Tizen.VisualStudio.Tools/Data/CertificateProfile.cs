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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Tizen.VisualStudio.Tools.Data
{
    /* Example Profiles.xml
       <profiles version="3.0">
        <profile name="tet">

        <profileitem \
            ca="C:\tizen-studio-test\tools\certificate-generator\certificates\developer\tizen-developer-ca.cer"
            distributor="0"
            key="C:/tizen-studio-data.1/keystore/author/tttttt.p12"
            password="yDL9Pk4NNCbGYRytc+XWyw=="
            rootca=""/>

        <profileitem ca="C:\tizen-studio-test\tools\certificate-generator\certificates\distributor\tizen-distributor-ca.cer"
            distributor="1"
            key="C:\tizen-studio-test\tools\certificate-generator\certificates\distributor\tizen-distributor-signer.p12"
            password="Vy63flx5JBMc5GA4iEf8oFy+8aKE7FX/+arrDcO4I5k="
            rootca=""/>

        <profileitem ca="" distributor="2" key="" password="xmEcrXPl1ss=" rootca=""/>
        </profile>
       </profiles>
    */

    public class CertificateProfileItem
    {
        public string caPath;
        // public string distributorNum;
        public string keyPath;
        public string keyPassword;
        public string rootca;

        public CertificateProfileItem(string key,
                                    string password,
                                    string distributor,
                                    string ca,
                                    string rootca)
        {
            this.keyPath = key;
            this.keyPassword = password;
            this.caPath = ca;
            this.rootca = rootca;
        }

        public override bool Equals(Object obj)
        {
            CertificateProfileItem personObj = obj as CertificateProfileItem;
            if (personObj == null)
            {
                return false;
            }
            else
            {
                return keyPath.Equals(personObj.keyPath)
                    && keyPassword.Equals(personObj.keyPassword)
                    && caPath.Equals(personObj.caPath)
                    && rootca.Equals(personObj.rootca);
            }
        }

        public override int GetHashCode()
        {
            return this.keyPath.GetHashCode()
                 + this.keyPassword.GetHashCode()
                 + this.caPath.GetHashCode()
                 + this.rootca.GetHashCode();
        }
    }

    public class CertificateProfileInfo
    {
        public string profileName;
        public Dictionary<string, CertificateProfileItem> profileItemDic =
            new Dictionary<string, CertificateProfileItem>();

        public override bool Equals(Object obj)
        {
            CertificateProfileInfo personObj = obj as CertificateProfileInfo;

            if (personObj == null)
            {
                return false;
            }

            if (profileName.Equals(personObj.profileName) == false)
            {
                return false;
            }

            foreach (var pair in profileItemDic)
            {
                if (!pair.Value.Equals(personObj.profileItemDic[pair.Key]))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            return this.profileName.GetHashCode();

        }
    }

    public class CertificateProfile
    {
        public Dictionary<string, CertificateProfileInfo> profileDic;
        private string activeProfile;

        public CertificateProfile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found");
            }

            if (!LoadProfileXml(filePath, out this.profileDic))
            {
                throw new NotSupportedException("LoadProfileXml failed");
            }
        }

        public bool LoadProfileXml(string profileFilePath)
        {
            return LoadProfileXml(profileFilePath,
                    out this.profileDic);
        }

        public bool LoadProfileXml(string profileFilePath,
                    out Dictionary<string, CertificateProfileInfo> profileDic)
        {
            bool ret = true;

            profileDic = new Dictionary<string, CertificateProfileInfo>();

            XmlDocument xml = new XmlDocument();
            try
            {
                xml.Load(profileFilePath);
            }
            catch (Exception)
            {
                return false;
            }

            XmlNodeList xnList = xml.SelectNodes("/profiles/profile");
            foreach (XmlNode xn in xnList)
            {
                CertificateProfileInfo certProfileInfo =
                    new CertificateProfileInfo();

                certProfileInfo.profileName = xn.Attributes["name"].Value;

                foreach (XmlNode xnProfileItem in xn.ChildNodes)
                {
                    CertificateProfileItem certProfileItem =
                        new CertificateProfileItem(
                            xnProfileItem.Attributes["key"].Value,
                            xnProfileItem.Attributes["password"].Value,
                            xnProfileItem.Attributes["distributor"].Value,
                            xnProfileItem.Attributes["ca"].Value,
                            xnProfileItem.Attributes["rootca"].Value);

                    certProfileInfo.profileItemDic.Add(
                        xnProfileItem.Attributes["distributor"].Value,
                        certProfileItem);
                }

                profileDic.Add(certProfileInfo.profileName, certProfileInfo);
            }

            XmlNode xnProfiles = xml.SelectSingleNode("/profiles");

            activeProfile = xnProfiles?.Attributes["active"]?.Value;

            return ret;
        }

        public List<string> GetProfileNameList()
        {
            if (profileDic.Count == 0)
            {
                return null;
            }

            return new List<string>(profileDic.Keys);
        }

        public CertificateProfileInfo GetProfileInfo(string profileName)
        {
            CertificateProfileInfo info = null;
            if (profileDic.Count == 0)
            {
                return null;
            }

            profileDic.TryGetValue(profileName, out info);

            return info;
        }

        public string GetActiveProfileName()
        {
            return activeProfile ?? "";
        }

        public override bool Equals(Object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            CertificateProfile certObj = obj as CertificateProfile;

            if (!String.IsNullOrEmpty(activeProfile)
                && !String.IsNullOrEmpty(certObj.activeProfile)
                && !this.activeProfile.Equals(certObj.activeProfile))
            {
                return false;
            }

            if (this.profileDic.Count != certObj.profileDic.Count)
            {
                return false;
            }

            bool isContentEquals = this.profileDic.Keys.All(
                k =>
                certObj.profileDic.ContainsKey(k) &&
                object.Equals(this.profileDic[k], certObj.profileDic[k]));

            return isContentEquals;
        }

        public override int GetHashCode()
        {
            return this.activeProfile.GetHashCode() ^ this.profileDic.GetHashCode();
        }
    }
}
