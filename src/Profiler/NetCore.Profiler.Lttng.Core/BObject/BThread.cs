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
using System.Linq;
using NetCore.Profiler.Common.Helpers;
using NetCore.Profiler.Lttng.Core.CTFObject;

namespace NetCore.Profiler.Lttng.Core.BObject
{
    public class BLostEventStartAtComparer : IComparer<BLostEvent>
    {
        int IComparer<BLostEvent>.Compare(BLostEvent x, BLostEvent y)
        {
            if (x == null || y == null)
            {
                throw new ArgumentNullException("x or y is null");
            }

            if (x.StartAt > y.StartAt)
            {
                return 1;
            }
            else if (x.StartAt < y.StartAt)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }
    }

    public class BLostEvent
    {
        public BThread BThread { get; set; }
        public ulong StartAt { get; set; }
        public ulong EndAt { get; set; }
        public string StartAtStr => StartAt.TimeStampToString(BThread.GlobalOffset, BThread.GlobalFreq);

        public string EndAtStr => EndAt.TimeStampToString(BThread.GlobalOffset, BThread.GlobalFreq);

        public ulong Count { get; set; }

        public static BLostEventStartAtComparer BLostEventStartAtComparer { get; set; } = new BLostEventStartAtComparer();
    }

    public class BThread
    {
        public ulong GlobalFreq { get; set; }
        public ulong GlobalOffset { get; set; }
        public ulong StartAt { get; set; }
        public ulong Duration { get; set; }
        public ulong EndAt { get; set; }
        public ulong Pid { get; set; }
        public ulong Tid { get; set; }
        public List<CTFERecord> Events { get; set; } = new List<CTFERecord>();
        public List<CTFELostRecord> LostEvents { get; set; } = new List<CTFELostRecord>();
        public List<BLostEvent> BLostEvents { get; set; } = new List<BLostEvent>();
        public List<BGCItem> GCItems { get; set; }
        public List<BAllocation> Allocations { get; set; } = new List<BAllocation>();
        public List<BException> Exceptions = new List<BException>();
        public Dictionary<ulong, uint> ExceptionsBySecond = new Dictionary<ulong, uint>();
        public List<BJit> Jits = new List<BJit>();
        public List<BJobsPercentage> JobsPercentages = new List<BJobsPercentage>();

        public ulong JitMaxDuration = 1;
        public ulong GCMaxDuration = 1;

        public string PidTid
        {
            get
            {
                return string.Format("Pid: {0}; Tid: {1};", Pid, Tid);
            }
        }

        public void GenerateLostEvents()
        {
            foreach (CTFELostRecord lrec in LostEvents)
            {
                BLostEvents.Add(new BLostEvent { BThread = this, Count = lrec.EvDiscarded, EndAt = lrec.TSe, StartAt = lrec.TSs });
            }

            BLostEvents.Sort(BLostEvent.BLostEventStartAtComparer);
        }

        public List<CTFERecord> GetCTFEvents(int id)
        {
            return Events.Where(ev => ev.Match(id)).ToList();
        }

        public enum SomeJobType
        {
            None,
            GC,
            Jit
        }

        public class SomeJob
        {
            public ulong StartTime { get; set; }
            public ulong EndTime { get; set; } 
            public SomeJobType Type { get; set; }
        }

        public void GenerateAllocations(List<CTFERecord> records)
        {
            Dictionary<string, int> types = new Dictionary<string, int>();
            foreach (CTFERecord record in records)
            {
                BAllocation allocation = null;
                string type = record.Er.GetValue("_TypeName").ToString();
                int index;
                if (!types.TryGetValue(type, out index))
                {
                    types[type] = index = types.Count;
                    allocation = new BAllocation();
                    allocation.Type = type;
                    Allocations.Add(allocation);
                }
                else
                {
                    allocation = Allocations[index];
                }

                allocation.Count++;
                ulong size = Convert.ToUInt64(record.Er.GetValue("_AllocationAmount64"));
                if (size == 0)
                {
                    uint size32 = Convert.ToUInt32(record.Er.GetValue("_AllocationAmount"));
                    allocation.Size += size32;
                }
                else
                {
                    allocation.Size += size;
                }
            }
        }

        public void GenerateJobsTimeline()
        {
            List<SomeJob> someJobs = new List<SomeJob>();

            // fill jobs with jit load func events
            foreach (BJit jit in Jits)
            {
                if (jit.IsFull == true)
                {
                    someJobs.Add(new SomeJob { StartTime = jit.JobStartAt, EndTime = jit.JobEndAt, Type = SomeJobType.Jit });
                }
            }

            foreach (BGCItem gcItem in GCItems)
            {
                if (gcItem.IsFull != true)
                {
                    continue;
                }

                bool isAdded = false;
                for (int i = 0; i < someJobs.Count; i++)
                {
                    SomeJob someJob = someJobs[i];
                    if (someJob.Type != SomeJobType.GC && gcItem.JobStartAt > someJob.StartTime && gcItem.JobStartAt < someJob.EndTime) // if gc job inside jit job
                    {
                        // lets know how it overlaps antoher
                        if (gcItem.JobEndAt < someJob.EndTime) // if gc is smaller than jit (how it must be)
                        {
                            //split large job
                            SomeJob firstPart = new SomeJob { StartTime = someJob.StartTime, EndTime = gcItem.JobStartAt, Type = SomeJobType.Jit };
                            SomeJob secondPart = new SomeJob { StartTime = gcItem.JobEndAt, EndTime = someJob.EndTime, Type = SomeJobType.Jit };
                            // create gc job
                            SomeJob gcPart = new SomeJob { StartTime = gcItem.JobStartAt, EndTime = gcItem.JobEndAt, Type = SomeJobType.GC };
                            // insert new jobs
                            someJobs.RemoveAt(i);
                            someJobs.Insert(i, secondPart);
                            someJobs.Insert(i, gcPart);
                            someJobs.Insert(i, firstPart);
                            i += 2;
                            isAdded = true;
                        }

                    }

                }

                if (isAdded == false)
                {
                    // create gc job
                    SomeJob gcJob = new SomeJob { StartTime = gcItem.JobStartAt, EndTime = gcItem.JobEndAt, Type = SomeJobType.GC };
                    int i = GetClosestJob(gcJob, someJobs);
                    if (i == -1) // if not found insert to the end
                    {
                        someJobs.Add(gcJob);
                    }
                    else // insert at index place
                    {
                        someJobs.Insert(i, gcJob);
                    }
                }
            }

            ulong startTime = GlobalOffset;
            ulong step = 1000000000; // 1 sec
            int index = 0;
            List<BJobsPercentage> jpbss = new List<BJobsPercentage>();

            if (someJobs.Count == 0)
            {
                return;
            }

            startTime = someJobs.First().StartTime;
            for (ulong sec = startTime; sec < someJobs.Last().EndTime; sec += step)
            {
                BJobsPercentage jpbs = new BJobsPercentage() { Time = sec, BThread = this };
                jpbss.Add(jpbs);
                List<SomeJob> jobs = GetJobsBetween(ref index, sec, sec + step, someJobs);
                if (jobs.Count == 0)
                {
                    continue;
                }

                if (jobs.First().StartTime < sec) // check if first job is already have started
                {
                    ulong endTime = jobs.First().EndTime > sec + step ? sec + step : jobs.First().EndTime;
                    switch (jobs.First().Type)
                    {
                        case SomeJobType.GC:
                            jpbs.GC += endTime - sec;
                            break;
                        case SomeJobType.Jit:
                            jpbs.Jit += endTime - sec;
                            break;
                    }
                }
                else
                {
                    ulong endTime = jobs.First().EndTime > sec + step ? sec + step : jobs.First().EndTime;
                    switch (jobs.First().Type)
                    {
                        case SomeJobType.GC:
                            jpbs.GC += endTime - jobs.First().StartTime;
                            break;
                        case SomeJobType.Jit:
                            jpbs.Jit += endTime - jobs.First().StartTime;
                            break;
                    }
                }

                for (int i = 1; i < jobs.Count - 1; i++)
                {
                    SomeJob prev = jobs[i - 1];
                    SomeJob current = jobs[i];

                    switch (current.Type)
                    {
                        case SomeJobType.GC:
                            jpbs.GC += current.EndTime - current.StartTime;
                            break;
                        case SomeJobType.Jit:
                            jpbs.Jit += current.EndTime - current.StartTime;
                            break;
                    }
                }

                if (jobs.Count > 1 && jobs.Last().EndTime > sec + step) // check if first job is longer than can be
                {
                    switch (jobs.Last().Type)
                    {
                        case SomeJobType.GC:
                            jpbs.GC += sec + step - jobs.Last().StartTime;
                            break;
                        case SomeJobType.Jit:
                            jpbs.Jit += sec + step - jobs.Last().StartTime;
                            break;
                    }
                }
                else if (jobs.Count > 1)
                {
                    switch (jobs.Last().Type)
                    {
                        case SomeJobType.GC:
                            {
                                jpbs.GC += jobs.Last().EndTime - jobs.Last().StartTime;
                                break;
                            }

                        case SomeJobType.Jit:
                            {
                                jpbs.Jit += jobs.Last().EndTime - jobs.Last().StartTime;
                                break;
                            }
                    }
                }
            }

            JobsPercentages = jpbss;
        }

        private List<SomeJob> GetJobsBetween(ref int indexa, ulong from, ulong till, List<SomeJob> someJobs)
        {
            List<SomeJob> jobs = new List<SomeJob>();
            for (int index = 0 ; index < someJobs.Count; index++)
            {
                if ((someJobs[index].StartTime >= from && someJobs[index].StartTime <= till) ||
                    (someJobs[index].EndTime >= from && someJobs[index].EndTime <= till) ||
                    (someJobs[index].StartTime <= from && someJobs[index].EndTime >= till))
                {
                    jobs.Add(someJobs[index]);
                }
            }

            return jobs;
        }

        public int GetClosestJob(SomeJob job, List<SomeJob> someJobs)
        {
            for (int i = 0; i < someJobs.Count; i++)
            {
                if (someJobs[i].StartTime >= job.EndTime)
                {
                    return i;
                }
            }

            return -1;
        }

        public void CalculateDuration()
        {
            if (Events.Count == 0)
            {
                return;
            }

            StartAt = Events.First().Time + GlobalOffset;
            EndAt = Events.Last().Time + GlobalOffset;
            Duration = EndAt - StartAt;
        }

        public List<Tuple<ulong, ulong[]>> GenerateHeapPoints(int points)
        {
            List<Tuple<ulong, ulong[]>> res = new List<Tuple<ulong, ulong[]>>();
            if (points == 0)
            {
                return res;
            }

            if (points > GCItems.Count)
            {
                foreach (BGCItem item in GCItems)
                {
                    if (item.IsFull == false)
                    {
                        continue;
                    }

                    ulong[] tmp = new ulong[4];
                    tmp[0] = item.HeapSize[0];
                    tmp[1] = item.HeapSize[1];
                    tmp[2] = item.HeapSize[2];
                    tmp[3] = item.HeapSize[3];
                    res.Add(new Tuple<ulong, ulong[]>(item.RestartEEEnd.Time, tmp));
                }
            }
            else
            {
                int step = GCItems.Count / points;

                ulong[] tmp = new ulong[4];
                for (int i = 0; i < GCItems.Count; i++)
                {
                    if (GCItems[i].IsFull == false)
                    {
                        continue;
                    }

                    tmp[0] += GCItems[i].HeapSize[0];
                    tmp[1] += GCItems[i].HeapSize[1];
                    tmp[2] += GCItems[i].HeapSize[2];
                    tmp[3] += GCItems[i].HeapSize[3];
                    if (i % step == 0)
                    {
                        tmp[0] /= (ulong)step;
                        tmp[1] /= (ulong)step;
                        tmp[2] /= (ulong)step;
                        tmp[3] /= (ulong)step;
                        res.Add(new Tuple<ulong, ulong[]>(GCItems[i].RestartEEEnd.Time, tmp));
                        tmp = new ulong[4];
                    }
                }
            }

            return res;
        }

        public List<Tuple<ulong, double[]>> GenerateJobPercentagePoints(int points)
        {
            List<Tuple<ulong, double[]>> res = new List<Tuple<ulong, double[]>>();
            if (points == 0)
            {
                return res;
            }

            if (points > JobsPercentages.Count)
            {
                foreach (BJobsPercentage item in JobsPercentages)
                {
                    double[] tmp = new double[3];
                    tmp[0] = item.NonePercent;
                    tmp[1] = item.GCPercent;
                    tmp[2] = item.JitPercent;
                    res.Add(new Tuple<ulong, double[]>(item.Time, tmp));
                }
            }
            else
            {
                int step = JobsPercentages.Count / points;

                double[] tmp = new double[3];
                for (int i = 0; i < JobsPercentages.Count; i++)
                {
                    tmp[0] += JobsPercentages[i].NonePercent;
                    tmp[1] += JobsPercentages[i].GCPercent;
                    tmp[2] += JobsPercentages[i].JitPercent;
                    if (i % step == 0)
                    {
                        tmp[0] /= (ulong)step;
                        tmp[1] /= (ulong)step;
                        tmp[2] /= (ulong)step;
                        res.Add(new Tuple<ulong, double[]>(JobsPercentages[i].Time, tmp));
                        tmp = new double[3];
                    }
                }
            }

            return res;
        }

    }
}
