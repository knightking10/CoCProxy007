using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace CoCProxy007
{
    class Program
    {
        public const int CoCPort = 9339; // clash of clans server port
        public const string CoCServerAddress = "gamea.clashofclans.com"; // offical clash of clans server address
        public const ProtocolType CoCProtocolType = ProtocolType.Tcp; // clash of clans protocol type
        public const string PacketsFolder = "PacketDumps";

        static void Main(string[] args)
        {
            Console.Title = "CoCProxy";

            Console.WriteLine("Clash of Clans Proxy by Moien007");

            // clash of clans server end point
            IPEndPoint server = new IPEndPoint(Utils.ParseIP(CoCServerAddress), CoCPort);

            // proxy endpoint 
            IPEndPoint proxy = new IPEndPoint(IPAddress.Any, CoCPort);


            // configure proxy
            SocketProxy cocProxy = new SocketProxy(server, proxy, CoCProtocolType, CoCProtocolType);

            cocProxy.OnAsyncWithClient += cocProxy_OnAsyncWithClient;
            cocProxy.OnClientConnected += cocProxy_OnClientConnected;
            cocProxy.OnReceive += cocProxy_OnReceive;
            cocProxy.OnSend += cocProxy_OnSend;

            // start proxy
            Thread proxyThread = new Thread(new ThreadStart(cocProxy.Start));
            proxyThread.IsBackground = true;

            proxyThread.Start();

            Console.WriteLine("Proxy Started on {0}:{1}", Utils.GetLocalIPAddress(), CoCPort);

            Console.WriteLine("Clients will redirected to {0}", server);

            // main loop
            while(true)
            {
                Thread.Sleep(1);
            }
        }

        static void cocProxy_OnSend(object sender, SocketProxy.ProxyClient clinet, byte[] buffer, int length)
        {
            LogPacket(false, buffer, length);
        }

        static void cocProxy_OnReceive(object sender, SocketProxy.ProxyClient client, byte[] buffer, int length)
        {
            LogPacket(true, buffer, length);
        }

        static void cocProxy_OnClientConnected(object sender, SocketProxy.ProxyClient client)
        {
            //Console.WriteLine("{0} Are Now Redirect to Server by {1}", client.Client.RemoteEndPoint, client.Server.RemoteEndPoint);
        }

        static void cocProxy_OnAsyncWithClient(object sender, Socket socket)
        {
            Console.WriteLine("Client Connected From {0}", socket.RemoteEndPoint);
        }

        static void LogPacket(bool fromServer, byte[] buffer, int bufferLength)
        {
            // get packet id
            ushort packetId = PacketParser.GetPacketID(buffer, bufferLength);

            // create packet file name
            string packetFileName = PacketsFolder + "\\" + string.Format("[{0}][{1}] {2}.packet", fromServer ? "S" : "C", DateTime.Now.ToString("hh.mm.ss.fff"), packetId);

            // write packet info to console
            Console.WriteLine("[S {0} C]  PacketID:{1} Length:{2}", fromServer ? "=>" : "<=", packetId, bufferLength);

            // create packet dumps directory when it does not exist
            if (!Directory.Exists(PacketsFolder))
                Directory.CreateDirectory(PacketsFolder);

            // create and replace packet file
            using(FileStream fileStream = File.Open(packetFileName, FileMode.Create, FileAccess.Write))
            {
                // write buffer
                fileStream.Write(buffer, 0, bufferLength);
            }
        }
    }
}
