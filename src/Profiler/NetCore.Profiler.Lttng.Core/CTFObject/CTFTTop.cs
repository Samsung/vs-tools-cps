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

using System.Collections.Generic;

namespace NetCore.Profiler.Lttng.Core.CTFObject
{
    public class CTFTClock
    {
        string name;
        string uuid;
        string description;
        public int Freq { get; set; }
        public ulong Offset { get; set; }

        public CTFTClock(List<CTFAssignmentExpression> lcae)
        {
            foreach (CTFAssignmentExpression cae in lcae)
            {
                string aname = cae.GetName();
                switch (aname)
                {
                    case "name":
                        name = cae.Src.GetValue().GetString();
                        break;
                    case "uuid":
                        uuid = cae.Src.GetValue().GetString();
                        break;
                    case "description":
                        description = cae.Src.GetValue().GetString();
                        break;
                    case "freq":
                        Freq = cae.Src.Calculate();
                        break;
                    case "offset":
                        Offset = cae.Src.GetULong();
                        break;
                }
            }
        }
    }

    internal class CTFTEnv
    {
        string hostname;
        string domain;
        string tracer_name;
        int tracer_major;
        int tracer_minor;

        public CTFTEnv(List<CTFAssignmentExpression> lcae)
        {
            foreach (CTFAssignmentExpression cae in lcae)
            {
                string name = cae.GetName();
                switch (name)
                {
                    case "hostname":
                        hostname = cae.Src.GetValue().GetString();
                        break;
                    case "domain":
                        domain = cae.Src.GetValue().GetString();
                        break;
                    case "tracer_name":
                        tracer_name = cae.Src.GetValue().GetString();
                        break;
                    case "tracer_major":
                        tracer_major = cae.Src.Calculate();
                        break;
                    case "tracer_minor":
                        tracer_minor = cae.Src.Calculate();
                        break;
                    default:
                        throw new CTFException();
                }
            }
        }
    }

    public class CTFTEvent
    {
        public string Name { get; private set; }
        public uint Id { get; private set; }
        int stream_id;
        int loglevel;
        public CTFType Fields { get; private set; }

        public CTFTEvent(CTFScope scope, List<CTFAssignmentExpression> lcae)
        {
            foreach (CTFAssignmentExpression cae in lcae)
            {
                string tname = cae.GetName();
                switch (tname)
                {
                    case "name":
                        Name = cae.Src.GetValue().GetString();
                        break;
                    case "id":
                        Id = (uint)cae.Src.Calculate();
                        break;
                    case "stream_id":
                        stream_id = cae.Src.Calculate();
                        break;
                    case "loglevel":
                        loglevel = cae.Src.Calculate();
                        break;
                    case "fields":
                        Fields = cae.GetType(scope);
                        break;
                    default:
                        throw new CTFException();
                }
            }
        }
    }

    internal class CTFTStream
    {

        int id;
        private CTFType eventHeader;
        public CTFType PacketContext { get; private set; }
        private CTFType eventContext;

        public CTFTStream(CTFScope scope, List<CTFAssignmentExpression> lcae)
        {
            foreach (CTFAssignmentExpression cae in lcae)
            {
                string name = cae.GetFullName();
                switch (name)
                {
                    case "id":
                        id = cae.Src.Calculate();
                        break;
                    case "event.header":
                        eventHeader = cae.GetType(scope);
                        break;
                    case "packet.context":
                        PacketContext = cae.GetType(scope);
                        break;
                    case "event.context":
                        eventContext = cae.GetType(scope);
                        break;
                    default:
                        throw new CTFException();
                }
            }
        }

        public CTFEventReader GetStreamReader(Dictionary<uint, CTFTEvent> events)
        {
            return new CTFEventReader(eventHeader, eventContext, events);
        }
    }

    internal class CTFTTrace
    {
        int major;
        int minor;
        string uuid;
        #pragma warning disable 0414
        bool msb;
        #pragma warning restore 0414
        public CTFType Header { get; private set; }

        public CTFTTrace(CTFScope scope, List<CTFAssignmentExpression> lcae)
        {
            foreach (CTFAssignmentExpression cae in lcae)
            {
                string name = cae.GetFullName();
                switch (name)
                {
                    case "major":
                        major = cae.Src.Calculate();
                        break;
                    case "minor":
                        minor = cae.Src.Calculate();
                        break;
                    case "uuid":
                        uuid = cae.Src.GetValue().GetString();
                        break;
                    case "byte_order":
                        switch (cae.Src.GetValue().GetString())
                        {
                            case "le":
                                msb = false;
                                break;
                            case "be":
                                msb = true;
                                break;
                            default:
                                throw new CTFException();
                        }

                        break;
                    case "packet.header":
                        Header = cae.GetType(scope);
                        break;
                    default:
                        throw new CTFException();
                }
            }
        }
    }
}