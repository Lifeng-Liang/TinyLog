using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using TinyIlDecoder;

namespace TinyLog
{
    public class ExceptionHandler
    {
        public string ToString(Exception exception)
        {
            var sb = new StringBuilder();
            AppendException(sb, exception);
            return sb.ToString();
        }

        private void AppendException(StringBuilder sb, Exception e)
        {
            try
            {
                if (e.InnerException != null)
                {
                    AppendException(sb, e.InnerException);
                    sb.AppendLine("-----------------------");
                }
                sb.AppendLine($"[{e.GetType()} : {e.Message}]");
                var st = new StackTrace(e, true);
                var frames = st.GetFrames();
                if (frames != null)
                {
                    foreach (var frame in frames)
                    {
                        ProcessFrame(sb, frame);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception ex)
            {
                sb.Append(e).Append(ex);
            }
        }

        private void ProcessFrame(StringBuilder sb, StackFrame frame)
        {
            var m = frame.GetMethod();
            sb.Append("  in ").Append(m.DeclaringType).Append(".").Append(m.Name);
            var x = frame.GetFileLineNumber();
            var y = frame.GetFileColumnNumber();
            if (x != 0 && y != 0)
            {
                sb.Append(" at (").Append(x).Append(",").Append(y).Append(")");
            }
            sb.Append(", ");
            var n = frame.GetILOffset();
            if (n > 0)
            {
                sb.Append("#").Append(n).Append(": ");
                var info = new RuntimeOpInfo(m, n);
                var p = new OpCodeProcessor(sb, info, false);
                p.Append("; ");
                p.Append(";");
                sb.TrimEnd(" ");
            }
            sb.AppendLine();
        }
    }
}
