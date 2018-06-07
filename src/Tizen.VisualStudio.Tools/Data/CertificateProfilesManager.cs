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

using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tizen.VisualStudio.Tools.Data
{

    public class CertificateProfileChangedEventArgs : EventArgs
    {
        public string ProfileFilePath { get; set; }
        public string ActiveProfile { get; set; }
        public bool ProfileFileContents { get; set; }
    }

    public class CertificateProfilesManager
    {
        private static IVsOutputWindowPane outputPane = null;

        private static CertificateProfile certProfile = null;

        private static FileSystemWatcher watcher = null;

        // Define a unique key for each event.
        private static readonly object EventKey = new object();
        private static EventHandlerList listEventDelegates = null;

        private static string profileFilePath = null;

        public static event EventHandler<CertificateProfileChangedEventArgs> ProfilesChanged
        {
            add
            {
                if (listEventDelegates != null)
                {
                    listEventDelegates.AddHandler(EventKey, value);
                }
            }

            remove
            {
                if (listEventDelegates != null)
                {
                    listEventDelegates.RemoveHandler(EventKey, value);
                }
            }
        }

        public static void Initialize(IVsOutputWindowPane outputPane)
        {
            CertificateProfilesManager.outputPane = outputPane;
        }

        public static void RegisterProfileFile(string filePath)
        {
            DeregisterWatchFile();

            if (!File.Exists(filePath))
            {
                return;
            }

            // Create new instance of certificate profile
            certProfile = new CertificateProfile(filePath);

            // Add watch event of profiles.xml
            String FileDirName = Path.GetDirectoryName(filePath);

            String FileName = Path.GetFileName(filePath);

            watcher = new FileSystemWatcher();

            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Path = FileDirName;
            watcher.Filter = FileName;

            watcher.Changed += new FileSystemEventHandler(OnFileChanged);

            listEventDelegates = new EventHandlerList();

            watcher.EnableRaisingEvents = true;

            profileFilePath = String.Copy(filePath);
        }

        public static void DeregisterWatchFile()
        {
            listEventDelegates?.Dispose();
            listEventDelegates = null;
            watcher?.Dispose();
            watcher = null;
            certProfile = null;
        }

        public static List<string> GetProfileNameList()
        {
            return certProfile?.GetProfileNameList(); // null if certProfile is null
        }

        public static CertificateProfileInfo GetProfileInfo(string profileName)
        {
            return certProfile?.GetProfileInfo(profileName); // null if certProfile is null
        }

        public static string GetActiveProfileName()
        {
            return certProfile?.GetActiveProfileName();
        }

        public static string GetProfileFilePath()
        {
            return profileFilePath;
        }

        private static void OnFileChanged(object source, FileSystemEventArgs e)
        {
            if (certProfile == null)
            {
                return;
            }

            CertificateProfile changedCertProfile;
            try
            {
                changedCertProfile = new CertificateProfile(e.FullPath);
            }
            catch
            {
                return;
            }

            // Check Something changed
            if (!Object.Equals(certProfile, changedCertProfile))
            {
                certProfile.LoadProfileXml(e.FullPath); // reload xml

                EventHandler<CertificateProfileChangedEventArgs> handler =
                    (EventHandler<CertificateProfileChangedEventArgs>)
                    listEventDelegates?[EventKey];

                if (handler == null)
                {
                    return;
                }

                CertificateProfileChangedEventArgs eArgs =
                    new CertificateProfileChangedEventArgs();

                eArgs.ProfileFilePath = e.FullPath;
                eArgs.ActiveProfile = certProfile.GetActiveProfileName();
                eArgs.ProfileFileContents = true;

                handler(source, eArgs);
            }
        }
    }
}
