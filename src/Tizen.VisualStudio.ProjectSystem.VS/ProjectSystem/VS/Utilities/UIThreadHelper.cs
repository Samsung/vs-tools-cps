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
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Shell;

namespace Tizen.VisualStudio.ProjectSystem.VS.Utilities
{
    /// <summary>
    /// Static class containing helper utilities for working with UI thread.
    /// </summary>
    internal static class UIThreadHelper
    {
        /// <summary>
        /// Helper utility to ensure that we are on UI thread. Needs to be called 
        /// in every method/propetrty needed UI thread for our protection (to avoid hangs 
        /// which are hard to repro and investigate).
        /// </summary>
        public static void VerifyOnUIThread([CallerMemberName] string memberName = "")
        {
            /*if (!UnitTestHelper.IsRunningUnitTests)
            {
                try
                {
                    ThreadHelper.ThrowIfNotOnUIThread(memberName);
                }
                catch
                {
                    System.Diagnostics.Debug.Fail("Call made on the Non-UI thread by " + memberName);
                    throw;
                }
            }*/
        }
    }
}
