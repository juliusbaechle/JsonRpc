using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;

namespace JsonRpc
{
    internal class Client : IClient, IDisposable
    {
        public Client(EndPoint a_endPoint, String a_name)
        {
            m_endPoint = a_endPoint;
            m_name = a_name;
            m_client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            SetupSocket(m_client);
            StartListening();
        }

        public Client(Socket a_socket, String a_name)
        {
            m_name = a_name;
            m_client = a_socket;
            m_endPoint = a_socket.RemoteEndPoint;
            SetupSocket(m_client);
            Send("name: " + m_name);
        }

        static void SetupSocket(Socket socket)
        {
            socket.Blocking = true;
            socket.ReceiveTimeout = 0;
            socket.SendTimeout = 0;
        }

        public void StartListening()
        {
            m_listeningThread = new(new ThreadStart(() =>
            {
                m_listeningThread?.Name = m_name + "-Thread";
                Log?.Invoke("New TCP Socket '" + m_name + "' at " + m_endPoint.ToString() + " (Thread-Id " + m_listeningThread?.ManagedThreadId + ")", LogSeverity.Info);
                Run();
            }));
            m_listeningThread.Start();
        }

        public void Dispose()
        {
            m_terminate = true;
            m_client.Shutdown(SocketShutdown.Both);
            m_listeningThread.Join();
            m_client.Dispose();
        }

        public ulong ClientId { get { return m_clientId; } }
        public string PeerName { get { return m_peerName; } }
        public bool Connected { get { return m_client.Connected; } }

        public event IClient.ConnectionStatusHandler? ConnectionStatusChanged;
        public event IClient.MsgHandler? ReceivedMsg;
        public event LogHandler? Log;

        public void Send(string a_msg)
        {
            var msg = a_msg + '\0';
            var messageBytes = Encoding.UTF8.GetBytes(msg);
            if (!m_client.Connected)
            {
                Log?.Invoke("Sending to disconnected socket", LogSeverity.Error);
                return;
            }

            var bytesSent = m_client.Send(messageBytes, SocketFlags.None);
            if (bytesSent == msg.Length)
            {
                Log?.Invoke("Sent to '" + m_peerName + "': " + msg, LogSeverity.Debug);
            }
            else if (bytesSent == 0)
            {
                Log?.Invoke("'" + m_name + "' socket to '" + m_peerName + "' disconnected", LogSeverity.Info);
                ConnectionStatusChanged?.Invoke(false);
            } else
            {
                Log?.Invoke("'" + m_name + "' socket failed to send '" + msg + "' to '" + m_peerName + "'", LogSeverity.Error);
            }
        }

        void Run()
        {
            while (!m_terminate)
            {
                if (m_client.Connected)
                {
                    ConnectedState();
                } else
                {
                    NotConnectedState();
                }
            }
        }

        void ConnectedState()
        {
            var buffer = new byte[1024];
            var task = m_client.ReceiveAsync(buffer);

            while (m_client.Connected && !m_terminate)
            {
                if (task.IsCompletedSuccessfully)
                {
                    Parse(buffer, task.Result);
                }
                if (task.IsCompleted)
                {
                    task = m_client.ReceiveAsync(buffer);
                }
                Thread.Sleep(5);
            }

            // Log?.Invoke("'" + m_name + "' socket to '" + m_peerName + "' disconnected", LogSeverity.Info);
            ConnectionStatusChanged?.Invoke(false);
        }

        void NotConnectedState()
        {
            Log?.Invoke("'" + m_name + "' socket connecting ...", LogSeverity.Info);
            
            do {
                var task = m_client.ConnectAsync(m_endPoint);
                if (task.IsFaulted)
                {
                    task = m_client.ConnectAsync(m_endPoint);
                }
                Thread.Sleep(5);
            } while (!m_client.Connected && !m_terminate);
            
            Log?.Invoke("'" + m_name + "' socket connected", LogSeverity.Info);
            ConnectionStatusChanged?.Invoke(true);
            Send("name: " + m_name);
        }

        void Parse(byte[] buffer, int bytesReceived)
        {
            if (bytesReceived <= 0 || buffer == null) 
                return;

            var begin = 0;
            var snippet = Encoding.UTF8.GetString(buffer, 0, bytesReceived);

            while (begin < bytesReceived)
            {
                var end = snippet.IndexOf('\0', begin);
                if (end >= 0)
                {
                    m_readBuffer += snippet.Substring(begin, end - begin);
                    OnMsgReceived(m_readBuffer);
                    m_readBuffer = "";
                    begin = end + 1;
                } else
                {
                    m_readBuffer += snippet.Substring(begin, bytesReceived - begin);
                }
            }
        }

        void OnMsgReceived(String a_msg)
        {
            if (a_msg == "")
                return;
            Log?.Invoke("Received from '" + m_peerName + "': " + a_msg, LogSeverity.Debug);
            if (a_msg.StartsWith("name: "))
            {
                m_peerName = a_msg.Substring(6);
            } else
            {
                ReceivedMsg?.Invoke(a_msg);
            }
        }

        Socket m_client;
        ulong m_clientId = 0;
        String m_peerName = "";
        String m_name = "";
        String m_readBuffer = "";
        EndPoint m_endPoint;
        volatile bool m_terminate = false;
        Thread m_listeningThread;
    }
}
