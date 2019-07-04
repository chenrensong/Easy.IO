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

        internal byte[] _data;

        /** The next byte Of application data byte to read in this segment. */
        internal int _pos;

        /** The first byte Of available data ready to be written to. */
        internal int _limit;

        /** True if other segments or byte strings use the same byte array. */
        internal bool _shared;

        /** True if this segment owns the byte array and can append to it, extending {@code limit}. */
        internal bool _owner;

        /** Next segment in a linked or circularly-linked list. */
        internal Segment _next;

        /** Previous segment in a circularly-linked list. */
        internal Segment _prev;

        public Segment()
        {
            this._data = new byte[SIZE];
            this._owner = true;
            this._shared = false;
        }

        public Segment(byte[] data, int pos, int limit, bool shared, bool owner)
        {
            this._data = data;
            this._pos = pos;
            this._limit = limit;
            this._shared = shared;
            this._owner = owner;
        }

        /**
         * Returns a new segment that shares the underlying byte array with this. Adjusting pos and limit
         * are safe but writes are forbidden. This also marks the current segment as shared, which
         * prevents it from being pooled.
         */
        public Segment SharedCopy()
        {
            _shared = true;
            return new Segment(_data, _pos, _limit, true, false);
        }

        /** Returns a new segment that its own private copy Of the underlying byte array. */
        public Segment UnsharedCopy()
        {
            byte[] copy = _data.Copy();
            return new Segment(copy, _pos, _limit, false, true);
        }

        /**
         * Removes this segment Of a circularly-linked list and returns its successor.
         * Returns null if the list is now empty.
         */
        public Segment Pop()
        {
            Segment result = _next != this ? _next : null;
            _prev._next = _next;
            _next._prev = _prev;
            _next = null;
            _prev = null;
            return result;
        }

        /**
         * Appends {@code segment} after this segment in the circularly-linked list.
         * Returns the pushed segment.
         */
        public Segment Push(Segment segment)
        {
            segment._prev = this;
            segment._next = _next;
            _next._prev = segment;
            _next = segment;
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
            if (byteCount <= 0 || byteCount > _limit - _pos) throw new IllegalArgumentException();
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
                Array.Copy(_data, _pos, prefix._data, 0, byteCount);
            }

            prefix._limit = prefix._pos + byteCount;
            _pos += byteCount;
            _prev.Push(prefix);
            return prefix;
        }

        /**
         * Call this when the tail and its predecessor may both be less than half
         * full. This will copy data so that segments can be recycled.
         */
        public void Compact()
        {
            if (_prev == this) throw new IllegalStateException();
            if (!_prev._owner) return; // Cannot compact: prev isn't writable.
            int byteCount = _limit - _pos;
            int availableByteCount = SIZE - _prev._limit + (_prev._shared ? 0 : _prev._pos);
            if (byteCount > availableByteCount) return; // Cannot compact: not enough writable space.
            WriteTo(_prev, byteCount);
            Pop();
            SegmentPool.Recycle(this);
        }

        /** Moves {@code byteCount} bytes from this segment to {@code sink}. */
        public void WriteTo(Segment sink, int byteCount)
        {
            if (!sink._owner) throw new IllegalArgumentException();
            if (sink._limit + byteCount > SIZE)
            {
                // We can't fit byteCount bytes at the sink's current position. Shift sink first.
                if (sink._shared) throw new IllegalArgumentException();
                if (sink._limit + byteCount - sink._pos > SIZE) throw new IllegalArgumentException();
                Array.Copy(sink._data, sink._pos, sink._data, 0, sink._limit - sink._pos);
                sink._limit -= sink._pos;
                sink._pos = 0;
            }

            Array.Copy(_data, _pos, sink._data, sink._limit, byteCount);
            sink._limit += byteCount;
            _pos += byteCount;
        }
    }

}
