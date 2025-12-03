using System;
using System.Collections.Generic;
using System.Text;

namespace JsonRpc
{
    enum LogSeverity { Debug, Info, Warning, Error };
    delegate void LogHandler(String msg, LogSeverity severity);
}
