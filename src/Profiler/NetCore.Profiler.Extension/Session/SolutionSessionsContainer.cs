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
using EnvDTE80;
using NetCore.Profiler.Extension.VSPackage;
using NetCore.Profiler.Session.Core;

namespace NetCore.Profiler.Extension.Session
{
    class SolutionSessionsContainer : ISolutionSessionsContainer
    {
        private List<ISavedSession> _sessions = new List<ISavedSession>();

        private readonly DTE2 _dte;

        public SolutionSessionsContainer(DTE2 dte)
        {
            _dte = dte;
        }

        public IEnumerable<ISavedSession> Sessions => _sessions;

        public void DeleteSession(ISavedSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }
            try
            {
                Directory.Delete(session.SessionFolder, true);
            }
            catch (Exception ex)
            {
                ProfilerPlugin.Instance.ShowError($"Could not delete \"{session.SessionFolder}\". {ex.Message}");
            }
            Update();
        }

        public event SessionsListUpdatedHandler SessionsListUpdated;

        public void Update()
        {
            _sessions.Clear();
            if (_dte.Solution != null)
            {
                if (_dte.Solution is Solution2 sol2)
                {
                    LoadSessions(sol2.FullName);
                }
            }
            SessionsListUpdated?.Invoke();
        }

        private void LoadSessions(string solutionFullName)
        {
            if (!string.IsNullOrEmpty(solutionFullName))
            {
                string solutionFolder = Path.GetDirectoryName(solutionFullName);
                _sessions.AddRange(
                    Directory.GetFiles(solutionFolder, SessionConstants.SessionFileName, SearchOption.AllDirectories)
                    .Select((sessionFile) => LoadSession(solutionFolder, sessionFile))
                    .Where(session => (session != null)));
            }
        }

        private static SavedSession LoadSession(string solutionFolder, string sessionFile)
        {
            try
            {
                var s = new SavedSession()
                {
                    SolutionFolder = solutionFolder,
                    ProjectFolder = Path.GetDirectoryName(Path.GetDirectoryName(sessionFile)),
                    SessionFile = sessionFile
                };
                s.Load();
                return s;
            }
            catch
            {
                return null;
            }
        }
    }
}
