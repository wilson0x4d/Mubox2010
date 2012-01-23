using System;
using System.Diagnostics;
using System.IO;

namespace Mubox.Diagnostics
{
    /// <summary>
    /// A custom trace listener that writes to a stream of choice, e.g. to a FileStream.
    /// </summary>
    public class TraceListenerStreamWriter
        : TraceListener
    {
        private TraceListenerStreamWriter()
        {
            throw new NotImplementedException();
        }

        private StreamWriter StreamWriter { get; set; }

        public TraceListenerStreamWriter(Stream stream)
        {
            Debug.Assert(stream != null);
            StreamWriter = new StreamWriter(stream);
            StreamWriter.NewLine = Environment.NewLine;
        }

        private object writerLock = new object();

        public override void Write(string message)
        {
            try
            {
                StreamWriter writer = this.StreamWriter;
                if (writer != null)
                {
                    writer.Write(message);
                    writer.Flush();
                }
            }
            catch (Exception ex)
            {
                this.StreamWriter = null;
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        public override void WriteLine(string message)
        {
            try
            {
                StreamWriter writer = this.StreamWriter;
                if (writer != null)
                {
                    writer.WriteLine(message);
                    writer.Flush();
                }
            }
            catch (Exception ex)
            {
                this.StreamWriter = null;
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }
    }
}