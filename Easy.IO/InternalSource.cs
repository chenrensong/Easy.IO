﻿using System;
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
                if (byteCount < 0) throw new ArgumentException("byteCount < 0: " + byteCount);
                if (byteCount == 0) return 0;
                try
                {
                    _timeout.ThrowIfReached();
                    Segment tail = sink.WritableSegment(1);
                    int maxToCopy = (int)Math.Min(byteCount, Segment.SIZE - tail.Limit);
                    int bytesRead = _in.Read(tail.Data, tail.Limit, maxToCopy);
                    if (bytesRead <= 0)
                    {
                        return -1;
                    }
                    tail.Limit += bytesRead;
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
