﻿/*
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


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Copyright (c) 2022 Samsung Electronics Co., LTD
// Distributed under the MIT License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace Tizen.VisualStudio.ProjectSystem.VS.Debug
{
    /// <summary>
    /// The capabilities that the runtime has with respect to edit and continue
    /// </summary>
    public sealed class EditAndContinueCapabilities
    {
        /// <summary>
        /// Edit and continue is generally available with the set of capabilities that Mono 6, .NET Framework and .NET 5 have in common.
        /// </summary>
        public static string Baseline = "Baseline";

        /// <summary>
        /// Adding a static or instance method to an existing type.
        /// </summary>
        public static string AddMethodToExistingType = "AddMethodToExistingType";

        /// <summary>
        /// Adding a static field to an existing type.
        /// </summary>
        public static string AddStaticFieldToExistingType = "AddStaticFieldToExistingType";

        /// <summary>
        /// Adding an instance field to an existing type.
        /// </summary>
        public static string AddInstanceFieldToExistingType = "AddInstanceFieldToExistingType";

        /// <summary>
        /// Creating a new type definition.
        /// </summary>
        public static string NewTypeDefinition = "NewTypeDefinition";

        /// <summary>
        /// Adding, updating and deleting of custom attributes (as distinct from pseudo-custom attributes)
        /// </summary>
        public static string ChangeCustomAttributes = "ChangeCustomAttributes";

        /// <summary>
        /// Whether the runtime supports updating the Param table, and hence related edits (eg parameter renames)
        /// </summary>
        public static string UpdateParameters = "UpdateParameters";
    }
    public readonly struct Update
    {
        public readonly Guid ModuleId;
        public readonly ImmutableArray<byte> ILDelta;
        public readonly ImmutableArray<byte> MetadataDelta;
        public readonly ImmutableArray<byte> PdbDelta;

        public Update(Guid moduleId, ImmutableArray<byte> ilDelta, ImmutableArray<byte> metadataDelta, ImmutableArray<byte> pdbDelta)
        {
            ModuleId = moduleId;
            ILDelta = ilDelta;
            MetadataDelta = metadataDelta;
            PdbDelta = pdbDelta;
        }
    }

    public class WatchHotReloadService
    {
        private readonly Func<Solution, CancellationToken, Task> startSession;
        private readonly Func<Solution, CancellationToken, ITuple> emitSolutionUpdateAsync;
        private readonly Action endSession;

        private static ImmutableArray<string> Net5RuntimeCapabilities = ImmutableArray.Create(EditAndContinueCapabilities.Baseline, EditAndContinueCapabilities.AddInstanceFieldToExistingType,
                                                                                        EditAndContinueCapabilities.AddStaticFieldToExistingType, EditAndContinueCapabilities.AddMethodToExistingType,
                                                                                        EditAndContinueCapabilities.NewTypeDefinition);

        private static ImmutableArray<string> Net6RuntimeCapabilities = Net5RuntimeCapabilities.AddRange(new[] { EditAndContinueCapabilities.ChangeCustomAttributes, EditAndContinueCapabilities.UpdateParameters });


        public WatchHotReloadService(HostWorkspaceServices services)
        {
            try
            {
                // Load Roslyn Assembly using a fully qualified assembly name.
                var roslynAssembly = Assembly.Load("Microsoft.CodeAnalysis.Features, Version=4.0.0.0, Culture = neutral, PublicKeyToken = 31bf3856ad364e35");
                var watchType = roslynAssembly.GetType("Microsoft.CodeAnalysis.ExternalAccess.Watch.Api.WatchHotReloadService");

                //We use the full set of capabilities for the latest version since it also works correctly on lower runtime versions to get deltas
                var watchHotReloadServiceInstance = Activator.CreateInstance(watchType, services, Net6RuntimeCapabilities);
                startSession = watchType.GetMethod("StartSessionAsync").CreateDelegate(typeof(Func<Solution, CancellationToken, Task>), watchHotReloadServiceInstance) as Func<Solution, CancellationToken, Task>;

                emitSolutionUpdateAsync = (solution, token) =>
                {
                    var emitSolutionResult = watchType.GetMethod("EmitSolutionUpdateAsync").Invoke(watchHotReloadServiceInstance, new object[] { solution, token });
                    var results = emitSolutionResult.GetType().GetProperty("Result").GetValue(emitSolutionResult, null) as ITuple;
                    return results;
                };

                endSession = watchType.GetMethod("EndSession").CreateDelegate(typeof(Action), watchHotReloadServiceInstance) as Action;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public Task StartSessionAsync(Solution currentSolution, CancellationToken cancellationToken)
        {
            return startSession(currentSolution, cancellationToken);
        }

        /// <summary>
        /// Emits updates for all projects that differ between the given <paramref name="solution"/> snapshot and the one given to the previous successful call or
        /// the one passed to <see cref="StartSessionAsync(Solution, CancellationToken)"/> for the first invocation.
        /// </summary>
        /// <param name="solution">Solution snapshot.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// Updates and Rude Edit diagnostics. Does not include syntax or semantic diagnostics.
        /// </returns>
        public (Update deltas, ImmutableArray<Diagnostic> diagnostics) EmitSolutionUpdate(Solution solution, CancellationToken cancellationToken)
        {
            var values = emitSolutionUpdateAsync(solution, cancellationToken);
            var moduleUpdates = (IList)values[0];
            var diagnostics = (ImmutableArray<Diagnostic>)values[1];
            var deltas = new Update();

            for (int i = 0; i < moduleUpdates.Count; i++)
            {
                object moduleUpdate = moduleUpdates[i];
                var updateType = moduleUpdate.GetType();

                deltas = new Update((Guid)updateType.GetField("ModuleId").GetValue(moduleUpdate),
                                        (ImmutableArray<byte>)updateType.GetField("ILDelta").GetValue(moduleUpdate),
                                        (ImmutableArray<byte>)updateType.GetField("MetadataDelta").GetValue(moduleUpdate),
                                        (ImmutableArray<byte>)updateType.GetField("PdbDelta").GetValue(moduleUpdate));

            }
            return (deltas, diagnostics);
        }

        public void EndSession() => endSession();
    }
}
