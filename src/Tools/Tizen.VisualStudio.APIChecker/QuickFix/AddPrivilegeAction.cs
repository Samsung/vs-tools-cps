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

using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Imaging.Interop;
using System.Threading;
using Tizen.VisualStudio.APIChecker.Model;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Documents;
using System.Xml.Linq;
using static Microsoft.VisualStudio.Shell.TaskProvider;

namespace Tizen.VisualStudio.APIChecker.QuickFix
{
    class AddPrivilegeAction : ISuggestedAction
    {
        private NeedsPrivilegeTask m_task;
        private string m_display;
        private XDocument xmlDoc;

        public AddPrivilegeAction(NeedsPrivilegeTask task)
        {
            m_task = task;
            List<string> reqPriv = task.GetRequiredPrivileges();
            string message = string.Join(",", reqPriv.ToArray());
            m_display = string.Format("Add '{0}' privilege to manifest file.", message);
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            xmlDoc = XDocument.Load(m_task.GetManifestFilePath());
            XNamespace ns = xmlDoc.Root.GetDefaultNamespace();
            XElement privileges = null;
            privileges = GetPrivilegesNode(xmlDoc);

            if (privileges == null)
            {
                xmlDoc.Root.Add(new XElement(ns + "privileges"));
                privileges = GetPrivilegesNode(xmlDoc);
            }

            if (privileges == null)
            {
                return null;
            }

            foreach (string privilege in m_task.GetRequiredPrivileges())
            {
                privileges.Add(new XElement(ns + "privilege", privilege));
            }

            var textBlock = new TextBlock();
            textBlock.Padding = new Thickness(5);
            textBlock.Inlines.Add(new Run() { Text = xmlDoc.ToString() });
            return Task.FromResult<object>(textBlock);
        }

        private static XElement GetPrivilegesNode(XDocument doc)
        {
            XElement privileges = null;
            foreach (XElement el in doc.Root.Elements())
            {
                if (el.Name.LocalName.Equals("privileges"))
                {
                    privileges = el;
                    break;
                }
            }

            return privileges;
        }

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<SuggestedActionSet>>(null);
        }

        public bool HasActionSets
        {
            get { return false; }
        }

        public string DisplayText
        {
            get { return m_display; }
        }

        public ImageMoniker IconMoniker
        {
            get { return default(ImageMoniker); }
        }

        public string IconAutomationText
        {
            get
            {
                return null;
            }
        }

        public string InputGestureText
        {
            get
            {
                return null;
            }
        }

        public bool HasPreview
        {
            get { return true; }
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            xmlDoc.Save(m_task.GetManifestFilePath());
            APICheckerWindowTaskProvider taskProvider = APICheckerWindowTaskProvider.GetTaskProvider();
            List<NeedsPrivilegeTask> removedTasks = new List<NeedsPrivilegeTask>();
            TaskCollection Tasks = APICheckerWindowTaskProvider.GetTaskProvider().Tasks;
            List<string> privileges = m_task.GetRequiredPrivileges().ToList();
            foreach (var task in Tasks)
            {
                if (task is NeedsPrivilegeTask)
                {
                    NeedsPrivilegeTask privTask = (NeedsPrivilegeTask)task;
                    int initPrivilegeCount = privTask.GetRequiredPrivileges().Count;
                    foreach (var privilge in privileges)
                    {
                        if (privTask.GetRequiredPrivileges().Contains(privilge))
                        {
                            privTask.GetRequiredPrivileges().Remove(privilge);
                        }
                    }

                    int finalCount = privTask.GetRequiredPrivileges().Count;
                    if (initPrivilegeCount != finalCount)
                    {
                        if (finalCount != 0)
                        {
                            taskProvider.ReportMissingPrivileges(privTask.GetRequiredPrivileges(), privTask.ApiName,
                                privTask.Line, privTask.Column, privTask.FileName, privTask.GetManifestFilePath());
                        }

                        removedTasks.Add(privTask);
                    }
                }
            }

            foreach (var task in removedTasks)
            {
                Tasks.Remove(task);
            }
        }

        public void Dispose()
        {
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            // This is a sample action and doesn't participate in LightBulb telemetry  
            telemetryId = Guid.Empty;
            return false;
        }

    }
}
