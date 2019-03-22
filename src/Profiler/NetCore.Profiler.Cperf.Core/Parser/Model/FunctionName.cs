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
    /// A <see cref="CperfParser"/> data model class for function info dump ("fun nam") %Core %Profiler
    /// trace log records. Contains a function name, return type and signature.
    /// </summary>
    public class FunctionName
    {
        public ulong InternalId { get; set; }

        public string FullName { get; set; }

        public string ReturnType { get; set; }

        public string Signature;

        public FunctionName(ulong internalId, string fullName, string returnType, string signature)
        {
            InternalId = internalId;
            FullName = fullName;
            ReturnType = returnType;
            Signature = signature;
        }
    }
}
