using System;
using System.IO;
using System.Text;

namespace TinyLog.Appenders
{
    public class FileAppender : Appender, IDisposable
    {
        private FileStream _stream;
        private StreamWriter _writer;
        private bool _isDisposed;
        private readonly string _lineFormat;
        private bool _isStarted;
        private readonly string _fileName;
        private readonly long _size;
        private int _index;
        private readonly Func<string> _fileChanged;

        public FileAppender(string fileName, string lineFormat, long size, Func<string> fileChanged)
        {
            _fileName = fileName;
            _lineFormat = lineFormat;
            _size = size;
            _fileChanged = fileChanged;
            _isDisposed = false;
        }

        public override void Append(LogMessage msg)
        {
            lock (LockRoot)
            {
                if (!_isDisposed)
                {
                    CheckAndOpenFile();
                    if (msg.ThreadId != 0)
                    {
                        _writer?.WriteLine(_lineFormat, msg.Time, msg.Level, msg.Message, msg.ThreadId);
                    }
                    else
                    {
                        _writer?.WriteLine(_lineFormat, msg.Time, msg.Level, msg.Message, "");
                    }
                }
            }
        }

        private void CheckAndOpenFile()
        {
            if (!_isStarted)
            {
                OpenFile(_fileName);
            }
            else
            {
                if (_size > 0)
                {
                    if (_stream.Length > _size)
                    {
                        Close();
                        File.Move(_fileName, GetUnusedFileName());
                        OpenFile(_fileName);
                    }
                }
            }
        }

        private string GetUnusedFileName()
        {
            while (true)
            {
                _index++;
                var fn = _fileName + "." + _index;
                if (!File.Exists(fn))
                {
                    return fn;
                }
            }
        }

        private void OpenFile(string fileName)
        {
            try
            {
                _stream = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.Read);
                _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };
                _isStarted = true;
                var msg = _fileChanged?.Invoke();
                if (!string.IsNullOrEmpty(msg))
                {
                    _writer.WriteLine(msg);
                }
            }
            catch (Exception)
            {
                Close();
                _isDisposed = true;
                throw;
            }
        }

        public void Dispose()
        {
            lock (LockRoot)
            {
                Close();
                _isDisposed = true;
            }
        }

        private void Close()
        {
            try
            {
                _writer?.Dispose();
            }
            catch (Exception)
            {
                //Nothing can do
            }
            _stream?.Dispose();
            _isStarted = false;
            _writer = null;
            _stream = null;
        }

        ~FileAppender()
        {
            Dispose();
        }
    }
}
