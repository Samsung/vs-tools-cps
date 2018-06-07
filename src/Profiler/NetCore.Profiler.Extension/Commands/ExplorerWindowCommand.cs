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
using NetCore.Profiler.Extension.VSPackage;

namespace NetCore.Profiler.Extension.Commands
{
    internal sealed class ExplorerWindowCommand : Command
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0113;

        public static Command Instance { get; private set; }


        private ExplorerWindowCommand(IServiceProvider serviceProvider) : base(serviceProvider, CommandId, (sender, args) => ProfilerPlugin.Instance.ExplorerWindow.Show(), true)
        {
        }

        public static void Initialize(IServiceProvider serviceProvider)
        {
            Instance = new ExplorerWindowCommand(serviceProvider);
        }
    }
}
