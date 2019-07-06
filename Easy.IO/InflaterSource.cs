using ICSharpCode.SharpZipLib.Zip.Compression;
using System;
using System.Collections.Generic;
using System.Text;

namespace Easy.IO
{
    public class InflaterSource : Source
    {
        private BufferedSource source;
        private Inflater inflater;

        /**
         * When we call Inflater.setInput(), the inflater keeps our byte array until
         * it needs input again. This tracks how many bytes the inflater is currently
         * holding on to.
         */
        private int bufferBytesHeldByInflater;
        private bool closed;

        public InflaterSource(Source source, Inflater inflater) : this(EasyIO.Buffer(source), inflater)
        {
        }

        public InflaterSource(BufferedSource source, Inflater inflater)
        {
            if (source == null) throw new IllegalArgumentException("source == null");
            if (inflater == null) throw new IllegalArgumentException("inflater == null");
            this.source = source;
            this.inflater = inflater;
        }


        public void Dispose()
        {
            if (closed) return;
            closed = true;
            source.Dispose();
        }


        public long Read(EasyBuffer sink, long byteCount)
        {
            if (byteCount < 0) throw new IllegalArgumentException("byteCount < 0: " + byteCount);
            if (closed) throw new IllegalStateException("closed");
            if (byteCount == 0) return 0;

            while (true)
            {
                var sourceExhausted = refill();

                // Decompress the inflater's compressed data into the sink.
                try
                {
                    Segment tail = sink.WritableSegment(1);
                    int toRead = (int)Math.Min(byteCount, Segment.SIZE - tail.Limit);
                    int bytesInflated = inflater.Inflate(tail.Data, tail.Limit, toRead);
                    if (bytesInflated > 0)
                    {
                        tail.Limit += bytesInflated;
                        sink.Size += bytesInflated;
                        return bytesInflated;
                    }
                    if (inflater.IsFinished || inflater.IsNeedingDictionary)
                    {
                        releaseInflatedBytes();
                        if (tail.Pos == tail.Limit)
                        {
                            // We allocated a tail segment, but didn't end up needing it. Recycle!
                            sink.Head = tail.Pop();
                            SegmentPool.Recycle(tail);
                        }
                        return -1;
                    }
                    if (sourceExhausted) throw new EOFException("source exhausted prematurely");
                }
                catch (Exception e)
                {
                }
            }
        }

        /// <summary>
        /// Refills the inflater with compressed data if it needs input. (And only if
        /// it needs input). Returns true if the inflater required input but the source
        /// was exhausted.
        /// </summary>
        /// <returns></returns>
        public bool refill()
        {
            if (!inflater.IsNeedingInput) return false;

            releaseInflatedBytes();

            if (inflater.RemainingInput != 0) throw new IllegalStateException("?"); // TODO: possible?

            // If there are compressed bytes in the source, assign them to the inflater.
            if (source.exhausted()) return true;

            // Assign buffer bytes to the inflater.
            Segment head = source.Buffer().Head;
            bufferBytesHeldByInflater = head.Limit - head.Pos;
            inflater.SetInput(head.Data, head.Pos, bufferBytesHeldByInflater);
            return false;
        }

        /** When the inflater has processed compressed data, remove it from the buffer. */
        private void releaseInflatedBytes()
        {
            if (bufferBytesHeldByInflater == 0) return;
            int toRelease = bufferBytesHeldByInflater - inflater.RemainingInput;
            bufferBytesHeldByInflater -= toRelease;
            source.Skip(toRelease);
        }


        public Timeout Timeout()
        {
            return source.Timeout();
        }
    }
}
