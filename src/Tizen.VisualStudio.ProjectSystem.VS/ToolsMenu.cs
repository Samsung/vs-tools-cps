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

//
//  Tizen.Extension.ToolsMenu.cs
//      Provides Custom Menu and Toolbar
//
using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Tizen.VisualStudio.APIChecker;
using Tizen.VisualStudio.ConnectToolbar;
using Tizen.VisualStudio.Tools.DebugBridge;
using Tizen.VisualStudio.ResourceManager;
using Tizen.VisualStudio.InstallLauncher;
using Tizen.VisualStudio.Tools.ExternalTool;
using Tizen.VisualStudio.Preview;
using Tizen.VisualStudio.ProjectWizard.View;
using System.Windows;
using System.Collections.Generic;
using System.Linq;

namespace Tizen.VisualStudio
{
    internal sealed class ToolsMenu
    {
        public static readonly Guid guidCommandSet =
            new Guid("14cd1b22-758c-4d5e-827e-0229a3371332");
        public static readonly Guid guidFileMenuCommandSet =
            new Guid("71b2c831-4da5-4b4a-8903-e9e336839b73");

        public const int cmdIdMenuItemPackageManager = 0x0100;
        //public const int cmdIdConnectCombo = 0x0101;
        //public const int cmdIdConnectComboGeList = 0x0102;
        public const int cmdIdRemoteDevice = 0x0103;
        public const int cmdIdToolBarEmulatorManager = 0x0104;
        public const int cmdIdMenuItemEmulatorManager = 0x0105;
        public const int cmdIdMenuItemSdbPrompt = 0x0107;
        public const int cmdIdMenuItemCertificateManager = 0x0108;
        public const int cmdIdMenuItemLogView = 0x0109;
        public const int cmdIdMenuItemDeviceManager = 0x0110;
        public const int cmdIdMenuItemAPIChecker = 0x0111;
        public const int cmdIdMenuItemResourceManager = 0x0112;
        public const int cmdIdMenuItemImportWgtProject = 0x0119;
        public const int cmdIdMenuItemInstallWizard = 0x0198;
        public const int cmdIdMenuItemTest = 0x0199;
        public const int CmdIdMenuItemSdbServerStart = 0x0200;
        public const int CmdIdMenuItemXamlPreview = 0x201;
        public const int CmdIdFileNewTizenWebProject = 0x0777;
        public const int cmdIdMenuItemImportProject = 0x0106;

        private static ToolsMenu instance;

        private IServiceProvider ServiceProvider
        {
            get { return this.package as IServiceProvider; }
        }

        private readonly VsPackage package;


        public static void Initialize(VsPackage package)
        {
            ToolsMenu.instance = new ToolsMenu(package);
            ToolsMenu.instance.RegisterMenuHandlers();
        }

        /// <summary>
        /// Initializes a new instance of the ToolsMenu class.
        /// Adds our command handlers for menu (commands must exist in the
        /// command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private ToolsMenu(VsPackage package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;
        }

        private void RegisterMenuHandlers()
        {
            OleMenuCommandService commandService =
                this.ServiceProvider.GetService(typeof(IMenuCommandService))
                                                as OleMenuCommandService;
            if (commandService != null)
            {
                RegMenuItem(commandService, cmdIdMenuItemPackageManager, HandleMenuItemPackageManager);
                RegMenuItem(commandService, cmdIdMenuItemEmulatorManager, HandleMenuItemEmulatorManager);
                RegMenuItem(commandService, cmdIdMenuItemSdbPrompt, HandleMenuItemSdbPrompt);
                //RegMenuItem(commandService, cmdIdConnectCombo, ConnectDeviceCombo.HandleConnectCombo);
                //RegMenuItem(commandService, cmdIdConnectComboGeList, ConnectDeviceCombo.HandleConnectComboList);
                RegMenuItem(commandService, cmdIdRemoteDevice, ConnectDeviceCombo.HandleRemoteDevice);
                RegMenuItem(commandService, cmdIdToolBarEmulatorManager, HandleMenuItemEmulatorManager);
                RegMenuItem(commandService, cmdIdMenuItemCertificateManager, HandleMenuItemCertificateManager);
                RegMenuItem(commandService, cmdIdMenuItemDeviceManager, HandleMenuItemDeviceManager);
                RegMenuItem(commandService, cmdIdMenuItemLogView, HandleMenuItemLogView);
                RegMenuItem(commandService, cmdIdMenuItemAPIChecker, HandleMenuItemAPIChecker);
                RegMenuItem(commandService, CmdIdMenuItemXamlPreview, HandleMenuItemXamlPreview);
                RegDebugModeMenuItem(commandService, cmdIdMenuItemResourceManager, HandleMenuItemResourceManager);
                RegMenuItem(commandService, cmdIdMenuItemImportWgtProject, HandleMenuItemImportWgtProject);
                RegDynamicMenuItem(commandService, CmdIdMenuItemSdbServerStart, HandleMenuItemSdbServerStart);

                RegDebugModeMenuItem(commandService, cmdIdMenuItemInstallWizard, HandleMenuItemInstallWizard);

                RegDebugModeMenuItem(commandService, cmdIdMenuItemTest, HandleMenuItemTest);
                RegFileMenuItem(commandService, CmdIdFileNewTizenWebProject, HandleFileMenuItemNewTizenProject);
                RegMenuItem(commandService, cmdIdMenuItemImportProject, HandleMenuItemImportProject);
            }
        }

        private void RegDebugModeMenuItem(OleMenuCommandService commandService, int commandID, EventHandler invokeHandler)
        {
            MenuCommand mItem = RegMenuItem(commandService, commandID, invokeHandler);
#if !DEBUG
            mItem.Visible = false;
#else
            mItem.Visible = true;
#endif
        }

        private void RegDynamicMenuItem(OleMenuCommandService commandService, int commandID, EventHandler invokeHandler)
        {
            MenuCommand mItem = RegMenuItem(commandService, commandID, invokeHandler);
            mItem.Visible = true;
        }

        private MenuCommand RegMenuItem(OleMenuCommandService commandService, int commandID, EventHandler invokeHandler)
        {
            CommandID cmdId;
            MenuCommand mItem;

            cmdId = new CommandID(guidCommandSet, commandID);
            mItem = new MenuCommand(invokeHandler, cmdId);

            commandService.AddCommand(mItem);

            return mItem;
        }

        private MenuCommand RegFileMenuItem(OleMenuCommandService commandService, int commandID, EventHandler invokeHandler)
        {
            CommandID cmdId;
            MenuCommand mItem;

            cmdId = new CommandID(guidFileMenuCommandSet, commandID);
            mItem = new MenuCommand(invokeHandler, cmdId);

            commandService.AddCommand(mItem);

            return mItem;
        }

        #region Command Handlers

        private void HandleFileMenuItemNewTizenProject(object sender, EventArgs e)
        {
            var projectWizard = new ProjectWizardCreateWizard();
            projectWizard.Owner = Application.Current.MainWindow;
            projectWizard.ShowDialog();
        }

        private void HandleMenuItemInstallWizard(object sender, EventArgs e)
        {
            new InstallWizard().ShowDialog();
        }

        private void HandleMenuItemTest(object sender, EventArgs e)
        {
            System.Windows.MessageBox.Show(DeviceManager.SelectedDevice?.Serial);
        }

        private void HandleMenuItemPackageManager(object sender, EventArgs e)
        {
            PackageManagerLauncher pmLauncher = new PackageManagerLauncher();
            pmLauncher.Launch();
        }

        private void HandleMenuItemImportProject(object sender, EventArgs e)
        {
            var importWizard = new ProjectWizardProjectImport();
            importWizard.Show();
        }

        private void HandleMenuItemEmulatorManager(object sender, EventArgs e)
        {
            EmulatorManagerLauncher emLauncher = new EmulatorManagerLauncher();
            emLauncher.Launch();
        }

        private void HandleMenuItemSdbPrompt(object sender, EventArgs e)
        {
            SdbCommandPrompt sPrompt = new SdbCommandPrompt();
            sPrompt.StartSdbCommandPrompt();
        }

        private void HandleMenuItemCertificateManager(object sender, EventArgs e)
        {
            CertificateManagerLauncher cmLauncher = new CertificateManagerLauncher();
            cmLauncher.Launch();
        }

        private void HandleMenuItemDeviceManager(object sender, EventArgs e)
        {
            DeviceManagerLauncher dmLauncher = new DeviceManagerLauncher();
            dmLauncher.Launch();
        }

        private void HandleMenuItemLogView(object sender, EventArgs e)
        {
            ToolWindowPane window = package.FindToolWindow(typeof(Tizen.VisualStudio.LogViewer.LogViewer), 0, true);
            if ((window == null) || (window.Frame == null))
            {
                throw new NotSupportedException("Failed to open Log View. Cannot create tool window");
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        private void HandleMenuItemAPIChecker(object sender, EventArgs e)
        {
            APICheckerCommand.Instance.MenuItemCallback(sender, e);
        }

        private void HandleMenuItemResourceManager(object sender, EventArgs e)
        {
            ResourceManagerLauncher launcher = ResourceManagerLauncher.getInstance();
            launcher.launch(package);
        }

        private void HandleMenuItemImportWgtProject(object sender, EventArgs e)
        {
            List<string> profileList = VsProjectHelper.GetInstance.GetProfileList("web");

            ProjectWizardProjectImportWgt wizard = new ProjectWizardProjectImportWgt(profileList)
            {
                Owner = Application.Current.MainWindow
            };
            wizard.ShowDialog();
        }
        private void HandleMenuItemSdbServerStart(object sender, EventArgs e)
        {
            DeviceManager.ResetDeviceMonitorRetry();
            DeviceManager.StopDeviceMonitor();
            DeviceManager.StartDeviceMonitor();
        }

        private void HandleMenuItemXamlPreview(object sender, EventArgs e)
        {
            new PreviewerTool().Preview(this.ServiceProvider);
        }

        #endregion
    }
}
