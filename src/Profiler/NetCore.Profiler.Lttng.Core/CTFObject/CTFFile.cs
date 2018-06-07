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
using System.Diagnostics;
using System.IO;

namespace NetCore.Profiler.Lttng.Core.CTFObject
{
    class CTFFile
    {
        private CTFScope scope;
        private CTFTTrace trace;
        private CTFTEnv env;
        public CTFTClock Clock { get; private set; }
        private CTFTStream stream;
        public Dictionary<uint, CTFTEvent> Events { get; private set; }

        internal CTFFile()
        {
            scope = new CTFScope();
            Events = new Dictionary<uint, CTFTEvent>();
            CTFITypeSpecifier.Init();
        }

        internal CTFStreamReader GetStreamReader()
        {
            return new CTFStreamReader(new CTFPacketReader(trace.Header, stream.PacketContext), stream.GetStreamReader(Events));
        }

        private List<CTFAssignmentExpression> ParseAEList(TokParser tp)
        {
            tp.Next();
            tp.MustBe(Token.EnumId.LCURL);
            List<CTFAssignmentExpression> lcae = CTFAssignmentExpression.ParseList(scope, tp);
            if (lcae == null)
            {
                throw new CTFException();
            }

            tp.MustBe(Token.EnumId.RCURL);
            return lcae;
        }

        private CTFFile Parse(TokParser tp)
        {
            for (;;)
            {
                switch (tp.Token.Id)
                {
                    case Token.EnumId.CLOCK:
                        Clock = new CTFTClock(ParseAEList(tp));
                        break;
                    case Token.EnumId.EVENT:
                        CTFTEvent ce = new CTFTEvent(scope, ParseAEList(tp));
                        Events.Add(ce.Id, ce);
                        break;
                    case Token.EnumId.STREAM:
                        stream = new CTFTStream(scope, ParseAEList(tp));
                        break;
                    case Token.EnumId.ENV:
                        env = new CTFTEnv(ParseAEList(tp));
                        break;
                    case Token.EnumId.TRACE:
                        trace = new CTFTTrace(scope, ParseAEList(tp));
                        break;
                    case Token.EnumId.TYPEALIAS:
                        tp.Next();
                        List<CTFTypeSpecifier> cds = CTFITypeSpecifier.ParseList(scope, tp);
                        if (cds == null)
                        {
                            throw new CTFException();
                        }

                        tp.MustBe(Token.EnumId.TYPE_ASSIGNMENT);
                        List <CTFTypeSpecifier> cds2 = CTFITypeSpecifier.ParseList(scope, tp);
                        CTFDeclarator cd = CTFDeclarator.Parse(tp);
                        CTFType.AddType(scope, cds, cds2, cd);
                        break;
                    case Token.EnumId.STRUCT:
                        CTFITypeSpecifier.ParseTypeSpecifier(scope, tp);
                        break;
                    case Token.EnumId.EOF:
                        return this;
                    case Token.EnumId.TERM:
                        tp.Next(); // Skip it
                        break;
                    default:
                        throw new CTFException();
                }
            }
        }

        public CTFFile ParseFile(TokParser tp)
        {
            return Parse(tp);
        }

        //public List<CTFERecord> readTrace(string metafile)
        public List<CTFThread> ReadTrace(string metafile)
        {
            List<CTFThread> threads = new List<CTFThread>();
            TokParser p = null;
            try
            {
                p = new TokParser(metafile);
                ParseFile(p);

                // Try to find all stream files
                int n = metafile.LastIndexOf(Path.DirectorySeparatorChar);
                if (n >= 0)
                {
                    CTFStreamReader cr = GetStreamReader();

                    string dir = Path.GetDirectoryName(metafile);
                    var files = Directory.GetFiles(dir);
                    var ctffiles = Array.FindAll(files, s => s.Contains("channel0_"));

                    foreach (string cfile in ctffiles)
                    {
                        cr.Open(cfile);
                        for (CTFERecord cer; (cer = cr.GetEvent()) != null;)
                        {
                            CTFThread thread = CTFThread.FirstOrCreateCTFThreadById(Convert.ToUInt64(cer.Vpid),
                                Convert.ToUInt64(cer.Vtid), threads);
                            thread.Records.Add(cer);
                            if (cr.IsEvDiscarded == true)
                            {
                                thread.LostRecords.Add(cr.EvDiscarded);
                                cr.IsEvDiscarded = false;
                            }
                        }

                        cr.Close();
                    }
                }

            }
            catch (Exception ex)
            {
                Debug.Print("Error {0}\n",ex.Message);
            }
            finally
            {
                p?.Close();
            }

            return threads;

        }
    }

}
