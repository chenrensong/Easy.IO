using System;
using System.IO;

namespace Easy.IO
{
    public partial class EasyIO
    {
        private class InternalSource : Source
        {
            private Timeout _timeout;
            private Stream _in;
            public InternalSource(Stream @in, Timeout timeout)
            {
                _in = @in;
                _timeout = timeout;
            }
            public void Dispose()
            {
                _in.Dispose();
            }

            public long Read(EasyBuffer sink, long byteCount)
            {
                if (byteCount < 0) throw new IllegalArgumentException("byteCount < 0: " + byteCount);
                if (byteCount == 0) return 0;
                try
                {
                    _timeout.ThrowIfReached();
                    Segment tail = sink.writableSegment(1);
                    int maxToCopy = (int)Math.Min(byteCount, Segment.SIZE - tail.limit);
                    int bytesRead = _in.Read(tail.data, tail.limit, maxToCopy);
                    if (bytesRead <= 0)
                    {
                        return -1;
                    }
                    tail.limit += bytesRead;
                    sink.Size += bytesRead;
                    return bytesRead;
                }
                catch (Exception e)
                {
                    throw e;
                }
            }

            public Timeout Timeout()
            {
                return _timeout;
            }
        }
    }
}
