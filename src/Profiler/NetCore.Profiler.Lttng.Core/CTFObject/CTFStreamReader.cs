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

using System.IO;

namespace NetCore.Profiler.Lttng.Core.CTFObject
{
    class CTFStreamReader
    {
        private BinaryReader r;
        private CTFPacketReader packetReader;
        private MemoryBitReader mb;
        private CTFEventReader eventReader;
        public bool IsEvDiscarded
        {
            get
            {
                return eventReader.IsEvDiscarded;
            }

            set
            {
                eventReader.IsEvDiscarded = value;
            }
        }

        public CTFELostRecord EvDiscarded
        {
            get
            {
                return eventReader.EvDiscarded;
            }
        }

        public CTFStreamReader(CTFPacketReader packetReader, CTFEventReader eventReader)
        {
            this.packetReader = packetReader;
            this.eventReader = eventReader;
        }

        public void Open(string filename)
        {
            r = new BinaryReader(File.Open(filename, FileMode.Open));
            mb = new MemoryBitReader(new FileBitReader(r), packetReader);
        }

        public CTFERecord GetEvent()
        {
            //Console.WriteLine("Mbpos 0x{0:x}", mb.getPos());
            return eventReader.GetEvent(mb);
        }

        internal void Close()
        {
            r.Close(); r = null; mb = null;
        }
    }
}
