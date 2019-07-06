using System;
using System.IO;

namespace Easy.IO
{
    public partial class EasyIO
    {
        private class InternalSink : Sink
        {
            private Timeout _timeout;
            private Stream _out;
            public InternalSink(Stream @out, Timeout timeout)
            {
                _out = @out;
                _timeout = timeout;
            }
            public void Dispose()
            {
                _out.Dispose();
            }

            public void Flush()
            {
                _out.Flush();
            }

            public Timeout Timeout()
            {
                return _timeout;
            }

            public void Write(EasyBuffer source, long byteCount)
            {
                Util.CheckOffsetAndCount(source.Size, 0, byteCount);
                while (byteCount > 0)
                {
                    _timeout.ThrowIfReached();
                    Segment head = source.Head;
                    int toCopy = (int)Math.Min(byteCount, head.Limit - head.Pos);
                    _out.Write(head.Data, head.Pos, toCopy);

                    head.Pos += toCopy;
                    byteCount -= toCopy;
                    source.Size -= toCopy;

                    if (head.Pos == head.Limit)
                    {
                        source.Head = head.Pop();
                        SegmentPool.Recycle(head);
                    }
                }
            }
        }
    }
}
