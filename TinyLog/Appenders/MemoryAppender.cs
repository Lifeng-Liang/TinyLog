using System.Collections.Generic;

namespace TinyLog.Appenders
{
    public class MemoryAppender : Appender
    {
        public static int DefaultCount = 100;
        private List<LogMessage> _list1 = new List<LogMessage>();
        private List<LogMessage> _list2 = new List<LogMessage>();

        public override void Append(LogMessage msg)
        {
            lock (LockRoot)
            {
                if (_list1.Count > DefaultCount)
                {
                    _list2 = _list1;
                    _list1 = new List<LogMessage>();
                }
                _list1.Add(msg);
            }
        }

        public override IEnumerable<LogMessage> Unsaved()
        {
            lock (LockRoot)
            {
                var list = new List<LogMessage>();
                list.AddRange(_list2);
                list.AddRange(_list1);
                return list;
            }
        }
    }
}
