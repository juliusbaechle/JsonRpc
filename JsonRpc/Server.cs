using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace JsonRpc
{
    public class Server : IServer, IDisposable
    {
        public Server(ushort a_port, String a_name)
        {
            m_name = a_name;
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_socket.Blocking = true;
            m_socket.Bind(new IPEndPoint(new IPAddress([127, 0, 0, 1]), a_port));
            m_socket.Listen(a_port);
            m_listenerThread = new(new ThreadStart(() => {
                Log?.Invoke("New TCP Socket '" + a_name + "' at localhost:" + a_port + " (Thread-Id " + m_listenerThread?.ManagedThreadId + ")", LogSeverity.Info);
                Listen();
            }));
            m_listenerThread.Start();
        }

        public void Dispose()
        {
            m_terminate = true;
            if (m_socket.Connected)
                m_socket.Shutdown(SocketShutdown.Both);
            m_socket.Close();
            m_listenerThread.Join();
        }

        public event IServer.ConnectHandler? ClientConnected;
        public event LogHandler? Log;

        void Listen() {
            m_listenerThread.Name = m_name + "-Thread";
            var task = m_socket.AcceptAsync();
            while (!m_terminate)
            {
                if (task.IsCompletedSuccessfully)
                {
                    OnNewClient(task.Result);
                    task = m_socket.AcceptAsync();
                }
                if (task.IsFaulted)
                {
                    task = m_socket.AcceptAsync();
                }
                Thread.Sleep(5);
            }
        }

        void OnNewClient(Socket socket) {
            Log?.Invoke("New client connected to '" + m_name + "' socket", LogSeverity.Info);
            var client = new Client(socket, m_name);
            ClientConnected?.Invoke(client);
            client.StartListening();
        }

        volatile bool m_terminate;
        Thread m_listenerThread;
        String m_address;
        ushort m_port;
        String m_name = "";
        Socket m_socket;
    }
}
