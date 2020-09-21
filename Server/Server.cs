using Include;
using Include.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static Include.Client;
using static Magnus.MessageResult;
using NetJ = Include.Json;
using SocketType = Include.SocketType;

namespace Magnus {
    // create server to listen to and manage incoming headset connections
    public class Server {
        public delegate void OnTCP(byte[] data);
        public delegate void OnUDP(byte[] data);
        public OnTCP OnTCPListener { get; set; }
        public OnUDP OnUDPListener { get; set; }

        public delegate void OnReceive(string clientId, SocketType socketType, DataType dataType, object data);
        public OnReceive OnReceiveListener { get; set; }

        public delegate void OnConnectedToRobot(string robotId);
        public OnConnectedToRobot OnConnectedToRobotListener;

        public IPEndPoint[] EndPoints { get; private set; }
        public int Port { get; private set; }

        public TcpListener TCP { get; private set; }
        public string[] Addresses { get; private set; }

        ConcurrentDictionary<string, Client> clients { get; set; } = new ConcurrentDictionary<string, Client>();
        public string[] Clients { get { return clients.Keys.ToArray(); } }

        ManualResetEvent tcpClientConnected = new ManualResetEvent(false);

        private bool running;

        // call this to create a server object
        // server should be used to listen to incoming connections
        // use first message to build library of devices
        // on receiving a message, add a new message onto message stack and trigger a callback
        public Server(string address = null, int port = 0) {
            Log.SetFileName("There");
            IPEndPoint endpoint = new IPEndPoint(address == null ? IPAddress.Any : IPAddress.Parse(address), 0);
            if (port != 0) endpoint.Port = port;
            TCP = new TcpListener(endpoint);
            //udp = new UdpClient();
        }

        /// <summary>
        /// destroys the server
        /// </summary>
        ~Server() {
            End();
        }

        // call this to begin listening
        Thread acceptClientsThread;
        public void Begin() {
            running = true;
            TCP.Start();

            Port = ((IPEndPoint)TCP.LocalEndpoint).Port;

            EndPoints = Dns.GetHostAddresses(Dns.GetHostName())
                .Where(x => x.AddressFamily == AddressFamily.InterNetwork)
                .Select(x => new IPEndPoint(x, Port))
                .ToArray();

            Log.D($"Server: bound to port {((IPEndPoint)TCP.LocalEndpoint).Port}");
            Addresses = Dns.GetHostAddresses(Dns.GetHostName()).Where(x => x.AddressFamily == AddressFamily.InterNetwork).Select(x => x.ToString()).ToArray();
            Log.D($"Server: awaiting connections on {string.Join(", ", Addresses)}");

            acceptClientsThread = new Thread(new ThreadStart(AcceptClients));
            acceptClientsThread.Start();
        }

        public void End() {
            running = false;
            acceptClientsThread.Join();
            TCP.Stop();

            foreach (var client in clients.Values) {
                client.Send(new Message() { type = MsgType.Disconnect });
            }
            clients.Clear();
        }

        #region Client Connection
        void AcceptClients() {
            while (running) {
                try {
                    // Set the event to nonsignaled state.
                    tcpClientConnected.Reset();

                    // Start to listen for connections from a client.
                    Log.D($"Server: waiting for new connection");

                    // Accept the connection. 
                    // BeginAcceptSocket() creates the accepted socket.
                    TCP.BeginAcceptTcpClient(new AsyncCallback(AcceptCallback), TCP);

                    // Wait until a connection is made and processed before 
                    // continuing.
                    tcpClientConnected.WaitOne();

                } catch (Exception e) {
                    Log.D("Server: server listener terminated or something went wrong");
                    Log.D(e.ToString());
                }
            }
        }

        void AcceptCallback(IAsyncResult ar) {
            // Get the listener that handles the client request.
            TcpListener listener = (TcpListener)ar.AsyncState;

            // End the operation and display the received data on 
            // the console.
            TcpClient client = listener.EndAcceptTcpClient(ar);

            ServerClient deviceClient = new ServerClient(client, OnClientInitialConnect);

            // Process the connection here. (Add the client to a
            // server table, read data, etc.)
            Log.D($"Server: client connected");

            // Signal the calling thread to continue.
            tcpClientConnected.Set();
        }

        void OnClientInitialConnect(Client client, ServerClient.InitialConnectStatus result) {
            if (result != ServerClient.InitialConnectStatus.Success) {
                Log.D($"Server: client failed to be registered due to {result}");
                return;
            }
            if (client.id != null) {
                string err = "";
                if (clients.ContainsKey(client.id)) {
                    Log.D($"Server: client({client.id}) already exists, disconnecting old client");
                    if (clients.TryRemove(client.id, out Client oldClient)) {
                        oldClient.End();
                        err = "client with same id was removed to connect this one";
                    } else {
                        Log.D($"Server: failed to remove client({client.id})");
                        client.Send(new MessageResult() { callingType = MsgType.Initialise, result = Result.Failure, error = "server failed to remove old client with same name" });
                    }
                }
                if (clients.TryAdd(client.id, client)) {
                    Log.D($"Server: client({client.id}) added successfully");
                    //client.OnReceiveListener += DebugListener(client.id);
                    client.OnReceiveListener += DisconnectionLogic(client.id);
                    client.OnReceiveListener += (type, protocol, data) => {
                        OnReceiveListener?.Invoke(client.id, type, protocol, data);
                    };
                    client.OnDisconnectListener += () => { DisconnectClient(client.id); };
                    client.Send(new MessageResult() {callingType = MsgType.Initialise , result = Result.Success, error = err });

                } else {
                    Log.D($"Server: failed to add client({client.id})");
                    client.Send(new MessageResult() { callingType = MsgType.Initialise, result = Result.Failure, error = "server failed to add client" });
                }
            }
        }

        Include.Client.OnReceive DebugListener(string clientId) {
            return (type, protocol, data) => {
                string msg = "No Message";
                switch (protocol) {
                    case DataType.Error:
                        msg = (string)data;
                        break;
                    case DataType.Undefined:
                        break;
                    case DataType.Bytes:
                        msg = BitConverter.ToString((byte[])data).Replace("-", "");
                        break;
                    case DataType.RawString:
                        msg = (string)data;
                        break;
                    case DataType.JSON:
                        msg = ((NetJ.Message)data).message;
                        break;
                }
                Log.D($"Server: got message from {clientId}: {msg}");
            };
        }

        Include.Client.OnReceive DisconnectionLogic(string clientId) {
            return (type, protocol, data) => {
                {
                    if (NetJ.Message.TryCast(protocol, data, (int)MsgType.Disconnect, out Message _)) {
                        DisconnectClient(clientId);
                    }
                }
            };
        }
        #endregion

        void DisconnectClient(string clientId) {
            if (clients.TryRemove(clientId, out Client client)) {
                client.End();
            }
        }

        #region Device Coms
        public void SendToAllClients(object data, Include.SocketType socketType = Include.SocketType.TCP, DataType dataType = DataType.JSON) {
            foreach (Client client in clients.Values) {
                client.Send(data, socketType, dataType);
            }
        }

        public void SendToClient(string clientId, object data, Include.SocketType socketType = Include.SocketType.TCP, DataType dataType = DataType.JSON) {
            if (clients.TryGetValue(clientId, out Client client)) {
                client.Send(data, socketType, dataType);
            }
        }
        #endregion
    }
}
