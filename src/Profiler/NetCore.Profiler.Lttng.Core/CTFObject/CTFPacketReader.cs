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

namespace NetCore.Profiler.Lttng.Core.CTFObject
{
    internal class CTFPacketReader
    {
        CTFType tracePacketHeader;
        CTFType packetContext;
        internal ulong EvsLost { get; private set; }
        internal ulong TSs { get; private set; }
        internal ulong TSe { get; private set; }

        public CTFPacketReader(CTFType tracePacketHeader, CTFType packetContext)
        {
            this.tracePacketHeader = tracePacketHeader;
            this.packetContext = packetContext;
        }

        internal bool Update(FileBitReader r, out int csize, out int npos, out byte[] bytes)
        {
            bytes = null;
            csize = 0; npos = 0;
            while (csize <= npos)
            {
                if (r.IsEmpty())
                {
                    return false;
                }

                int spos = r.GetPos();
                CTFRecord cr = tracePacketHeader.Read(r);
                CTFRecord pc = packetContext.Read(r);
                int epos = r.GetPos();
                npos = epos - spos;
                csize = (int)(ulong)pc.GetValue("content_size");
                int psize = (int)(ulong)pc.GetValue("packet_size");
                object o = pc.GetValue("events_discarded");
                if (o is uint)
                {
                    EvsLost = (ulong)(uint)o;
                }
                else
                {
                    EvsLost = (ulong)o;
                }

                TSs = (ulong)pc.GetValue("timestamp_begin");
                TSe = (ulong)pc.GetValue("timestamp_end");
                psize /= 8;
                bytes = new byte[psize];
                r.Read(bytes, npos, psize - npos);
                csize /= 8;
            }

            return true;
        }
    }

    internal class MemoryBitReader : BitReader
    {
        public ulong prev = 0;
        public ulong evDisc = 0;
        private byte[] bytes;
        private int csize;
        CTFPacketReader packetReader;
        FileBitReader r;

        internal ulong GetEvsLost()
        {
            return packetReader.EvsLost;
        }

        internal ulong GetTSs()
        {
            return packetReader.TSs;
        }

        internal ulong GetTSe()
        {
            return packetReader.TSe;
        }

        public MemoryBitReader(FileBitReader r, CTFPacketReader packetReader)
        {
            this.r = r;
            this.packetReader = packetReader;
            Update();
        }

        public bool Update()
        {
            return packetReader.Update(r, out csize, out apos, out bytes);
        }

        public void CheckPos()
        {
            if (apos >= csize)
            {
                Update();
            }
        }

        internal override bool IsEmpty()
        {
            return (apos >= csize) && !Update();
        }

        internal override int ReadByte()
        {
            CheckPos();
            return bytes[apos];
        }

        internal override short ReadInt16()
        {
            bpos = 0;
            CheckPos();
            Int16 u = BitConverter.ToInt16(bytes, apos);
            return u;
        }

        internal override int ReadInt32()
        {
            bpos = 0;
            CheckPos();
            Int32 u = BitConverter.ToInt32(bytes, apos);
            return u;
        }

        internal override uint ReadUByte()
        {
            bpos = 0;
            CheckPos();
            return (uint)bytes[apos] & 0xff;
        }

        internal override ushort ReadUInt16()
        {
            bpos = 0;
            CheckPos();
            UInt16 u = BitConverter.ToUInt16(bytes, apos);
            return u;
        }

        internal override uint ReadUInt32()
        {
            bpos = 0;
            CheckPos();
            UInt32 u = BitConverter.ToUInt32(bytes, apos);
            return u;
        }

        internal override ulong ReadUInt64()
        {
            bpos = 0;
            CheckPos();
            UInt64 u = BitConverter.ToUInt64(bytes, apos);
            return u;
        }

        internal override void Align(int v)
        {
            if (v < 8)
            {
                return;
            }

            bpos = 0;
            v = v / 8;
            apos = (apos + (v - 1)) & (-v);
        }
    }
}
