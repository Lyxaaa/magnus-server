using DatabaseConnection;
using Include;
using System;
using Msg = Include.Json.Message;

namespace Magnus {
    class Program {
        static void Main(string[] args) {
            var mydatabase = new Database();
            var server = new Server(null, 12345);

            server.OnReceiveListener += (clientId, socketType, dataType, data) => {
                if (Msg.TryCast(dataType, data, (int)MsgType.Disconnect, out Message _)) {

                }
            };

            server.Begin();

            //Console.ReadKey();
            //server.End();
        }
    }
}
