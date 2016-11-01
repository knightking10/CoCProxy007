/*
 * Monecraft Proxy C#
 * 
 * Module Name : Proxy.cs
 * 
 * Author : Moien007
 * 
 * Desc : Redirect Clients Connection to Server
 */

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


    public class SocketProxy
    {
        /// <summary>
        /// Get Proxy Socket 
        /// </summary>
        public Socket Server { get; private set; }

        /// <summary>
        /// Get or Set Host End Point
        /// </summary>
        public IPEndPoint HostEndPoint { get; set; }

        /// <summary>
        /// Proxy IP End Point
        /// </summary>
        public IPEndPoint ProxyEndPoint { get; set; }

        /// <summary>
        /// Proxy Protocol Type
        /// </summary>
        public ProtocolType ProxyProtocolType { get; set; }


        /// <summary>
        /// Proxy Socket Type
        /// </summary>
        public SocketType _SocketType { get; set; }

        /// <summary>
        /// Buffer Size
        /// </summary>
        public int BufferSize { get; set; }

        /// <summary>
        /// Backlog
        /// </summary>
        public int Backlog { get; set; }


        /// <summary>
        /// Create New Instance of Proxy
        /// </summary>
        /// <param name="HostEP">The server IP Address</param>
        /// <param name="ProxyEP">Proxy endpoint</param>
        /// <param name="HostProtoType"></param>
        /// <param name="ProxyProtoType"></param>
        public SocketProxy(IPEndPoint HostEP, IPEndPoint ProxyEP, ProtocolType HostProtoType, ProtocolType ProxyProtoType, SocketType sockettype = SocketType.Stream)
        {
            HostEndPoint = HostEP;
            ProxyEndPoint = ProxyEP;
            ProxyProtocolType = ProxyProtoType;

            OnClientConnected = null;
            OnAsyncWithClient = null;
            OnSend = null;
            OnReceive = null;

            _SocketType = sockettype;

            BufferSize = 1024;

            Backlog = 50;

            Server = null;
        }

        /// <summary>
        /// Start Proxy
        /// </summary>
        /// <param name="backlog"></param>
        public void Start()
        {
            if (Server != null)
            {
                return;
            }

            // create proxy socket
            Server = new Socket(AddressFamily.InterNetwork, _SocketType, ProxyProtocolType);

            // bind it
            Server.Bind(ProxyEndPoint);

            // start proxy
            Server.Listen(Backlog);

            ManualResetEvent waitEvent = new ManualResetEvent(true);

            while (Server != null & Server.IsBound)
            {
                waitEvent.Reset();

                Server.BeginAccept(ar =>
                {
                    waitEvent.Set();

                    // get remote socket of connected client
                    Socket serverSocket = (Socket)ar.AsyncState;
                    Socket clientSocket = serverSocket.EndAccept(ar);

                    if (OnAsyncWithClient != null)
                    {
                        OnAsyncWithClient(this, clientSocket);
                    }

                    // handle client and redirect client sends to host
                    HandleClient(clientSocket);

                }, Server);

                waitEvent.WaitOne();
            }
        }

        private void HandleClient(Socket client)
        {
            // create remote socket to server
            Socket server = new Socket(client.AddressFamily, client.SocketType, client.ProtocolType);

            // connect it server
            server.Connect(HostEndPoint);

            ProxyClient proxyClient;
            proxyClient.Client = client;
            proxyClient.Server = server;

            if (OnClientConnected != null)
            {
                OnClientConnected(this, proxyClient);
            }

            NetworkStream serverStream = new NetworkStream(server);
            NetworkStream clientStream = new NetworkStream(client);

            #region Proxy Server to Client
            new Task(() =>
            {
                while (true)
                {
                    try
                    {
                        byte[] buffer = new byte[BufferSize];
                        

                        // server byte = length of received data
                        int serverBytes = serverStream.Read(buffer, 0, BufferSize);

                        // send it to client
                        clientStream.Write(buffer, 0, serverBytes);

                        if (serverBytes == 0)
                        {
                            break;
                        }

                        if(OnReceive != null)
                        {
                            OnReceive(this, proxyClient, buffer, serverBytes);
                        }
                    }
                    catch (Exception) { break; }
                }

                // and don't forget start it
            }).Start();
            #endregion

            #region Proxy Client to Server
            new Task(() =>
            {

                while (true)
                {
                    try
                    {
                        byte[] buffer = new byte[BufferSize];

                        int clientBytes = clientStream.Read(buffer, 0, BufferSize);

                        if (clientBytes == 0)
                        {
                            // disconnected
                            break;
                        }

                        serverStream.Write(buffer, 0, clientBytes);

                        if(OnSend != null)
                        {
                            OnSend(this, proxyClient, buffer, clientBytes);
                        }
                    }
                    catch (Exception) { break; }
                }

            }).Start();
            #endregion

        }

        public struct ProxyClient
        {
            public Socket Client;
            public Socket Server;
        }

        public delegate void ClientConnected(object sender, ProxyClient client);
        public delegate void AsyncWithClient(object sender, Socket socket);
        public delegate void Send(object sender, ProxyClient client, byte[] buffer, int length);
        public delegate void Receive(object sender, ProxyClient client, byte[] buffer, int length);

        public event ClientConnected OnClientConnected;
        public event AsyncWithClient OnAsyncWithClient;
        public event Send OnSend;
        public event Receive OnReceive;

    }

