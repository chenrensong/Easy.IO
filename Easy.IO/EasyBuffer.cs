﻿using System;
using System.IO;
using System.Text;

namespace Easy.IO
{
    public class EasyBuffer : BufferedSink<EasyBuffer>, BufferedSource<EasyBuffer>
    {
        private static byte[] DIGITS ={ (byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6',
            (byte)'7', (byte)'8', (byte)'9', (byte)'a', (byte)'b', (byte)'c',(byte) 'd', (byte)'e',(byte) 'f' };

        public const int REPLACEMENT_CHARACTER = '\ufffd';
        private Segment _head;
        private long _size;

        public long Size
        {
            get
            {
                return _size;
            }
            set
            {
                _size = value;
            }
        }

        internal void Clear()
        {
            try
            {
                Skip(_size);
            }
            catch (Exception ex)
            {
            }
        }

        public Segment Head
        {
            get
            {
                return _head;
            }
            set
            {
                _head = value;
            }
        }

        public EasyBuffer Buffer()
        {
            return this;
        }

        public long CompleteSegmentByteCount()
        {
            long result = _size;
            if (result == 0) return 0;
            // Omit the tail if it's still writable.
            Segment tail = _head.Prev;
            if (tail.Limit < Segment.SIZE && tail.Owner)
            {
                result -= tail.Limit - tail.Pos;
            }
            return result;
        }

        public EasyBuffer Write(ByteString byteString)
        {
            if (byteString == null) throw new ArgumentException("byteString == null");
            byteString.Write(this);
            return this;
        }

        public EasyBuffer Write(byte[] source)
        {
            if (source == null) throw new ArgumentException("source == null");
            return Write(source, 0, source.Length);
        }

        public EasyBuffer Write(byte[] source, int offset, int byteCount)
        {
            if (source == null) throw new ArgumentException("source == null");
            Util.CheckOffsetAndCount(source.Length, offset, byteCount);

            int limit = offset + byteCount;
            while (offset < limit)
            {
                Segment tail = WritableSegment(1);

                int toCopy = Math.Min(limit - offset, Segment.SIZE - tail.Limit);
                Array.Copy(source, offset, tail.Data, tail.Limit, toCopy);

                offset += toCopy;
                tail.Limit += toCopy;
            }

            _size += byteCount;
            return this;
        }

        public long WriteAll(Source source)
        {
            if (source == null) throw new ArgumentException("source == null");
            long totalBytesRead = 0;
            for (long readCount; (readCount = source.Read(this, Segment.SIZE)) != -1;)
            {
                totalBytesRead += readCount;
            }
            return totalBytesRead;
        }

        public EasyBuffer Write(Source source, long byteCount)
        {
            while (byteCount > 0)
            {
                long read = source.Read(this, byteCount);
                if (read == -1)
                {
                    throw new Exception();
                }
                byteCount -= read;
            }
            return this;
        }

        public EasyBuffer WriteUtf8(string @string)
        {
            return WriteUtf8(@string, 0, @string.Length);
        }

        public EasyBuffer WriteUtf8(string @string, int beginIndex, int endIndex)
        {
            if (@string == null) throw new ArgumentException("string == null");
            if (beginIndex < 0) throw new ArgumentException("beginIndex < 0: " + beginIndex);
            if (endIndex < beginIndex)
            {
                throw new ArgumentException("endIndex < beginIndex: " + endIndex + " < " + beginIndex);
            }
            if (endIndex > @string.Length)
            {
                throw new ArgumentException(
                    "endIndex > string.length: " + endIndex + " > " + @string.Length);
            }

            // Transcode a UTF-16 Java string to UTF-8 bytes.
            for (int i = beginIndex; i < endIndex;)
            {
                int c = @string.CharAt(i);

                if (c < 0x80)
                {
                    Segment tail = WritableSegment(1);
                    byte[] data = tail.Data;
                    int segmentOffset = tail.Limit - i;
                    int runLimit = Math.Min(endIndex, Segment.SIZE - segmentOffset);

                    // Emit a 7-bit character with 1 byte.
                    data[segmentOffset + i++] = (byte)c; // 0xxxxxxx

                    // Fast-path contiguous runs Of ASCII characters. This is ugly, but yields a ~4x performance
                    // improvement over independent calls to writeByte().
                    while (i < runLimit)
                    {
                        c = @string.CharAt(i);
                        if (c >= 0x80) break;
                        data[segmentOffset + i++] = (byte)c; // 0xxxxxxx
                    }

                    int runSize = i + segmentOffset - tail.Limit; // Equivalent to i - (previous i).
                    tail.Limit += runSize;
                    _size += runSize;

                }
                else if (c < 0x800)
                {
                    // Emit a 11-bit character with 2 bytes.
                    WriteByte(c >> 6 | 0xc0); // 110xxxxx
                    WriteByte(c & 0x3f | 0x80); // 10xxxxxx
                    i++;

                }
                else if (c < 0xd800 || c > 0xdfff)
                {
                    // Emit a 16-bit character with 3 bytes.
                    WriteByte(c >> 12 | 0xe0); // 1110xxxx
                    WriteByte(c >> 6 & 0x3f | 0x80); // 10xxxxxx
                    WriteByte(c & 0x3f | 0x80); // 10xxxxxx
                    i++;

                }
                else
                {
                    // c is a surrogate. Make sure it is a high surrogate & that its successor is a low
                    // surrogate. If not, the UTF-16 is invalid, in which case we emit a replacement character.
                    int low = i + 1 < endIndex ? @string.CharAt(i + 1) : 0;
                    if (c > 0xdbff || low < 0xdc00 || low > 0xdfff)
                    {
                        WriteByte('?');
                        i++;
                        continue;
                    }

                    // UTF-16 high surrogate: 110110xxxxxxxxxx (10 bits)
                    // UTF-16 low surrogate:  110111yyyyyyyyyy (10 bits)
                    // Unicode code point:    00010000000000000000 + xxxxxxxxxxyyyyyyyyyy (21 bits)
                    int codePoint = 0x010000 + ((c & ~0xd800) << 10 | low & ~0xdc00);

                    // Emit a 21-bit character with 4 bytes.
                    WriteByte(codePoint >> 18 | 0xf0); // 11110xxx
                    WriteByte(codePoint >> 12 & 0x3f | 0x80); // 10xxxxxx
                    WriteByte(codePoint >> 6 & 0x3f | 0x80); // 10xxyyyy
                    WriteByte(codePoint & 0x3f | 0x80); // 10yyyyyy
                    i += 2;
                }
            }

            return this;
        }



        public EasyBuffer WriteUtf8CodePoint(int codePoint)
        {
            if (codePoint < 0x80)
            {
                // Emit a 7-bit code point with 1 byte.
                WriteByte(codePoint);

            }
            else if (codePoint < 0x800)
            {
                // Emit a 11-bit code point with 2 bytes.
                WriteByte(codePoint >> 6 | 0xc0); // 110xxxxx
                WriteByte(codePoint & 0x3f | 0x80); // 10xxxxxx

            }
            else if (codePoint < 0x10000)
            {
                if (codePoint >= 0xd800 && codePoint <= 0xdfff)
                {
                    // Emit a replacement character for a partial surrogate.
                    WriteByte('?');
                }
                else
                {
                    // Emit a 16-bit code point with 3 bytes.
                    WriteByte(codePoint >> 12 | 0xe0); // 1110xxxx
                    WriteByte(codePoint >> 6 & 0x3f | 0x80); // 10xxxxxx
                    WriteByte(codePoint & 0x3f | 0x80); // 10xxxxxx
                }

            }
            else if (codePoint <= 0x10ffff)
            {
                // Emit a 21-bit code point with 4 bytes.
                WriteByte(codePoint >> 18 | 0xf0); // 11110xxx
                WriteByte(codePoint >> 12 & 0x3f | 0x80); // 10xxxxxx
                WriteByte(codePoint >> 6 & 0x3f | 0x80); // 10xxxxxx
                WriteByte(codePoint & 0x3f | 0x80); // 10xxxxxx

            }
            else
            {
                throw new ArgumentException(
                    "Unexpected code point: " + codePoint.ToString("x"));
            }

            return this;
        }

        public EasyBuffer WriteString(string @string, Encoding charset)
        {
            return WriteString(@string, 0, @string.Length(), charset);
        }

        public EasyBuffer WriteString(string @string, int beginIndex, int endIndex, Encoding charset)
        {
            if (@string == null) throw new ArgumentException("string == null");
            if (beginIndex < 0) throw new IllegalAccessException("beginIndex < 0: " + beginIndex);
            if (endIndex < beginIndex)
            {
                throw new ArgumentException("endIndex < beginIndex: " + endIndex + " < " + beginIndex);
            }
            if (endIndex > @string.Length())
            {
                throw new ArgumentException(
                    "endIndex > string.length: " + endIndex + " > " + @string.Length());
            }
            if (charset == null) throw new ArgumentException("charset == null");
            if (charset.Equals(Util.UTF_8)) return WriteUtf8(@string, beginIndex, endIndex);
            byte[] data = charset.GetBytes(@string.Substring(beginIndex, endIndex));
            return Write(data, 0, data.Length);
        }

        public EasyBuffer WriteByte(int b)
        {
            Segment tail = WritableSegment(1);
            tail.Data[tail.Limit++] = (byte)b;
            _size += 1;
            return this;
        }

        public EasyBuffer WriteShort(int s)
        {
            Segment tail = WritableSegment(2);
            byte[] data = tail.Data;
            int limit = tail.Limit;
            data[limit++] = (byte)((s >> 8) & 0xff);
            data[limit++] = (byte)(s & 0xff);
            tail.Limit = limit;
            _size += 2;
            return this;
        }

        public EasyBuffer WriteShortLe(int s)
        {
            return WriteShort(Util.ReverseBytesShort((short)s));
        }

        public EasyBuffer WriteInt(int i)
        {
            Segment tail = WritableSegment(4);
            byte[] data = tail.Data;
            int limit = tail.Limit;
            data[limit++] = (byte)((i >> 24) & 0xff);
            data[limit++] = (byte)((i >> 16) & 0xff);
            data[limit++] = (byte)((i >> 8) & 0xff);
            data[limit++] = (byte)(i & 0xff);
            tail.Limit = limit;
            _size += 4;
            return this;
        }

        public EasyBuffer WriteIntLe(int i)
        {
            return WriteInt(Util.ReverseBytesInt(i));
        }

        public EasyBuffer WriteLong(long v)
        {
            Segment tail = WritableSegment(8);
            byte[] data = tail.Data;
            int limit = tail.Limit;
            data[limit++] = (byte)((v >> 56) & 0xff);
            data[limit++] = (byte)((v >> 48) & 0xff);
            data[limit++] = (byte)((v >> 40) & 0xff);
            data[limit++] = (byte)((v >> 32) & 0xff);
            data[limit++] = (byte)((v >> 24) & 0xff);
            data[limit++] = (byte)((v >> 16) & 0xff);
            data[limit++] = (byte)((v >> 8) & 0xff);
            data[limit++] = (byte)(v & 0xff);
            tail.Limit = limit;
            _size += 8;
            return this;
        }

        public EasyBuffer WriteLongLe(long v)
        {
            return WriteLong(Util.ReverseBytesLong(v));
        }

        public EasyBuffer WriteDecimalLong(long v)
        {
            if (v == 0)
            {
                // Both a shortcut and required since the following code can't handle zero.
                return WriteByte('0');
            }

            bool negative = false;
            if (v < 0)
            {
                v = -v;
                if (v < 0)
                { // Only true for Long.MIN_VALUE.
                    return WriteUtf8("-9223372036854775808");
                }
                negative = true;
            }

            // Binary search for character width which favors matching lower numbers.
            int width = //
                  v < 100000000L
                ? v < 10000L
                ? v < 100L
                ? v < 10L ? 1 : 2
                : v < 1000L ? 3 : 4
                : v < 1000000L
                ? v < 100000L ? 5 : 6
                : v < 10000000L ? 7 : 8
                : v < 1000000000000L
                ? v < 10000000000L
                ? v < 1000000000L ? 9 : 10
                : v < 100000000000L ? 11 : 12
                : v < 1000000000000000L
                ? v < 10000000000000L ? 13
                : v < 100000000000000L ? 14 : 15
                : v < 100000000000000000L
                ? v < 10000000000000000L ? 16 : 17
                : v < 1000000000000000000L ? 18 : 19;
            if (negative)
            {
                ++width;
            }

            Segment tail = WritableSegment(width);
            byte[] data = tail.Data;
            int pos = tail.Limit + width; // We write backwards from right to left.
            while (v != 0)
            {
                int digit = (int)(v % 10);
                data[--pos] = DIGITS[digit];
                v /= 10;
            }
            if (negative)
            {
                data[--pos] = (byte)'-';
            }

            tail.Limit += width;
            this._size += width;
            return this;
        }

        public EasyBuffer WriteHexadecimalUnsignedLong(long v)
        {
            if (v == 0)
            {
                // Both a shortcut and required since the following code can't handle zero.
                return WriteByte('0');
            }
            int width = v.HighestOneBit().NumberOfTrailingZeros() / 4 + 1;
            Segment tail = WritableSegment(width);
            byte[] data = tail.Data;
            for (int pos = tail.Limit + width - 1, start = tail.Limit; pos >= start; pos--)
            {
                data[pos] = DIGITS[(int)(v & 0xF)];
                v >>= 4;
            }
            tail.Limit += width;
            Size += width;
            return this;
        }

        public EasyBuffer Emit()
        {
            return this;
        }

        public EasyBuffer EmitCompleteSegments()
        {
            return this; // Nowhere to emit to!
        }

        public Stream OutputStream()
        {
            return new EasyStream(this);
        }

        public void Write(EasyBuffer source, long byteCount)
        {
            // Move bytes from the head Of the source buffer to the tail Of this buffer
            // while balancing two conflicting goals: don't waste CPU and don't waste
            // memory.
            //
            //
            // Don't waste CPU (ie. don't copy data around).
            //
            // Copying large amounts Of data is expensive. Instead, we prefer to
            // reassign entire segments from one buffer to the other.
            //
            //
            // Don't waste memory.
            //
            // As an invariant, adjacent pairs Of segments in a buffer should be at
            // least 50% full, except for the head segment and the tail segment.
            //
            // The head segment cannot maintain the invariant because the application is
            // consuming bytes from this segment, decreasing its level.
            //
            // The tail segment cannot maintain the invariant because the application is
            // producing bytes, which may require new nearly-empty tail segments to be
            // appended.
            //
            //
            // Moving segments between buffers
            //
            // When writing one buffer to another, we prefer to reassign entire segments
            // over copying bytes into their most compact form. Suppose we have a buffer
            // with these segment levels [91%, 61%]. If we append a buffer with a
            // single [72%] segment, that yields [91%, 61%, 72%]. No bytes are copied.
            //
            // Or suppose we have a buffer with these segment levels: [100%, 2%], and we
            // want to append it to a buffer with these segment levels [99%, 3%]. This
            // operation will yield the following segments: [100%, 2%, 99%, 3%]. That
            // is, we do not spend time copying bytes around to achieve more efficient
            // memory use like [100%, 100%, 4%].
            //
            // When combining buffers, we will compact adjacent buffers when their
            // combined level doesn't exceed 100%. For example, when we start with
            // [100%, 40%] and append [30%, 80%], the result is [100%, 70%, 80%].
            //
            //
            // Splitting segments
            //
            // Occasionally we write only part Of a source buffer to a sink buffer. For
            // example, given a sink [51%, 91%], we may want to write the first 30% Of
            // a source [92%, 82%] to it. To simplify, we first transform the source to
            // an equivalent buffer [30%, 62%, 82%] and then move the head segment,
            // yielding sink [51%, 91%, 30%] and source [62%, 82%].

            if (source == null) throw new ArgumentException("source == null");
            if (source == this) throw new ArgumentException("source == this");
            Util.CheckOffsetAndCount(source._size, 0, byteCount);

            while (byteCount > 0)
            {
                // Is a prefix Of the source's head segment all that we need to move?
                if (byteCount < (source._head.Limit - source._head.Pos))
                {
                    Segment tail = _head != null ? _head.Prev : null;
                    if (tail != null && tail.Owner
                        && (byteCount + tail.Limit - (tail.Shared ? 0 : tail.Pos) <= Segment.SIZE))
                    {
                        // Our existing segments are sufficient. Move bytes from source's head to our tail.
                        source._head.WriteTo(tail, (int)byteCount);
                        source._size -= byteCount;
                        _size += byteCount;
                        return;
                    }
                    else
                    {
                        // We're going to need another segment. Split the source's head
                        // segment in two, then move the first Of those two to this buffer.
                        source._head = source._head.Split((int)byteCount);
                    }
                }

                // Remove the source's head segment and append it to our tail.
                Segment segmentToMove = source._head;
                long movedByteCount = segmentToMove.Limit - segmentToMove.Pos;
                source._head = segmentToMove.Pop();
                if (_head == null)
                {
                    _head = segmentToMove;
                    _head.Next = _head.Prev = _head;
                }
                else
                {
                    Segment tail = _head.Prev;
                    tail = tail.Push(segmentToMove);
                    tail.Compact();
                }
                source._size -= movedByteCount;
                _size += movedByteCount;
                byteCount -= movedByteCount;
            }
        }

        public void Flush()
        {
        }

        public Timeout Timeout()
        {
            return IO.Timeout.NONE;
        }

        public void Dispose()
        {
        }

        public bool Exhausted()
        {
            return _size == 0;
        }

        public void Require(long byteCount)
        {
            if (_size < byteCount) throw new IndexOutOfRangeException();
        }

        public bool Request(long byteCount)
        {
            return _size >= byteCount;
        }

        public byte ReadByte()
        {
            if (_size == 0) throw new IllegalStateException("size == 0");

            Segment segment = _head;
            int pos = segment.Pos;
            int limit = segment.Limit;

            byte[] data = segment.Data;
            byte b = data[pos++];
            _size -= 1;

            if (pos == limit)
            {
                _head = segment.Pop();
                SegmentPool.Recycle(segment);
            }
            else
            {
                segment.Pos = pos;
            }

            return b;
        }

        public short ReadShort()
        {
            if (_size < 2) throw new IllegalStateException("size < 2: " + _size);

            Segment segment = _head;
            int pos = segment.Pos;
            int limit = segment.Limit;

            // If the short is split across multiple segments, delegate to readByte().
            if (limit - pos < 2)
            {
                int ss = (ReadByte() & 0xff) << 8
                    | (ReadByte() & 0xff);
                return (short)ss;
            }

            byte[] data = segment.Data;
            int s = (data[pos++] & 0xff) << 8
                | (data[pos++] & 0xff);
            _size -= 2;

            if (pos == limit)
            {
                _head = segment.Pop();
                SegmentPool.Recycle(segment);
            }
            else
            {
                segment.Pos = pos;
            }

            return (short)s;
        }

        public short ReadShortLe()
        {
            return Util.ReverseBytesShort(ReadShort());
        }

        public int ReadInt()
        {
            if (_size < 4) throw new IllegalStateException("size < 4: " + _size);

            Segment segment = _head;
            int pos = segment.Pos;
            int limit = segment.Limit;

            // If the int is split across multiple segments, delegate to readByte().
            if (limit - pos < 4)
            {
                return (ReadByte() & 0xff) << 24
                    | (ReadByte() & 0xff) << 16
                    | (ReadByte() & 0xff) << 8
                    | (ReadByte() & 0xff);
            }

            byte[] data = segment.Data;
            int i = (data[pos++] & 0xff) << 24
                | (data[pos++] & 0xff) << 16
                | (data[pos++] & 0xff) << 8
                | (data[pos++] & 0xff);
            _size -= 4;

            if (pos == limit)
            {
                _head = segment.Pop();
                SegmentPool.Recycle(segment);
            }
            else
            {
                segment.Pos = pos;
            }

            return i;
        }

        public int ReadIntLe()
        {
            return Util.ReverseBytesInt(ReadInt());
        }

        public long ReadLong()
        {
            if (_size < 8) throw new IllegalStateException("size < 8: " + _size);

            Segment segment = _head;
            int pos = segment.Pos;
            int limit = segment.Limit;

            // If the long is split across multiple segments, delegate to readInt().
            if (limit - pos < 8)
            {
                return (ReadInt() & 0xffffffffL) << 32
                    | (ReadInt() & 0xffffffffL);
            }

            byte[] data = segment.Data;
            long v = (data[pos++] & 0xffL) << 56
                | (data[pos++] & 0xffL) << 48
                | (data[pos++] & 0xffL) << 40
                | (data[pos++] & 0xffL) << 32
                | (data[pos++] & 0xffL) << 24
                | (data[pos++] & 0xffL) << 16
                | (data[pos++] & 0xffL) << 8
                | (data[pos++] & 0xffL);
            _size -= 8;

            if (pos == limit)
            {
                _head = segment.Pop();
                SegmentPool.Recycle(segment);
            }
            else
            {
                segment.Pos = pos;
            }

            return v;
        }

        public long ReadLongLe()
        {
            return Util.ReverseBytesLong(ReadLong());
        }

        public long ReadDecimalLong()
        {
            if (_size == 0) throw new IllegalStateException("size == 0");

            // This value is always built negatively in order to accommodate Long.MIN_VALUE.
            long value = 0;
            int seen = 0;
            var negative = false;
            var done = false;

            long overflowZone = long.MinValue / 10;
            long overflowDigit = (long.MinValue % 10) + 1;

            do
            {
                Segment segment = _head;

                byte[] data = segment.Data;
                int pos = segment.Pos;
                int limit = segment.Limit;

                for (; pos < limit; pos++, seen++)
                {
                    byte b = data[pos];
                    if (b >= '0' && b <= '9')
                    {
                        int digit = '0' - b;

                        // Detect when the digit would cause an overflow.
                        if (value < overflowZone || value == overflowZone && digit < overflowDigit)
                        {
                            EasyBuffer buffer = (EasyBuffer)new EasyBuffer().WriteDecimalLong(value).WriteByte(b);
                            if (!negative) buffer.ReadByte(); // Skip negative sign.
                            throw new FormatException("Number too large: " + buffer.ReadUtf8());
                        }
                        value *= 10;
                        value += digit;
                    }
                    else if (b == '-' && seen == 0)
                    {
                        negative = true;
                        overflowDigit -= 1;
                    }
                    else
                    {
                        if (seen == 0)
                        {
                            throw new FormatException(
                                "Expected leading [0-9] or '-' character but was 0x" + b.ToString("x"));
                        }
                        // Set a flag to stop iteration. We still need to run through segment updating below.
                        done = true;
                        break;
                    }
                }

                if (pos == limit)
                {
                    _head = segment.Pop();
                    SegmentPool.Recycle(segment);
                }
                else
                {
                    segment.Pos = pos;
                }
            } while (!done && _head != null);

            _size -= seen;
            return negative ? value : -value;
        }

        public ulong ReadHexadecimalUnsignedLong()
        {
            if (_size == 0) throw new IllegalStateException("size == 0");

            ulong value = 0;
            int seen = 0;
            bool done = false;

            do
            {
                Segment segment = _head;

                byte[] data = segment.Data;
                int pos = segment.Pos;
                int limit = segment.Limit;

                for (; pos < limit; pos++, seen++)
                {
                    int digit;

                    byte b = data[pos];
                    if (b >= '0' && b <= '9')
                    {
                        digit = b - '0';
                    }
                    else if (b >= 'a' && b <= 'f')
                    {
                        digit = b - 'a' + 10;
                    }
                    else if (b >= 'A' && b <= 'F')
                    {
                        digit = b - 'A' + 10; // We never write uppercase, but we support reading it.
                    }
                    else
                    {
                        if (seen == 0)
                        {
                            throw new FormatException(
                                "Expected leading [0-9a-fA-F] character but was 0x" + b.ToString("x"));
                        }
                        // Set a flag to stop iteration. We still need to run through segment updating below.
                        done = true;
                        break;
                    }

                    // Detect when the shift will overflow.
                    if ((value & 0xf000000000000000L) != 0)
                    {
                        var buffer = new EasyBuffer().WriteHexadecimalUnsignedLong((long)value).WriteByte(b);
                        throw new FormatException("Number too large: " + buffer.ReadUtf8());
                    }

                    value <<= 4;
                    value |= (uint)digit;
                }

                if (pos == limit)
                {
                    _head = segment.Pop();
                    SegmentPool.Recycle(segment);
                }
                else
                {
                    segment.Pos = pos;
                }
            } while (!done && _head != null);

            _size -= seen;
            return value;
        }

        public void Skip(long byteCount)
        {
            while (byteCount > 0)
            {
                if (_head == null) throw new IndexOutOfRangeException();

                int toSkip = (int)Math.Min(byteCount, _head.Limit - _head.Pos);
                _size -= toSkip;
                byteCount -= toSkip;
                _head.Pos += toSkip;

                if (_head.Pos == _head.Limit)
                {
                    Segment toRecycle = _head;
                    _head = toRecycle.Pop();
                    SegmentPool.Recycle(toRecycle);
                }
            }
        }

        public ByteString ReadByteString()
        {
            return new ByteString(ReadByteArray());
        }

        public ByteString ReadByteString(long byteCount)
        {
            return new ByteString(ReadByteArray(byteCount));
        }

        public int Select(Options options)
        {
            int index = SelectPrefix(options, false);
            if (index == -1) return -1;

            // If the prefix match actually matched a full byte string, consume it and return it.
            int selectedSize = options._byteStrings[index].Size();
            try
            {
                Skip(selectedSize);
            }
            catch (Exception e)
            {
                throw new AssertionException();
            }
            return index;
        }

        public byte[] ReadByteArray()
        {
            return ReadByteArray(_size);
        }

        public byte[] ReadByteArray(long byteCount)
        {
            Util.CheckOffsetAndCount(_size, 0, byteCount);
            if (byteCount > int.MaxValue)
            {
                throw new ArgumentException("byteCount > Integer.MAX_VALUE: " + byteCount);
            }

            byte[] result = new byte[(int)byteCount];
            ReadFully(result);
            return result;
        }

        public int Read(byte[] sink)
        {
            return Read(sink, 0, sink.Length);
        }

        public void ReadFully(byte[] sink)
        {
            int offset = 0;
            while (offset < sink.Length)
            {
                int read = this.Read(sink, offset, sink.Length - offset);
                if (read == -1) throw new IndexOutOfRangeException();
                offset += read;
            }
        }

        public int Read(byte[] sink, int offset, int byteCount)
        {
            Util.CheckOffsetAndCount(sink.Length, offset, byteCount);

            Segment s = _head;
            if (s == null) return -1;
            int toCopy = Math.Min(byteCount, s.Limit - s.Pos);
            Array.Copy(s.Data, s.Pos, sink, offset, toCopy);

            s.Pos += toCopy;
            _size -= toCopy;

            if (s.Pos == s.Limit)
            {
                _head = s.Pop();
                SegmentPool.Recycle(s);
            }

            return toCopy;
        }

        public void ReadFully(EasyBuffer sink, long byteCount)
        {
            if (_size < byteCount)
            {
                sink.Write(this, _size); // Exhaust ourselves.
                throw new IndexOutOfRangeException();
            }
            sink.Write(this, byteCount);
        }

        public long ReadAll(Sink sink)
        {
            long byteCount = _size;
            if (byteCount > 0)
            {
                sink.Write(this, byteCount);
            }
            return byteCount;
        }

        public string ReadUtf8()
        {
            return ReadString(_size, Util.UTF_8);
        }

        public string ReadUtf8(long byteCount)
        {
            return ReadString(byteCount, Util.UTF_8);
        }

        public string ReadUtf8Line()
        {
            long newline = IndexOf((byte)'\n');
            if (newline == -1)
            {
                return _size != 0 ? ReadUtf8(_size) : null;
            }
            return ReadUtf8Line(newline);
        }

        public string ReadUtf8Line(long newline)
        {
            if (newline > 0 && GetByte(newline - 1) == '\r')
            {
                // Read everything until '\r\n', then skip the '\r\n'.
                string result = ReadUtf8((newline - 1));
                Skip(2);
                return result;
            }
            else
            {
                // Read everything until '\n', then skip the '\n'.
                string result = ReadUtf8(newline);
                Skip(1);
                return result;
            }
        }

        public byte GetByte(long pos)
        {
            Util.CheckOffsetAndCount(_size, pos, 1);
            if (_size - pos > pos)
            {
                for (Segment s = _head; true; s = s.Next)
                {
                    int segmentByteCount = s.Limit - s.Pos;
                    if (pos < segmentByteCount) return s.Data[s.Pos + (int)pos];
                    pos -= segmentByteCount;
                }
            }
            else
            {
                pos -= _size;
                for (Segment s = _head.Prev; true; s = s.Prev)
                {
                    pos += s.Limit - s.Pos;
                    if (pos >= 0) return s.Data[s.Pos + (int)pos];
                }
            }
        }

        public string ReadUtf8LineStrict()
        {
            return ReadUtf8LineStrict(long.MaxValue);
        }

        public string ReadUtf8LineStrict(long limit)
        {
            if (limit < 0) throw new ArgumentException("limit < 0: " + limit);
            long scanLength = limit == long.MaxValue ? long.MaxValue : limit + 1;
            long newline = IndexOf((byte)'\n', 0, scanLength);
            if (newline != -1) return ReadUtf8Line(newline);
            if (scanLength < _size
                && GetByte(scanLength - 1) == '\r' && GetByte(scanLength) == '\n')
            {
                return ReadUtf8Line(scanLength); // The line was 'limit' UTF-8 bytes followed by \r\n.
            }
            var data = new EasyBuffer();
            CopyTo(data, 0, Math.Min(32, _size));
            throw new IndexOutOfRangeException("\\n not found: limit=" + Math.Min(_size, limit)
                + " content=" + data.ReadByteString().Hex() + '…');
        }

        public int ReadUtf8CodePoint()
        {
            if (_size == 0) throw new IndexOutOfRangeException();

            byte b0 = GetByte(0);
            int codePoint;
            int byteCount;
            int min;

            if ((b0 & 0x80) == 0)
            {
                // 0xxxxxxx.
                codePoint = b0 & 0x7f;
                byteCount = 1; // 7 bits (ASCII).
                min = 0x0;

            }
            else if ((b0 & 0xe0) == 0xc0)
            {
                // 0x110xxxxx
                codePoint = b0 & 0x1f;
                byteCount = 2; // 11 bits (5 + 6).
                min = 0x80;

            }
            else if ((b0 & 0xf0) == 0xe0)
            {
                // 0x1110xxxx
                codePoint = b0 & 0x0f;
                byteCount = 3; // 16 bits (4 + 6 + 6).
                min = 0x800;

            }
            else if ((b0 & 0xf8) == 0xf0)
            {
                // 0x11110xxx
                codePoint = b0 & 0x07;
                byteCount = 4; // 21 bits (3 + 6 + 6 + 6).
                min = 0x10000;

            }
            else
            {
                // We expected the first byte Of a code point but got something else.
                Skip(1);
                return REPLACEMENT_CHARACTER;
            }

            if (_size < byteCount)
            {
                throw new IndexOutOfRangeException("size < " + byteCount + ": " + _size
                    + " (to read code point prefixed 0x" + b0.ToString("x") + ")");
            }

            // Read the continuation bytes. If we encounter a non-continuation byte, the sequence consumed
            // thus far is truncated and is decoded as the replacement character. That non-continuation byte
            // is left in the stream for processing by the next call to readUtf8CodePoint().
            for (int i = 1; i < byteCount; i++)
            {
                byte b = GetByte(i);
                if ((b & 0xc0) == 0x80)
                {
                    // 0x10xxxxxx
                    codePoint <<= 6;
                    codePoint |= b & 0x3f;
                }
                else
                {
                    Skip(i);
                    return REPLACEMENT_CHARACTER;
                }
            }

            Skip(byteCount);

            if (codePoint > 0x10ffff)
            {
                return REPLACEMENT_CHARACTER; // Reject code points larger than the Unicode maximum.
            }

            if (codePoint >= 0xd800 && codePoint <= 0xdfff)
            {
                return REPLACEMENT_CHARACTER; // Reject partial surrogates.
            }

            if (codePoint < min)
            {
                return REPLACEMENT_CHARACTER; // Reject overlong code points.
            }

            return codePoint;
        }

        public string ReadString(Encoding charset)
        {
            return ReadString(_size, charset);
        }

        public string ReadString(long byteCount, Encoding charset)
        {
            Util.CheckOffsetAndCount(_size, 0, byteCount);
            if (charset == null) throw new ArgumentException("charset == null");
            if (byteCount > int.MaxValue)
            {
                throw new ArgumentException("byteCount > Integer.MAX_VALUE: " + byteCount);
            }
            if (byteCount == 0) return "";

            Segment s = _head;
            if (s.Pos + byteCount > s.Limit)
            {
                // If the string spans multiple segments, delegate to readBytes().
                return charset.GetString(ReadByteArray(byteCount));
            }
            string result = charset.GetString(s.Data, s.Pos, (int)byteCount);
            s.Pos += (int)byteCount;
            _size -= byteCount;

            if (s.Pos == s.Limit)
            {
                _head = s.Pop();
                SegmentPool.Recycle(s);
            }

            return result;
        }

        public long IndexOf(byte b)
        {
            return IndexOf(b, 0, long.MaxValue);
        }

        public long IndexOf(byte b, long fromIndex)
        {
            return IndexOf(b, fromIndex, long.MaxValue);
        }

        public long IndexOf(byte b, long fromIndex, long toIndex)
        {
            if (fromIndex < 0 || toIndex < fromIndex)
            {
                throw new ArgumentException(
                    string.Format("size=%s fromIndex=%s toIndex=%s", _size, fromIndex, toIndex));
            }

            if (toIndex > _size) toIndex = _size;
            if (fromIndex == toIndex) return -1L;

            Segment s;
            long offset;

        // TODO(jwilson): extract this to a shared helper method when can do so without allocating.
        findSegmentAndOffset:
            {
                // Pick the first segment to scan. This is the first segment with offset <= fromIndex.
                s = _head;
                if (s == null)
                {
                    // No segments to scan!
                    return -1L;
                }
                else if (_size - fromIndex < fromIndex)
                {
                    // We're scanning in the back half Of this buffer. Find the segment starting at the back.
                    offset = _size;
                    while (offset > fromIndex)
                    {
                        s = s.Prev;
                        offset -= (s.Limit - s.Pos);
                    }
                }
                else
                {
                    // We're scanning in the front half Of this buffer. Find the segment starting at the front.
                    offset = 0L;
                    for (long nextOffset; (nextOffset = offset + (s.Limit - s.Pos)) < fromIndex;)
                    {
                        s = s.Next;
                        offset = nextOffset;
                    }
                }
            }

            // Scan through the segments, searching for b.
            while (offset < toIndex)
            {
                byte[] data = s.Data;
                int limit = (int)Math.Min(s.Limit, s.Pos + toIndex - offset);
                int pos = (int)(s.Pos + fromIndex - offset);
                for (; pos < limit; pos++)
                {
                    if (data[pos] == b)
                    {
                        return pos - s.Pos + offset;
                    }
                }

                // Not in this segment. Try the next one.
                offset += (s.Limit - s.Pos);
                fromIndex = offset;
                s = s.Next;
            }

            return -1L;
        }

        public long IndexOf(ByteString bytes)
        {
            return IndexOf(bytes, 0);
        }

        public long IndexOf(ByteString bytes, long fromIndex)
        {
            if (bytes.Size() == 0) throw new ArgumentException("bytes is empty");
            if (fromIndex < 0) throw new ArgumentException("fromIndex < 0");

            Segment s;
            long offset;

        // TODO(jwilson): extract this to a shared helper method when can do so without allocating.
        findSegmentAndOffset:
            {
                // Pick the first segment to scan. This is the first segment with offset <= fromIndex.
                s = _head;
                if (s == null)
                {
                    // No segments to scan!
                    return -1L;
                }
                else if (_size - fromIndex < fromIndex)
                {
                    // We're scanning in the back half Of this buffer. Find the segment starting at the back.
                    offset = _size;
                    while (offset > fromIndex)
                    {
                        s = s.Prev;
                        offset -= (s.Limit - s.Pos);
                    }
                }
                else
                {
                    // We're scanning in the front half Of this buffer. Find the segment starting at the front.
                    offset = 0L;
                    for (long nextOffset; (nextOffset = offset + (s.Limit - s.Pos)) < fromIndex;)
                    {
                        s = s.Next;
                        offset = nextOffset;
                    }
                }
            }

            // Scan through the segments, searching for the lead byte. Each time that is found, delegate to
            // rangeEquals() to check for a complete match.
            byte b0 = bytes.GetByte(0);
            int bytesSize = bytes.Size();
            long resultLimit = _size - bytesSize + 1;
            while (offset < resultLimit)
            {
                // Scan through the current segment.
                byte[] data = s.Data;
                int segmentLimit = (int)Math.Min(s.Limit, s.Pos + resultLimit - offset);
                for (int pos = (int)(s.Pos + fromIndex - offset); pos < segmentLimit; pos++)
                {
                    if (data[pos] == b0 && RangeEquals(s, pos + 1, bytes, 1, bytesSize))
                    {
                        return pos - s.Pos + offset;
                    }
                }

                // Not in this segment. Try the next one.
                offset += (s.Limit - s.Pos);
                fromIndex = offset;
                s = s.Next;
            }

            return -1L;
        }

        public long IndexOfElement(ByteString targetBytes)
        {
            return IndexOfElement(targetBytes, 0);
        }

        public long IndexOfElement(ByteString targetBytes, long fromIndex)
        {
            if (fromIndex < 0) throw new ArgumentException("fromIndex < 0");

            Segment s;
            long offset;

        // TODO(jwilson): extract this to a shared helper method when can do so without allocating.
        findSegmentAndOffset:
            {
                // Pick the first segment to scan. This is the first segment with offset <= fromIndex.
                s = _head;
                if (s == null)
                {
                    // No segments to scan!
                    return -1L;
                }
                else if (_size - fromIndex < fromIndex)
                {
                    // We're scanning in the back half Of this buffer. Find the segment starting at the back.
                    offset = _size;
                    while (offset > fromIndex)
                    {
                        s = s.Prev;
                        offset -= (s.Limit - s.Pos);
                    }
                }
                else
                {
                    // We're scanning in the front half Of this buffer. Find the segment starting at the front.
                    offset = 0L;
                    for (long nextOffset; (nextOffset = offset + (s.Limit - s.Pos)) < fromIndex;)
                    {
                        s = s.Next;
                        offset = nextOffset;
                    }
                }
            }

            // Special case searching for one Of two bytes. This is a common case for tools like Moshi,
            // which search for pairs Of chars like `\r` and `\n` or {@code `"` and `\`. The impact Of this
            // optimization is a ~5x speedup for this case without a substantial cost to other cases.
            if (targetBytes.Size() == 2)
            {
                // Scan through the segments, searching for either Of the two bytes.
                byte b0 = targetBytes.GetByte(0);
                byte b1 = targetBytes.GetByte(1);
                while (offset < _size)
                {
                    byte[] data = s.Data;
                    for (int pos = (int)(s.Pos + fromIndex - offset), limit = s.Limit; pos < limit; pos++)
                    {
                        int b = data[pos];
                        if (b == b0 || b == b1)
                        {
                            return pos - s.Pos + offset;
                        }
                    }

                    // Not in this segment. Try the next one.
                    offset += (s.Limit - s.Pos);
                    fromIndex = offset;
                    s = s.Next;
                }
            }
            else
            {
                // Scan through the segments, searching for a byte that's also in the array.
                byte[] targetByteArray = targetBytes.InternalArray();
                while (offset < _size)
                {
                    byte[] data = s.Data;
                    for (int pos = (int)(s.Pos + fromIndex - offset), limit = s.Limit; pos < limit; pos++)
                    {
                        int b = data[pos];
                        foreach (var t in targetByteArray)
                        {
                            if (b == t) return pos - s.Pos + offset;
                        }
                    }

                    // Not in this segment. Try the next one.
                    offset += (s.Limit - s.Pos);
                    fromIndex = offset;
                    s = s.Next;
                }
            }

            return -1L;
        }

        public bool RangeEquals(long offset, ByteString bytes)
        {
            return RangeEquals(offset, bytes, 0, bytes.Size());
        }

        public bool RangeEquals(long offset, ByteString bytes, int bytesOffset, int byteCount)
        {
            if (offset < 0
          || bytesOffset < 0
          || byteCount < 0
          || _size - offset < byteCount
          || bytes.Size() - bytesOffset < byteCount)
            {
                return false;
            }
            for (int i = 0; i < byteCount; i++)
            {
                if (GetByte(offset + i) != bytes.GetByte(bytesOffset + i))
                {
                    return false;
                }
            }
            return true;
        }


        public Stream InputStream()
        {
            return new EasyStream(this);
        }

        public long Read(EasyBuffer sink, long byteCount)
        {
            if (sink == null) throw new ArgumentException("sink == null");
            if (byteCount < 0) throw new ArgumentException("byteCount < 0: " + byteCount);
            if (_size == 0) return -1L;
            if (byteCount > _size) byteCount = _size;
            sink.Write(this, byteCount);
            return byteCount;
        }

        public Segment WritableSegment(int minimumCapacity)
        {
            if (minimumCapacity < 1 || minimumCapacity > Segment.SIZE) throw new ArgumentException();
            if (_head == null)
            {
                _head = SegmentPool.Take(); // Acquire a first segment.
                return _head.Next = _head.Prev = _head;
            }
            Segment tail = _head.Prev;
            if (tail.Limit + minimumCapacity > Segment.SIZE || !tail.Owner)
            {
                tail = tail.Push(SegmentPool.Take()); // Append a new empty segment to fill up.
            }
            return tail;
        }
        /** Copy the contents Of this to {@code out}. */
        public EasyBuffer CopyTo(Stream @out)
        {
            return CopyTo(@out, 0, _size);
        }

        /**
         * Copy {@code byteCount} bytes from this, starting at {@code offset}, to
         * {@code out}.
         */
        public EasyBuffer CopyTo(Stream @out, long offset, long byteCount)
        {
            if (@out == null) throw new ArgumentException("out == null");
            Util.CheckOffsetAndCount(_size, offset, byteCount);
            if (byteCount == 0) return this;

            // Skip segments that we aren't copying from.
            Segment s = _head;
            for (; offset >= (s.Limit - s.Pos); s = s.Next)
            {
                offset -= (s.Limit - s.Pos);
            }

            // Copy from one segment at a time.
            for (; byteCount > 0; s = s.Next)
            {
                int pos = (int)(s.Pos + offset);
                int toCopy = (int)Math.Min(s.Limit - pos, byteCount);
                @out.Write(s.Data, pos, toCopy);
                byteCount -= toCopy;
                offset = 0;
            }

            return this;
        }

        /** Copy {@code byteCount} bytes from this, starting at {@code offset}, to {@code out}. */
        public EasyBuffer CopyTo(EasyBuffer @out, long offset, long byteCount)
        {
            if (@out == null) throw new ArgumentException("out == null");
            Util.CheckOffsetAndCount(_size, offset, byteCount);
            if (byteCount == 0)
            {
                return this;
            }
            @out._size += byteCount;

            // Skip segments that we aren't copying from.
            Segment s = _head;
            for (; offset >= (s.Limit - s.Pos); s = s.Next)
            {
                offset -= (s.Limit - s.Pos);
            }

            // Copy one segment at a time.
            for (; byteCount > 0; s = s.Next)
            {
                Segment copy = s.SharedCopy();
                copy.Pos += (int)offset;
                copy.Limit = Math.Min(copy.Pos + (int)byteCount, copy.Limit);
                if (@out._head == null)
                {
                    @out._head = copy.Next = copy.Prev = copy;
                }
                else
                {
                    @out._head.Prev.Push(copy);
                }
                byteCount -= copy.Limit - copy.Pos;
                offset = 0;
            }

            return this;
        }

        /// <summary>
        /// Returns true if the range within this buffer starting at {@code segmentPos} in {@code segment} is equal to { @code bytes [bytesOffset..bytesLimit)}.
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="segmentPos"></param>
        /// <param name="bytes"></param>
        /// <param name="bytesOffset"></param>
        /// <param name="bytesLimit"></param>
        /// <returns></returns>
        private bool RangeEquals(
            Segment segment, int segmentPos, ByteString bytes, int bytesOffset, int bytesLimit)
        {
            int segmentLimit = segment.Limit;
            byte[] data = segment.Data;

            for (int i = bytesOffset; i < bytesLimit;)
            {
                if (segmentPos == segmentLimit)
                {
                    segment = segment.Next;
                    data = segment.Data;
                    segmentPos = segment.Pos;
                    segmentLimit = segment.Limit;
                }

                if (data[segmentPos] != bytes.GetByte(i))
                {
                    return false;
                }

                segmentPos++;
                i++;
            }

            return true;
        }


        /**
         * Returns the index of a value in options that is a prefix of this buffer. Returns -1 if no value
         * is found. This method does two simultaneous iterations: it iterates the trie and it iterates
         * this buffer. It returns when it reaches a result in the trie, when it mismatches in the trie,
         * and when the buffer is exhausted.
         *
         * @param selectTruncated true to return -2 if a possible result is present but truncated. For
         *     example, this will return -2 if the buffer contains [ab] and the options are [abc, abd].
         *     Note that this is made complicated by the fact that options are listed in preference order,
         *     and one option may be a prefix of another. For example, this returns -2 if the buffer
         *     contains [ab] and the options are [abc, a].
         */
        internal int SelectPrefix(Options options, bool selectTruncated)
        {
            Segment head = this._head;
            if (head == null)
            {
                if (selectTruncated) return -2; // A result is present but truncated.
                return options.IndexOf(ByteString.EMPTY);
            }

            Segment s = head;
            byte[] data = head.Data;
            int pos = head.Pos;
            int limit = head.Limit;

            int[] trie = options._trie;
            int triePos = 0;

            int prefixIndex = -1;

        navigateTrie:
            while (true)
            {
                int scanOrSelect = trie[triePos++];

                int possiblePrefixIndex = trie[triePos++];
                if (possiblePrefixIndex != -1)
                {
                    prefixIndex = possiblePrefixIndex;
                }

                int nextStep = 0;

                if (s == null)
                {
                    break;
                }
                else if (scanOrSelect < 0)
                {
                    // Scan: take multiple bytes from the buffer and the trie, looking for any mismatch.
                    int scanByteCount = -1 * scanOrSelect;
                    int trieLimit = triePos + scanByteCount;
                    while (true)
                    {
                        int b = data[pos++] & 0xff;
                        if (b != trie[triePos++]) return prefixIndex; // Fail 'cause we found a mismatch.
                        var scanComplete = (triePos == trieLimit);

                        // Advance to the next buffer segment if this one is exhausted.
                        if (pos == limit)
                        {
                            s = s.Next;
                            pos = s.Pos;
                            data = s.Data;
                            limit = s.Limit;
                            if (s == head)
                            {
                                if (!scanComplete) break; // We were exhausted before the scan completed.
                                s = null; // We were exhausted at the end of the scan.
                            }
                        }

                        if (scanComplete)
                        {
                            nextStep = trie[triePos];
                            break;
                        }
                    }
                }
                else
                {
                    // Select: take one byte from the buffer and find a match in the trie.
                    int selectChoiceCount = scanOrSelect;
                    int b = data[pos++] & 0xff;
                    int selectLimit = triePos + selectChoiceCount;
                    while (true)
                    {
                        if (triePos == selectLimit) return prefixIndex; // Fail 'cause we didn't find a match.

                        if (b == trie[triePos])
                        {
                            nextStep = trie[triePos + selectChoiceCount];
                            break;
                        }

                        triePos++;
                    }

                    // Advance to the next buffer segment if this one is exhausted.
                    if (pos == limit)
                    {
                        s = s.Next;
                        pos = s.Pos;
                        data = s.Data;
                        limit = s.Limit;
                        if (s == head)
                        {
                            s = null; // No more segments! The next trie node will be our last.
                        }
                    }
                }

                if (nextStep >= 0) return nextStep; // Found a matching option.
                triePos = -nextStep; // Found another node to continue the search.
            }

            // We break out of the loop above when we've exhausted the buffer without exhausting the trie.
            if (selectTruncated) return -2; // The buffer is a prefix of at least one option.
            return prefixIndex; // Return any matches we encountered while searching for a deeper match.
        }

    }


}
