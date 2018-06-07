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
    internal class CTFEventReader
    {
        CTFType streamEventHeader;
        CTFType streamEventContext;
        Dictionary<uint, CTFTEvent> events;
        internal bool IsEvDiscarded { get; set; } = false;
        internal CTFELostRecord EvDiscarded { get; private set; }

        public CTFEventReader(CTFType streamEventHeader, CTFType streamEventContext, Dictionary<uint, CTFTEvent> events)
        {
            this.streamEventHeader = streamEventHeader;
            this.streamEventContext = streamEventContext;
            this.events = events;
        }

        public CTFERecord GetEvent(MemoryBitReader r)
        {
            r.Align(streamEventHeader.Align());
            if (r.IsEmpty())
            {
                return null;
            }

            CTFRecord eh = streamEventHeader.Read(r);
            CTFRecord ec = streamEventContext.Read(r);
            uint id = (uint)eh.GetValue("id");
            object o = eh.GetValue("timestamp");
            ulong time = 0;
            if (o is uint)
            {
                ulong value = (uint)o;
                ulong mask = (1L << 27) - 1;

                if (value < (r.prev & mask))
                {
                    value = value + (1L << 27);
                }

                r.prev = r.prev & ~mask;

                r.prev = r.prev + value;
                time = r.prev;
            }
            else
            {
                time = (ulong)o;
            }

            r.prev = time;
            CTFTEvent e = events[id];
            CTFRecord er = e.Fields.Read(r);
            ulong evLost = r.GetEvsLost();
            ulong TSs = 0;
            ulong TSe = 0;
            if (r.evDisc != evLost)
            {
                IsEvDiscarded = true;
                r.evDisc = evLost;
                TSs = r.GetTSs();
                TSe = r.GetTSe();
                EvDiscarded = new CTFELostRecord(evLost, r.GetTSs(), r.GetTSe(), ec);
            }

            return new CTFERecord(e, time, ec, er);
        }
    }
}