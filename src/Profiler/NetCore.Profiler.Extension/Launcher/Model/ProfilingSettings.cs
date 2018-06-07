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

using NetCore.Profiler.Extension.Common;

namespace NetCore.Profiler.Extension.Launcher.Model
{
    public class ProfilingSettings : NotifyPropertyChanged
    {
        private int _samplingInterval = 10;
        private bool _traceExecution;
        private bool _traceCpu;
        private bool _traceProcessCpu;
        private bool _traceThreadCpu;
        private int _cpuTraceInterval;
        private bool _traceMemoryAllocation;
        private bool _traceSourceLines;
        private bool _stackTrack;
        private bool _highGranularitySampling;
        private bool _delayedStart;
        private bool _isCollectMethodSampling;
        private ProfilingMethod _collectMethod;
        private bool _traceGarbageCollection;


        /// <summary>
        /// Gets or sets sampling interval in milliseconds
        /// </summary>
        public int SamplingInterval
        {
            get => _samplingInterval;
            set => SetProperty(ref _samplingInterval, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to trace execution
        /// </summary>
        public bool TraceExecution
        {
            get => _traceExecution;
            set
            {
                _traceExecution = value;
                if (value == false && _traceMemoryAllocation == false)
                {
                    TraceSourceLines = false;
                }

                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to trace CPU
        /// </summary>
        public bool TraceCpu
        {
            get => _traceCpu;
            set
            {
                _traceCpu = value;
                if (value == false)
                {
                    TraceProcessCpu = false;
                    TraceThreadCpu = false;
                }
                else
                {
                    TraceProcessCpu = true;
                    TraceThreadCpu = true;
                }

                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to trace Process CPU
        /// </summary>
        public bool TraceProcessCpu
        {
            get => _traceProcessCpu;
            set
            {
                _traceProcessCpu = value;
                if (value == false && !_traceThreadCpu && _traceCpu)
                {
                    TraceThreadCpu = true;
                }

                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to trace Threads CPU
        /// </summary>
        public bool TraceThreadCpu
        {
            get => _traceThreadCpu;
            set
            {
                _traceThreadCpu = value;
                if (value == false && !_traceProcessCpu && _traceCpu)
                {
                    TraceProcessCpu = true;
                }

                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a CPU trace interval
        /// </summary>
        public int CpuTraceInterval
        {
            get => _cpuTraceInterval;
            set => SetProperty(ref _cpuTraceInterval, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to trace Memory Allocation
        /// </summary>
        public bool TraceMemoryAllocation
        {
            get => _traceMemoryAllocation;
            set
            {
                _traceMemoryAllocation = value;
                if (value == false)
                {
                    StackTrack = false;
                    if (_traceExecution == false)
                    {
                        TraceSourceLines = false;
                    }

                    TraceGarbageCollection = false;
                }
                else
                {
                    StackTrack = true;
                }

                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to trace source lines
        /// </summary>
        public bool TraceSourceLines
        {
            get => _traceSourceLines;
            set => SetProperty(ref _traceSourceLines, value);
        }

        public bool StackTrack
        {
            get => _stackTrack;
            set => SetProperty(ref _stackTrack, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to perform High Granularity Sampling
        /// </summary>
        public bool HighGranularitySampling
        {
            get => _highGranularitySampling;
            set => SetProperty(ref _highGranularitySampling, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to delay start
        /// </summary>
        public bool DelayedStart
        {
            get => _delayedStart;
            set => SetProperty(ref _delayedStart, value);
        }

        public bool IsCollectMethodSampling
        {
            get => _isCollectMethodSampling;
            set => SetProperty(ref _isCollectMethodSampling, value);
        }

        public ProfilingMethod CollectMethod
        {
            get => _collectMethod;
            set
            {
                _collectMethod = value;
                if (value == ProfilingMethod.Sampling)
                {
                    IsCollectMethodSampling = true;
                }
                else
                {
                    IsCollectMethodSampling = false;
                    HighGranularitySampling = false;
                }

                OnPropertyChanged();
            }
        }


        /// <summary>
        /// Gets or sets a value indicating whether to trace Memory Allocation
        /// </summary>
        public bool TraceGarbageCollection
        {
            get => _traceGarbageCollection;
            set => SetProperty(ref _traceGarbageCollection, value);
        }

        public ProfilingSettings Copy()
        {
            return new ProfilingSettings
            {
                SamplingInterval = SamplingInterval,
                TraceExecution = TraceExecution,
                TraceCpu = TraceCpu,
                TraceProcessCpu = TraceProcessCpu,
                TraceThreadCpu = TraceThreadCpu,
                CpuTraceInterval = CpuTraceInterval,
                TraceMemoryAllocation = TraceMemoryAllocation,
                IsCollectMethodSampling = IsCollectMethodSampling,
                CollectMethod = CollectMethod,
                TraceSourceLines = TraceSourceLines,
                HighGranularitySampling = HighGranularitySampling,
                TraceGarbageCollection = TraceGarbageCollection,
                StackTrack = StackTrack,
                DelayedStart = DelayedStart
            };
        }
    }
}
