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

using System.Collections.Generic;

namespace NetCore.Profiler.Extension.Launcher.Model
{
    public class ProfilingPreset
    {
        /// <summary>
        /// Gets or sets the Name of the preset
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the profiling settings
        /// </summary>
        public ProfilingSettings ProfilingSettings { get; set; }

        /// <summary>
        /// Gets list of default predefined presets
        /// </summary>
        public static List<ProfilingPreset> PredefinedPresets => new List<ProfilingPreset> { CpuSampling, MemoryAllocation, ComplexProfiling };

        /// <summary>
        /// CPU Sampling preset
        /// </summary>
        public static ProfilingPreset CpuSampling => new ProfilingPreset
        {
            Name = "CPU Sampling",
            ProfilingSettings = new ProfilingSettings
            {
                CollectMethod = ProfilingMethod.Sampling,
                TraceExecution = true,
                TraceSourceLines = true,
                TraceCpu = true,
                TraceProcessCpu = true,
                TraceThreadCpu = true,
                CpuTraceInterval = 10,
                SamplingInterval = 10,
                HighGranularitySampling = true,
                TraceMemoryAllocation = false,
                TraceGarbageCollection = false,
                StackTrack = false
            }
        };

        /// <summary>
        /// Memory Allocation Sampling preset
        /// </summary>
        public static ProfilingPreset MemoryAllocation => new ProfilingPreset
        {
            Name = "Memory Allocation",
            ProfilingSettings = new ProfilingSettings
            {
                CollectMethod = ProfilingMethod.Sampling,
                TraceMemoryAllocation = true,
                TraceSourceLines = true,
                TraceCpu = true,
                TraceProcessCpu = true,
                TraceThreadCpu = true,
                CpuTraceInterval = 10,
                SamplingInterval = 10,
                HighGranularitySampling = true,
                TraceGarbageCollection = true,
                StackTrack = false,
                TraceExecution = false
            }
        };

        /// <summary>
        /// Complex Profiling Preset
        /// </summary>
        public static ProfilingPreset ComplexProfiling => new ProfilingPreset
        {
            Name = "Complex Profiling",
            ProfilingSettings = new ProfilingSettings
            {
                CollectMethod = ProfilingMethod.Sampling,
                TraceExecution = true,
                TraceMemoryAllocation = true,
                TraceSourceLines = true,
                TraceCpu = true,
                TraceProcessCpu = true,
                TraceThreadCpu = true,
                CpuTraceInterval = 10,
                SamplingInterval = 10,
                HighGranularitySampling = true,
                TraceGarbageCollection = true,
                StackTrack = true
            }
        };
        public override string ToString()
        {
            return Name;
        }

    }
}
