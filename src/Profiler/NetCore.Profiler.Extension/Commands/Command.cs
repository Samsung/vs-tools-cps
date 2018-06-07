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
    internal abstract class Command
    {

        private readonly MenuCommand _windowMenuCommand;

        public bool Enabled
        {
            set => _windowMenuCommand.Enabled = value;
        }

        protected Command(IServiceProvider serviceProvider, int commandId, EventHandler handler, bool enabled = false)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            OleMenuCommandService commandService = serviceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            commandService?.AddCommand(_windowMenuCommand = new MenuCommand(handler, new CommandID(GeneralProperties.CommandSet, commandId))
            {
                Enabled = enabled
            });
        }

    }
}
