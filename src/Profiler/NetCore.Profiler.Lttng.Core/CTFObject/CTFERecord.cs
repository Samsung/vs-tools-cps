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

namespace NetCore.Profiler.Lttng.Core.CTFObject
{

    public class CTFRecord
    {
        CTFStructType ct; // description
        public Object[] array;

        public CTFRecord(CTFType type, object[] array)
        {
            ct = (CTFStructType)type;
            this.array = array;
        }

        internal object GetValue(string name)
        {
            return array[ct.GetPos(name)];
        }
    }

    public class CTFELostRecord
    {
        public ulong EvDiscarded { get; set; }
        public ulong TSs { get; set; }
        public ulong TSe { get; set; }
        public int Vpid { get; private set; }
        public int Vtid { get; private set; }

        public CTFELostRecord(ulong evDiscarded, ulong TSs, ulong TSe, CTFRecord ec)
        {
            this.EvDiscarded = evDiscarded;
            this.TSs = TSs;
            this.TSe = TSe;
            Vpid = (int)ec.GetValue("_vpid");
            Vtid = (int)ec.GetValue("_vtid");
        }
    }

    public class CTFERecord
    {
        // Class to represent one event from a stream
        public ulong Time { get; private set; }
        public int Vpid { get; private set; }
        public int Vtid { get; private set; }
        private CTFTEvent e;
        public CTFRecord Er { get; private set; }

        public static IComparer<CTFERecord> S_SortByTime { get; } = new CTFERecordTimeComparer();

        public CTFERecord(CTFTEvent e, ulong time, CTFRecord ec, CTFRecord er)
        {
            this.e = e;
            this.Time = time;
            this.Er = er;
            Vpid = (int)ec.GetValue("_vpid");
            Vtid = (int)ec.GetValue("_vtid");
            //Console.WriteLine("Event " + e.name);
        }

        public bool Match(int id)
        {
            return (e.Id == id);
        }

        public bool Match(CTFERecord rec)
        {
            return (e.Id == rec.e.Id);
        }

        public string Name()
        {
            return e.Name;
        }
    }

    class CTFERecordTimeComparer : IComparer<CTFERecord>
    {
        int IComparer<CTFERecord>.Compare(CTFERecord x, CTFERecord y)
        {
            if (x == null || y == null)
            {
                throw new ArgumentNullException("x or y is null");
            }

            if (x.Time > y.Time)
            {
                return 1;
            }
            else if (x.Time < y.Time)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }
    }
}
