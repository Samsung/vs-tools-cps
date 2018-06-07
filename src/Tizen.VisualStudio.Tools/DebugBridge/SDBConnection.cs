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
using System.Net;
using System.Net.Sockets;
using System.Text;
using Tizen.VisualStudio.Tools.Utilities;

namespace Tizen.VisualStudio.Tools.DebugBridge
{
    public interface ISDBSocketDevice
    {
        void OnItem(SDBSocket socket, string name);
    }

    public class SDBConnection
    {
        private SDBSocket sdbsocket = null;

        public const string DefaultEncoding = "ISO-8859-1";
        private const int SDBPort = 26099;

        private SDBConnection()
        {
        }

        public static SDBConnection Create()
        {
            SDBConnection sdbconnection = new SDBConnection();

            sdbconnection.Initialize();

            if (!sdbconnection.ConnectSDB())
            {
                if (!sdbconnection.RunDaemon())
                {
                    return null;
                }

                if (!sdbconnection.ConnectSDB())
                {
                    return null;
                }
            }

            return sdbconnection;
        }

        public void Dispose()
        {
            if (this.sdbsocket != null)
            {
                this.sdbsocket.Dispose();
            }
        }

        protected void Initialize()
        {
            this.sdbsocket = new SDBSocket();
        }

        protected bool ConnectSDB()
        {
            bool success = true;

            try
            {
                this.sdbsocket.NoDelay = true;
                this.sdbsocket.Connect(IPAddress.Loopback, SDBConnection.SDBPort);
            }
            catch (ObjectDisposedException e)
            {
                success = false;
                Console.WriteLine(e.Message);
            }
            catch (SocketException e)
            {
                success = false;
                Console.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                success = false;
                Console.WriteLine(e.Message);
            }

            return success;
        }

        public void Shutdown()
        {
            if (this.sdbsocket != null)
            {
                this.sdbsocket.Shutdown();
            }
        }

        public void Close()
        {
            if (this.sdbsocket != null)
            {
                this.sdbsocket.Close();
                this.sdbsocket = null;
            }
        }

        private bool RunDaemon()
        {
            StartServerWatier waiter = new StartServerWatier();
            var sdbProcess = SDBLib.CreateSdbProcess(true, true);
            if (sdbProcess == null)
            {
                return false;
            }

            var task = SDBLib.RunSdbProcessAsync(sdbProcess,
                                                 "start-server",
                                                 false,
                                                 waiter);
            waiter.Waiter.WaitOne(SDBSocket.TimeOutStart);
            return true;
        }

        public SDBResponse Send(SDBRequest request)
        {
            SDBResponse response = new DebugBridge.SDBResponse();
            if (!this.sdbsocket.Write(request.Request))
            {
                return response;
            }

            byte[] reply = new byte[4];
            if (!this.sdbsocket.Read(reply))
            {
                return response;
            }

            response.IOSuccess = true;
            if (IsOkay(reply))
            {
                response.Okay = true;
            }
            else if (IsFail(reply))
            {
                response.Message = GetFailMessage();
                response.Okay = false;
            }
            else
            {
                response.Okay = false;
            }

            return response;
        }

        public bool ConnectionError()
        {
            return this.sdbsocket.ConnectionError();
        }

        public bool DataAvailable()
        {
            return this.sdbsocket.DataAvailable();
        }

        public string ReadData(byte[] data)
        {
            if (this.sdbsocket.Read(data, -1, SDBSocket.TimeOut))
            {
                return data.GetString(DefaultEncoding);
            }

            return String.Empty;
        }

        public int ReadLength()
        {
            byte[] buffer = new byte[4];    // 4 byte buffer for length
            string msg = ReadData(buffer);

            if (!String.IsNullOrEmpty(msg))
            {
                try
                {
                    int len = Int32.Parse(msg, System.Globalization
                                                     .NumberStyles
                                                     .HexNumber);
                    return len;
                }
                catch (FormatException e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            return -1;
        }

        private bool IsOkay(byte[] reply)
        {
            return reply.GetString().Equals("OKAY");
        }

        private bool IsFail(byte[] reply)
        {
            return reply.GetString().Equals("FAIL");
        }

        private string GetFailMessage()
        {
            int length;

            length = ReadLength();
            if (length > 0)
            {
                byte[] buffer = new byte[length];
                return ReadData(buffer);
            }

            return string.Empty;
        }

/*
        private string ResponseToString(byte[] response)
        {
            string result = String.Empty;

            try
            {
                result = Encoding.Default.GetString(response);
            }
            catch (DecoderFallbackException uee)
            {
                Console.WriteLine(uee.Message);
            }
            return result;
        }
*/

/*
        private int ResponseToInt32(byte[] response)
        {
            int result = -1;

            try
            {
                string strvalue = Encoding.Default.GetString(response);
                result = int.Parse(strvalue,
                                   System.Globalization.NumberStyles.HexNumber);
            }
            catch (DecoderFallbackException e)
            {
                Console.WriteLine(e.Message);
            }
            catch (FormatException e)
            {
                Console.WriteLine(e.Message);
            }

            return result;
        }
*/

/*
        private SDBResponse ReadSdbResponse(bool readDiagString)
        {
            SDBResponse resp = new SDBResponse();
            byte[] reply = new byte[4];

            if (!this.sdbsocket.Read(reply))
            {
                return resp;
            }

            resp.IOSuccess = true;

            if (IsOkay(reply))
            {
                resp.Okay = true;
            }
            else
            {
                readDiagString = true;
                resp.Okay = false;
            }

            while (readDiagString)
            {
                byte[] lenBuf = new byte[4];
                if (!this.sdbsocket.Read(lenBuf))
                {
                    break;
                }

                int len = ResponseToInt32(lenBuf);
                if (len <= 0)
                {
                    break;
                }

                byte[] msg = new byte[len];
                if (!this.sdbsocket.Read(msg))
                {
                    break;
                }

                resp.Message = ResponseToString(msg);
                break;
            }
            return resp;
        }
*/

        public static SDBRequest MakeRequest(string req)
        {
            string resultStr = String.Format("{0}{1}\n",
                                             req.Length.ToString("X4"),
                                             req);
            byte[] result;

            try
            {
                result = resultStr.GetBytes(DefaultEncoding);
            }
            catch (EncoderFallbackException e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

            System.Diagnostics.Debug.Assert(
                result.Length == req.Length + 5,
                String.Format("result: {1}{0}\nreq: {3}{2}",
                                result.Length,
                                result.GetString(DefaultEncoding),
                                req.Length,
                                req));

            return new SDBRequest(result);
        }
    }

    /// <summary>
    /// Byte to string parsing
    /// </summary>
    public static partial class ByteArrayExtenstions
    {
        public static string GetString(this byte[] bytes)
        {
            return GetString(bytes, Encoding.Default);
        }

        public static string GetString(this byte[] bytes, Encoding encoding)
        {
            return encoding.GetString(bytes, 0, bytes.Length);
        }

        public static string GetString(this byte[] bytes, string encoding)
        {
            Encoding enc = Encoding.GetEncoding(encoding);
            return GetString(bytes, enc);
        }

        public static string GetString(this byte[] bytes, int index, int count)
        {
            return GetString(bytes, index, count, Encoding.Default);
        }

        public static string GetString(this byte[] bytes, int index, int count, Encoding encoding)
        {
            return encoding.GetString(bytes, index, count);
        }

        public static string GetString(this byte[] bytes, int index, int count, string encoding)
        {
            Encoding enc = Encoding.GetEncoding(encoding);
            return GetString(bytes, index, count, enc);
        }

        public static byte[] GetBytes(this string str, string encoding)
        {
            Encoding enc = Encoding.GetEncoding(encoding);
            return enc.GetBytes(str);
        }
    }

    internal class StartServerWatier : TizenAutoWaiter
    {
        public override bool IsWaiterSet(string value)
        {
            if (value.Trim().Equals("* server started successfully *"))
            {
                return true;
            }

            return false;
        }

        public override void OnExit()
        {
        }
    }
}
