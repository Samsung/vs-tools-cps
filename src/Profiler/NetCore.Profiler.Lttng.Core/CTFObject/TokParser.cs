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
using System.Text;

namespace NetCore.Profiler.Lttng.Core.CTFObject
{
    public class TokParser
    {
        public Token Token { get; private set; }
        BinaryReader r;
        Flex lex;

        public TokParser(string metafile)
        {
            r = new BinaryReader(File.Open(metafile,FileMode.Open), Encoding.ASCII);
            lex = new Flex(new CTFMetaReader(r));
            Token = lex.CollectNext();
        }

        public void Next()
        {
            Token = lex.CollectNext();
            //token.output();
        }

        public void MustBe(Token.EnumId id)
        {
            if (Token.Id != id)
            {
                throw new CTFException();
            }

            Next();
        }

        public string GetIden()
        {
            if (Token.Id != Token.EnumId.IDEN)
            {
                throw new CTFException();
            }

            string name = Token.Buffer;
            Next();
            return name;
        }

        public bool Match(Token.EnumId id)
        {
            if (Token.Id != id)
            {
                return false;
            }

            Next();
            return true;
        }

        public void Close()
        {
            r.Close();
        }

    }

    internal class CTFMetaReader 
    {
        private BinaryReader r;
        int psize;
        int csize;
        bool eof;
        int cpos;
        byte[] buffer;

        public CTFMetaReader(BinaryReader r)
        {
            this.r = r;
            eof = false;
        }

        internal int Read()
        {
            if (eof)
            {
                return -1;
            }

            if (cpos >= csize)
            {
                return ReadHeader();
            }

            int c = buffer[cpos++];
            return c;
        }

        private int ReadHeader()
        {
            try
            {
                do
                {
                    // Check magic value
                    if (r.ReadInt32() != 0x75d11d57)
                    {
                        eof = true; return -1;
                    }

                    r.ReadBytes(16 + 4); /* Skip 16 + 4 bytes */
                    csize = r.ReadInt32() / 8 - (4 + 16 + 4 + 4 + 4 + 5); // bits -> bytes - header 0x25
                    psize = r.ReadInt32() / 8 - (4 + 16 + 4 + 4 + 4 + 5); // bits -> bytes - header 0x25
                    r.ReadBytes(5);          /* Skip 5 bytes */
                    buffer = r.ReadBytes(psize);
                }
                while (csize <= 0);

                cpos = 0;
                return buffer[cpos++];
            }
            catch (EndOfStreamException)
            {
                eof = true; return -1;
            }
        }
    }
}
