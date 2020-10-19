using Include;
using Include.Util;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using I = Include.Json;

namespace Magnus {
    public abstract class Client : Include.Client {
        public string id { get; internal set; }
        public const int defaultBroadcastPort = 29262;
        public static int portRange { get; internal set; } = 16;

        public void Send(object data, Include.SocketType socketType = Include.SocketType.TCP, DataType dataType = DataType.JSON) {
            byte[] datata;

            switch (dataType) {
                case DataType.Bytes:
                    datata = (byte[])data;
                    break;
                case DataType.Error:
                case DataType.RawString:
                    datata = Encoding.ASCII.GetBytes((string)data);
                    break;
                case DataType.JSON:
                    datata = ((Message)data).GetBytes();
                    break;
                default:
                    datata = new byte[0];
                    break;
            }
            Send(socketType, dataType, datata);
        }

        long ping = 0;
        long lastHeartBeatAck = 0;
        int heartBeatInterval = 1000;
        Timer heartBeat;

        // sends a new heartbeat every {heartBeatInterval}ms
        protected void StartHeartBeat() {
            heartBeat = new Timer() {
                AutoReset = true,
                Enabled = true,
                Interval = heartBeatInterval
            };

            heartBeat.Elapsed += (sender, args) => {
                Send(new Message() { type = MsgType.Heartbeat });
            };
            heartBeat.Start();
        }

        protected void HeartBeatAck(Include.SocketType socketType, DataType dataType, object data) {
            if (I.Message.TryCast(dataType, data, (int)MsgType.Heartbeat, out Message hb)) {
                Send(new Ack() { acknowledging = hb.type, oldTimeStamp = hb.timestamp });
            }

            if (I.Message.TryCast(dataType, data, (int)MsgType.Ack, out Ack ack)) {
                lastHeartBeatAck = ack.oldTimeStamp; // we use the old timestamp because that's ours, the server could be running on a different time
                ping = Net.Util.GetTimeMillis() - ack.oldTimeStamp;
            }
        }
    }

    public class ServerClient : Client {
        public enum InitialConnectStatus {
            Success,
            Failure,
            Timeout
        }
        public delegate void OnInitialConnect(Client client, InitialConnectStatus status);
        public OnInitialConnect OnIntialConnectListener;

        public ServerClient(TcpClient client, OnInitialConnect listener) {
            OnIntialConnectListener += listener;
            Timer timer = new Timer(5000);
            timer.Start();
            timer.Elapsed += (object source, ElapsedEventArgs e) => {
                OnIntialConnectListener?.Invoke(this, InitialConnectStatus.Timeout);
                OnIntialConnectListener = null;
                timer.Stop();
            };

            OnReceive initial = (type, protocol, data) => {
                timer.Stop();
                if (I.Message.TryCast(protocol, data, (int)MsgType.Initialise, out Initialise init)) {
                    id = init.id;
                    OnIntialConnectListener?.Invoke(this, InitialConnectStatus.Success);
                    OnIntialConnectListener = null;
                    OnReceiveListener += HeartBeatAck;
                    StartHeartBeat();

                    tcp.OnSocketClosedListener += () => { OnDisconnectListener?.Invoke(); };

                } else {
                    OnIntialConnectListener?.Invoke(this, InitialConnectStatus.Failure);
                    OnIntialConnectListener = null;
                }
            };

            OnReceiveListener += initial;
            OnReceiveListener += DebugListener(id);

            tcp = new TCPSocket(client);
            tcp.OnReceiveListener += OnTCP;
            tcp.OnSocketClosedListener += () => { OnDisconnectListener?.Invoke(); };
            tcp.Begin();
        }

        OnReceive DebugListener(string clientId) {
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
                        msg = ((I.Message)data).message;
                        break;
                }
                if (protocol == DataType.JSON && ((I.Message)data).type == (int)MsgType.Heartbeat) return;
                Log.D($"{clientId}: got message from {clientId}: {msg}");
            };
        }
    }

    public class TestClient : Client {
        public TestClient(string remoteAddress, int port) {
            id = Guid.NewGuid().ToString();
            OnReceiveListener += DebugListener(id);

            tcp = new TCPSocket(remoteAddress, port);
            tcp.OnReceiveListener += OnTCP;
            tcp.OnSocketClosedListener += () => { OnDisconnectListener?.Invoke(); };
            tcp.Begin();

            Send(new Initialise() { id = id });
        }

        List<DataType> ignoredDataTypes = new List<DataType>();

        public void FilterDataType(DataType type, bool ignore) {
            if (ignore && !ignoredDataTypes.Contains(type)) {
                ignoredDataTypes.Add(type);
            } else if (!ignore && ignoredDataTypes.Contains(type)) {
                ignoredDataTypes.Remove(type);
            }
        }

        List<int> ignoredMessageTypes = new List<int>();

        public void FilterMessageType(MsgType type, bool ignore) {
            if(ignore && !ignoredMessageTypes.Contains((int)type)) {
                ignoredMessageTypes.Add((int)type);
            } else if(!ignore && ignoredMessageTypes.Contains((int)type)) {
                ignoredMessageTypes.Remove((int)type);
            }
        }

        OnReceive DebugListener(string clientId) {
            return (type, protocol, data) => {
                string msg = "No Message";

                if (ignoredDataTypes.Contains(protocol)) return;

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
                        msg = ((I.Message)data).message;
                        if (ignoredMessageTypes.Contains(((I.Message)data).type)) return;
                        break;
                }
                Log.D($"Client: got message from {clientId}: {msg}");
            };
        }
    }
}

