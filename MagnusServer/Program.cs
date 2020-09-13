using DatabaseConnection;
using System;

namespace Magnus {
    class Program {
        static void Main(string[] args) {
            var mydatabase = new Database();
            var server = new Server(null, 12345);
            server.Begin();



            //Console.ReadKey();
            //server.End();
        }
    }
}
