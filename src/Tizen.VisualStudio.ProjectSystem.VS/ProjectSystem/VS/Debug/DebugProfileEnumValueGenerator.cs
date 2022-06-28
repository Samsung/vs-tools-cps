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
using System.Threading.Tasks;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Tizen.VisualStudio.Tools.DebugBridge;
using System.Linq;
using System;
using System.Collections.ObjectModel;

namespace Tizen.VisualStudio.ProjectSystem.VS.Debug
{
    /// <summary>
    /// Generates dynamic enum values.
    /// </summary>
	public class DebugProfileEnumValueGenerator : IDynamicEnumValuesGenerator
    {
        /// <summary>
        /// Gets whether the dropdown property UI should allow users to type in custom strings
        /// which will be validated by <see cref="TryCreateEnumValueAsync"/>.
        /// </summary>
        public bool AllowCustomValues
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// The list of values for this property that should be displayed to the user as common options.
        /// It may not be a comprehensive list of all admissible values however.
        /// </summary>
        /// <returns>List of sdb devices</returns>
        /// <seealso cref="AllowCustomValues"/>
        /// <seealso cref="TryCreateEnumValueAsync"/>
        public async Task<ICollection<IEnumValue>> GetListedValuesAsync()
        {
            await Task.Yield();

            return GetEnumeratorEnumValues();
        }

        internal static ICollection<IEnumValue> GetEnumeratorEnumValues()
        {
            bool hasDevice = DeviceManager.DeviceInfoList?.Count > 0;

            if (hasDevice)
            {
                Collection<IEnumValue> values = new Collection<IEnumValue>();
                DeviceManager.DeviceInfoList.Reverse();
                for (int i=0; i< DeviceManager.DeviceInfoList.Count; ++i)
                {
                    SDBDeviceInfo profile = DeviceManager.DeviceInfoList[i];
                    if (i == 0)
                        values.Add(new PageEnumValue(new EnumValue() { Name = string.Format("{0} ({1})", profile.Name, profile.Serial), DisplayName = string.Format("{0} ({1})", profile.Name, profile.Serial), IsDefault = true }));
                    else
                        values.Add(new PageEnumValue(new EnumValue() { Name = string.Format("{0} ({1})", profile.Name, profile.Serial), DisplayName = string.Format("{0} ({1})", profile.Name, profile.Serial) }));
                }
                return values;
            }
            else
            {
                return new Collection<IEnumValue>()
                {
                    new PageEnumValue(new EnumValue() { Name = DeviceManager.LaunchEmulator, DisplayName = DeviceManager.LaunchEmulator, IsDefault = true })
                };
            }
        }

        /// <summary>
        /// Tries to find or create an <see cref="IEnumValue"/> based on some user supplied string.
        /// </summary>
        /// <param name="userSuppliedValue">The string entered by the user in the property page UI.</param>
        /// <returns>
        /// An instance of <see cref="IEnumValue"/> if the <paramref name="userSuppliedValue"/> was successfully used
        /// to generate or retrieve an appropriate matching <see cref="IEnumValue"/>.
        /// A task whose result is <c>null</c> otherwise.
        /// </returns>
        /// <remarks>
        /// If <see cref="AllowCustomValues"/> is false, this method is expected to return a task with a <c>null</c> result
        /// unless the <paramref name="userSuppliedValue"/> matches a value in <see cref="GetListedValuesAsync"/>.
        /// A new instance of an <see cref="IEnumValue"/> for a value
        /// that was previously included in <see cref="GetListedValuesAsync"/> may be returned.
        /// </remarks>
        public async Task<IEnumValue> TryCreateEnumValueAsync(string userSuppliedValue)
        {
            await Task.Yield();

            return new PageEnumValue(new EnumValue() { Name = userSuppliedValue, DisplayName = userSuppliedValue });
            //return new PageEnumValue(new EnumValue() { Name = "userSuppliedValue", DisplayName = "userSuppliedValue" });
        }
    }
}
