using Magnus;
using System;
using System.Threading;
using System.Drawing;
using System.IO;

namespace Test {
    class Program {
        static void Main(string[] args) {
            string ip = "127.0.0.1";
            // others string ip = "192.168.1.42";
            int port = 2457;

            TestClient client = new TestClient(ip, port);

            // filter only for jsons
            client.FilterDataType(Include.DataType.Bytes, true);
            client.FilterDataType(Include.DataType.Error, true);
            client.FilterDataType(Include.DataType.RawString, true);
            client.FilterDataType(Include.DataType.Undefined, true);

            // ignore the heartbeat and acks
            client.FilterMessageType(MsgType.Heartbeat, true);
            client.FilterMessageType(MsgType.Ack, true);

            Thread.Sleep(1000);
            System.Console.WriteLine("add friend");
            client.Send(new AcceptFriend() { fromEmail = "oscarmahon@hotmail.com", toEmail = "user2" });

            //Image img = Image.FromFile("C:\\images\\Mark.jpg");
            //img.Save("C:\\images\\testResult1.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
            //byte[] bitmap = imgToByteArray(img);
            //System.Console.WriteLine(bitmap.ToString());

            //System.Console.WriteLine("login");
            //client.Send(new Login() {email = "markbirdy92@gmail.com", password = "password" });


            Thread.Sleep(1000);

            //System.Console.WriteLine("update");
            //client.Send(new UpdateUserProfile() { email = "markbirdy92@gmail.com", name = "Mark", bio = "lol lol", profile = null });



            Thread.Sleep(1000);
            //System.Console.WriteLine("update2");
            //client.Send(new UpdateUserProfile() { email = "markbirdy92@gmail.com", name = "Mark", bio = "lol", profile = bitmap });
            //System.Console.WriteLine("RegisterUser");
            //client.Send(new RegisterUser() { email = "thisisatest", password = "1234", name = "Mark", bio = "lol"});


            Console.ReadKey();
        }
        //convert image to bytearray
        public static byte[] imgToByteArray(Image img)
        {
            using (MemoryStream mStream = new MemoryStream())
            {
                img.Save(mStream, img.RawFormat);
                return mStream.ToArray();
            }
        }
    }
}
