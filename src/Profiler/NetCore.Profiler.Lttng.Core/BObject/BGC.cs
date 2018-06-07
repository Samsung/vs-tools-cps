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
using NetCore.Profiler.Common.Helpers;
using NetCore.Profiler.Lttng.Core.CTFObject;

namespace NetCore.Profiler.Lttng.Core.BObject
{
    interface IStackGC
    {
        ulong[] HeapSize { get; set; }
    }

    public class BGCItem : IBJob, IPoint, IStackGC
    {
        public BThread BThread { get; set; }
        public CTFERecord SuspendEEBegin { get; set; }
        public CTFERecord SuspendEEEnd { get; set; }
        public CTFERecord RestartEEBegin { get; set; }
        public CTFERecord RestartEEEnd { get; set; }
        public List<CTFERecord> GCGenerationRanges { get; set; } = new List<CTFERecord>();
        public ulong GCGenerationRangesBorder { get; set; } = 0;
        public Dictionary<int, List<BGenerationInfo>> GenerationInfos = new Dictionary<int, List<BGenerationInfo>>();
        public bool IsFull
        {
            get
            {
                return (BThread != null &&
                    SuspendEEBegin != null &&
                    SuspendEEEnd != null &&
                    RestartEEBegin != null &&
                    RestartEEEnd != null);
            }
        }

        #region IPoint
        public double X
        {
            get
            {
                return JobStartAt;
            }
        }

        public double Y
        {
            get
            {
                return 0.5;
            }
        }

        public double Weight
        {
            get
            {
                return (RestartEEEnd.Time - SuspendEEBegin.Time) / 1000000000.0 * 50;
            }
        }
        #endregion IPoint
        #region IBJob
        public ulong JobStartAt
        {
            get
            {
                if (SuspendEEBegin == null)
                {
                    return 0;
                }
                else
                {
                    return BThread.GlobalOffset + SuspendEEBegin.Time;
                }
            }
        }

        public ulong JobDuration
        {
            get
            {
                return JobEndAt - JobStartAt;
            }
        }

        public ulong JobEndAt
        {
            get
            {
                if (RestartEEEnd == null)
                {
                    return JobStartAt;
                }
                else
                {
                    return BThread.GlobalOffset + RestartEEEnd.Time;
                }
            }
        }
        #endregion IBJob
        #region IStackGC
        public ulong[] HeapSize { get; set; } = new ulong[4];
        public ulong[] HeapSizeStart { get; set; } = new ulong[4];
        public ulong[] ReservedSize { get; set; } = new ulong[4];

        #endregion IStackGC

        public ulong Duration
        {
            get
            {
                if (RestartEEEnd == null || SuspendEEBegin == null)
                {
                    return 0;
                }
                else
                {
                    return RestartEEEnd.Time - SuspendEEBegin.Time;
                }
            }
        }

        public string SuspendBeginAtStr
        {
            get
            {
                if (SuspendEEBegin == null)
                {
                    return Double.PositiveInfinity.ToString();
                }
                else
                {
                    return SuspendEEBegin.Time.TimeStampToString(BThread.GlobalOffset, BThread.GlobalFreq);
                }
            }
        }

        public string SuspendDurationStr
        {
            get
            {
                if (SuspendEEEnd == null || SuspendEEBegin == null)
                {
                    return Double.PositiveInfinity.ToString();
                }
                else
                {
                    return (SuspendEEEnd.Time - SuspendEEBegin.Time).TimeToString(BThread.GlobalFreq);
                }
            }
        }

        public string RestartBeginAtStr
        {
            get
            {
                if (RestartEEBegin == null)
                {
                    return Double.PositiveInfinity.ToString();
                }
                else
                {
                    return RestartEEBegin.Time.TimeStampToString(BThread.GlobalOffset, BThread.GlobalFreq);
                }
            }
        }

        public string RestartDurationStr
        {
            get
            {
                if (RestartEEEnd == null || RestartEEBegin == null)
                {
                    return Double.PositiveInfinity.ToString();
                }
                else
                {
                    return (RestartEEEnd.Time - RestartEEBegin.Time).TimeToString(BThread.GlobalFreq);
                }
            }
        }

        public string DurationStr
        {
            get
            {
                if (RestartEEEnd == null || SuspendEEBegin == null)
                {
                    return Double.PositiveInfinity.ToString();
                }
                else
                {
                    return (RestartEEEnd.Time - SuspendEEBegin.Time).TimeToString(BThread.GlobalFreq);
                }
            }
        }

        public class BGenerationInfo
        {
            public string Address { get; set; }
            public string Used { get; set; }
            public string Reserved { get; set; }
            public string Status { get; set; }
        }

        public void GenerateGenearationInfo()
        {
            GenerationInfos[0] = new List<BGenerationInfo>();
            GenerationInfos[1] = new List<BGenerationInfo>();
            GenerationInfos[2] = new List<BGenerationInfo>();
            GenerationInfos[3] = new List<BGenerationInfo>();

            //array: diff used, diff reserved, final used, final reserved
            Dictionary<int, long[]> totalInfo = new Dictionary<int, long[]>();

            totalInfo[0] = new long[4];
            totalInfo[1] = new long[4];
            totalInfo[2] = new long[4];
            totalInfo[3] = new long[4];

            for (int i = 0; i < GCGenerationRanges.Count; i++)
            {
                CTFERecord rec = GCGenerationRanges[i];
                BGenerationInfo generationInfo = new BGenerationInfo();
                generationInfo.Address = string.Format("0x{0:X}", rec.Er.GetValue("_RangeStart"));
                generationInfo.Used = rec.Er.GetValue("_RangeUsedLength").ToString();
                generationInfo.Reserved = rec.Er.GetValue("_RangeReservedLength").ToString();

                int gen = Convert.ToInt32(rec.Er.GetValue("_Generation").ToString());

                if (rec.Time < GCGenerationRangesBorder)
                {
                    generationInfo.Status = "Before";
                }
                else
                {
                    generationInfo.Status = "After";
                }

                int mltp = rec.Time < GCGenerationRangesBorder ? -1 : 1;

                totalInfo[gen][0] += mltp * Convert.ToInt64(generationInfo.Used);
                totalInfo[gen][1] += mltp * Convert.ToInt64(generationInfo.Reserved);

                if (mltp > 0)
                {
                    totalInfo[gen][2] += Convert.ToInt64(generationInfo.Used);
                    totalInfo[gen][3] += Convert.ToInt64(generationInfo.Reserved);
                }

                GenerationInfos[gen].Add(generationInfo);
            }

            for (int i = 0; i < 4; i++)
            {
                GenerationInfos[i].Insert(0, new BGenerationInfo()
                {
                    Used = string.Format("{0:+0;-0;+0}", totalInfo[i][0]),
                    Reserved = string.Format("{0:+0;-0;+0}", totalInfo[i][1]),
                    Status = "Changes"
                });

                HeapSize[i] = (ulong)totalInfo[i][2];

                HeapSizeStart[i] = HeapSize[i] - (ulong)totalInfo[i][0];

                ReservedSize[i] = (ulong)totalInfo[i][3];

                GenerationInfos[i].Insert(0, new BGenerationInfo()
                {
                    Used = string.Format("{0}", totalInfo[i][2]),
                    Reserved = string.Format("{0}", totalInfo[i][3]),
                    Status = "Result"
                });
            }
        }
    }
}
