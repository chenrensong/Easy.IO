using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Easy.IO
{

    public partial class EasyIO
    {
        public static BufferedSource Buffer(Source source)
        {
            return new RealBufferedSource(source);
        }

        public static BufferedSink Buffer(Sink sink)
        {
            return new RealBufferedSink(sink);
        }

        public static Sink Sink(Socket socket)
        {
            return Sink(new NetworkStream(socket), new Timeout());
        }

        public static Sink Sink(Stream @out)
        {
            return Sink(@out, new Timeout());
        }

        private static Sink Sink(Stream @out, Timeout timeout)
        {
            if (@out == null)
            {
                throw new ArgumentException("out == null");
            }
            if (timeout == null)
            {
                throw new ArgumentException("timeout == null");
            }
            var internalSink = new InternalSink(@out, timeout);
            return internalSink;
        }

        public static Source Source(Socket socket)
        {
            return Source(new NetworkStream(socket), new Timeout());
        }

        public static Source Source(Stream @in)
        {
            return Source(@in, new Timeout());
        }

        public static Source Source(string path)
        {
            if (path == null)
            {
                throw new ArgumentException("path == null");
            }
            return Source(File.OpenRead(path));
        }

        public static Sink Sink(string path)
        {
            if (path == null)
            {
                throw new ArgumentException("path == null");
            }
            return Sink(File.OpenWrite(path));
        }

        public static Sink Sink(string path, FileMode fileMode)
        {
            if (path == null)
            {
                throw new ArgumentException("path == null");
            }
            return Sink(File.Open(path, fileMode));
        }

        private static Source Source(Stream @in, Timeout timeout)
        {
            if (@in == null)
            {
                throw new ArgumentException("in == null");
            }
            if (timeout == null)
            {
                throw new ArgumentException("timeout == null");
            }
            var internalSource = new InternalSource(@in, timeout);
            return internalSource;
        }

        public static Source Source(FileInfo file)
        {
            if (file == null)
            {
                throw new ArgumentException("file == null");
            }
            return Source(file.OpenRead());
        }

        public static Sink Sink(FileInfo file)
        {
            if (file == null)
            {
                throw new ArgumentException("file == null");
            }
            return Sink(file.OpenWrite());
        }
    }
}
