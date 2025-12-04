using System;
using System.Collections.Generic;
using System.Text;

namespace JsonRpc
{
    public interface IServer : IDisposable
    {
        delegate void ConnectHandler(IClient client);
        event ConnectHandler ClientConnected;
        event LogHandler Log;
    }
}
