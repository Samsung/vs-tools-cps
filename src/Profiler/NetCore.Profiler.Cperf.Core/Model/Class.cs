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

using System.ComponentModel;
using System.Linq;

namespace NetCore.Profiler.Cperf.Core.Model
{
    /// <summary>
    /// A <see cref="DataContainer"/> data model class for an application class from a %Core %Profiler log.
    /// </summary>
    public class Class : IIdentifiable
    {
        public ulong InternalId { get; set; }

        public ulong Id { get; set; }

        public ulong ModuleId { get; set; }

        public int? Token { get; set; }

        public string Name { get; set; }

        public override string ToString()
        {
            return TypeDescriptor.GetProperties(this).Cast<PropertyDescriptor>()
                .Aggregate("Class[", (current, descriptor) => current + ("{" + descriptor.Name + ": " + descriptor.GetValue(this) + "}")) + "]";
        }
    }
}
