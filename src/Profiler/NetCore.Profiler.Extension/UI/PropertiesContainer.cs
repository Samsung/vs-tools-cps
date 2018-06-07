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
using System.ComponentModel;

namespace NetCore.Profiler.Extension.UI
{
    internal class PropertiesContainer : CustomTypeDescriptor
    {
        public PropertiesContainer(string firstName, string secondName)
        {
            _secondName = secondName;
            _firstName = firstName;
        }

        // Gets the bold/first name shown in the gridview header
        private readonly string _firstName;

        //Gets the light/second name shown in the gridview header
        private readonly string _secondName;

        /// <summary>
        /// Returns the name of the class represented by this type descriptor.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> containing the name of the component instance this type descriptor is describing. The default is null.
        /// </returns>
        public sealed override string GetComponentName()
        {
            return _firstName;
        }

        /// <summary>
        /// Returns the fully qualified name of the class represented by this type descriptor.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> containing the fully qualified class name of the type this type descriptor is describing. The default is null.
        /// </returns>
        public sealed override string GetClassName()
        {
            return _secondName;
        }

        TypeConverter _rawDescriptor;
        TypeConverter Raw
        {
            get { return _rawDescriptor ?? (_rawDescriptor = TypeDescriptor.GetConverter(this, true)); }
        }

        /// <summary>
        /// Returns a collection of property descriptors for the object represented by this type descriptor.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.ComponentModel.PropertyDescriptorCollection"/> containing the property descriptions for the object represented by this type descriptor. The default is <see cref="F:System.ComponentModel.PropertyDescriptorCollection.Empty"/>.
        /// </returns>
        public override PropertyDescriptorCollection GetProperties()
        {
            return Raw.GetProperties(this);
        }

        /// <summary>
        /// Returns a type converter for the type represented by this type descriptor.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.ComponentModel.TypeConverter"/> for the type represented by this type descriptor. The default is a newly created <see cref="T:System.ComponentModel.TypeConverter"/>.
        /// </returns>
        public override TypeConverter GetConverter()
        {
            return Raw;
        }

        /// <summary>
        /// Returns a filtered collection of property descriptors for the object represented by this type descriptor.
        /// </summary>
        /// <param name="attributes">An array of attributes to use as a filter. This can be null.</param>
        /// <returns>
        /// A <see cref="T:System.ComponentModel.PropertyDescriptorCollection"/> containing the property descriptions for the object represented by this type descriptor. The default is <see cref="F:System.ComponentModel.PropertyDescriptorCollection.Empty"/>.
        /// </returns>
        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            return Raw.GetProperties(null, null, attributes);
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </returns>
        public override string ToString()
        {
            return _secondName;
        }

        /// <summary>
        /// Returns an object that contains the property described by the specified property descriptor.
        /// </summary>
        /// <param name="pd">The property descriptor for which to retrieve the owning object.</param>
        /// <returns>
        /// An <see cref="T:System.Object"/> that owns the given property specified by the type descriptor. The default is null.
        /// </returns>
        public override object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }


    }
}