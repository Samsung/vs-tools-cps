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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Tizen.VisualStudio.APIChecker.Model;

namespace Tizen.VisualStudio.APIChecker.QuickFix
{
    internal class APICheckerSuggestedActionsSource : ISuggestedActionsSource
    {
        private readonly APICheckerSuggestedActionsSourceProvider m_factory;
        private readonly ITextBuffer m_textBuffer;
        private readonly ITextView m_textView;

        public event EventHandler<EventArgs> SuggestedActionsChanged;

        public APICheckerSuggestedActionsSource(APICheckerSuggestedActionsSourceProvider testSuggestedActionsSourceProvider, ITextView textView, ITextBuffer textBuffer)
        {
            EventHandler<EventArgs> handler = SuggestedActionsChanged;
            m_factory = testSuggestedActionsSourceProvider;
            m_textBuffer = textBuffer;
            m_textView = textView;
        }

        private bool TryGetWordUnderCaret(out TextExtent wordExtent)
        {
            ITextCaret caret = m_textView.Caret;
            SnapshotPoint point;

            if (caret.Position.BufferPosition > 0)
            {
                point = caret.Position.BufferPosition - 1;
            }
            else
            {
                wordExtent = default(TextExtent);
                return false;
            }

            ITextStructureNavigator navigator = m_factory.NavigatorService.GetTextStructureNavigator(m_textBuffer);

            wordExtent = navigator.GetExtentOfWord(point);
            return true;
        }

        public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            int line = range.Start.GetContainingLine().LineNumber;
            return Task.Factory.StartNew(() =>
            {
                APICheckerWindowTaskProvider taskProvider = APICheckerWindowTaskProvider.GetTaskProvider();
                if (taskProvider == null)
                {
                    return false;
                }

                foreach (Microsoft.VisualStudio.Shell.TaskListItem task in taskProvider.Tasks)
                {
                    if (task.Line == line)
                    {
                        return true;
                    }
                }

                return false;
            });
        }

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            //TextExtent extent;
            APICheckerWindowTaskProvider taskProvider = APICheckerWindowTaskProvider.GetTaskProvider();
            if (taskProvider == null)
            {
                return Enumerable.Empty<SuggestedActionSet>();
            }

            int line = range.Start.GetContainingLine().LineNumber;
            foreach (Microsoft.VisualStudio.Shell.TaskListItem task in taskProvider.Tasks)
            {
                if ((task.Line == line) && (task is APICheckerTask))
                {
                    if (task is NeedsPrivilegeTask)
                    {
                        var addPrivilegeAction = new AddPrivilegeAction((NeedsPrivilegeTask)task);
                        return new SuggestedActionSet[] { new SuggestedActionSet(
                            "addPrivilegeAction",
                            new ISuggestedAction[] { addPrivilegeAction },
                            "addPrivilegeAction",
                            SuggestedActionSetPriority.None,
                            null) };
                    }
                    //TODO: Handle Unused Privilege QuickFix
                }
            }

            return Enumerable.Empty<SuggestedActionSet>();
        }

        public void Dispose()
        {
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            // This is a sample provider and doesn't participate in LightBulb telemetry  
            telemetryId = Guid.Empty;
            return false;
        }
    }
}