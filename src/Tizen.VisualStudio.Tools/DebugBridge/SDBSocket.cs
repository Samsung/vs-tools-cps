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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Tizen.VisualStudio.Tools.DebugBridge
{
    public class SDBSocket
    {
        public const int TimeOut = 0;//Infinite

        private Socket socket = null;
        private int id = 0;

        public bool NoDelay
        {
            get { return this.socket.NoDelay; }
            set { this.socket.NoDelay = value; }
        }

        public bool Connected
        {
            get
            {
                if (this.socket == null)
                {
                    return false;
                }

                return this.socket.Connected;
            }
        }

        private void InitSocket(AddressFamily addressFamily,
                                  SocketType socketType,
                                  ProtocolType protocolType)
        {
            this.socket = new Socket(addressFamily, socketType, protocolType);
        }

        public SDBSocket()
        {
            InitSocket(AddressFamily.InterNetwork,
                       SocketType.Stream,
                       ProtocolType.Tcp);
        }

        public SDBSocket(AddressFamily addressFamily,
                         SocketType socketType,
                         ProtocolType protocolType)
        {
            InitSocket(addressFamily, socketType, protocolType);
        }


        public void Connect(EndPoint remoteEP)
        {
            this.socket.Connect(remoteEP);
        }

        public void Connect(IPAddress ip, int port)
        {
            Connect(new IPEndPoint(ip, port));
            id = ((IPEndPoint)this.socket.LocalEndPoint).Port;
            Debug.WriteLine("{0} SDBSocket({1}) connect to port {2}", DateTime.Now, id, port);
        }

        public void Close()
        {
            if (this.socket != null)
            {
                Debug.WriteLine("{0} SDBSocket({1}) close", DateTime.Now, id);
                this.socket.Close();
                this.socket.Dispose();
                this.socket = null;
            }
        }

        public void Shutdown()
        {
            if (this.socket != null)
            {
                try
                {
                    Debug.WriteLine("{0} SDBSocket({1}) shutdown", DateTime.Now, id);
                    this.socket.Shutdown(SocketShutdown.Both);
                }
                catch (ObjectDisposedException e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        public bool ConnectionError()
        {
            return this.socket.Poll(0, SelectMode.SelectError);
        }

        public bool DataAvailable()
        {
            return this.socket.Poll(TimeOut * 1000, SelectMode.SelectRead);
        }

        public void Dispose()
        {
            if (this.socket != null)
            {
                this.socket.Dispose();
            }
        }

        public bool Write(byte[] data)
        {
            bool success = true;

            try
            {
                success = Write(data, -1, TimeOut);
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
                success = false;
            }

            return success;
        }

        public string LogData(byte[] data)
        {
            if (data.Length <= 0)
            {
                return $"";
            }
            string str = $"";
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] > 31 && data[i] < 127)
                {
                    str += System.Text.Encoding.UTF8.GetString(data, i, 1);
                }
                else
                {
                    str += String.Format("'0x{0:X}'", data[i]);
                }
            }
            return str;
        }

        public bool Write(byte[] data, int length, int timeout)
        {
            int count = -1;
            bool success = true;
            Debug.WriteLine("{0} SDBSocket({1}) write: {2}", DateTime.Now, id, LogData(data));

            if (this.socket == null)
            {
                return false;
            }

            try
            {
                this.socket.SendTimeout = timeout;
                count = this.socket.Send(data,
                                         0,
                                         length != -1 ? length  : data.Length,
                                         SocketFlags.None);
                if (count < 0)
                {
                    success = false;
                }
            }
            catch (SocketException e)
            {
                success = false;
                Console.WriteLine(e.Message);
            }
            catch (ObjectDisposedException e)
            {
                // connection lost
                success = false;
                Console.WriteLine(e.Message);
            }

            return success;
        }

        public bool Read(byte[] data)
        {
            bool success = true;
            try
            {
                success = Read(data, -1, TimeOut);
            }
            catch (Exception e)
            {
                success = false;
                Console.WriteLine(e.Message);
            }

            return success;
        }

        public bool Read(byte[] data, int length, int timeout)
        {
            int expLen = length != -1 ? length : data.Length;
            int count = -1;
            int totalRead = 0;
            bool success = true;

            if (this.socket == null)
            {
                return false;
            }

            while (success && count != 0 && totalRead < expLen)
            {
                try
                {
                    int left = expLen - totalRead;
                    int buflen = left < socket.ReceiveBufferSize ?
                                    left :
                                    socket.ReceiveBufferSize;
                    byte[] buffer = new byte[buflen];

                    this.socket.ReceiveTimeout = timeout;
                    this.socket.ReceiveBufferSize = expLen;
                    count = this.socket.Receive(buffer, buflen, SocketFlags.None);
                    if (count < 0)
                    {
                        success = false;
                    }
                    else if (count > 0)
                    {
                        Array.Copy(buffer, 0, data, totalRead, count);
                        totalRead += count;
                    }
                }
                catch (SocketException e)
                {
                    success = false;
                    Console.WriteLine(e.Message);
                }
            }

            return success;
        }
    }
}
