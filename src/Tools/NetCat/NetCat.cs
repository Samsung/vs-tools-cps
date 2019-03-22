using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.IO;

namespace Tizen.VisualStudio
{
    public class NetCat
    {
        static void Run(string host, int port)
        {
            string logFile = Path.Combine(Path.GetTempPath(), "mi-log.txt");

            using (Socket remote = GetConnectedSocket(host, port))
            using (StreamWriter log = new StreamWriter(logFile))
            using (NetworkStream stream = new NetworkStream(remote))
            using (StreamReader reader = new StreamReader(stream))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                Task receiveTask = ReceiveLines(reader, log);
                string text;

                while ((text = Console.ReadLine()) != "")
                {
                    writer.WriteLine(text);
                    writer.Flush();
                    Log(log, "< ", text);
                }

                remote.Shutdown(SocketShutdown.Send);
                receiveTask.Wait();
            }
        }

        private static Socket GetConnectedSocket(string host, int port)
        {
            Socket remote = null;
            try
            {
                IPAddress[] ipAddresses = Dns.GetHostAddresses(host);
                foreach (IPAddress ipAddress in ipAddresses)
                {
                    if (ipAddress.AddressFamily != AddressFamily.InterNetwork)
                    {
                        continue;
                    }

                    remote = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    IPEndPoint remoteEndPoint = new IPEndPoint(ipAddress, port);
                    try
                    {
                        remote.Connect(remoteEndPoint);
                        break;
                    }
                    catch (SocketException e)
                    {
                        Console.Error.WriteLine(e.ToString());
                        Console.Error.WriteLine();
                        remote.Dispose();
                        remote = null; // prevent from disposing it twice
                    }
                }
            }
            catch (Exception)
            {
                remote?.Dispose();
                throw;
            }

            return remote;
        }

        private static async Task ReceiveLines(StreamReader reader, StreamWriter log)
        {
            string receiveText;

            while ((receiveText = await reader.ReadLineAsync()) != null)
            {
                Console.WriteLine(receiveText);
                Log(log, "> ", receiveText);
            }
            Environment.Exit(0);
        }

        private static void Log(StreamWriter log, string tag, string text)
        {
            lock (log)
            {
                log.WriteLine(tag + text);
                log.Flush();
            }
        }

        public static int Main(String[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("NetCat.exe <host> <port>");
                return 1;
            }
            Run(args[0], Int32.Parse(args[1]));
            return 0;
        }
    }
}
