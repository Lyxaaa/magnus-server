using Magnus;
using System;
using System.Threading;

namespace Test {
    class Program {
        static void Main(string[] args) {
            string ip = "192.168.1.42";
            int port = 2457;

            TestClient client = new TestClient(ip, port);

            // filter only for jsons
            client.FilterDataType(Include.DataType.Bytes, true);
            client.FilterDataType(Include.DataType.Error, true);
            client.FilterDataType(Include.DataType.RawString, true);
            client.FilterDataType(Include.DataType.Undefined, true);

            // ignore the heartbeat and acks
            //client.FilterMessageType(MsgType.Heartbeat, true);
            //client.FilterMessageType(MsgType.Ack, true);

            Thread.Sleep(1000);

            client.Send(new Login() {email = "", password = "" });

            Thread.Sleep(1000);
            
            Console.ReadKey();
        }
    }
}
