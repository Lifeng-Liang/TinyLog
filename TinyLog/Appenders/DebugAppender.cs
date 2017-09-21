using System;
using System.IO;

namespace TinyLog.Appenders
{
    public class DebugAppender : Appender
    {
        public override void Append(LogMessage msg)
        {
            LogLog($"{DateTime.Now:yyyy-MM-dd HH:mm:ss,fff} {msg.Level} {msg.Message}");
        }

        public static void LogLog(string msg)
        {
            lock (LockRoot)
            {
                try
                {
                    using (var s = new FileStream(@"c:\temp\test.log", FileMode.Append, FileAccess.Write, FileShare.Read))
                    {
                        using (var sw = new StreamWriter(s))
                        {
                            sw.WriteLine(msg);
                        }
                    }
                }
                catch (Exception)
                {
                    //Nothing can do
                }
            }
        }
    }
}
