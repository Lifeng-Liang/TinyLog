using System;

namespace TinyLog
{
    public static class Log
    {
        public static readonly Logger Instance;

        static Log()
        {
            Instance = new Logger();
        }

        public static void Fatal(Exception ex)
        {
            Instance.Append(LogLevel.Fatal, null, ex);
        }

        public static void Fatal(Func<string> callback, Exception ex = null)
        {
            Instance.Append(LogLevel.Fatal, callback, ex);
        }

        public static void Error(Exception ex)
        {
            Instance.Append(LogLevel.Error, null, ex);
        }

        public static void Error(Func<string> callback, Exception ex = null)
        {
            Instance.Append(LogLevel.Error, callback, ex);
        }

        public static void Warn(Exception ex)
        {
            Instance.Append(LogLevel.Warn, null, ex);
        }

        public static void Warn(Func<string> callback, Exception ex = null)
        {
            Instance.Append(LogLevel.Warn, callback, ex);
        }

        public static void Trace(Func<string> callback)
        {
            Instance.Append(LogLevel.Trace, callback, null);
        }

        public static void Debug(Func<string> callback)
        {
            Instance.Append(LogLevel.Debug, callback, null);
        }

        public static void Info(Func<string> callback)
        {
            Instance.Append(LogLevel.Info, callback, null);
        }

        public static void Finest(Func<string> callback)
        {
            Instance.Append(LogLevel.Finest, callback, null);
        }
    }
}
