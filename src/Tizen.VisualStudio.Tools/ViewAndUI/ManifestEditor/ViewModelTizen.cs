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
using System.Linq;
using System.ComponentModel;
using Microsoft.VisualStudio.XmlEditor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Package;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Reflection;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Tizen.VisualStudio.ManifestEditor
{
    public class ResourceInfo
    {
        public static string ErrorMessageBoxTitle = "VsTemplateDesigner";
        public static string FieldNameDescription = "Description";
        public static string FieldNameId = "ID";
        public static string InvalidVsTemplateData
            = "The vstemplate file you are attempting to load is missing TemplateData";
        public static string SynchronizeBuffer = "Synchronize XML file with view";
        public static string ReformatBuffer = "Reformat";
        public static string ValidationFieldMaxLength
            = "{0} must be {1} characters or less.";
        public static string ValidationRequiredField
            = "{0} is a required value.";
    }

    public class ViewModelTizen : IViewModelTizen, IDataErrorInfo, INotifyPropertyChanged
    {
        const int MaxIdLength = 100;
        const int MaxProductNameLength = 60;
        const int MaxDescriptionLength = 1024;
        static List<string> VersionList = new List<string>();

        XmlModel _xmlModel;
        XmlStore _xmlStore;
        manifest _tizenManifestModel;

        IServiceProvider _serviceProvider;
        IVsTextLines _buffer;

        bool _synchronizing;
        long _dirtyTime;
        EventHandler<XmlEditingScopeEventArgs> _editingScopeCompletedHandler;
        EventHandler<XmlEditingScopeEventArgs> _undoRedoCompletedHandler;
        EventHandler _bufferReloadedHandler;

        LanguageService _xmlLanguageService;
        string applicationApiVersionValue;

        public event EventHandler ViewModelChanged;

        public ViewModelTizen(XmlStore xmlStore, XmlModel xmlModel, IServiceProvider provider, IVsTextLines buffer)
        {
            BufferDirty = false;
            DesignerDirty = false;

            _serviceProvider = provider ?? throw new ArgumentNullException("provider");
            _buffer = buffer ?? throw new ArgumentNullException("buffer");

            _xmlStore = xmlStore ?? throw new ArgumentNullException("xmlStore");
            // OnUnderlyingEditCompleted
            _editingScopeCompletedHandler = new EventHandler<XmlEditingScopeEventArgs>(OnUnderlyingEditCompleted);
            _xmlStore.EditingScopeCompleted += _editingScopeCompletedHandler;
            // OnUndoRedoCompleted
            _undoRedoCompletedHandler = new EventHandler<XmlEditingScopeEventArgs>(OnUndoRedoCompleted);
            _xmlStore.UndoRedoCompleted += _undoRedoCompletedHandler;

            _xmlModel = xmlModel ?? throw new ArgumentNullException("xmlModel");
            // BufferReloaded
            _bufferReloadedHandler += new EventHandler(BufferReloaded);
            _xmlModel.BufferReloaded += _bufferReloadedHandler;

            LoadModelFromXmlModel();
            //It needs to load from api version meta file.
            if (!VersionList.Contains("4"))
            {
                VersionList.Add("4");
            }

            if (!VersionList.Contains("5"))
            {
                VersionList.Add("5");
            }

            if (!VersionList.Contains("5.5"))
            {
                VersionList.Add("5.5");
            }

            if (!VersionList.Contains("6"))
            {
                VersionList.Add("6");
            }

            if (!VersionList.Contains("6.5"))
            {
                VersionList.Add("6.5");
            }
	    
            if (!VersionList.Contains("7.0"))
            {
                VersionList.Add("7.0");
            }

        }

        public void Close()
        {
            //Unhook the events from the underlying XmlStore/XmlModel
            if (_xmlStore != null)
            {
                _xmlStore.EditingScopeCompleted -= _editingScopeCompletedHandler;
                _xmlStore.UndoRedoCompleted -= _undoRedoCompletedHandler;
            }

            if (_xmlModel != null)
            {
                _xmlModel.BufferReloaded -= _bufferReloadedHandler;
            }
        }

        bool BufferDirty { get; set; }

        public bool DesignerDirty { get; set; }

        bool IsXmlEditorParsing
        {
            get
            {
                LanguageService langsvc = GetXmlLanguageService();
                return langsvc != null ? langsvc.IsParsing : false;
            }
        }

        public void DoIdle()
        {
            if (BufferDirty || DesignerDirty)
            {
                int delay = 100;

                if ((Environment.TickCount - _dirtyTime) > delay)
                {
                    // Must not try and sync while XML editor is parsing otherwise we just confuse matters.
                    if (IsXmlEditorParsing)
                    {
                        _dirtyTime = Environment.TickCount;
                        return;
                    }

                    //If there is contention, give the preference to the designer.
                    if (DesignerDirty)
                    {
                        SaveModelToXmlModel(ResourceInfo.SynchronizeBuffer);
                        //We don't do any merging, so just overwrite whatever was in the buffer.
                        BufferDirty = false;
                    }
                    else if (BufferDirty)
                    {
                        LoadModelFromXmlModel();
                    }
                }
            }
        }

        private void LoadModelFromXmlModel()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(manifest));

                using (XmlReader reader = GetParseTree().CreateReader())
                {
                    _tizenManifestModel = (manifest)serializer.Deserialize(reader);
                }

                if (_tizenManifestModel == null)
                {
                    throw new Exception(ResourceInfo.InvalidVsTemplateData);
                }
            }
            catch (Exception e)
            {
                //Display error message
                ErrorHandler.ThrowOnFailure(
                    VsShellUtilities.ShowMessageBox(_serviceProvider,
                        ResourceInfo.InvalidVsTemplateData + e.Message,
                        ResourceInfo.ErrorMessageBoxTitle,
                        OLEMSGICON.OLEMSGICON_CRITICAL,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST));
            }

            BufferDirty = false;

            if (ViewModelChanged != null)
            {
                // Update the Designer View
                ViewModelChanged(this, new EventArgs());
            }
        }


        XDocument GetParseTree()
        {
            LanguageService langsvc = GetXmlLanguageService();

            // don't crash if the language service is not available
            if (langsvc != null)
            {
                Source src = langsvc.GetSource(_buffer);

                // We need to access this method to get the most up to date parse tree.
                // public virtual XmlDocument GetParseTree(Source source, IVsTextView view, int line, int col, ParseReason reason) {
                MethodInfo mi = langsvc.GetType().GetMethod("GetParseTree");
                int line = 0, col = 0;

                mi?.Invoke(langsvc, new object[] { src, null, line, col, ParseReason.Check });
            }
            // Now the XmlDocument should be up to date also.
            return _xmlModel.Document;
        }

        LanguageService GetXmlLanguageService()
        {
            if (_xmlLanguageService == null)
            {
                IOleServiceProvider vssp = _serviceProvider.GetService(typeof(IOleServiceProvider)) as IOleServiceProvider;
                Guid xmlEditorGuid = new Guid("f6819a78-a205-47b5-be1c-675b3c7f0b8e");
                Guid iunknown = new Guid("00000000-0000-0000-C000-000000000046");

                if (vssp != null)
                {
                    if (ErrorHandler.Succeeded(vssp.QueryService(ref xmlEditorGuid, ref iunknown, out IntPtr ptr)))
                    {
                        try
                        {
                            _xmlLanguageService = Marshal.GetObjectForIUnknown(ptr) as LanguageService;
                        }
                        finally
                        {
                            Marshal.Release(ptr);
                        }
                    }
                }
            }

            return _xmlLanguageService;
        }

        void FormatBuffer(Source src)
        {
            using (EditArray edits = new EditArray(src, null, false, ResourceInfo.ReformatBuffer))
            {
                TextSpan span = src.GetDocumentSpan();
                src.ReformatSpan(edits, span);
            }
        }

        Source Source
        {
            get
            {
                LanguageService langsvc = GetXmlLanguageService();
                if (langsvc == null)
                {
                    return null;
                }

                return langsvc.GetSource(_buffer);
            }
        }

        void SaveModelToXmlModel(string undoEntry)
        {
            LanguageService langsvc = GetXmlLanguageService();

            try
            {
                //We can't edit this file (perhaps the user cancelled a SCC prompt, etc...)
                if (!CanEditFile())
                {
                    DesignerDirty = false;
                    BufferDirty = true;
                    throw new Exception();
                }

                //PopulateModelFromReferencesBindingList();
                //PopulateModelFromContentBindingList();

                XmlSerializer serializer = new XmlSerializer(typeof(manifest));
                var xmlnsEmpty = new XmlSerializerNamespaces();
                xmlnsEmpty.Add("", "http://tizen.org/ns/packages");
                XDocument documentFromDesignerState = new XDocument();
                using (XmlWriter w = documentFromDesignerState.CreateWriter())
                {
                    serializer.Serialize(w, _tizenManifestModel, xmlnsEmpty);
                }

                _synchronizing = true;
                XDocument document = GetParseTree();
                Source src = Source;
                if (src == null || langsvc == null)
                {
                    return;
                }

                langsvc.IsParsing = true; // lock out the background parse thread.

                // Wrap the buffer sync and the formatting in one undo unit.
                using (CompoundAction ca = new CompoundAction(src, ResourceInfo.SynchronizeBuffer))
                {
                    using (XmlEditingScope scope = _xmlStore.BeginEditingScope(ResourceInfo.SynchronizeBuffer, this))
                    {
                        //Replace the existing XDocument with the new one we just generated.
                        document.Root.ReplaceWith(documentFromDesignerState.Root);
                        scope.Complete();
                    }

                    ca.FlushEditActions();
                    FormatBuffer(src);
                }

                DesignerDirty = false;
            }
            catch (Exception)
            {
                // if the synchronization fails then we'll just try again in a second.
                _dirtyTime = Environment.TickCount;
            }
            finally
            {
                if (langsvc != null)
                {
                    langsvc.IsParsing = false;
                }

                _synchronizing = false;
            }
        }

        private void BufferReloaded(object sender, EventArgs e)
        {
            if (!_synchronizing)
            {
                BufferDirty = true;
                _dirtyTime = Environment.TickCount;
            }
        }

        private void OnUndoRedoCompleted(object sender, XmlEditingScopeEventArgs e)
        {
            if (!_synchronizing)
            {
                BufferDirty = true;
                _dirtyTime = Environment.TickCount;
            }
        }

        private void OnUnderlyingEditCompleted(object sender, XmlEditingScopeEventArgs e)
        {
            if (e.EditingScope.UserState != this && !_synchronizing)
            {
                BufferDirty = true;
                _dirtyTime = Environment.TickCount;
            }
        }

        #region Soruce Contorl
        bool? _canEditFile;
        bool _gettingCheckoutStatus;
        private bool CanEditFile()
        {
            // Cache the value so we don't keep asking the user over and over.
            if (_canEditFile.HasValue)
            {
                return (bool)_canEditFile;
            }

            // Check the status of the recursion guard
            if (_gettingCheckoutStatus)
            {
                return false;
            }

            _canEditFile = false; // assume the worst
            try
            {
                // Set the recursion guard
                _gettingCheckoutStatus = true;

                // Get the QueryEditQuerySave service
                IVsQueryEditQuerySave2 queryEditQuerySave
                    = _serviceProvider.GetService(typeof(SVsQueryEditQuerySave))
                      as IVsQueryEditQuerySave2;

                string filename = _xmlModel.Name;

                // Now call the QueryEdit method to find the edit status of this file
                string[] documents = { filename };

                // Note that this function can popup a dialog to ask the user to checkout the file.
                // When this dialog is visible, it is possible to receive other request to change
                // the file and this is the reason for the recursion guard
                if (queryEditQuerySave != null)
                {
                    int hr = queryEditQuerySave.QueryEditFiles(
                    0,              // Flags
                    1,              // Number of elements in the array
                    documents,      // Files to edit
                    null,           // Input flags
                    null,           // Input array of VSQEQS_FILE_ATTRIBUTE_DATA
                    out uint result,     // result of the checkout
                    out uint outFlags);  // Additional flags
                    if (ErrorHandler.Succeeded(hr) && (result == (uint)tagVSQueryEditResult.QER_EditOK))
                    {
                        // In this case (and only in this case) we can return true from this function
                        _canEditFile = true;
                    }
                }
            }
            finally
            {
                _gettingCheckoutStatus = false;
            }

            return (bool)_canEditFile;
        }
        #endregion

        #region IViewModel Methods

        private bool visibilityvalue;
        public bool UIvisibility
        {
            get
            {
                return visibilityvalue;
            }

            set
            {
                visibilityvalue = value;
                NotifyPropertyChanged();
            }
        }

        private bool servicevisibilityvalue;
        public bool Servicevisibility
        {
            get
            {
                return servicevisibilityvalue;
            }

            set
            {
                servicevisibilityvalue = value;
                NotifyPropertyChanged();
            }
        }

        private bool servicevisibilityvalueRev;
        public bool ServicevisibilityRev
        {
            get
            {
                return servicevisibilityvalueRev;
            }

            set
            {
                servicevisibilityvalueRev = value;
                NotifyPropertyChanged();
            }
        }

        private bool widgetvisibilityvalue;
        public bool Widgetvisibility
        {
            get
            {
                return widgetvisibilityvalue;
            }

            set
            {
                widgetvisibilityvalue = value;
                NotifyPropertyChanged();
            }
        }

        private bool watchvisibilityvalue;
        public bool Watchvisibility
        {
            get
            {
                return watchvisibilityvalue;
            }

            set
            {
                watchvisibilityvalue = value;
                NotifyPropertyChanged();
            }
        }

        private bool uiandservicevisibilityvalue;
        public bool UiandServicevisibility
        {
            get
            {
                return uiandservicevisibilityvalue;
            }

            set
            {
                uiandservicevisibilityvalue = value;
                NotifyPropertyChanged();
            }
        }

        public ItemsChoiceType AppType
        {
            get
            {
                return _tizenManifestModel.Apptype;
            }

            set
            {
                _tizenManifestModel.Apptype = value;
                if (value == ItemsChoiceType.serviceapplication)
                {
                    UIvisibility = false;
                    Servicevisibility = true;
                    ServicevisibilityRev = false;
                    Widgetvisibility = false;
                    Watchvisibility = false;
                    UiandServicevisibility = true;
                }
                else if (value == ItemsChoiceType.uiapplication)
                {
                    UIvisibility = true;
                    Servicevisibility = false;
                    ServicevisibilityRev = true;
                    Widgetvisibility = false;
                    Watchvisibility = false;
                    UiandServicevisibility = true;
                }
                else if (value == ItemsChoiceType.widgetapplication)
                {
                    UIvisibility = false;
                    Servicevisibility = false;
                    ServicevisibilityRev = true;
                    Widgetvisibility = true;
                    Watchvisibility = false;
                    UiandServicevisibility = false;
                }
                else if (value == ItemsChoiceType.watchapplication)
                {
                    UIvisibility = false;
                    Servicevisibility = false;
                    ServicevisibilityRev = true;
                    Widgetvisibility = false;
                    Watchvisibility = true;
                    UiandServicevisibility = false;
                }
            }
        }

        public List<string> ApiVersionList
        {
            get
            {
                return VersionList;
            }
        }

        public string ApiVersion
        {
            get
            {
                return _tizenManifestModel.apiversion;
            }

            set
            {
                if (_tizenManifestModel.apiversion != value)
                {
                    _tizenManifestModel.apiversion = value;
                    float val = -1;
                    float.TryParse(value, out val);

                    if (val != -1 && val >= 5.5)
                    {
                        if (_tizenManifestModel.applicationField != null)
                            _tizenManifestModel.applicationField.apiversion = applicationApiVersionValue;
                    } else
                    {
                        if (_tizenManifestModel.applicationField != null)
                        {
                            applicationApiVersionValue = _tizenManifestModel.applicationField.apiversion;
                            _tizenManifestModel.applicationField.apiversion = null;
                        }
                    }
                
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool ApiVersionGreaterThanFive
        {
            get
            {
                float val = -1;
                float.TryParse(ApiVersion, out val);
                return (val != -1 && val >= 5.5);
            }

            set
            {
            }
        }

        public string ApplicationID
        {
            get
            {
                var uiAppObject = _tizenManifestModel.applicationField;
                if (uiAppObject == null)
                {
                    return string.Empty;
                }
                else
                {
                    return uiAppObject.appid;
                }
            }

            set
            {
                var uiAppObject = _tizenManifestModel.applicationField;
                if (uiAppObject == null)
                {
                    _tizenManifestModel.applicationField = new uiapplication();
                }

                if (_tizenManifestModel.applicationField.appid != value)
                {
                    _tizenManifestModel.applicationField.appid = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }

            }
        }

        public string UpdatePeriod
        {
            get
            {
                var uiAppObject = _tizenManifestModel.applicationField;
                if (uiAppObject == null)
                {
                    return string.Empty;
                }
                else
                {
                    return uiAppObject.updateperiod;
                }
            }

            set
            {
                var uiAppObject = _tizenManifestModel.applicationField;
                if (uiAppObject == null)
                {
                    _tizenManifestModel.applicationField = new uiapplication();
                }

                if (_tizenManifestModel.applicationField.updateperiod != value)
                {
                    _tizenManifestModel.applicationField.updateperiod = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }

            }
        }

        public profile Profile
        {
            get
            {
                return _tizenManifestModel.profileType;
            }

            set
            {
                if (_tizenManifestModel.profileType.name != value.name)
                {
                    _tizenManifestModel.profileType = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Author
        {
            get
            {
                if (_tizenManifestModel.authorField != null)
                {
                    if (_tizenManifestModel.authorField.Text == null)
                    {
                        return string.Empty;
                    }
                    else
                    {
                        return string.Join("\n", _tizenManifestModel.authorField.Text);
                    }
                }
                else
                {
                    return string.Empty;
                }
            }

            set
            {
                if (_tizenManifestModel.authorField == null || _tizenManifestModel.authorField.Text == null)
                {
                    string email = null;
                    string href = null;

                    if (_tizenManifestModel.authorField != null)
                    {
                        if (_tizenManifestModel.authorField.email != null)
                        {
                            email = _tizenManifestModel.authorField.email;
                        }

                        if (_tizenManifestModel.authorField.href != null && _tizenManifestModel.authorField != null)
                        {
                            href = _tizenManifestModel.authorField.href;
                        }
                    }

                    _tizenManifestModel.authorField = new author() { email = email, href = href };
                }

                if (_tizenManifestModel.authorField.Text != value)
                {
                    _tizenManifestModel.authorField.Text = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Description
        {
            get
            {
                if (_tizenManifestModel.descriptionField == null)
                {
                    return string.Empty;
                }
                else
                {
                    return _tizenManifestModel.descriptionField.Text;
                }
            }

            set
            {
                if (_tizenManifestModel.descriptionField == null)
                {
                    _tizenManifestModel.descriptionField = new description();
                }

                else if (_tizenManifestModel.descriptionField.Text != value)
                {
                    _tizenManifestModel.descriptionField.Text = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Email
        {
            get
            {
                if (_tizenManifestModel.authorField != null)
                {
                    return _tizenManifestModel.authorField.email;
                }
                else
                {
                    return string.Empty;
                }
            }

            set
            {
                if (_tizenManifestModel.authorField == null)
                {
                    _tizenManifestModel.authorField = new author();
                }

                if (_tizenManifestModel.authorField.email != value)
                {
                    _tizenManifestModel.authorField.email = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Exec
        {
            get
            {
                var uiAppObject = _tizenManifestModel.applicationField;
                if (uiAppObject == null)
                {
                    return string.Empty;
                }
                else
                {
                    return uiAppObject.exec;
                }
            }

            set
            {
                var uiAppObject = _tizenManifestModel.applicationField;
                if (uiAppObject == null)
                {
                    _tizenManifestModel.applicationField = new uiapplication();
                }

                if (_tizenManifestModel.applicationField.exec != value)
                {
                    _tizenManifestModel.applicationField.exec = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Icon
        {
            get
            {
                if (_tizenManifestModel.iconField == null)
                {
                    return string.Empty;
                }
                else
                {
                    try
                    {
                        return string.Join("\n", _tizenManifestModel.iconField.Text);
                    }
                    catch
                    {
                    }

                    return string.Empty;
                }
            }

            set
            {
                if (_tizenManifestModel.iconField == null || _tizenManifestModel.iconField.Text == null)
                {
                    _tizenManifestModel.iconField = new icon();
                }

                if (_tizenManifestModel.iconField != null)
                {
                    if (_tizenManifestModel.iconField.Text[0] != value)
                    {
                        _tizenManifestModel.iconField.Text = value.Split('\n');
                        DesignerDirty = true;
                        NotifyPropertyChanged();
                    }
                }
            }
        }

        public string Label
        {
            get
            {
                if (_tizenManifestModel.labelField == null)
                {
                    return string.Empty;
                }

                try
                {
                    return string.Join("\n", _tizenManifestModel.labelField.Text);
                }
                catch
                {
                }

                return string.Empty;
            }

            set
            {
                if (_tizenManifestModel.labelField == null || _tizenManifestModel.labelField.Text == null)
                {
                    _tizenManifestModel.labelField = new label();
                }

                if (_tizenManifestModel.labelField != null)
                {
                    if (_tizenManifestModel.labelField.Text[0] != value)
                    {
                        _tizenManifestModel.labelField.Text = value.Split('\n');
                        DesignerDirty = true;
                        NotifyPropertyChanged();
                    }
                }
            }
        }

        public string Package
        {
            get
            {
                return _tizenManifestModel.package;
            }

            set
            {
                if (_tizenManifestModel.package != value)
                {
                    _tizenManifestModel.package = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Version
        {
            get
            {
                return _tizenManifestModel.version;
            }

            set
            {
                if (_tizenManifestModel.version != value)
                {
                    _tizenManifestModel.version = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Website
        {
            get
            {
                if (_tizenManifestModel.authorField != null)
                {
                    return _tizenManifestModel.authorField.href;
                }
                else
                {
                    return string.Empty;
                }
            }

            set
            {
                if (_tizenManifestModel.authorField == null)
                {
                    _tizenManifestModel.authorField = new author();
                }

                if (_tizenManifestModel.authorField.href != value)
                {
                    _tizenManifestModel.authorField.href = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
            }
        }

        public List<label> LocalizationLabels
        {
            get
            {
                return _tizenManifestModel.LocalizationLabelList;
            }

            set
            {
                if (_tizenManifestModel.LocalizationLabelList != value)
                {
                    _tizenManifestModel.LocalizationLabelList = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
            }
        }

        public List<icon> LocalizationIcons
        {
            get
            {
                return _tizenManifestModel.LocalizationIconList;
            }

            set
            {
                if (_tizenManifestModel.LocalizationIconList != value)
                {
                    _tizenManifestModel.LocalizationIconList = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
            }
        }

        public List<description> LocalizationDescriptions
        {
            get
            {
                return _tizenManifestModel.LocalizationDescriptionList;
            }

            set
            {
                if (_tizenManifestModel.LocalizationDescriptionList != value)
                {
                    _tizenManifestModel.LocalizationDescriptionList = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion

        #region IDataErrorInfo
        public string Error
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public List<feature> FeatureField
        {
            get
            {
                return _tizenManifestModel.featureList;
            }

            set
            {
                if (_tizenManifestModel.featureList != value)
                {
                    _tizenManifestModel.featureList = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
            }
        }

        public privileges PrivilegeList
        {
            get
            {
                if (_tizenManifestModel.privileges == null)
                {
                    return new privileges();
                }

                return _tizenManifestModel.privileges;
            }

            set
            {
                if (_tizenManifestModel.privileges != value)
                {
                    _tizenManifestModel.privileges = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
            }
        }

        public List<appdefprivilege> ConsumerappdefprivilegeList
        {
            get
            {
                if (_tizenManifestModel.privileges == null)
                {
                    _tizenManifestModel.privileges = new privileges();
                }

                if (_tizenManifestModel.privileges.consumerPrivList == null)
                {
                    _tizenManifestModel.privileges.consumerPrivList = new List<appdefprivilege>();
                }

                var returnList = new List<appdefprivilege>();
                foreach (var item in _tizenManifestModel.privileges.consumerPrivList)
                {
                    if (item is appdefprivilege)
                    {
                        returnList.Add(item as appdefprivilege);
                    }
                }

                return returnList;
            }

            set
            {
                if (_tizenManifestModel.privileges.consumerPrivList != value)
                {
                    _tizenManifestModel.privileges.consumerPrivList = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
            }
        }

        public List<string> DefaultprivilegeList
        {
            get
            {
                if (_tizenManifestModel.privileges == null)
                {
                    _tizenManifestModel.privileges = new privileges();
                }

                if (_tizenManifestModel.privileges.platformPrivList == null)
                {
                    _tizenManifestModel.privileges.platformPrivList = new List<string>();
                }

                var returnList = new List<string>();
                foreach (var item in _tizenManifestModel.privileges.platformPrivList)
                {
                    if (item is string)
                    {
                        returnList.Add(item as string);
                    }
                }

                return returnList;

            }

            set
            {
                if (_tizenManifestModel.privileges.platformPrivList != value)
                {
                    _tizenManifestModel.privileges.platformPrivList = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
            }
        }

        public List<metadata> AdvanceMetaList
        {
            get
            {
                if (_tizenManifestModel.AdvanceMetadataList == null)
                {
                    return new List<metadata>();
                }

                return _tizenManifestModel.AdvanceMetadataList;
            }

            set
            {
                if (_tizenManifestModel.AdvanceMetadataList != value)
                {
                    _tizenManifestModel.AdvanceMetadataList = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
            }
        }

        public List<packages> AdvancePkgList
        {
            get
            {
                if (_tizenManifestModel.dependencies == null)
                {
                    _tizenManifestModel.dependencies = new dependencies();
                }

                if (_tizenManifestModel.dependencies.dependencyList == null)
                {
                    _tizenManifestModel.dependencies.dependencyList = new List<packages>();
                }

                var returnList = new List<packages>();
                foreach (var item in _tizenManifestModel.dependencies.dependencyList)
                {
                    if (item is packages)
                    {
                        returnList.Add(item as packages);
                    }
                }

                return returnList;

            }

            set
            {
                if (_tizenManifestModel.dependencies.dependencyList != value)
                {
                    _tizenManifestModel.dependencies.dependencyList = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
            }
        }

        public List<datacontrol> AdvanceDataControlList
        {
            get
            {
                if (_tizenManifestModel.AdvanceDataControlList == null)
                {
                    return new List<datacontrol>();
                }

                return _tizenManifestModel.AdvanceDataControlList;
            }

            set
            {
                if (_tizenManifestModel.AdvanceDataControlList != value)
                {
                    _tizenManifestModel.AdvanceDataControlList = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
            }
        }

        public ManageTaskType TaskManage
        {
            get
            {
                var uiAppObject = _tizenManifestModel.applicationField;
                if (uiAppObject == null)
                {
                    return ManageTaskType.True;
                }
                else
                {
                    return uiAppObject.taskmanage;
                }
            }

            set
            {
                var uiAppObject = _tizenManifestModel.applicationField;
                if (uiAppObject == null)
                {
                    _tizenManifestModel.applicationField = new uiapplication() { taskmanage = ManageTaskType.True };
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
                else
                {
                    if (_tizenManifestModel.applicationField.taskmanage != value)
                    {
                        _tizenManifestModel.applicationField.taskmanage = value;
                        if (value == ManageTaskType.None)
                        {
                            _tizenManifestModel.applicationField.taskmanageSpecified = false;
                        }
                        else
                        {
                            _tizenManifestModel.applicationField.taskmanageSpecified = true;
                        }

                        DesignerDirty = true;
                        NotifyPropertyChanged();
                    }
                }
            }
        }

        public NoDisplayType NoDisplay
        {
            get
            {
                var uiAppObject = _tizenManifestModel.applicationField;
                if (uiAppObject == null)
                {
                    return NoDisplayType.False;
                }
                else
                {
                    return uiAppObject.nodisplay;
                }
            }

            set
            {
                var uiAppObject = _tizenManifestModel.applicationField;
                if (uiAppObject == null)
                {
                    _tizenManifestModel.applicationField = new uiapplication() { nodisplay = NoDisplayType.False };
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
                else
                {
                    if (_tizenManifestModel.applicationField.nodisplay != value)
                    {
                        _tizenManifestModel.applicationField.nodisplay = value;
                        if (value == NoDisplayType.None)
                        {
                            _tizenManifestModel.applicationField.nodisplaySpecified = false;
                        }
                        else
                        {
                            _tizenManifestModel.applicationField.nodisplaySpecified = true;
                        }

                        DesignerDirty = true;
                        NotifyPropertyChanged();
                    }
                }
            }
        }

        public AutorestartType Autorestart
        {
            get
            {
                var uiAppObject = _tizenManifestModel.applicationField;
                if (uiAppObject == null)
                {
                    return AutorestartType.False;
                }
                else
                {
                    return uiAppObject.autorestart;
                }
            }

            set
            {
                var uiAppObject = _tizenManifestModel.applicationField;
                if (uiAppObject == null)
                {
                    _tizenManifestModel.applicationField = new uiapplication() { autorestart = AutorestartType.False };
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
                else
                {
                    if (_tizenManifestModel.applicationField.autorestart != value)
                    {
                        _tizenManifestModel.applicationField.autorestart = value;
                        if (value == AutorestartType.None)
                        {
                            _tizenManifestModel.applicationField.autorestartSpecified = false;
                        }
                        else
                        {
                            _tizenManifestModel.applicationField.autorestartSpecified = true;
                        }

                        DesignerDirty = true;
                        NotifyPropertyChanged();
                    }
                }
            }
        }

        public OnbootType Onboot
        {
            get
            {
                var uiAppObject = _tizenManifestModel.applicationField;
                if (uiAppObject == null)
                {
                    return OnbootType.False;
                }
                else
                {
                    return uiAppObject.onboot;
                }
            }

            set
            {
                var uiAppObject = _tizenManifestModel.applicationField;
                if (uiAppObject == null)
                {
                    _tizenManifestModel.applicationField = new uiapplication() { onboot = OnbootType.False };
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
                else
                {
                    if (_tizenManifestModel.applicationField.onboot != value)
                    {
                        _tizenManifestModel.applicationField.onboot = value;
                        if (value == OnbootType.None)
                        {
                            _tizenManifestModel.applicationField.onbootSpecified = false;
                        }
                        else
                        {
                            _tizenManifestModel.applicationField.onbootSpecified = true;
                        }

                        DesignerDirty = true;
                        NotifyPropertyChanged();
                    }
                }
            }
        }

        public NewHWaccelerationType HWAcceleration
        {
            get
            {
                var uiAppObject = _tizenManifestModel.applicationField;
                if (uiAppObject == null)
                {
                    return NewHWaccelerationType.None;
                }
                else
                {
                    return uiAppObject.hwacceleration;
                }
            }

            set
            {
                var uiAppObject = _tizenManifestModel.applicationField;
                if (uiAppObject == null)
                {
                    _tizenManifestModel.applicationField = new uiapplication() { hwacceleration = NewHWaccelerationType.None };
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
                else
                {
                    if (_tizenManifestModel.applicationField.hwacceleration != value)
                    {
                        _tizenManifestModel.applicationField.hwacceleration = value;
                        if (value == NewHWaccelerationType.None)
                        {
                            _tizenManifestModel.applicationField.hwaccelerationSpecified = false;
                        }
                        else
                        {
                            _tizenManifestModel.applicationField.hwaccelerationSpecified = true;
                        }

                        DesignerDirty = true;
                        NotifyPropertyChanged();
                    }
                }
            }
        }

        public AmbientType AmbientSupport
        {
            get
            {
                var uiAppObject = _tizenManifestModel.applicationField;
                if (uiAppObject == null)
                {
                    return AmbientType.None;
                }
                else
                {
                    return uiAppObject.ambientsupport;
                }
            }

            set
            {
                var uiAppObject = _tizenManifestModel.applicationField;
                if (uiAppObject == null)
                {
                    _tizenManifestModel.applicationField = new uiapplication() { ambientsupport = AmbientType.None };
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
                else
                {
                    if (_tizenManifestModel.applicationField.ambientsupport != value)
                    {
                        _tizenManifestModel.applicationField.ambientsupport = value;
                        if (value == AmbientType.None)
                        {
                            _tizenManifestModel.applicationField.ambientsupportSpecified = false;
                        }
                        else
                        {
                            _tizenManifestModel.applicationField.ambientsupportSpecified = true;
                        }

                        DesignerDirty = true;
                        NotifyPropertyChanged();
                    }
                }
            }
        }

        public NewDisplaySplashType NewDisplaySplash
        {
            get
            {
                var uiAppObject = _tizenManifestModel.applicationField;
                if (uiAppObject == null)
                {
                    return NewDisplaySplashType.None;
                }
                else
                {
                    return uiAppObject.newdisplaysplash;
                }
            }

            set
            {
                var uiAppObject = _tizenManifestModel.applicationField;
                if (uiAppObject == null)
                {
                    _tizenManifestModel.applicationField = new uiapplication() { newdisplaysplash = NewDisplaySplashType.None };
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
                else
                {
                    if (_tizenManifestModel.applicationField.newdisplaysplash != value)
                    {
                        _tizenManifestModel.applicationField.newdisplaysplash = value;
                        if (value == NewDisplaySplashType.None)
                        {
                            _tizenManifestModel.applicationField.newdisplaysplashSpecified = false;
                        }
                        else
                        {
                            _tizenManifestModel.applicationField.newdisplaysplashSpecified = true;
                        }

                        DesignerDirty = true;
                        NotifyPropertyChanged();
                    }
                }
            }
        }

        public LaunchType LaunchMode
        {
            get
            {
                var uiAppObject = _tizenManifestModel.applicationField;
                if (uiAppObject == null)
                {
                    return LaunchType.None;
                }
                else
                {
                    return uiAppObject.launch_mode;
                }
            }

            set
            {
                var uiAppObject = _tizenManifestModel.applicationField;
                if (uiAppObject == null)
                {
                    _tizenManifestModel.applicationField = new uiapplication() { launch_mode = LaunchType.None, launch_modeSpecified = false };
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
                else
                {
                    if (_tizenManifestModel.applicationField.launch_mode != value)
                    {
                        _tizenManifestModel.applicationField.launch_mode = value;
                        if (value == LaunchType.None)
                        {
                            _tizenManifestModel.applicationField.launch_modeSpecified = false;
                        }
                        else
                        {
                            _tizenManifestModel.applicationField.launch_modeSpecified = true;
                        }

                        DesignerDirty = true;
                        NotifyPropertyChanged();
                    }
                }
            }
        }

        public List<appcontrol> AdvanceAppControlList
        {
            get
            {
                if (_tizenManifestModel.AdvanceAppControlList == null)
                {
                    return new List<appcontrol>();
                }

                return _tizenManifestModel.AdvanceAppControlList;
            }

            set
            {
                if (_tizenManifestModel.AdvanceAppControlList != value)
                {
                    _tizenManifestModel.AdvanceAppControlList = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
            }
        }

        public List<string> IntegratedprivilegeList
        {
            get
            {
                List<string> PrivList = new List<string>();
                PrivList.RemoveAll(x => x != null);
                if (PrivilegeList != null)
                {
                    if (PrivilegeList.platformPrivList != null)
                    {
                        foreach (var item in PrivilegeList.platformPrivList)
                        {
                            PrivList.Add(item);
                        }
                    }

                    if (PrivilegeList.consumerPrivList != null)
                    {
                        foreach (var item in PrivilegeList.consumerPrivList)
                        {
                            if (item.License == null)
                            {
                                PrivList.Add("[App-defined-consumer] " + item.Value);
                            }
                            else
                            {
                                PrivList.Add("[App-defined-consumer] " + item.Value + " License: " + item.License);
                            }
                        }
                    }
                }

                if (AppdefprivilegeList != null)
                {
                    foreach (var item in AppdefprivilegeList)
                    {
                        if (item.License == null)
                        {
                            PrivList.Add("[App-defined-provider] " + item.Value);
                        }
                        else
                        {
                            PrivList.Add("[App-defined-provider] " + item.Value + " License: " + item.License);
                        }
                    }
                }

                return PrivList;
            }
        }

        public List<appdefprivilege> AppdefprivilegeList
        {
            get
            {
                if (_tizenManifestModel.appdefprivilegelistField == null)
                {
                    _tizenManifestModel.appdefprivilegelistField = new appdefprivilegelist();
                }

                if (_tizenManifestModel.appdefprivilegelistField.providerPrivList == null)
                {
                    return new List<appdefprivilege>();
                }

                var returnList = new List<appdefprivilege>();
                foreach (var item in _tizenManifestModel.appdefprivilegelistField.providerPrivList)
                {
                    if (item is appdefprivilege)
                    {
                        returnList.Add(item as appdefprivilege);
                    }

                }

                return returnList;
            }

            set
            {
                if (_tizenManifestModel.appdefprivilegelistField.providerPrivList != value)
                {
                    _tizenManifestModel.appdefprivilegelistField.providerPrivList = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
            }
        }

        public List<shortcut> ShortcutList
        {
            get
            {
                if (_tizenManifestModel.shortcutlistField == null)
                {
                    _tizenManifestModel.shortcutlistField = new shortcutlist();
                }

                if (_tizenManifestModel.shortcutlistField.Items == null)
                {
                    return new List<shortcut>();
                }

                var returnList = new List<shortcut>();
                foreach (var item in _tizenManifestModel.shortcutlistField.Items)
                {
                    if (item is shortcut)
                    {
                        returnList.Add(item as shortcut);
                    }
                }

                return returnList;
            }

            set
            {
                if (_tizenManifestModel.shortcutlistField.Items != value)
                {
                    _tizenManifestModel.shortcutlistField.Items = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
            }
        }

        public List<background> BackgroundCategoryList
        {
            get
            {
                if (_tizenManifestModel.applicationField.backgroundcategories == null)
                {
                    return new List<background>();
                }

                return _tizenManifestModel.applicationField.backgroundcategories.ToList();
            }

            set
            {
                if (_tizenManifestModel.applicationField.backgroundcategories != value.ToArray())
                {
                    _tizenManifestModel.applicationField.backgroundcategories = value.ToArray();
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
            }
        }


        public List<account> AccountField
        {
            get
            {
                if (_tizenManifestModel.accountList == null)
                {
                    return new List<account>();
                }

                return _tizenManifestModel.accountList;
            }

            set
            {
                if (_tizenManifestModel.accountList != value)
                {
                    _tizenManifestModel.accountList = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
            }
        }

        public List<splashscreen> SplashscreenList
        {
            get
            {

                if (_tizenManifestModel.applicationField.splashscreens == null)
                {
                    _tizenManifestModel.applicationField.splashscreens = new splash();
                }

                if (_tizenManifestModel.applicationField.splashscreens.splashscreen == null)
                {
                    return new List<splashscreen>();
                }

                var returnList = new List<splashscreen>();
                foreach (var item in _tizenManifestModel.applicationField.splashscreens.splashscreen)
                {
                    if (item is splashscreen)
                    {
                        returnList.Add(item as splashscreen);
                    }
                }
                return returnList;
            }

            set
            {
                if (_tizenManifestModel.applicationField.splashscreens.splashscreen != value)
                {
                    _tizenManifestModel.applicationField.splashscreens.splashscreen = value;
                    DesignerDirty = true;
                    NotifyPropertyChanged();
                }
            }
        }

        public string this[string columnName]
        {
            get
            {
                string error = null;
                switch (columnName)
                {
                    case "ID":
                        error = ValidateId();
                        break;
                    case "Description":
                        error = ValidateDescription();
                        break;
                }

                return error;
            }
        }

        private string ParseDescription(string dirtyDescription)
        {
            string description = dirtyDescription;

            description = description.Replace("\t", string.Empty);
            description = description.Replace("\r", string.Empty);

            if (description.StartsWith("\r"))
            {
                description = description.Remove(0, 1);
            }

            return description;
        }

        private string ValidateId()
        {
            //TODO
            /*
            if (string.IsNullOrEmpty(TemplateID))
            {
                return string.Format(ResourceInfo.ValidationRequiredField, ResourceInfo.FieldNameId);
            }

            if (TemplateID.Length > MaxIdLength)
            {
                return string.Format(ResourceInfo.ValidationFieldMaxLength, ResourceInfo.FieldNameId, MaxIdLength);
            }
            */
            return null;
        }

        private string ValidateDescription()
        {
            //TODO
            /*
            if (string.IsNullOrEmpty(Description))
            {
                return string.Format(ResourceInfo.ValidationRequiredField, ResourceInfo.FieldNameDescription);
            }

            if (Description.Length > MaxDescriptionLength)
            {
                return string.Format(ResourceInfo.ValidationFieldMaxLength, ResourceInfo.FieldNameDescription, MaxDescriptionLength);
            }*/
            return null;
        }
        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
