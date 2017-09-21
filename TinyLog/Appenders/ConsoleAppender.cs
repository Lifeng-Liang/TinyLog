using System;

namespace TinyLog.Appenders
{
    public class ConsoleAppender : Appender
    {
        public override void Append(LogMessage msg)
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss,fff} {msg.Level} {msg.Message}");
        }
    }
}
