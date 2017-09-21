using System;
using TinyLog;
using TinyLog.Appenders;

namespace CoreTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Debug(() => "Before set appender.");
            Log.Instance.SetAppender(new ConsoleAppender());
            Log.Trace(() => "After set appender.");
            try
            {
                TestA();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            Log.Instance.SetLevel(LogLevel.Off);
            Log.Debug(() => "Output nothing.");
        }

        private static void TestA()
        {
            TestB(0);
        }

        private static int TestB(int n)
        {
            return 5 / n;
        }
    }
}
