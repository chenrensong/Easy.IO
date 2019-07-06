using ICSharpCode.SharpZipLib.Zip.Compression;
using System;
using System.Collections.Generic;
using System.Text;

namespace Easy.IO
{
    public class DeflaterSink : Sink
    {

        private BufferedSink sink;
        private Deflater deflater;
        private bool closed;

        public DeflaterSink(Sink sink, Deflater deflater) : this(EasyIO.Buffer(sink), deflater)
        {

        }

        /**
         * This package-private constructor shares a buffer with its trusted caller.
         * In general we can't share a BufferedSource because the deflater holds input
         * bytes until they are inflated.
         */
        DeflaterSink(BufferedSink sink, Deflater deflater)
        {
            if (sink == null) throw new IllegalArgumentException("source == null");
            if (deflater == null) throw new IllegalArgumentException("inflater == null");
            this.sink = sink;
            this.deflater = deflater;
        }

        public void Dispose()
        {
            if (closed) return;
            try
            {
                sink.Dispose();
            }
            catch
            {

            }
            closed = true;
        }

        public void Flush()
        {
            deflater.Finish();
            deflate();
            sink.Flush();
        }

        public Timeout Timeout()
        {
            return sink.Timeout();
        }

        public void Write(EasyBuffer source, long byteCount)
        {
            Util.CheckOffsetAndCount(source.Size, 0, byteCount);
            while (byteCount > 0)
            {
                // Share bytes from the head segment of 'source' with the deflater.
                Segment head = source.Head;
                int toDeflate = (int)Math.Min(byteCount, head.Limit - head.Pos);
                deflater.SetInput(head.Data, head.Pos, toDeflate);
                deflater.Flush();
      
                // Deflate those bytes into sink.
                deflate();

                // Mark those bytes as read.
                source.Size -= toDeflate;
                head.Pos += toDeflate;
                if (head.Pos == head.Limit)
                {
                    source.Head = head.Pop();
                    SegmentPool.Recycle(head);
                }

                byteCount -= toDeflate;
            }

   

        }

        private void deflate()
        {
            var buffer = sink.Buffer();
            while (true)
            {
                Segment s = buffer.WritableSegment(1);
                int deflated = deflater.Deflate(s.Data, s.Limit, Segment.SIZE - s.Limit);
                if (deflated > 0)
                {
                    s.Limit += deflated;
                    buffer.Size += deflated;
                    sink.EmitCompleteSegments();
                }
                else if (deflater.IsNeedingInput)
                {
                    if (s.Pos == s.Limit)
                    {
                        // We allocated a tail segment, but didn't end up needing it. Recycle!
                        buffer.Head = s.Pop();
                        SegmentPool.Recycle(s);
                    }
                    return;
                }
            }
        }
    }
}
