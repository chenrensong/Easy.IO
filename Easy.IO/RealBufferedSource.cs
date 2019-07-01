using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Easy.IO
{
    public class RealBufferedSource : BufferedSource
    {
        private EasyBuffer easyBuffer = new EasyBuffer();
        private bool closed = false;
        private Source source;

        public RealBufferedSource(Source source)
        {
            this.source = source;
        }

        public EasyBuffer Buffer()
        {
            return easyBuffer;
        }

        public void Dispose()
        {
            if (closed) return;
            closed = true;
            source.Dispose();
            easyBuffer.clear();
        }

        public bool exhausted()
        {
            if (closed) throw new IllegalStateException("closed");
            return easyBuffer.exhausted() && source.Read(easyBuffer, Segment.SIZE) == -1;
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
            if (closed) throw new IllegalStateException("closed");
            if (fromIndex < 0 || toIndex < fromIndex)
            {
                throw new IllegalArgumentException(
                    string.Format("fromIndex=%s toIndex=%s", fromIndex, toIndex));
            }

            while (fromIndex < toIndex)
            {
                long result = easyBuffer.IndexOf(b, fromIndex, toIndex);
                if (result != -1L) return result;

                // The byte wasn't in the buffer. Give up if we've already reached our target size or if the
                // underlying stream is exhausted.
                long lastBufferSize = easyBuffer.Size;
                if (lastBufferSize >= toIndex || source.Read(easyBuffer, Segment.SIZE) == -1) return -1L;

                // Continue the search from where we left off.
                fromIndex = Math.Max(fromIndex, lastBufferSize);
            }
            return -1L;
        }

        public long IndexOf(ByteString bytes)
        {
            return IndexOf(bytes, 0);
        }

        public long IndexOf(ByteString bytes, long fromIndex)
        {
            if (closed) throw new IllegalStateException("closed");

            while (true)
            {
                long result = easyBuffer.IndexOf(bytes, fromIndex);
                if (result != -1) return result;

                long lastBufferSize = easyBuffer.Size;
                if (source.Read(easyBuffer, Segment.SIZE) == -1) return -1L;

                // Keep searching, picking up from where we left off.
                fromIndex = Math.Max(fromIndex, lastBufferSize - bytes.Size() + 1);
            }
        }

        public long IndexOfElement(ByteString targetBytes)
        {
            return IndexOfElement(targetBytes, 0);
        }

        public long IndexOfElement(ByteString targetBytes, long fromIndex)
        {
            if (closed) throw new IllegalStateException("closed");

            while (true)
            {
                long result = easyBuffer.IndexOfElement(targetBytes, fromIndex);
                if (result != -1) return result;

                long lastBufferSize = easyBuffer.Size;
                if (source.Read(easyBuffer, Segment.SIZE) == -1) return -1L;

                // Keep searching, picking up from where we left off.
                fromIndex = Math.Max(fromIndex, lastBufferSize);
            }
        }

        public Stream InputStream()
        {
            throw new NotImplementedException();
        }

        public bool RangeEquals(long offset, ByteString bytes)
        {
            return RangeEquals(offset, bytes, 0, bytes.Size());
        }

        public bool RangeEquals(long offset, ByteString bytes, int bytesOffset, int byteCount)
        {
            if (closed) throw new IllegalStateException("closed");

            if (offset < 0
                || bytesOffset < 0
                || byteCount < 0
                || bytes.Size() - bytesOffset < byteCount)
            {
                return false;
            }
            for (int i = 0; i < byteCount; i++)
            {
                long bufferOffset = offset + i;
                if (!Request(bufferOffset + 1)) return false;
                if (easyBuffer.getByte(bufferOffset) != bytes.GetByte(bytesOffset + i)) return false;
            }
            return true;
        }

        public int Read(byte[] sink)
        {
            return Read(sink, 0, sink.Length);
        }

        public int Read(byte[] sink, int offset, int byteCount)
        {
            Util.CheckOffsetAndCount(sink.Length, offset, byteCount);

            if (easyBuffer.Size == 0)
            {
                long read = source.Read(easyBuffer, Segment.SIZE);
                if (read == -1) return -1;
            }

            int toRead = (int)Math.Min(byteCount, easyBuffer.Size);
            return easyBuffer.Read(sink, offset, toRead);
        }

        public long Read(EasyBuffer sink, long byteCount)
        {
            if (sink == null) throw new IllegalArgumentException("sink == null");
            if (byteCount < 0) throw new IllegalArgumentException("byteCount < 0: " + byteCount);
            if (closed) throw new IllegalStateException("closed");

            if (easyBuffer.Size == 0)
            {
                long read = source.Read(easyBuffer, Segment.SIZE);
                if (read == -1) return -1;
            }

            long toRead = Math.Min(byteCount, easyBuffer.Size);
            return easyBuffer.Read(sink, toRead);
        }

        public long ReadAll(Sink sink)
        {
            if (sink == null) throw new IllegalArgumentException("sink == null");

            long totalBytesWritten = 0;
            while (source.Read(easyBuffer, Segment.SIZE) != -1)
            {
                long emitByteCount = easyBuffer.completeSegmentByteCount();
                if (emitByteCount > 0)
                {
                    totalBytesWritten += emitByteCount;
                    sink.Write(easyBuffer, emitByteCount);
                }
            }
            if (easyBuffer.Size > 0)
            {
                totalBytesWritten += easyBuffer.Size;
                sink.Write(easyBuffer, easyBuffer.Size);
            }
            return totalBytesWritten;
        }

        public byte ReadByte()
        {
            Require(1);
            return easyBuffer.ReadByte();
        }

        public byte[] ReadByteArray()
        {
            easyBuffer.WriteAll(source);
            return easyBuffer.ReadByteArray();
        }

        public byte[] ReadByteArray(long byteCount)
        {
            Require(byteCount);
            return easyBuffer.ReadByteArray(byteCount);
        }

        public ByteString ReadByteString()
        {
            easyBuffer.WriteAll(source);
            return easyBuffer.ReadByteString();
        }

        public ByteString ReadByteString(long byteCount)
        {
            Require(byteCount);
            return easyBuffer.ReadByteString(byteCount);
        }

        public long ReadDecimalLong()
        {
            Require(1);
            for (int pos = 0; Request(pos + 1); pos++)
            {
                byte b = easyBuffer.getByte(pos);
                if ((b < '0' || b > '9') && (pos != 0 || b != '-'))
                {
                    // Non-digit, or non-leading negative sign.
                    if (pos == 0)
                    {
                        throw new NumberFormatException(string.Format(
                            "Expected leading [0-9] or '-' character but was %#x", b));
                    }
                    break;
                }
            }
            return easyBuffer.ReadDecimalLong();
        }

        public void ReadFully(byte[] sink)
        {
            try
            {
                Require(sink.Length);
            }
            catch (EOFException e)
            {
                // The underlying source is exhausted. Copy the bytes we got before rethrowing.
                int offset = 0;
                while (easyBuffer.Size > 0)
                {
                    int read = easyBuffer.Read(sink, offset, (int)easyBuffer.Size);
                    if (read == -1) throw new AssertionException();
                    offset += read;
                }
                throw e;
            }
            easyBuffer.ReadFully(sink);
        }

        public void ReadFully(EasyBuffer sink, long byteCount)
        {
            try
            {
                Require(byteCount);
            }
            catch (EOFException e)
            {
                // The underlying source is exhausted. Copy the bytes we got before rethrowing.
                sink.WriteAll(easyBuffer);
                throw e;
            }
            easyBuffer.ReadFully(sink, byteCount);
        }

        public ulong ReadHexadecimalUnsignedLong()
        {
            Require(1);
            for (int pos = 0; Request(pos + 1); pos++)
            {
                byte b = easyBuffer.getByte(pos);
                if ((b < '0' || b > '9') && (b < 'a' || b > 'f') && (b < 'A' || b > 'F'))
                {
                    // Non-digit, or non-leading negative sign.
                    if (pos == 0)
                    {
                        throw new NumberFormatException(string.Format(
                            "Expected leading [0-9a-fA-F] character but was %#x", b));
                    }
                    break;
                }
            }
            return easyBuffer.ReadHexadecimalUnsignedLong();
        }

        public int ReadInt()
        {
            Require(4);
            return easyBuffer.ReadInt();
        }

        public int ReadIntLe()
        {
            Require(4);
            return easyBuffer.ReadIntLe();
        }

        public long ReadLong()
        {
            Require(8);
            return easyBuffer.ReadLong();
        }

        public long ReadLongLe()
        {
            Require(8);
            return easyBuffer.ReadLongLe();
        }

        public short ReadShort()
        {
            Require(2);
            return easyBuffer.ReadShort();
        }

        public short ReadShortLe()
        {
            Require(2);
            return easyBuffer.ReadShortLe();
        }

        public string ReadString(Encoding charset)
        {
            if (charset == null) throw new IllegalArgumentException("charset == null");

            easyBuffer.WriteAll(source);
            return easyBuffer.ReadString(charset);
        }

        public string ReadString(long byteCount, Encoding charset)
        {
            Require(byteCount);
            if (charset == null) throw new IllegalArgumentException("charset == null");
            return easyBuffer.ReadString(byteCount, charset);
        }

        public string ReadUtf8()
        {
            easyBuffer.WriteAll(source);
            return easyBuffer.ReadUtf8();
        }

        public string ReadUtf8(long byteCount)
        {
            Require(byteCount);
            return easyBuffer.ReadUtf8(byteCount);
        }

        public int ReadUtf8CodePoint()
        {
            Require(1);

            byte b0 = easyBuffer.getByte(0);
            if ((b0 & 0xe0) == 0xc0)
            {
                Require(2);
            }
            else if ((b0 & 0xf0) == 0xe0)
            {
                Require(3);
            }
            else if ((b0 & 0xf8) == 0xf0)
            {
                Require(4);
            }

            return easyBuffer.ReadUtf8CodePoint();
        }

        public string ReadUtf8Line()
        {
            long newline = IndexOf((byte)'\n');
            if (newline == -1)
            {
                return easyBuffer.Size != 0 ? ReadUtf8(easyBuffer.Size) : null;
            }
            return easyBuffer.readUtf8Line(newline);
        }

        public string ReadUtf8LineStrict()
        {
            return ReadUtf8LineStrict(long.MaxValue);
        }

        public string ReadUtf8LineStrict(long limit)
        {
            if (limit < 0) throw new IllegalArgumentException("limit < 0: " + limit);
            long scanLength = limit == long.MaxValue ? long.MaxValue : limit + 1;
            long newline = IndexOf((byte)'\n', 0, scanLength);
            if (newline != -1) return easyBuffer.readUtf8Line(newline);
            if (scanLength < long.MaxValue
                && Request(scanLength) && easyBuffer.getByte(scanLength - 1) == '\r'
                && Request(scanLength + 1) && easyBuffer.getByte(scanLength) == '\n')
            {
                return easyBuffer.readUtf8Line(scanLength); // The line was 'limit' UTF-8 bytes followed by \r\n.
            }
            var data = new EasyBuffer();
            easyBuffer.copyTo(data, 0, Math.Min(32, easyBuffer.Size));
            throw new EOFException("\\n not found: limit=" + Math.Min(easyBuffer.Size, limit)
                + " content=" + data.ReadByteString().Hex() + '…');
        }

        public bool Request(long byteCount)
        {
            if (byteCount < 0) throw new IllegalArgumentException("byteCount < 0: " + byteCount);
            if (closed) throw new IllegalStateException("closed");
            while (easyBuffer.Size < byteCount)
            {
                if (source.Read(easyBuffer, Segment.SIZE) == -1) return false;
            }
            return true;
        }

        public void Require(long byteCount)
        {
            if (!Request(byteCount)) throw new EOFException();
        }

        public int Select(Options options)
        {
            //if (closed) throw new IllegalStateException("closed");

            //while (true)
            //{
            //    int index = easyBuffer.selectPrefix(options, true);
            //    if (index == -1) return -1;
            //    if (index == -2)
            //    {
            //        // We need to grow the buffer. Do that, then try it all again.
            //        if (source.read(easyBuffer, Segment.SIZE) == -1L) return -1;
            //    }
            //    else
            //    {
            //        // We matched a full byte string: consume it and return it.
            //        int selectedSize = options.byteStrings[index].size();
            //        easyBuffer.skip(selectedSize);
            //        return index;
            //    }
            //}

            throw new PlatformNotSupportedException();
        }

        public void Skip(long byteCount)
        {
            if (closed) throw new IllegalStateException("closed");
            while (byteCount > 0)
            {
                if (easyBuffer.Size == 0 && source.Read(easyBuffer, Segment.SIZE) == -1)
                {
                    throw new EOFException();
                }
                long toSkip = Math.Min(byteCount, easyBuffer.Size);
                easyBuffer.Skip(toSkip);
                byteCount -= toSkip;
            }
        }

        public Timeout Timeout()
        {
            return source.Timeout();
        }


    }
}
