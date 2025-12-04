using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml.Schema;

namespace JsonRpc
{
    public interface IClient : IDisposable
    {
        ulong ClientId { get; }
        String PeerName { get; }

        bool Connected { get; }
        delegate void ConnectionStatusHandler(bool connected);
        event ConnectionStatusHandler ConnectionStatusChanged;

        void Send(String msg);

        delegate void MsgHandler(String msg);
        event MsgHandler ReceivedMsg;

        event LogHandler Log;
    }
}
