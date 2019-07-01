using System;
using System.Collections.Generic;
using System.Text;

namespace Easy.IO
{
    //https://juejin.im/post/5856680c8e450a006c6474bd
    public class Segment
    {
        /** The size Of all segments in bytes. */
        internal static int SIZE = 8192;

        /** Segments will be shared when doing so avoids {@code arraycopy()} Of this many bytes. */
        static int SHARE_MINIMUM = 1024;

        internal byte[] data;

        /** The next byte Of application data byte to read in this segment. */
        internal int pos;

        /** The first byte Of available data ready to be written to. */
        internal int limit;

        /** True if other segments or byte strings use the same byte array. */
        internal bool shared;

        /** True if this segment owns the byte array and can append to it, extending {@code limit}. */
        internal bool owner;

        /** Next segment in a linked or circularly-linked list. */
        internal Segment next;

        /** Previous segment in a circularly-linked list. */
        internal Segment prev;

        public Segment()
        {
            this.data = new byte[SIZE];
            this.owner = true;
            this.shared = false;
        }

        public Segment(byte[] data, int pos, int limit, bool shared, bool owner)
        {
            this.data = data;
            this.pos = pos;
            this.limit = limit;
            this.shared = shared;
            this.owner = owner;
        }

        /**
         * Returns a new segment that shares the underlying byte array with this. Adjusting pos and limit
         * are safe but writes are forbidden. This also marks the current segment as shared, which
         * prevents it from being pooled.
         */
        public Segment SharedCopy()
        {
            shared = true;
            return new Segment(data, pos, limit, true, false);
        }

        /** Returns a new segment that its own private copy Of the underlying byte array. */
        public Segment UnsharedCopy()
        {
            byte[] copy = data.Copy();
            return new Segment(copy, pos, limit, false, true);
        }

        /**
         * Removes this segment Of a circularly-linked list and returns its successor.
         * Returns null if the list is now empty.
         */
        public Segment Pop()
        {
            Segment result = next != this ? next : null;
            prev.next = next;
            next.prev = prev;
            next = null;
            prev = null;
            return result;
        }

        /**
         * Appends {@code segment} after this segment in the circularly-linked list.
         * Returns the pushed segment.
         */
        public Segment Push(Segment segment)
        {
            segment.prev = this;
            segment.next = next;
            next.prev = segment;
            next = segment;
            return segment;
        }

        /**
         * Splits this head Of a circularly-linked list into two segments. The first
         * segment contains the data in {@code [pos..pos+byteCount)}. The second
         * segment contains the data in {@code [pos+byteCount..limit)}. This can be
         * useful when moving partial segments from one buffer to another.
         *
         * <p>Returns the new head Of the circularly-linked list.
         */
        public Segment Split(int byteCount)
        {
            if (byteCount <= 0 || byteCount > limit - pos) throw new IllegalArgumentException();
            Segment prefix;

            // We have two competing performance goals:
            //  - Avoid copying data. We accomplish this by sharing segments.
            //  - Avoid short shared segments. These are bad for performance because they are readonly and
            //    may lead to long chains Of short segments.
            // To balance these goals we only share segments when the copy will be large.
            if (byteCount >= SHARE_MINIMUM)
            {
                prefix = SharedCopy();
            }
            else
            {
                prefix = SegmentPool.Take();
                Array.Copy(data, pos, prefix.data, 0, byteCount);
            }

            prefix.limit = prefix.pos + byteCount;
            pos += byteCount;
            prev.Push(prefix);
            return prefix;
        }

        /**
         * Call this when the tail and its predecessor may both be less than half
         * full. This will copy data so that segments can be recycled.
         */
        public void Compact()
        {
            if (prev == this) throw new IllegalStateException();
            if (!prev.owner) return; // Cannot compact: prev isn't writable.
            int byteCount = limit - pos;
            int availableByteCount = SIZE - prev.limit + (prev.shared ? 0 : prev.pos);
            if (byteCount > availableByteCount) return; // Cannot compact: not enough writable space.
            WriteTo(prev, byteCount);
            Pop();
            SegmentPool.Recycle(this);
        }

        /** Moves {@code byteCount} bytes from this segment to {@code sink}. */
        public void WriteTo(Segment sink, int byteCount)
        {
            if (!sink.owner) throw new IllegalArgumentException();
            if (sink.limit + byteCount > SIZE)
            {
                // We can't fit byteCount bytes at the sink's current position. Shift sink first.
                if (sink.shared) throw new IllegalArgumentException();
                if (sink.limit + byteCount - sink.pos > SIZE) throw new IllegalArgumentException();
                Array.Copy(sink.data, sink.pos, sink.data, 0, sink.limit - sink.pos);
                sink.limit -= sink.pos;
                sink.pos = 0;
            }

            Array.Copy(data, pos, sink.data, sink.limit, byteCount);
            sink.limit += byteCount;
            pos += byteCount;
        }
    }

}
