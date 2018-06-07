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
using System.IO;

namespace NetCore.Profiler.Lttng.Core.CTFObject
{
    public abstract class BitReader
    {
        protected int apos; // real position for alignment
        protected int bpos; // bit pos
        protected byte value; // part of byte

        internal int GetPos() => apos;

        internal object ReadIntObject(bool signed, int align, int size)
        {
            object r;
            if (align % 8 == 0)
            { // current value/bpos is unimportant
                bpos = 0;
                if (align > 8)
                {
                    align /= 8;
                    apos = (apos + (align - 1)) & (-align);
                }

                if (size % 8 == 0)
                {
                    switch (size)
                    {
                        case 64:
                            r =  signed ? (object)ReadInt64() : (object)ReadUInt64();
                            apos += 8;
                            return r;
                        case 32:
                            r = signed ? (object)ReadInt32() : (object)ReadUInt32();
                            apos += 4;
                            return r;
                        case 16:
                            r = signed ? (object)ReadInt16() : (object)ReadUInt16();
                            apos += 2;
                            return r;
                        case 8:
                            r = signed ? (object)ReadByte() : (object)ReadUByte();
                            apos++;
                            return r;
                        default:
                            throw new CTFException();                   
                    }
                }
            }

            if (signed)
            {
                throw new CTFException(); // unsupported
            }

            // non standard values
            uint rvalue = 0;
            int offset = 0;
            while (size > 0)
            {
                if (bpos > 0)
                {
                    // We must collect bits
                    rvalue = (uint)value >> bpos;
                    size -= 8 - bpos; // TODO
                    offset = 8 - bpos;
                    bpos = 0;
                }
                else // byte aligned
                {
                    value = (byte)(int)ReadByte();
                    apos++;
                    if (size < 8)
                    {
                        uint v = (uint)value & (uint)((1 << size) - 1);
                        bpos = size;
                        rvalue |= v << offset;
                        break;
                    }
                    else
                    {
                        rvalue |= ((uint)value & 0xff) << offset;
                        size -= 8;
                        offset += 8;
                    }
                }
            }

            return rvalue;
        }

        internal virtual UInt16 ReadUInt16()
        {
            throw new NotImplementedException();
        }

        internal virtual Int16 ReadInt16()
        {
            throw new NotImplementedException();
        }

        internal abstract UInt64 ReadUInt64();
        internal virtual Int64 ReadInt64()
        {
            throw new NotImplementedException();
        }

        internal abstract UInt32 ReadUInt32();
        internal abstract Int32 ReadInt32();
        internal abstract int ReadByte();
        internal abstract uint ReadUByte();
        internal virtual void Align(int v)
        {
        } // Important for memory
        internal abstract bool IsEmpty();
        internal int ReadChar()
        {
            int c = ReadByte();
            apos++;
            return c;
        }
    }

    class FileBitReader : BitReader
    {
        BinaryReader br;
        long length;

        public FileBitReader(BinaryReader br)
        {
            this.br = br;
            length = br.BaseStream.Length;
        }

        internal void Read(byte[] bytes, int npos, int count)
        {
            apos += count;
            int len = br.Read(bytes, npos, count);
            //Console.WriteLine("Len {0}", len);
        }

        internal override UInt64 ReadUInt64() => br.ReadUInt64();

        internal override Int64 ReadInt64() => br.ReadInt64();

        internal override UInt32 ReadUInt32() => br.ReadUInt32();

        internal override Int32 ReadInt32() => br.ReadInt32();

        internal override int ReadByte() => br.ReadByte();

        internal override uint ReadUByte() => br.ReadByte();

        internal override bool IsEmpty() => ((long)apos + 16) > length; // as minimum 2 longs

    }
}
