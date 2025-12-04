using System;
using System.Collections.Generic;
using System.Text;

namespace JsonRpc
{
    public enum LogSeverity { Debug, Info, Warning, Error };
    public delegate void LogHandler(String msg, LogSeverity severity);
}
