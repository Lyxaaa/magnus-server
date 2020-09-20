using DatabaseConnection;
using System;
using Msg = Include.Json.Message;

namespace Magnus {
    class Program {

        static object lockObj = new object();

        static void Main(string[] args) {
            var database = new Database();
            var server = new Server(null, 2457);

            server.OnReceiveListener += (clientId, socketType, dataType, data) =>
            {
                #region Login
                if (Msg.TryCast(dataType, data, (int)MsgType.Login, out Login login))
                {
                    var result = database.GetSelectUserProfile(login.email);

                    if (result.Item1 != login.email)
                    {
                        server.SendToClient(clientId, new LoginResult()
                        {
                            result = Result.Failure,
                            error = "invalid email or password"
                        });
                    }
                    else if (result.Item2 == login.password)
                    {
                        server.SendToClient(clientId, new LoginResult()
                        {
                            result = Result.Success,
                            userName = result.Item3,
                            bio = result.Item4
                            //profile = result.Item5 // < this is a file directory, convert this into bytes and then send it
                        });
                    }
                    else
                    {
                        server.SendToClient(clientId, new LoginResult()
                        {
                            result = Result.Failure,
                            error = "invalid password or email"
                        });
                    }
                }
                #endregion
            };

            server.Begin();
            
            Console.ReadKey();
            server.End();
        }
    }
}
