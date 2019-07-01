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

        public static Sink sink(Stream @out)
        {
            return Sink(@out, new Timeout());
        }

        private static Sink Sink(Stream @out, Timeout timeout)
        {
            if (@out == null)
            {
                throw new IllegalArgumentException("out == null");
            }
            if (timeout == null)
            {
                throw new IllegalArgumentException("timeout == null");
            }
            var internalSink = new InternalSink(@out, timeout);
            return internalSink;
        }

        public static Source Source(Stream @in)
        {
            return Source(@in, new Timeout());
        }

        public static Source Source(string path)
        {
            if (path == null)
            {
                throw new IllegalArgumentException("path == null");
            }
            return Source(File.OpenRead(path));
        }

        public static Sink Sink(string path)
        {
            if (path == null)
            {
                throw new IllegalArgumentException("path == null");
            }
            return sink(File.OpenWrite(path));
        }

        private static Source Source(Stream @in, Timeout timeout)
        {
            if (@in == null)
            {
                throw new IllegalArgumentException("in == null");
            }
            if (timeout == null)
            {
                throw new IllegalArgumentException("timeout == null");
            }
            var internalSource = new InternalSource(@in, timeout);
            return internalSource;
        }

        public static Source Source(FileInfo file)
        {
            if (file == null)
            {
                throw new IllegalArgumentException("file == null");
            }
            return Source(file.OpenRead());
        }

        public static Sink Sink(FileInfo file)
        {
            if (file == null)
            {
                throw new IllegalArgumentException("file == null");
            }
            return sink(file.OpenWrite());
        }
    }
}
