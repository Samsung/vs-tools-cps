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
using System.Collections.Generic;
using NetCore.Profiler.Lttng.Core.CTFObject;

namespace NetCore.Profiler.Lttng.Core.BObject
{

    public class BDataContainer
    {

        public List<BThread> BThreads { get; } = new List<BThread>();

        public string FilePath { get; }

        public BDataContainer(string filePath)
        {

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("BDataContainer");
            }

            FilePath = filePath;
        }

        public void Load()
        {
            var ctfContainer = new CTFDataContainer(FilePath);
            foreach (var thread in ctfContainer.CTFThreads)
            {
                LoadThread(thread, ctfContainer);
            }
        }

        private void LoadThread(CTFThread thread, CTFDataContainer ctfContainer)
        {
            var bThread = new BThread { Pid = thread.Pid, Tid = thread.Tid, Events = thread.Records, LostEvents = thread.LostRecords };
            BThreads.Add(bThread);

            bThread.GenerateLostEvents();
            bThread.GlobalFreq = (ulong)ctfContainer.Clock.Freq;
            bThread.GlobalOffset = ctfContainer.Clock.Offset;

            bThread.Events.Sort(CTFERecord.S_SortByTime);

            var exceptionsId = new List<uint>();
            var gcItems = new Dictionary<string, List<CTFERecord>>
            {
                ["GC"] = new List<CTFERecord>(),
                ["GCAllocationTick"] = new List<CTFERecord>(),
                [":Exception"] = new List<CTFERecord>(),
                ["MethodJittingStarted_V1"] = new List<CTFERecord>(),
                ["MethodLoad"] = new List<CTFERecord>()
            };

            foreach (var item in ctfContainer.EventTypes.Values)
            {
                foreach (var key in gcItems.Keys)
                {
                    if (item.Name.IndexOf(key) >= 0)
                    {
                        gcItems[key].AddRange(bThread.GetCTFEvents(Convert.ToInt32(item.Id)));
                        if (key == ":Exception")
                        {
                            exceptionsId.Add(item.Id);
                        }
                    }
                }
            }

            foreach (var list in gcItems.Values)
            {
                list.Sort(CTFERecord.S_SortByTime);
            }

            bThread.GCItems = GenerateGCS_EPairs(bThread, gcItems);
            bThread.GenerateAllocations(gcItems["GCAllocationTick"]);

            bThread.Exceptions = GenerateExceptionGroups(bThread, gcItems[":Exception"], exceptionsId);

            bThread.ExceptionsBySecond = GenerateExceptionsBySecond(bThread.Exceptions);

            bThread.Jits = GenerateJits(bThread, gcItems);

            bThread.GenerateJobsTimeline();
            bThread.CalculateDuration();
            bThread.Events = null; // Remove useless records
        }

        private List<BJit> GenerateJits(BThread bthread, IReadOnlyDictionary<string, List<CTFERecord>> gcItems)
        {
            var jits = new List<BJit>();

            var index = 0;

            foreach (var rec in gcItems["MethodJittingStarted_V1"])
            {
                var jit = new BJit
                {
                    BThread = bthread,
                    MethodJittingStarted = rec
                };
                while (index < gcItems["MethodLoad"].Count && gcItems["MethodLoad"][index].Time < rec.Time)
                {
                    index++;
                }

                if (index < gcItems["MethodLoad"].Count)
                {
                    jit.MethodLoad = gcItems["MethodLoad"][index];
                }

                jits.Add(jit);
                bthread.JitMaxDuration = Math.Max(bthread.JitMaxDuration, jit.Duration);
            }

            return jits;
        }

        private Dictionary<ulong, uint> GenerateExceptionsBySecond(IReadOnlyList<BException> exceptionGroups)
        {
            var result = new Dictionary<ulong, uint>();

            if (exceptionGroups.Count == 0)
            {
                return result;
            }

            var startTime = exceptionGroups[0].StartAtTS;

            foreach (var exceptionGroup in exceptionGroups)
            {
                var res = (exceptionGroup.StartAtTS - startTime) / 1000000000;
                if (!result.ContainsKey(res))
                {
                    result.Add(res, 0);
                }

                result[res] += 1;
            }

            return result;
        }

        private void UpdateExcItem(ref BException item, ICollection<BException> items, BThread bThread)
        {
            items.Add(item);
            item = new BException { BThread = bThread };
        }

        private List<BException> GenerateExceptionGroups(BThread bThread, IEnumerable<CTFERecord> gcItems, List<uint> exceptionsId)
        {
            var exceptionGroups = new List<BException>();

            var exceptionGroup = new BException { BThread = bThread };
            var isCatch = false;

            foreach (var tmp in gcItems)
            {
                if (exceptionGroup.StartAtTS == 0)
                {
                    exceptionGroup.StartAtTS = tmp.Time;
                }

                switch (tmp.Name())
                {
                    case "\"DotNETRuntime:ExceptionCatchStart\"":
                        exceptionGroup.ExcCatchStart.Add(tmp);
                        isCatch = true;
                        break;
                    case "\"DotNETRuntime:ExceptionFilterStart\"":
                        if (isCatch)
                        {
                            UpdateExcItem(ref exceptionGroup, exceptionGroups, bThread);
                            isCatch = false;
                        }

                        exceptionGroup.ExcFilterStart.Add(tmp);
                        break;
                    case "\"DotNETRuntime:ExceptionFinallyStart\"":
                        if (isCatch)
                        {
                            UpdateExcItem(ref exceptionGroup, exceptionGroups, bThread);
                            isCatch = false;
                        }

                        exceptionGroup.ExcFinallyStart.Add(tmp);
                        break;
                    default:
                        if (isCatch)
                        {
                            UpdateExcItem(ref exceptionGroup, exceptionGroups, bThread);
                            isCatch = false;
                        }

                        break;
                }
            }

            return exceptionGroups;
        }

        /*private CTFERecord GetNextEvent(BThread bThread, CTFERecord orig, ref int lastIndex)
        {
            int index = bThread.Events.IndexOf(orig, lastIndex);
            lastIndex = index;

            return bThread.Events[++index];
        }

        private ulong FindFirstNotEq(BThread bThread, CTFERecord orig, ref int lastIndex)
        {
            int index = bThread.Events.IndexOf(orig, lastIndex);
            lastIndex = index;

            for (int i = index; i < bThread.Events.Count; i++)
            {
                if (!bThread.Events[i].match(orig))
                {
                    return bThread.Events[i].time;
                }
            }

            return 0;
        }
        */
        private void UpdateGcItem(ref BGCItem item, ICollection<BGCItem> items, BThread bThread)
        {
            items.Add(item);
            item.GenerateGenearationInfo();
            bThread.GCMaxDuration = Math.Max(bThread.GCMaxDuration, item.Duration);
            item = new BGCItem { BThread = bThread };
        }

        private List<BGCItem> GenerateGCS_EPairs(BThread bThread, IReadOnlyDictionary<string, List<CTFERecord>> gcItems)
        {
            var items = new List<BGCItem>();
            if (gcItems["GC"].Count == 0)
            {
                return items;
            }

            var gcItem = new BGCItem { BThread = bThread };
            var isRange = false;

            foreach (var current in gcItems["GC"])
            {
                switch (current.Name())
                {
                    case "\"DotNETRuntime:GCSuspendEEBegin_V1\"":
                        if (gcItem.SuspendEEBegin != null)
                        {
                            UpdateGcItem(ref gcItem, items, bThread);
                        }

                        gcItem.SuspendEEBegin = current;
                        isRange = false;
                        break;
                    case "\"DotNETRuntime:GCSuspendEEEnd_V1\"":
                        if (gcItem.SuspendEEEnd != null)
                        {
                            UpdateGcItem(ref gcItem, items, bThread);
                        }

                        gcItem.SuspendEEEnd = current;
                        isRange = false;
                        break;
                    case "\"DotNETRuntime:GCRestartEEBegin_V1\"":
                        if (gcItem.RestartEEBegin != null)
                        {
                            UpdateGcItem(ref gcItem, items, bThread);
                        }

                        gcItem.RestartEEBegin = current;
                        isRange = false;
                        break;
                    case "\"DotNETRuntime:GCRestartEEEnd_V1\"":
                        if (gcItem.RestartEEEnd != null)
                        {
                            UpdateGcItem(ref gcItem, items, bThread);
                        }

                        gcItem.RestartEEEnd = current;
                        UpdateGcItem(ref gcItem, items, bThread);
                        isRange = false;
                        break;
                    case "\"DotNETRuntime:GCGenerationRange\"":
                        gcItem.GCGenerationRanges.Add(current);
                        isRange = true;
                        break;
                    default:
                        if (isRange && gcItem.GCGenerationRangesBorder == 0)
                        {
                            gcItem.GCGenerationRangesBorder = current.Time;
                            isRange = false;
                        }

                        break;
                }
            }

            UpdateGcItem(ref gcItem, items, bThread);

            return items;
        }
    }
}
