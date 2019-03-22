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

namespace NetCore.Profiler.Cperf.Core.Parser.Model
{
    /// <summary>
    /// A <see cref="CperfParser"/> data model class for source line information ("lin src") %Core %Profiler
    /// trace log records.
    /// </summary>
    public class SourceLine
    {
        public ulong Id { get; set; }

        public ulong SourceFileIntId { get; set; }

        public ulong FunctionIntId { get; set; }

        public ulong StartLine { get; set; }

        public ulong EndLine { get; set; }

        public ulong StartColumn { get; set; }

        public ulong EndColumn { get; set; }

        public SourceLine(ulong internalId, ulong sourceFileIntId, ulong functionIntId, ulong startLine, ulong startColumn, ulong endLine, ulong endColumn)
        {
            Id = internalId;
            SourceFileIntId = sourceFileIntId;
            FunctionIntId = functionIntId;
            StartLine = startLine;
            EndLine = endLine;
            StartColumn = startColumn;
            EndColumn = endColumn;
        }
    }
}
