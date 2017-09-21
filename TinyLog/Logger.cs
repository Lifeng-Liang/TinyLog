using Fp;
using System;
using System.Threading;
using TinyLog.Appenders;

namespace TinyLog
{
    public class Logger
    {
        private readonly ExceptionHandler _exceptionHandler = new ExceptionHandler();
        public LogLevel Level { get; private set; } = LogLevel.All;
        public Appender Appender { get; private set; } = new MemoryAppender();
        public bool IncludeThreadId { get; private set; }

        public void SetLevel(LogLevel lvl)
        {
            Level = lvl;
        }

        public void SetAppender(Appender inst)
        {
            var temp = Appender;
            Appender = inst ?? throw new ArgumentNullException(nameof(inst));
            var list = temp.Unsaved();
            try
            {
                list?.Filter(lm => lm.Level <= Level).ForEach(lm => Appender.Append(lm));
            }
            catch (Exception)
            {
                Appender = new EmptyAppender();
            }
            (temp as IDisposable)?.Dispose();
        }

        public void SetIncludeThreadId(bool includeThreadId)
        {
            IncludeThreadId = includeThreadId;
        }

        protected virtual void Append(LogLevel lvl, string msg)
        {
            try
            {
                Appender.Append(new LogMessage(lvl, msg, IncludeThreadId));
            }
            catch (Exception)
            {
                Appender = new EmptyAppender();
            }
        }

        public virtual void Append(LogLevel lvl, Func<string> callback, Exception ex)
        {
            try
            {
                if (lvl <= Level)
                {
                    string msg = null;
                    if (callback != null)
                    {
                        msg = callback();
                    }
                    if (ex == null)
                    {
                        Append(lvl, msg);
                    }
                    else if (string.IsNullOrEmpty(msg))
                    {
                        Append(lvl, _exceptionHandler.ToString(ex));
                    }
                    else
                    {
                        var space = (msg.Length > 0 && msg[msg.Length - 1] == ' ') ? "" : " ";
                        Append(lvl, msg + space + _exceptionHandler.ToString(ex));
                    }
                }
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception ex1)
            {
                Append(LogLevel.Fatal, _exceptionHandler.ToString(ex1));
            }
        }
    }
}
