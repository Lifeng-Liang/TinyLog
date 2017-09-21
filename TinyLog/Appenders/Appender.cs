using System.Collections.Generic;

namespace TinyLog.Appenders
{
    public class Appender
    {
        protected static readonly object LockRoot = new object();

        public virtual void Append(LogMessage msg)
        {
        }

        public virtual IEnumerable<LogMessage> Unsaved()
        {
            return null;
        }
    }
}
