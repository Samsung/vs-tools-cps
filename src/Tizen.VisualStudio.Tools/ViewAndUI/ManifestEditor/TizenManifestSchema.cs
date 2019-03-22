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
using System.Linq;


namespace Tizen.VisualStudio.ManifestEditor
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Xml.Serialization;


    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public enum InstallLocationType
    {

        auto,

        [System.Xml.Serialization.XmlEnumAttribute("internal-only")]
        internalonly,

        [System.Xml.Serialization.XmlEnumAttribute("prefer-external")]
        preferexternal,
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public enum PackageType
    {

        rpm,

        tpk,

        wgt,

        apk,

        coretpk,
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public enum ProfileType
    {

        mobile,

        tv,

        wearable,

        common,
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public enum TypeType
    {

        capp,

        [System.Xml.Serialization.XmlEnumAttribute("c++app")]
        capp1,

        webapp,

        dotnet,

        [System.Xml.Serialization.XmlEnumAttribute("dotnet-inhouse")]
        dotnetinhouse,
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public enum HwAccelerationType
    {

        [System.Xml.Serialization.XmlEnumAttribute("use-GL")]
        useGL,

        [System.Xml.Serialization.XmlEnumAttribute("not-use-GL")]
        notuseGL,

        [System.Xml.Serialization.XmlEnumAttribute("use-system-setting")]
        usesystemsetting,
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public enum ScreenReaderType
    {

        [System.Xml.Serialization.XmlEnumAttribute("screenreader-off")]
        screenreaderoff,

        [System.Xml.Serialization.XmlEnumAttribute("screenreader-on")]
        screenreaderon,

        [System.Xml.Serialization.XmlEnumAttribute("use-system-setting")]
        usesystemsetting,
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public enum RecentImage
    {

        icon,

        capture,
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public enum SectionType
    {

        notification,

        setting,
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public enum LaunchType
    {

        None,

        caller,

        single,

        group,
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public enum ResolutionType
    {
        None,

        xxhdpi,

        xhdpi,

        hdpi,

        mdpi,

        ldpi,
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public enum ManageTaskType
    {
        None,
        [System.Xml.Serialization.XmlEnumAttribute("true")]
        True,
        [System.Xml.Serialization.XmlEnumAttribute("false")]
        False,
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public enum NoDisplayType
    {
        None,
        [System.Xml.Serialization.XmlEnumAttribute("true")]
        True,
        [System.Xml.Serialization.XmlEnumAttribute("false")]
        False,
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public enum NewHWaccelerationType
    {
        None,
        on,
        off,
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public enum NewDisplaySplashType
    {
        None,
        [System.Xml.Serialization.XmlEnumAttribute("true")]
        True,
        [System.Xml.Serialization.XmlEnumAttribute("false")]
        False,
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public enum AmbientType
    {
        None,
        [System.Xml.Serialization.XmlEnumAttribute("true")]
        True,
        [System.Xml.Serialization.XmlEnumAttribute("false")]
        False,
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public enum AutorestartType
    {
        None,
        [System.Xml.Serialization.XmlEnumAttribute("true")]
        True,
        [System.Xml.Serialization.XmlEnumAttribute("false")]
        False,
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public enum OnbootType
    {
        None,
        [System.Xml.Serialization.XmlEnumAttribute("true")]
        True,
        [System.Xml.Serialization.XmlEnumAttribute("false")]
        False,
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class manifest : object, System.ComponentModel.INotifyPropertyChanged
    {
        private ItemsChoiceType AppTypeField;
        private object[] itemsField;
        private ItemsChoiceType[] itemsElementNameField;
        private string storeclientidField;
        private InstallLocationType installlocationField;
        private bool installlocationFieldSpecified;
        private string packageField;
        private PackageType typeField;
        private bool typeFieldSpecified;
        private string versionField;
        private string sizeField;
        private string root_pathField;
        private string csc_pathField;
        private bool appsettingField;
        private bool appsettingFieldSpecified;
        private bool nodisplaysettingField;
        private bool nodisplaysettingFieldSpecified;
        private string urlField;
        private bool supportdisableField;
        private bool supportdisableFieldSpecified;
        private bool motherpackageField;
        private bool motherpackageFieldSpecified;
        private string apiversionField;
        private string supportmodeField;
        private string supportresetField;
        private feature[] featureField;
        private appdefprivilegelist appdefprivilegeListField;
        private privileges privilegesField;
        private author pauthorField;
        private shortcutlist shortcutListField;
        private account[] accountListField;
        private description[] descriptionListField;
        private profile profileField;

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }

        [System.Xml.Serialization.XmlElementAttribute("author", typeof(author))]
        public author authorField
        {
            get
            {
                return pauthorField;
            }

            set
            {
                pauthorField = value;
            }
        }

        [XmlIgnore()]
        public ItemsChoiceType Apptype
        {
            get
            {
                return AppTypeField;
            }

            set
            {
                AppTypeField = value;
            }
        }

        [XmlIgnore()]
        public application applicationField
        {
            get
            {
                var item = this.Items.FirstOrDefault(x => x is application);
                if (item == null)
                {
                    return null;
                }
                else
                {
                    return (item as application);
                }
            }

            set
            {
                var item = this.Items.FirstOrDefault(x => x is application);
                if (item == null)
                {
                    var array = this.Items;
                    Array.Resize(ref array, this.Items.Length + 1);
                    array[array.Length - 1] = value;
                    this.Items = array;
                }
                else
                {
                    item = value;
                }
            }
        }

        [XmlIgnore()]
        public label labelField
        {
            get
            {
                if (this.applicationField == null)
                {
                    return new label();
                }
                else
                {
                    var itemLabel = this.applicationField.Items
                                            .FirstOrDefault(x => x is label);
                    if (itemLabel == null)
                    {
                        return new label();
                    }
                    else
                    {
                        return itemLabel as label;
                    }
                }
            }

            set
            {
                if (this.applicationField == null)
                {
                    this.applicationField = new uiapplication(); //FIXME : other application type
                }
                var itemLabel = this.applicationField.Items
                                            .FirstOrDefault(x => x is label);
                if (itemLabel == null)
                {
                    var array = this.applicationField.Items;
                    Array.Resize(ref array, this.applicationField.Items.Length + 1);
                    array[array.Length - 1] = value;
                    this.applicationField.Items = array;
                }
                else
                {
                    itemLabel = value;
                }
            }
        }

        [XmlIgnore()]
        public label[] labels
        {
            get
            {
                if (this.applicationField == null)
                {
                    return null;
                }
                else
                {
                    var labelArr = from item
                                   in this.applicationField.Items
                                   where item is label
                                   select item;
                    var labelAllArr = labelArr.Cast<label>();

                    var labelLangArr = from item in labelAllArr
                                       where string.IsNullOrEmpty(item.lang) == false
                                       select item;

                    return labelLangArr.ToArray();
                }
            }

            set
            {
                List<object> objectArr = this.applicationField.Items.ToList();
                List<object> newArr = new List<object>();

                foreach (var item in objectArr)
                {
                    if (item is label)
                    {
                        label itemLabel = item as label;

                        if (string.IsNullOrEmpty(itemLabel.lang))
                        {
                            newArr.Add(item);
                        }
                    }
                    else
                    {
                        newArr.Add(item);
                    }
                }

                this.applicationField.Items = newArr.Concat(value).ToArray();
            }
        }

        [XmlIgnore()]
        public List<label> LocalizationLabelList
        {
            get
            {
                if (this.labels == null)
                {
                    return new List<label>();
                }
                return this.labels.ToList();
            }

            set
            {
                this.labels = value.ToArray();
            }
        }

        [XmlIgnore()]
        public metadata[] metadatas
        {
            get
            {
                if (this.applicationField == null)
                {
                    return null;
                }
                else
                {
                    var metadataArr = from item
                                      in this.applicationField.Items
                                      where item is metadata
                                      select item;
                    var metadataAllArr = metadataArr.Cast<metadata>();
                    return metadataAllArr.ToArray();
                }
            }

            set
            {
                List<object> objectArr = this.applicationField.Items.ToList();
                List<object> newArr = new List<object>();
                var notMetadataArr = from item
                                     in this.applicationField.Items
                                     where (item is metadata) == false
                                     select item;
                this.applicationField.Items
                    = notMetadataArr.ToList().Concat(value).ToArray();
            }
        }

        [XmlIgnore()]
        public List<metadata> AdvanceMetadataList
        {
            get
            {
                if (this.metadatas == null)
                {
                    return new List<metadata>();
                }
                return this.metadatas.ToList();
            }

            set
            {
                this.metadatas = value.ToArray();
            }
        }

        [XmlIgnore()]
        public datacontrol[] datacontrols
        {
            get
            {
                if (this.applicationField == null)
                {
                    return null;
                }
                else
                {
                    var datacontrolArr = from item
                                         in this.applicationField.Items
                                         where item is datacontrol
                                         select item;
                    var datacontrolAllArr = datacontrolArr.Cast<datacontrol>();
                    return datacontrolAllArr.ToArray();
                }
            }

            set
            {
                List<object> objectArr = this.applicationField.Items.ToList();
                List<object> newArr = new List<object>();
                var notDataControlArr = from item
                                        in this.applicationField.Items
                                        where (item is datacontrol) == false
                                        select item;
                this.applicationField.Items
                    = notDataControlArr.ToList().Concat(value).ToArray();
            }
        }

        [XmlIgnore()]
        public List<datacontrol> AdvanceDataControlList
        {
            get
            {
                if (this.datacontrols == null)
                {
                    return new List<datacontrol>();
                }
                return this.datacontrols.ToList();
            }

            set
            {
                this.datacontrols = value.ToArray();
            }
        }

        [XmlIgnore()]
        public appcontrol[] appcontrols
        {
            get
            {
                if (this.applicationField == null)
                {
                    return null;
                }
                else
                {
                    var appcontrolArr = from item
                                        in this.applicationField.Items
                                        where item is appcontrol
                                        select item;
                    var appcontrolAllArr = appcontrolArr.Cast<appcontrol>();
                    return appcontrolAllArr.ToArray();
                }
            }

            set
            {
                List<object> objectArr = this.applicationField.Items.ToList();
                List<object> newArr = new List<object>();
                var notAppControlArr = from item
                                       in this.applicationField.Items
                                       where (item is appcontrol) == false
                                       select item;
                this.applicationField.Items
                    = notAppControlArr.ToList().Concat(value).ToArray();
            }
        }

        [XmlIgnore()]
        public List<appcontrol> AdvanceAppControlList
        {
            get
            {
                if (this.appcontrols == null)
                {
                    return new List<appcontrol>();
                }
                return this.appcontrols.ToList();
            }

            set
            {
                this.appcontrols = value.ToArray();
            }
        }

        [XmlIgnore()]
        public List<feature> featureList
        {
            get
            {
                if (this.feature == null)
                {
                    return new List<feature>();
                }
                return this.feature.ToList();
            }

            set
            {
                this.feature = value.ToArray();
            }
        }

        [XmlIgnore()]
        public icon[] icons
        {
            get
            {
                if (this.applicationField == null)
                {
                    return null;
                }
                else
                {
                    var iconArr = from item
                                  in this.applicationField.Items
                                  where item is icon
                                  select item;

                    var iconAllArr = iconArr.Cast<icon>();

                    var iconLangArr = from item
                                      in iconAllArr
                                      where (string.IsNullOrEmpty(item.lang) == false || string.IsNullOrEmpty(item.resolution) == false)
                                      select item;

                    return iconLangArr.ToArray();
                }
            }

            set
            {
                List<object> objectArr = this.applicationField.Items.ToList();
                List<object> newArr = new List<object>();

                foreach (var item in objectArr)
                {
                    if (item is icon)
                    {
                        icon itemIcon = item as icon;

                        if (string.IsNullOrEmpty(itemIcon.lang) && string.IsNullOrEmpty(itemIcon.resolution))
                        {
                            newArr.Add(item);
                        }
                    }
                    else
                    {
                        newArr.Add(item);
                    }
                }

                this.applicationField.Items = newArr.Concat(value).ToArray();
            }
        }

        [XmlIgnore()]
        public List<icon> LocalizationIconList
        {
            get
            {
                if (this.icons == null)
                {
                    return new List<icon>();
                }
                return this.icons.ToList();
            }

            set
            {
                this.icons = value.ToArray();
            }
        }

        [XmlIgnore()]
        public icon iconField
        {
            get
            {
                var item = this.applicationField;
                if (this.applicationField == null)
                {
                    return new icon();
                }
                else
                {
                    var itemIcon = this.applicationField.Items
                                                .FirstOrDefault(x => x is icon);
                    if (itemIcon == null)
                    {
                        return new icon();
                    }
                    else
                    {
                        return (itemIcon as icon);
                    }
                }
            }

            set
            {
                if (this.applicationField == null)
                {
                    this.applicationField = new uiapplication(); //FIXME : other application type
                }

                var item = this.applicationField.Items.FirstOrDefault(x => x is icon);
                if (item == null)
                {

                    var array = this.applicationField.Items;
                    Array.Resize(ref array,
                                 this.applicationField.Items.Length + 1);
                    array[array.Length - 1] = value;

                    this.applicationField.Items = array;
                }
                else
                {
                    item = value;
                }
            }
        }

        [XmlIgnore()]
        public description descriptionField
        {
            get
            {
                if (this.descriptionListField == null)
                {
                    return null;
                }
                else
                {
                    var item = this.descriptionListField.FirstOrDefault(x => (x.lang == null));
                    return item;
                }
            }

            set
            {
                var item = this.descriptionListField;
                if (item == null)
                {
                    item = (description[])Array.CreateInstance(typeof(description), 1);
                    item[item.Length - 1] = value;
                }
                else
                {
                    Array.Resize(ref item, item.Length + 1);
                    item[item.Length - 1] = value;
                }
                this.descriptionListField = item;
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("description", typeof(description))]
        public description[] descriptions
        {
            get
            {
                return this.descriptionListField;
            }

            set
            {
                this.descriptionListField = value;
            }
        }

        [XmlIgnore()]
        public List<description> LocalizationDescriptionList
        {
            get
            {
                if (this.descriptions == null)
                {
                    return new List<description>();
                }
                var newArr = from item in this.descriptions where (item.lang != null) select item;
                return newArr.ToList();
            }

            set
            {
                var item = value;
                if (descriptionField != null)
                {
                    item.Insert(0, descriptionField);
                }
                this.descriptions = item.ToArray();
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("profile", typeof(profile))]
        public profile profileType
        {
            get
            {
                return profileField;
            }
            set
            {
                profileField = value;

            }
        }


        [System.Xml.Serialization.XmlElementAttribute("compatibility", typeof(compatibility))]
        [System.Xml.Serialization.XmlElementAttribute("dynamicbox", typeof(object))]
        [System.Xml.Serialization.XmlElementAttribute("font", typeof(object))]
        [System.Xml.Serialization.XmlElementAttribute("icon", typeof(icon))]
        [System.Xml.Serialization.XmlElementAttribute("ime", typeof(object))]
        [System.Xml.Serialization.XmlElementAttribute("ime-application", typeof(imeapplication))]
        [System.Xml.Serialization.XmlElementAttribute("label", typeof(label))]
        [System.Xml.Serialization.XmlElementAttribute("livebox", typeof(object))]
        [System.Xml.Serialization.XmlElementAttribute("notifications", typeof(object))]
        [System.Xml.Serialization.XmlElementAttribute("ui-application", typeof(uiapplication))]
        [System.Xml.Serialization.XmlElementAttribute("service-application", typeof(serviceapplication))]
        [System.Xml.Serialization.XmlElementAttribute("widget-application", typeof(widgetapplication))]
        [System.Xml.Serialization.XmlElementAttribute("watch-application", typeof(watchapplication))]
        [System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemsElementName")]
        public object[] Items
        {
            get
            {
                return this.itemsField;
            }

            set
            {
                this.itemsField = value;
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("shortcut-list", typeof(shortcutlist))]
        public shortcutlist shortcutlistField
        {
            get
            {
                return shortcutListField;
            }

            set
            {
                shortcutListField = value;
            }
        }

        [XmlIgnore()]
        public List<account> accountList
        {
            get
            {
                if (this.account == null)
                {
                    return new List<account>();
                }
                return this.account.ToList();
            }

            set
            {
                this.account = value.ToArray();
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("account")]
        public account[] account
        {
            get
            {
                return this.accountListField;
            }

            set
            {
                this.accountListField = value;
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("privileges")]
        public privileges privileges
        {
            get
            {
                return this.privilegesField;
            }

            set
            {
                this.privilegesField = value;
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("provides-appdefined-privileges", typeof(appdefprivilegelist))]
        public appdefprivilegelist appdefprivilegelistField
        {
            get
            {
                return this.appdefprivilegeListField;
            }

            set
            {
                this.appdefprivilegeListField = value;
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("feature")]
        public feature[] feature
        {
            get
            {
                return this.featureField;
            }

            set
            {
                this.featureField = value;
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("ItemsElementName")]
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public ItemsChoiceType[] ItemsElementName
        {
            get
            {
                return this.itemsElementNameField;
            }

            set
            {
                this.itemsElementNameField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("storeclient-id")]
        public string storeclientid
        {
            get
            {
                return this.storeclientidField;
            }

            set
            {
                this.storeclientidField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("install-location")]
        public InstallLocationType installlocation
        {
            get
            {
                return this.installlocationField;
            }

            set
            {
                this.installlocationField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool installlocationSpecified
        {
            get
            {
                return this.installlocationFieldSpecified;
            }

            set
            {
                this.installlocationFieldSpecified = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string package
        {
            get
            {
                return this.packageField;
            }

            set
            {
                this.packageField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public PackageType type
        {
            get
            {
                return this.typeField;
            }

            set
            {
                this.typeField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool typeSpecified
        {
            get
            {
                return this.typeFieldSpecified;
            }

            set
            {
                this.typeFieldSpecified = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "NMTOKEN")]
        public string version
        {
            get
            {
                return this.versionField;
            }

            set
            {
                this.versionField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "NMTOKEN")]
        public string size
        {
            get
            {
                return this.sizeField;
            }

            set
            {
                this.sizeField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string root_path
        {
            get
            {
                return this.root_pathField;
            }

            set
            {
                this.root_pathField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string csc_path
        {
            get
            {
                return this.csc_pathField;
            }

            set
            {
                this.csc_pathField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool appsetting
        {
            get
            {
                return this.appsettingField;
            }

            set
            {
                this.appsettingField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool appsettingSpecified
        {
            get
            {
                return this.appsettingFieldSpecified;
            }

            set
            {
                this.appsettingFieldSpecified = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("nodisplay-setting")]
        public bool nodisplaysetting
        {
            get
            {
                return this.nodisplaysettingField;
            }

            set
            {
                this.nodisplaysettingField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool nodisplaysettingSpecified
        {
            get
            {
                return this.nodisplaysettingFieldSpecified;
            }

            set
            {
                this.nodisplaysettingFieldSpecified = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string url
        {
            get
            {
                return this.urlField;
            }

            set
            {
                this.urlField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("support-disable")]
        public bool supportdisable
        {
            get
            {
                return this.supportdisableField;
            }

            set
            {
                this.supportdisableField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool supportdisableSpecified
        {
            get
            {
                return this.supportdisableFieldSpecified;
            }

            set
            {
                this.supportdisableFieldSpecified = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("mother-package")]
        public bool motherpackage
        {
            get
            {
                return this.motherpackageField;
            }

            set
            {
                this.motherpackageField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool motherpackageSpecified
        {
            get
            {
                return this.motherpackageFieldSpecified;
            }

            set
            {
                this.motherpackageFieldSpecified = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("api-version")]
        public string apiversion
        {
            get
            {
                return this.apiversionField;
            }

            set
            {
                this.apiversionField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("support-mode")]
        public string supportmode
        {
            get
            {
                return this.supportmodeField;
            }

            set
            {
                this.supportmodeField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("support-reset")]
        public string supportreset
        {
            get
            {
                return this.supportresetField;
            }

            set
            {
                this.supportresetField = value;
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = true)]
    public partial class feature : object, System.ComponentModel.INotifyPropertyChanged
    {
        private string nameField;
        private string valueField;

        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "anyURI")]
        public string name
        {
            get
            {
                return this.nameField;
            }

            set
            {
                this.nameField = value;
            }
        }

        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value
        {
            get
            {
                return this.valueField;
            }

            set
            {
                this.valueField = value;
            }
        }
        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.Serializable()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class author : object, System.ComponentModel.INotifyPropertyChanged
    {
        private string emailField;
        private string hrefField;
        private string langField;
        private string textField;

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string email
        {
            get
            {
                if (this.emailField == "")
                    return null;
                return this.emailField;
            }

            set
            {
                this.emailField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string href
        {
            get
            {
                if (this.hrefField == "")
                    return null;
                return this.hrefField;
            }

            set
            {
                this.hrefField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/XML/1998/namespace")]
        public string lang
        {
            get
            {
                return this.langField;
            }

            set
            {
                this.langField = value;
            }
        }

        [System.Xml.Serialization.XmlTextAttribute()]
        public string Text
        {
            get
            {
                return this.textField;
            }

            set
            {
                this.textField = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class compatibility
    {
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = true)]
    public partial class description : object, System.ComponentModel.INotifyPropertyChanged
    {
        private string langField;
        private string textField;

        [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/XML/1998/namespace")]
        public string lang
        {
            get
            {
                return this.langField;
            }

            set
            {
                this.langField = value;
            }
        }

        [System.Xml.Serialization.XmlTextAttribute()]
        public string Text
        {
            get
            {
                return this.textField;
            }

            set
            {
                if (value.StartsWith("\r\n    "))
                {
                    string removePreFix = value.Substring("\r\n    ".Length);
                    this.textField = removePreFix.Replace("\r\n    ", "\r\n");
                }
                else
                {
                    this.textField = value;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = true)]
    public partial class icon : object, System.ComponentModel.INotifyPropertyChanged
    {
        private string langField;
        private string sectionField;
        private string resolutionField;
        private bool resolutionFieldSpecified;
        private string[] textField;

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }

        [System.Xml.Serialization.XmlTextAttribute()]
        public string[] Text
        {
            get
            {
                return this.textField;
            }

            set
            {
                this.textField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/XML/1998/namespace")]
        public string lang
        {
            get
            {
                return this.langField;
            }

            set
            {
                this.langField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string section
        {
            get
            {
                return this.sectionField;
            }

            set
            {
                this.sectionField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("dpi")]
        public string resolution
        {
            get
            {
                return this.resolutionField;
            }

            set
            {
                this.resolutionField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool resolutionSpecified
        {
            get
            {
                return this.resolutionFieldSpecified;
            }

            set
            {
                this.resolutionFieldSpecified = value;
            }
        }

    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute("ime-application", Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class imeapplication : object, System.ComponentModel.INotifyPropertyChanged
    {
        private object[] itemsField;
        private string appidField;
        private string execField;
        private bool multipleField;
        private bool multipleFieldSpecified;
        private bool nodisplayField;
        private bool nodisplayFieldSpecified;
        private TypeType typeField;

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }

        [System.Xml.Serialization.XmlElementAttribute("icon", typeof(icon))]
        [System.Xml.Serialization.XmlElementAttribute("label", typeof(label))]
        public object[] Items
        {
            get
            {
                return this.itemsField;
            }

            set
            {
                this.itemsField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string appid
        {
            get
            {
                return this.appidField;
            }

            set
            {
                this.appidField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "NCName")]
        public string exec
        {
            get
            {
                return this.execField;
            }

            set
            {
                this.execField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool multiple
        {
            get
            {
                return this.multipleField;
            }

            set
            {
                this.multipleField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool multipleSpecified
        {
            get
            {
                return this.multipleFieldSpecified;
            }

            set
            {
                this.multipleFieldSpecified = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool nodisplay
        {
            get
            {
                return this.nodisplayField;
            }

            set
            {
                this.nodisplayField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool nodisplaySpecified
        {
            get
            {
                return this.nodisplayFieldSpecified;
            }

            set
            {
                this.nodisplayFieldSpecified = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public TypeType type
        {
            get
            {
                return this.typeField;
            }

            set
            {
                this.typeField = value;
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class label : object, System.ComponentModel.INotifyPropertyChanged
    {
        private string langField;
        private string[] textField;

        [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/XML/1998/namespace")]
        public string lang
        {
            get
            {
                return this.langField;
            }

            set
            {
                this.langField = value;
            }
        }

        [System.Xml.Serialization.XmlTextAttribute()]
        public string[] Text
        {
            get
            {
                return this.textField;
            }

            set
            {
                this.textField = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = true)]
    public partial class privileges : object, System.ComponentModel.INotifyPropertyChanged
    {
        private List<string> privilegeField;
        private List<appdefprivilege> appdefprivilegeField;

        [System.Xml.Serialization.XmlElementAttribute("privilege", DataType = "anyURI")]
        public List<string> platformPrivList
        {
            get
            {
                return this.privilegeField;
            }

            set
            {
                this.privilegeField = value;
            }
        }        

        [System.Xml.Serialization.XmlElementAttribute("appdefined-privilege", typeof(appdefprivilege))]
        public List<appdefprivilege> consumerPrivList
        {
            get
            {
                return this.appdefprivilegeField;
            }

            set
            {
                this.appdefprivilegeField = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = true)]
    public partial class appdefprivilegelist : object, System.ComponentModel.INotifyPropertyChanged
    {
        private List<appdefprivilege> appdefprivilegeField;

        [System.Xml.Serialization.XmlElementAttribute("appdefined-privilege", typeof(appdefprivilege))]
        public List<appdefprivilege> providerPrivList
        {
            get
            {
                return this.appdefprivilegeField;
            }

            set
            {
                this.appdefprivilegeField = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = true)]
    public partial class background : object, System.ComponentModel.INotifyPropertyChanged
    {
        private string valueField;

        [System.Xml.Serialization.XmlAttributeAttribute("value")]
        public string value
        {
            get
            {
                return this.valueField;
            }

            set
            {
                this.valueField = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class profile : object, System.ComponentModel.INotifyPropertyChanged
    {
        private ProfileType nameField;

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ProfileType name
        {
            get
            {
                return this.nameField;
            }

            set
            {
                this.nameField = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute("shortcut-list", Namespace = "http://tizen.org/ns/packages", IsNullable = true)]
    public partial class shortcutlist : object, System.ComponentModel.INotifyPropertyChanged
    {
        private List<shortcut> itemsField;

        [System.Xml.Serialization.XmlElementAttribute("shortcut", typeof(shortcut))]
        public List<shortcut> Items
        {
            get
            {
                return this.itemsField;
            }

            set
            {
                this.itemsField = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute("ui-application", Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class uiapplication : application
    {
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute("service-application", Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class serviceapplication : application
    {
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute("widget-application", Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class widgetapplication : application
    {
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute("watch-application", Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class watchapplication : application
    {
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute("application", Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class application : object, System.ComponentModel.INotifyPropertyChanged
    {
        private object[] itemsField;
        private string appidField;
        private string updateperiodField;
        private string execField;
        private bool multipleField;
        private bool multipleFieldSpecified;
        private NoDisplayType nodisplayField;
        private bool nodisplayFieldSpecified;
        private ManageTaskType taskmanageField;
        private bool taskmanageFieldSpecified;
        private bool enabledField;
        private bool enabledFieldSpecified;
        private TypeType typeField;
        private string categoriesField;
        private string extraidField;
        private NewHWaccelerationType hwaccelerationField;
        private bool hwaccelerationFieldSpecified;
        private NewDisplaySplashType newdisplaysplashField;
        private bool newdisplaysplashFieldSpecified;
        private AmbientType ambientField;
        private bool ambientFieldSpecified;
        private ScreenReaderType screenreaderField;
        private bool screenreaderFieldSpecified;
        private RecentImage recentimageField;
        private bool recentimageFieldSpecified;
        private bool mainappField;
        private bool mainappFieldSpecified;
        private bool indicatordisplayField;
        private bool indicatordisplayFieldSpecified;
        private string portraiteffectimageField;
        private string landscapeeffectimageField;
        private string effectimagetypeField;
        private string guestmodevisibilityField;
        private bool launchconditionField;
        private bool launchconditionFieldSpecified;
        private string permissiontypeField;
        private string componenttypeField;
        private bool submodeField;
        private bool submodeFieldSpecified;
        private string submodemainidField;
        private bool processpoolField;
        private bool processpoolFieldSpecified;
        private AutorestartType autorestartField;
        private bool autorestartFieldSpecified;
        private OnbootType onbootField;
        private bool onbootFieldSpecified;
        private bool multiinstanceField;
        private bool multiinstanceFieldSpecified;
        private string multiinstancemainidField;
        private bool uigadgetField;
        private bool uigadgetFieldSpecified;
        private LaunchType launch_modeField;
        private bool launch_modeFieldSpecified;
        private background[] backgroundcategoriesField;
        private splash splashscreenField;

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }

        [System.Xml.Serialization.XmlElementAttribute("app-control", typeof(appcontrol))]
        [System.Xml.Serialization.XmlElementAttribute("application-service", typeof(applicationservice))]
        [System.Xml.Serialization.XmlElementAttribute("category", typeof(category))]
        [System.Xml.Serialization.XmlElementAttribute("datacontrol", typeof(datacontrol))]
        [System.Xml.Serialization.XmlElementAttribute("support-size", typeof(supportsize))]
        [System.Xml.Serialization.XmlElementAttribute("eventsystem", typeof(eventsystem))]
        [System.Xml.Serialization.XmlElementAttribute("label", typeof(label))]
        [System.Xml.Serialization.XmlElementAttribute("icon", typeof(icon))]
        [System.Xml.Serialization.XmlElementAttribute("image", typeof(image))]
        [System.Xml.Serialization.XmlElementAttribute("metadata", typeof(metadata))]
        [System.Xml.Serialization.XmlElementAttribute("permission", typeof(permission))]
        public object[] Items
        {
            get
            {
                return this.itemsField;
            }

            set
            {
                this.itemsField = value;
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("background-category")]
        public background[] backgroundcategories
        {
            get
            {
                return this.backgroundcategoriesField;
            }

            set
            {
                this.backgroundcategoriesField = value;
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("splash-screens")]
        public splash splashscreens
        {
            get
            {
                return this.splashscreenField; //FIXME : null splash screen
            }

            set
            {
                this.splashscreenField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string appid
        {
            get
            {
                return this.appidField;
            }

            set
            {
                this.appidField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("update-period")]
        public string updateperiod
        {
            get
            {
                return this.updateperiodField;
            }

            set
            {
                this.updateperiodField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string exec
        {
            get
            {
                return this.execField;
            }

            set
            {
                this.execField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool multiple
        {
            get
            {
                return this.multipleField;
            }

            set
            {
                this.multipleField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool multipleSpecified
        {
            get
            {
                return this.multipleFieldSpecified;
            }

            set
            {
                this.multipleFieldSpecified = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public NoDisplayType nodisplay
        {
            get
            {
                return this.nodisplayField;
            }

            set
            {
                this.nodisplayField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool nodisplaySpecified
        {
            get
            {
                return this.nodisplayFieldSpecified;
            }

            set
            {
                this.nodisplayFieldSpecified = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("taskmanage")]
        public ManageTaskType taskmanage
        {
            get
            {
                return this.taskmanageField;
            }

            set
            {
                this.taskmanageField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool taskmanageSpecified
        {
            get
            {
                return this.taskmanageFieldSpecified;
            }

            set
            {
                this.taskmanageFieldSpecified = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool enabled
        {
            get
            {
                return this.enabledField;
            }

            set
            {
                this.enabledField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool enabledSpecified
        {
            get
            {
                return this.enabledFieldSpecified;
            }

            set
            {
                this.enabledFieldSpecified = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public TypeType type
        {
            get
            {
                return this.typeField;
            }

            set
            {
                this.typeField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute(DataType = "NCName")]
        public string categories
        {
            get
            {
                return this.categoriesField;
            }

            set
            {
                this.categoriesField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string extraid
        {
            get
            {
                return this.extraidField;
            }

            set
            {
                this.extraidField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("hw-acceleration")]
        public NewHWaccelerationType hwacceleration
        {
            get
            {
                return this.hwaccelerationField;
            }

            set
            {
                this.hwaccelerationField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool hwaccelerationSpecified
        {
            get
            {
                return this.hwaccelerationFieldSpecified;
            }

            set
            {
                this.hwaccelerationFieldSpecified = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("splash-screen-display")]
        public NewDisplaySplashType newdisplaysplash
        {
            get
            {
                return this.newdisplaysplashField;
            }

            set
            {
                this.newdisplaysplashField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool newdisplaysplashSpecified
        {
            get
            {
                return this.newdisplaysplashFieldSpecified;
            }

            set
            {
                this.newdisplaysplashFieldSpecified = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("ambient-support")]
        public AmbientType ambientsupport
        {
            get
            {
                return this.ambientField;
            }

            set
            {
                this.ambientField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool ambientsupportSpecified
        {
            get
            {
                return this.ambientFieldSpecified;
            }

            set
            {
                this.ambientFieldSpecified = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("screen-reader")]
        public ScreenReaderType screenreader
        {
            get
            {
                return this.screenreaderField;
            }

            set
            {
                this.screenreaderField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool screenreaderSpecified
        {
            get
            {
                return this.screenreaderFieldSpecified;
            }

            set
            {
                this.screenreaderFieldSpecified = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public RecentImage recentimage
        {
            get
            {
                return this.recentimageField;
            }

            set
            {
                this.recentimageField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool recentimageSpecified
        {
            get
            {
                return this.recentimageFieldSpecified;
            }

            set
            {
                this.recentimageFieldSpecified = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool mainapp
        {
            get
            {
                return this.mainappField;
            }

            set
            {
                this.mainappField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool mainappSpecified
        {
            get
            {
                return this.mainappFieldSpecified;
            }

            set
            {
                this.mainappFieldSpecified = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool indicatordisplay
        {
            get
            {
                return this.indicatordisplayField;
            }

            set
            {
                this.indicatordisplayField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool indicatordisplaySpecified
        {
            get
            {
                return this.indicatordisplayFieldSpecified;
            }

            set
            {
                this.indicatordisplayFieldSpecified = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("portrait-effectimage")]
        public string portraiteffectimage
        {
            get
            {
                return this.portraiteffectimageField;
            }

            set
            {
                this.portraiteffectimageField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("landscape-effectimage")]
        public string landscapeeffectimage
        {
            get
            {
                return this.landscapeeffectimageField;
            }

            set
            {
                this.landscapeeffectimageField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("effectimage-type")]
        public string effectimagetype
        {
            get
            {
                return this.effectimagetypeField;
            }

            set
            {
                this.effectimagetypeField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("guestmode-visibility")]
        public string guestmodevisibility
        {
            get
            {
                return this.guestmodevisibilityField;
            }

            set
            {
                this.guestmodevisibilityField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool launchcondition
        {
            get
            {
                return this.launchconditionField;
            }

            set
            {
                this.launchconditionField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool launchconditionSpecified
        {
            get
            {
                return this.launchconditionFieldSpecified;
            }

            set
            {
                this.launchconditionFieldSpecified = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("permission-type")]
        public string permissiontype
        {
            get
            {
                return this.permissiontypeField;
            }

            set
            {
                this.permissiontypeField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("component-type")]
        public string componenttype
        {
            get
            {
                return this.componenttypeField;
            }

            set
            {
                this.componenttypeField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool submode
        {
            get
            {
                return this.submodeField;
            }

            set
            {
                this.submodeField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool submodeSpecified
        {
            get
            {
                return this.submodeFieldSpecified;
            }

            set
            {
                this.submodeFieldSpecified = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("submode-mainid")]
        public string submodemainid
        {
            get
            {
                return this.submodemainidField;
            }

            set
            {
                this.submodemainidField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("process-pool")]
        public bool processpool
        {
            get
            {
                return this.processpoolField;
            }

            set
            {
                this.processpoolField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool processpoolSpecified
        {
            get
            {
                return this.processpoolFieldSpecified;
            }

            set
            {
                this.processpoolFieldSpecified = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("auto-restart")]
        public AutorestartType autorestart
        {
            get
            {
                return this.autorestartField;
            }

            set
            {
                this.autorestartField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool autorestartSpecified
        {
            get
            {
                return this.autorestartFieldSpecified;
            }

            set
            {
                this.autorestartFieldSpecified = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("on-boot")]
        public OnbootType onboot
        {
            get
            {
                return this.onbootField;
            }

            set
            {
                this.onbootField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool onbootSpecified
        {
            get
            {
                return this.onbootFieldSpecified;
            }

            set
            {
                this.onbootFieldSpecified = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("multi-instance")]
        public bool multiinstance
        {
            get
            {
                return this.multiinstanceField;
            }

            set
            {
                this.multiinstanceField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool multiinstanceSpecified
        {
            get
            {
                return this.multiinstanceFieldSpecified;
            }

            set
            {
                this.multiinstanceFieldSpecified = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("multi-instance-mainid")]
        public string multiinstancemainid
        {
            get
            {
                return this.multiinstancemainidField;
            }

            set
            {
                this.multiinstancemainidField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("ui-gadget")]
        public bool uigadget
        {
            get
            {
                return this.uigadgetField;
            }

            set
            {
                this.uigadgetField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool uigadgetSpecified
        {
            get
            {
                return this.uigadgetFieldSpecified;
            }

            set
            {
                this.uigadgetFieldSpecified = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public LaunchType launch_mode
        {
            get
            {
                return this.launch_modeField;
            }

            set
            {
                this.launch_modeField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool launch_modeSpecified
        {
            get
            {
                return this.launch_modeFieldSpecified;
            }

            set
            {
                this.launch_modeFieldSpecified = value;
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute("app-control", Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class appcontrol : object, System.ComponentModel.INotifyPropertyChanged
    {
        private object[] itemsField;
        private List<string> privilegeListField = new List<string>();

        [System.Xml.Serialization.XmlElementAttribute("mime", typeof(mime))]
        [System.Xml.Serialization.XmlElementAttribute("operation", typeof(operation))]
        [System.Xml.Serialization.XmlElementAttribute("subapp", typeof(subapp))]
        [System.Xml.Serialization.XmlElementAttribute("uri", typeof(uri))]

        public object[] Items
        {
            get
            {
                return this.itemsField;
            }

            set
            {
                this.itemsField = value;
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("privilege", DataType = "anyURI")]
        public List<string> privilegeList
        {
            get
            {
                return this.privilegeListField;
            }

            set
            {
                this.privilegeListField = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class mime : object, System.ComponentModel.INotifyPropertyChanged
    {
        private string nameField;

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name
        {
            get
            {
                return this.nameField;
            }

            set
            {
                this.nameField = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class operation : object, System.ComponentModel.INotifyPropertyChanged
    {
        private string nameField;

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name
        {
            get
            {
                return this.nameField;
            }

            set
            {
                this.nameField = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class subapp : object, System.ComponentModel.INotifyPropertyChanged
    {
        private string nameField;

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name
        {
            get
            {
                return this.nameField;
            }

            set
            {
                this.nameField = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class uri : object, System.ComponentModel.INotifyPropertyChanged
    {
        private string nameField;

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name
        {
            get
            {
                return this.nameField;
            }

            set
            {
                this.nameField = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute("application-service", Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class applicationservice : object, System.ComponentModel.INotifyPropertyChanged
    {
        private object[] itemsField;

        [System.Xml.Serialization.XmlElementAttribute("mime", typeof(mime))]
        [System.Xml.Serialization.XmlElementAttribute("operation", typeof(operation))]
        [System.Xml.Serialization.XmlElementAttribute("subapp", typeof(subapp))]
        [System.Xml.Serialization.XmlElementAttribute("uri", typeof(uri))]
        public object[] Items
        {
            get
            {
                return this.itemsField;
            }

            set
            {
                this.itemsField = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class category : object, System.ComponentModel.INotifyPropertyChanged
    {
        private System.Xml.XmlElement[] itemsField;
        private System.Xml.XmlAttribute[] anyAttrField;

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }

        [System.Xml.Serialization.XmlAnyElementAttribute()]
        public System.Xml.XmlElement[] Items
        {
            get
            {
                return this.itemsField;
            }

            set
            {
                this.itemsField = value;
            }
        }

        [System.Xml.Serialization.XmlAnyAttributeAttribute()]
        public System.Xml.XmlAttribute[] AnyAttr
        {
            get
            {
                return this.anyAttrField;
            }

            set
            {
                this.anyAttrField = value;
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class supportsize : object, System.ComponentModel.INotifyPropertyChanged
    {
        private string previewField;
        private string textField;

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string preview
        {
            get
            {
                return this.previewField;
            }

            set
            {
                this.previewField = value;
            }
        }

        [System.Xml.Serialization.XmlTextAttribute()]
        public string Text
        {
            get
            {
                return this.textField;
            }

            set
            {
                this.textField = value;
            }
        }

    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class datacontrol : object, System.ComponentModel.INotifyPropertyChanged
    {
        private string provideridField;
        private string accessField;
        private string typeField;
        private string trustedField;
        private List<string> privilegeListField = new List<string>();

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }

        [System.Xml.Serialization.XmlElementAttribute("privilege", DataType = "anyURI")]
        public List<string> privilegeList
        {
            get
            {
                return this.privilegeListField;
            }

            set
            {
                this.privilegeListField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string providerid
        {
            get
            {
                return this.provideridField;
            }

            set
            {
                this.provideridField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string access
        {
            get
            {
                return this.accessField;
            }

            set
            {
                this.accessField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string type
        {
            get
            {
                return this.typeField;
            }

            set
            {
                this.typeField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string trusted
        {
            get
            {
                return this.trustedField;
            }

            set
            {
                if (value == "false")
                    this.trustedField = null;
                else
                    this.trustedField = value;
            }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class eventsystem : object, System.ComponentModel.INotifyPropertyChanged
    {
        private string eventidField;
        private string oneventlaunchField;
        private string typeField;

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string eventid
        {
            get
            {
                return this.eventidField;
            }

            set
            {
                this.eventidField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("on-event-launch")]
        public string oneventlaunch
        {
            get
            {
                return this.oneventlaunchField;
            }

            set
            {
                this.oneventlaunchField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string type
        {
            get
            {
                return this.typeField;
            }

            set
            {
                this.typeField = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class image : object, System.ComponentModel.INotifyPropertyChanged
    {
        private string langField;
        private string sectionField;
        private string[] textField;

        [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/XML/1998/namespace")]
        public string lang
        {
            get
            {
                return this.langField;
            }

            set
            {
                this.langField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string section
        {
            get
            {
                return this.sectionField;
            }

            set
            {
                this.sectionField = value;
            }
        }

        [System.Xml.Serialization.XmlTextAttribute()]
        public string[] Text
        {
            get
            {
                return this.textField;
            }

            set
            {
                this.textField = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class metadata : object, System.ComponentModel.INotifyPropertyChanged
    {
        private string keyField;
        private string valueField;

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string key
        {
            get
            {
                return this.keyField;
            }

            set
            {
                this.keyField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string value
        {
            get
            {
                return this.valueField;
            }

            set
            {
                this.valueField = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class appdefprivilege : object, System.ComponentModel.INotifyPropertyChanged
    {
        private string licenseField;
        private string valueField;

        [System.Xml.Serialization.XmlAttributeAttribute("license")]
        public string License
        {
            get
            {
                return this.licenseField;
            }
            set
            {
                this.licenseField = value;
            }
        }

        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class shortcut : object, System.ComponentModel.INotifyPropertyChanged
    {
        private List<object> itemsField = new List<object>();
        private string appIdField;
        private string extraDataField;
        private string extraKeyField;
        private icon iconField;

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string appid
        {
            get
            {
                return this.appIdField;
            }

            set
            {
                this.appIdField = value;
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("icon", typeof(icon))]
        public icon icon
        {
            get
            {
                return this.iconField;
            }

            set
            {
                this.iconField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string extra_data
        {
            get
            {
                return this.extraDataField;
            }

            set
            {
                this.extraDataField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string extra_key
        {
            get
            {
                return this.extraKeyField;
            }

            set
            {
                this.extraKeyField = value;
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("label", typeof(label))]
        public List<object> Items
        {
            get
            {
                return this.itemsField;
            }

            set
            {
                this.itemsField = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class splash : object, System.ComponentModel.INotifyPropertyChanged
    {
        private List<splashscreen> splashscreenField = new List<splashscreen>();

        [System.Xml.Serialization.XmlElementAttribute("splash-screen")]
        public List<splashscreen> splashscreen
        {
            get
            {
                return this.splashscreenField;
            }

            set
            {
                this.splashscreenField = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class splashscreen : object, System.ComponentModel.INotifyPropertyChanged
    {
        private string appcontroloperationField;
        private string dpiField;
        private string indicatordisplayField;
        private string orientationField;
        private string srcField;
        private string typeField;


        [System.Xml.Serialization.XmlAttributeAttribute("app-control-operation")]
        public string appcontroloperation
        {
            get
            {
                return this.appcontroloperationField;
            }

            set
            {
                this.appcontroloperationField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("dpi")]
        public string dpi
        {
            get
            {
                return this.dpiField;
            }

            set
            {
                this.dpiField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("indicator-display")]
        public string indicatordisplay
        {
            get
            {
                return this.indicatordisplayField;
            }

            set
            {
                this.indicatordisplayField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("orientation")]
        public string orientation
        {
            get
            {
                return this.orientationField;
            }

            set
            {
                this.orientationField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("src")]
        public string src
        {
            get
            {
                return this.srcField;
            }

            set
            {
                this.srcField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("type")]
        public string type
        {
            get
            {
                return this.typeField;
            }

            set
            {
                this.typeField = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }



    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class account : object, System.ComponentModel.INotifyPropertyChanged
    {
        private accountprovider accountproviderField = new accountprovider();

        [System.Xml.Serialization.XmlElementAttribute("account-provider")]
        public accountprovider accountprovider
        {
            get
            {
                return this.accountproviderField;
            }

            set
            {
                this.accountproviderField = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class accountprovider : object, System.ComponentModel.INotifyPropertyChanged
    {
        private string appIdField;
        private string multipleaccountsField;
        private string provideridField;
        private string iconnField;
        private string iconnsmallField;
        private icon iconField = new icon();
        private icon iconsmallField = new icon();
        private List<icon> iconListField = new List<icon>();
        private List<label> itemsField = new List<label>();
        private List<string> capabilityField = new List<string>();

        [System.Xml.Serialization.XmlAttributeAttribute("appid")]
        public string appid
        {
            get
            {
                return this.appIdField;
            }

            set
            {
                this.appIdField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("multiple-accounts-support")]
        public string multipleaccounts
        {
            get
            {
                return this.multipleaccountsField;
            }

            set
            {
                this.multipleaccountsField = value;
            }
        }

        [System.Xml.Serialization.XmlAttributeAttribute("providerid")]
        public string providerid
        {
            get
            {
                return this.provideridField;
            }

            set
            {
                this.provideridField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnore()]
        public string icon
        {
            get
            {
                if (this.iconListField[0].section == "account")
                    return this.iconListField[0].Text[0];
                else if (this.iconListField[1].section == "account")
                    return this.iconListField[1].Text[0];
                else
                    return string.Empty;
            }

            set
            {
                this.iconnField = value;
            }
        }

        [System.Xml.Serialization.XmlIgnore()]
        public string iconsmall
        {
            get
            {
                if (this.iconListField[1].section == "account-small")
                    return this.iconListField[1].Text[0];
                else if (this.iconListField[0].section == "account-small")
                    return this.iconListField[0].Text[0];
                else
                    return string.Empty;
            }

            set
            {
                this.iconnsmallField = value;
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("icon", typeof(icon))]
        public List<icon> iconlist
        {
            get
            {
                return this.iconListField;
            }

            set
            {
                this.iconListField = value;
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("label", typeof(label))]
        public List<label> Items
        {
            get
            {
                return this.itemsField;
            }

            set
            {
                this.itemsField = value;
            }
        }

        [System.Xml.Serialization.XmlElementAttribute("capability")]
        public List<string> capability
        {
            get
            {
                return this.capabilityField;
            }

            set
            {
                this.capabilityField = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tizen.org/ns/packages")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tizen.org/ns/packages", IsNullable = false)]
    public partial class permission : object, System.ComponentModel.INotifyPropertyChanged
    {
        private string typeField;

        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string type
        {
            get
            {
                return this.typeField;
            }

            set
            {
                this.typeField = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { }
            remove { }
        }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.0.30319.34283")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://tizen.org/ns/packages", IncludeInSchema = false)]
    public enum ItemsChoiceType
    {

        account,

        author,

        compatibility,

        dynamicbox,

        font,

        icon,

        ime,

        [System.Xml.Serialization.XmlEnumAttribute("ime-application")]
        imeapplication,

        label,

        livebox,

        notifications,

        profile,

        [System.Xml.Serialization.XmlEnumAttribute("shortcut-list")]
        shortcutlist,

        [System.Xml.Serialization.XmlEnumAttribute("ui-application")]
        uiapplication,

        [System.Xml.Serialization.XmlEnumAttribute("service-application")]
        serviceapplication,

        [System.Xml.Serialization.XmlEnumAttribute("widget-application")]
        widgetapplication,

        [System.Xml.Serialization.XmlEnumAttribute("watch-application")]
        watchapplication,

        description,

        feature,

        privileges,
    }
}
