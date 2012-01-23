using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace StreamExtensions
{
    public static class SystemIoStreamExtensions
    {
        public static void CopyTo(this Stream source, Stream destination, int bufferSize)
        {
            Debug.WriteLine("CopyTo size=" + source.Length);
            long ts = DateTime.Now.Ticks;
            long cbTotal = 0L;
            byte[] read_buffer = new byte[bufferSize];
            int cb = source.Read(read_buffer, 0, read_buffer.Length);
            AutoResetEvent writeLock = new AutoResetEvent(false);
            while (cb > 0)
            {
                cbTotal += cb;
                try
                {
                    byte[] write_buffer = read_buffer;
                    destination.BeginWrite(write_buffer, 0, cb, (AsyncCallback)delegate(IAsyncResult ar)
                    {
                        try
                        {
                            destination.EndWrite(ar);
                        }
                        finally
                        {
                            writeLock.Set();
                        }
                    }, null);
                }
                catch
                {
                    writeLock.Set();
                    throw;
                }
                read_buffer = new byte[bufferSize];
                cb = source.Read(read_buffer, 0, read_buffer.Length);
                writeLock.WaitOne();
            }
            ts = DateTime.Now.Ticks - ts;
        }
    }
}