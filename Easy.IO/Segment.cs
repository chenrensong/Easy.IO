using System;
using System.Collections.Generic;
using System.Text;

namespace Easy.IO
{
    //https://juejin.im/post/5856680c8e450a006c6474bd
    public class Segment
    {
        /** The size Of all segments in bytes. */
        public static int SIZE = 8192;

        /** Segments will be shared when doing so avoids {@code arraycopy()} Of this many bytes. */
        static int SHARE_MINIMUM = 1024;

        internal byte[] Data { get; set; }

        /** The next byte Of application data byte to read in this segment. */
        internal int Pos { get; set; }

        /** The first byte Of available data ready to be written to. */
        internal int Limit { get; set; }

        /** True if other segments or byte strings use the same byte array. */
        internal bool Shared { get; set; }

        /** True if this segment owns the byte array and can append to it, extending {@code limit}. */
        internal bool Owner { get; set; }

        /** Next segment in a linked or circularly-linked list. */
        internal Segment Next { get; set; }

        /** Previous segment in a circularly-linked list. */
        internal Segment Prev { get; set; }

        public Segment()
        {
            this.Data = new byte[SIZE];
            this.Owner = true;
            this.Shared = false;
        }

        public Segment(byte[] data, int pos, int limit, bool shared, bool owner)
        {
            this.Data = data;
            this.Pos = pos;
            this.Limit = limit;
            this.Shared = shared;
            this.Owner = owner;
        }

        /**
         * Returns a new segment that shares the underlying byte array with this. Adjusting pos and limit
         * are safe but writes are forbidden. This also marks the current segment as shared, which
         * prevents it from being pooled.
         */
        public Segment SharedCopy()
        {
            Shared = true;
            return new Segment(Data, Pos, Limit, true, false);
        }

        /** Returns a new segment that its own private copy Of the underlying byte array. */
        public Segment UnsharedCopy()
        {
            byte[] copy = Data.Copy();
            return new Segment(copy, Pos, Limit, false, true);
        }

        /**
         * Removes this segment Of a circularly-linked list and returns its successor.
         * Returns null if the list is now empty.
         */
        public Segment Pop()
        {
            Segment result = Next != this ? Next : null;
            Prev.Next = Next;
            Next.Prev = Prev;
            Next = null;
            Prev = null;
            return result;
        }

        /**
         * Appends {@code segment} after this segment in the circularly-linked list.
         * Returns the pushed segment.
         */
        public Segment Push(Segment segment)
        {
            segment.Prev = this;
            segment.Next = Next;
            Next.Prev = segment;
            Next = segment;
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
            if (byteCount <= 0 || byteCount > Limit - Pos) throw new IllegalArgumentException();
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
                Array.Copy(Data, Pos, prefix.Data, 0, byteCount);
            }

            prefix.Limit = prefix.Pos + byteCount;
            Pos += byteCount;
            Prev.Push(prefix);
            return prefix;
        }

        /**
         * Call this when the tail and its predecessor may both be less than half
         * full. This will copy data so that segments can be recycled.
         */
        public void Compact()
        {
            if (Prev == this) throw new IllegalStateException();
            if (!Prev.Owner) return; // Cannot compact: prev isn't writable.
            int byteCount = Limit - Pos;
            int availableByteCount = SIZE - Prev.Limit + (Prev.Shared ? 0 : Prev.Pos);
            if (byteCount > availableByteCount) return; // Cannot compact: not enough writable space.
            WriteTo(Prev, byteCount);
            Pop();
            SegmentPool.Recycle(this);
        }

        /** Moves {@code byteCount} bytes from this segment to {@code sink}. */
        public void WriteTo(Segment sink, int byteCount)
        {
            if (!sink.Owner) throw new IllegalArgumentException();
            if (sink.Limit + byteCount > SIZE)
            {
                // We can't fit byteCount bytes at the sink's current position. Shift sink first.
                if (sink.Shared) throw new IllegalArgumentException();
                if (sink.Limit + byteCount - sink.Pos > SIZE) throw new IllegalArgumentException();
                Array.Copy(sink.Data, sink.Pos, sink.Data, 0, sink.Limit - sink.Pos);
                sink.Limit -= sink.Pos;
                sink.Pos = 0;
            }

            Array.Copy(Data, Pos, sink.Data, sink.Limit, byteCount);
            sink.Limit += byteCount;
            Pos += byteCount;
        }
    }

}
