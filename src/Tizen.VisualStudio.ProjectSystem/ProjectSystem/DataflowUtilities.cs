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

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tizen.VisualStudio.ProjectSystem
{
    /// <summary>
    ///     Provides common well-known project flags.
    /// </summary>
    internal static class DataflowUtilities
    {
        /// <summary>
        /// Wraps a delegate in a repeatably executable delegate that runs within an ExecutionContext captured at the time of *this* method call.
        /// </summary>
        /// <typeparam name="TInput">The type of input parameter that is taken by the delegate.</typeparam>
        /// <param name="function">The delegate to invoke when the returned delegate is invoked.</param>
        /// <returns>The wrapper delegate.</returns>
        /// <remarks>
        /// This is useful because Dataflow doesn't capture or apply ExecutionContext for its delegates,
        /// so the delegate runs in whatever ExecutionContext happened to call ITargetBlock.Post, which is
        /// never the behavior we actually want. We've been bitten several times by bugs due to this.
        /// Ironically, in Dataflow's early days it *did* have the desired behavior but they removed it
        /// when they pulled it out of the Framework so it could be 'security transparent'.
        /// By passing block delegates through this wrapper, we can reattain the old behavior.
        /// </remarks>
        internal static Func<TInput, Task> CaptureAndApplyExecutionContext<TInput>(Func<TInput, Task> function)
        {
            var context = ExecutionContext.Capture();
            return input =>
            {
                var currentSynchronizationContext = SynchronizationContext.Current;
                using (var copy = context.CreateCopy())
                {
                    Task result = null;
                    ExecutionContext.Run(
                        copy,
                        state =>
                        {
                            SynchronizationContext.SetSynchronizationContext(currentSynchronizationContext);
                            result = function(input);
                        },
                        null);
                    return result;
                }
            };
        }
    }
}
