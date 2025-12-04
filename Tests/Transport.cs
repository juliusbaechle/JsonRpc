using JsonRpc;
using System.Net;

namespace Tests
{
    [TestClass]
    public sealed class Transport
    {
        static void Log(String msg, LogSeverity severity)
        {
            Console.Out.WriteLine(severity.ToString().ToUpper() + ": " + msg);
        }

        [TestMethod]
        public void TestTransport()
        {
            bool pinged = false;

            var endPoint = new IPEndPoint(new IPAddress([127, 0, 0, 1]), 1234);
            IClient client = new Client(endPoint, "client");
            client.Log += Log;
            client.ReceivedMsg += client.Send; // Loopback

            IServer server = new Server(1234, "server");
            List<IClient> clients = [];
            server.Log += Log;
            server.ClientConnected += (client) =>
            {
                client.Log += Log;
                client.ReceivedMsg += (msg) => { pinged = true; };
                client.Send("Heartbeat");
                clients.Add(client);
            };

            while (!pinged) ;

            client.Dispose();
            server.Dispose();
            clients.ForEach(c => c.Dispose());
            Console.Out.WriteLine("END");
        }
    }
}
