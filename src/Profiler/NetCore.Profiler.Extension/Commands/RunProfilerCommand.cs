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
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using NetCore.Profiler.Extension.VSPackage;

namespace NetCore.Profiler.Extension.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class RunProfilerCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0114;

        /// <summary>
        /// Initializes a new instance of the <see cref="RunProfilerCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="serviceProvider">Owner package, not null.</param>
        private RunProfilerCommand(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            OleMenuCommandService commandService = serviceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            commandService?.AddCommand(new MenuCommand(MenuItemCallback, new CommandID(GeneralProperties.CommandSet, CommandId)));
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static RunProfilerCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="serviceProvider">Owner package, not null.</param>
        public static void Initialize(IServiceProvider serviceProvider)
        {
            Instance = new RunProfilerCommand(serviceProvider);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            ProfilerPlugin.Instance.StartProfiler();
        }
    }
}
