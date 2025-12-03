// See https://aka.ms/new-console-template for more information
using JsonRpc;
using System.Net;
using System.Runtime.CompilerServices;

static void log(String msg, LogSeverity severity)
{
    Console.Out.WriteLine(severity.ToString().ToUpper() + ": " + msg);
}


bool pinged = false;

var endPoint = new IPEndPoint(new IPAddress([127, 0, 0, 1]), 1234);
Client client = new Client(endPoint, "client");
client.Log += log;
client.ReceivedMsg += client.Send; // Loopback

Server server = new Server(1234, "server");
server.Log += log;
server.ClientConnected += (client) =>
{
    client.Log += log;
    client.ReceivedMsg += (msg) => { pinged = true; };
    client.Send("Heartbeat");
};

while (!pinged);

client.Dispose();
server.Dispose();
Console.Out.WriteLine("END");