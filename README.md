TinyLog
==========

Introduce
----------

This is a log library for .net standard 2.0.

Usage
----------

````c#
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
````

Log Level
----------

Off < Fatal < Error < Warn < Trace < Debug < Info < Finest = All

Why closure
----------

With closure we can ensure the steps of build the log message to be lazy execute. So it will cost less CPU to call the log function when we set log level to less than the function specified.

Tiny Il Decoder
----------

With tiny il decoder, TinyLog will output some Il code the function was calling while the exception occur.

Fp functions
----------

There're some FP functions such as Filter, Map, Reduce etc in this library. Just add `using Fp;` to use them.

Contact
----------

lifeng.liang(at)gmail.com