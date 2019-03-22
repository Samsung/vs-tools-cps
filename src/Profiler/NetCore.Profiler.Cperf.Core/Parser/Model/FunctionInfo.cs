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

using System.Collections.Generic;

namespace NetCore.Profiler.Cperf.Core.Parser.Model
{
    /// <summary>
    /// A <see cref="CperfParser"/> data model class for function info ("fun inf")
    /// %Core %Profiler trace log records. Describes a function information.
    /// </summary>
    public class FunctionInfo
    {
        public ulong InternalId { get; }

        public ulong Id { get; }

        public ulong ClassId { get; }

        public ulong ModuleId { get; }

        public ulong FuncToken { get; }

        public List<CodeInfo> CodeInfos { get; } = new List<CodeInfo>();

        public List<CodeMapping> CodeMappings { get; } = new List<CodeMapping>();

        public FunctionInfo(ulong internalId, ulong id, ulong classId, ulong moduleId, ulong funcToken)
        {
            InternalId = internalId;
            Id = id;
            ClassId = classId;
            ModuleId = moduleId;
            FuncToken = funcToken;
        }
    }
}
