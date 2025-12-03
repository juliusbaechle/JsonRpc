using System;
using System.Collections.Generic;
using System.Text;

namespace JsonRpc
{
    internal interface IServer
    {
        delegate void ConnectHandler(IClient client);
        event ConnectHandler ClientConnected;
        event LogHandler Log;
    }
}
