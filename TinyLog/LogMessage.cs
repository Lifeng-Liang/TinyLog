using System;
using System.Threading;

namespace TinyLog
{
    public class LogMessage
    {
        public DateTime Time;
        public LogLevel Level;
        public string Message;
        public int ThreadId;

        public LogMessage(DateTime time, LogLevel lvl, string msg, bool includeThreadId)
        {
            Time = time;
            Level = lvl;
            Message = msg;
            if (includeThreadId)
            {
                ThreadId = Thread.CurrentThread.ManagedThreadId;
            }
        }

        public LogMessage(LogLevel lvl, string msg, bool includeThreadId)
            : this(DateTime.Now, lvl, msg, includeThreadId)
        {
        }
    }
}
