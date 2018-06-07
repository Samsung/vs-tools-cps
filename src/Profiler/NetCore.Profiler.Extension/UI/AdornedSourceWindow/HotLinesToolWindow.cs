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
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using NetCore.Profiler.Analytics.Model;
using NetCore.Profiler.Extension.Session;
using NetCore.Profiler.Extension.VSPackage;

namespace NetCore.Profiler.Extension.UI.AdornedSourceWindow
{
    [Guid("60f48b34-3b66-4af6-bba1-2014b60c671c")]
    public class HotLinesToolWindow : ToolWindowPane, IOleCommandTarget
    {
        private readonly IVsInvisibleEditorManager _invisibleEditorManager;

        //This adapter allows us to convert between Visual Studio 2010 editor components and
        //the legacy components from Visual Studio 2008 and earlier.
        private readonly IVsEditorAdaptersFactoryService _editorAdapter;

        private IVsCodeWindow _codeWindow;

        private IVsTextView _textView;

        private IWpfTextViewHost _textViewHost;

        private readonly HotLinesToolWindowControl _windowControl;

        private IVsTextLines _docData;

        public HotLinesToolWindow() : base(null)
        {
            this.Caption = "ProjBufferToolWindow";

            var componentModel = (IComponentModel)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SComponentModel));
            _invisibleEditorManager = (IVsInvisibleEditorManager)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SVsInvisibleEditorManager));
            _editorAdapter = componentModel.GetService<IVsEditorAdaptersFactoryService>();

            _windowControl = new HotLinesToolWindowControl();

        }

        public void SetInputSource(string path, ISourceLineStatistics line, ISourceLinesQueryResult lines)
        {
            Caption = $"Hot Lines - {Path.GetFileName(path)}";
            ClearEditor();
            CreateEditor(path);
            _textView.SetCaretPos((int)line.StartLine, (int)line.StartColumn);
            _windowControl.View.Content = _textViewHost;
            new HotLineAdornment(_textViewHost.TextView, line, lines);

        }

        public void Show()
        {
            (Frame as IVsWindowFrame)?.Show();
        }

        protected override void OnClose()
        {
            ClearEditor();
        }

        public override object Content => _windowControl;


        private void CreateEditor(string filePath)
        {
            //IVsInvisibleEditors are in-memory represenations of typical Visual Studio editors.
            //Language services, highlighting and error squiggles are hooked up to these editors
            //for us once we convert them to WpfTextViews. 
            IVsInvisibleEditor invisibleEditor;
            ErrorHandler.ThrowOnFailure(_invisibleEditorManager.RegisterInvisibleEditor(
                filePath
                , pProject: null
                , dwFlags: (uint)_EDITORREGFLAGS.RIEF_ENABLECACHING
                , pFactory: null
                , ppEditor: out invisibleEditor));

            //Then when creating the IVsInvisibleEditor, find and lock the document 
            IntPtr docData;
            IVsHierarchy hierarchy;
            var runningDocTable = (IVsRunningDocumentTable)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SVsRunningDocumentTable));

            uint docCookie;
            ErrorHandler.ThrowOnFailure(runningDocTable.FindAndLockDocument(
                dwRDTLockType: (uint)_VSRDTFLAGS.RDT_ReadLock,
                pszMkDocument: filePath,
                ppHier: out hierarchy,
                pitemid: out uint itemId,
                ppunkDocData: out docData,
                pdwCookie: out docCookie));

            IntPtr docDataPointer;
            var guidIVsTextLines = typeof(IVsTextLines).GUID;

            ErrorHandler.ThrowOnFailure(invisibleEditor.GetDocData(
                fEnsureWritable: 1
                , riid: ref guidIVsTextLines
                , ppDocData: out docDataPointer));

            _docData = (IVsTextLines)Marshal.GetObjectForIUnknown(docDataPointer);

            //Make Buffer Readonly
            _docData.GetStateFlags(out uint oldFlags);
            _docData.SetStateFlags(oldFlags | (uint)BUFFERSTATEFLAGS.BSF_USER_READONLY);

            //Create a code window adapter
            _codeWindow = _editorAdapter.CreateVsCodeWindowAdapter(ProfilerPlugin.Instance.OLEServiceProvider);

            //Disable the splitter control on the editor as leaving it enabled causes a crash if the user
            //tries to use it here :(
            IVsCodeWindowEx codeWindowEx = (IVsCodeWindowEx)_codeWindow;
            INITVIEW[] initView = new INITVIEW[1];
            codeWindowEx.Initialize((uint)_codewindowbehaviorflags.CWB_DISABLESPLITTER,// | ((uint)TextViewInitFlags2.VIF_READONLY),
                VSUSERCONTEXTATTRIBUTEUSAGE.VSUC_Usage_Filter,
                szNameAuxUserContext: "",
                szValueAuxUserContext: "",
                InitViewFlags: 0,
                pInitView: initView);

            ErrorHandler.ThrowOnFailure(_codeWindow.SetBuffer(_docData));

            //Get a text view for our editor which we will then use to get the WPF control for that editor.
            ErrorHandler.ThrowOnFailure(_codeWindow.GetPrimaryView(out _textView));
            _textViewHost = _editorAdapter.GetWpfTextViewHost(_textView);
        }

        private void ClearEditor()
        {

            if (_docData != null)
            {
                _docData.GetStateFlags(out uint oldFlags);
                _docData.SetStateFlags(oldFlags & ~(uint)BUFFERSTATEFLAGS.BSF_USER_READONLY);
                _docData = null;
            }

            if (_codeWindow != null)
            {
                _codeWindow.Close();
                _codeWindow = null;
            }

            if (_textView != null)
            {
                _textView.CloseView();
                _textView = null;
            }

            _textViewHost = null;
        }

        public override void OnToolWindowCreated()
        {
            //We need to set up the tool window to respond to key bindings
            //They're passed to the tool window and its buffers via Query() and Exec()
            var windowFrame = (IVsWindowFrame)Frame;
            var cmdUi = VSConstants.GUID_TextEditorFactory;
            windowFrame.SetGuidProperty((int)__VSFPROPID.VSFPROPID_InheritKeyBindings, ref cmdUi);
            base.OnToolWindowCreated();
        }

        protected override bool PreProcessMessage(ref Message m)
        {
            if (_textViewHost != null)
            {
                // copy the Message into a MSG[] array, so we can pass
                // it along to the active core editor's IVsWindowPane.TranslateAccelerator
                var pMsg = new MSG[1];
                pMsg[0].hwnd = m.HWnd;
                pMsg[0].message = (uint)m.Msg;
                pMsg[0].wParam = m.WParam;
                pMsg[0].lParam = m.LParam;

                var vsWindowPane = (IVsWindowPane)_textView;
                return vsWindowPane.TranslateAccelerator(pMsg) == 0;
            }

            return base.PreProcessMessage(ref m);
        }

        int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var hr =
                (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;

            if (_textView != null)
            {
                var cmdTarget = (IOleCommandTarget)_textView;
                hr = cmdTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }

            return hr;
        }

        int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            var hr =
                (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_NOTSUPPORTED;

            if (_textView != null)
            {
                var cmdTarget = (IOleCommandTarget)_textView;
                hr = cmdTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
            }

            return hr;
        }

    }
}
