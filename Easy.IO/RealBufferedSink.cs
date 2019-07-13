using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Easy.IO
{
    public class RealBufferedSink : BufferedSink
    {
        private EasyBuffer _easyBuffer = new EasyBuffer();
        private Sink _sink;
        private bool _closed = false;

        public RealBufferedSink(Sink sink)
        {
            this._sink = sink;
        }

        public EasyBuffer Buffer()
        {
            return _easyBuffer;
        }

        public void Dispose()
        {
            if (_closed) return;
            try
            {
                if (_easyBuffer.Size > 0)
                {
                    _sink.Write(_easyBuffer, _easyBuffer.Size);
                }
            }
            catch (Exception ex)
            {

            }

            try
            {
                _sink.Dispose();
            }
            catch (Exception ex)
            {

            }
            _closed = true;
        }

        public BufferedSink Emit()
        {
            if (_closed) throw new IllegalStateException("closed");
            long byteCount = _easyBuffer.Size;
            if (byteCount > 0) _sink.Write(_easyBuffer, byteCount);
            return this;
        }

        public BufferedSink EmitCompleteSegments()
        {
            if (_closed) throw new IllegalStateException("closed");
            long byteCount = _easyBuffer.completeSegmentByteCount();
            if (byteCount > 0) _sink.Write(_easyBuffer, byteCount);
            return this;
        }

        public void Flush()
        {
            if (_closed) throw new IllegalStateException("closed");
            if (_easyBuffer.Size > 0)
            {
                _sink.Write(_easyBuffer, _easyBuffer.Size);
            }
            _sink.Flush();
        }

        public Stream OutputStream()
        {
            throw new NotImplementedException();
        }

        public Timeout Timeout()
        {
            return _sink.Timeout();
        }

        public BufferedSink Write(ByteString byteString)
        {
            if (_closed) throw new IllegalStateException("closed");
            _easyBuffer.Write(byteString);
            return EmitCompleteSegments();
        }

        public BufferedSink Write(byte[] source)
        {
            if (_closed) throw new IllegalStateException("closed");
            _easyBuffer.Write(source);
            return EmitCompleteSegments();
        }

        public BufferedSink Write(byte[] source, int offset, int byteCount)
        {
            if (_closed) throw new IllegalStateException("closed");
            _easyBuffer.Write(source, offset, byteCount);
            return EmitCompleteSegments();
        }

        public BufferedSink Write(Source source, long byteCount)
        {
            while (byteCount > 0)
            {
                long read = source.Read(_easyBuffer, byteCount);
                if (read == -1) throw new IndexOutOfRangeException();
                byteCount -= read;
                EmitCompleteSegments();
            }
            return this;
        }

        public void Write(EasyBuffer source, long byteCount)
        {
            if (_closed) throw new IllegalStateException("closed");
            _easyBuffer.Write(source, byteCount);
            EmitCompleteSegments();
        }

        public long WriteAll(Source source)
        {
            if (source == null) throw new ArgumentException("source == null");
            long totalBytesRead = 0;
            for (long readCount; (readCount = source.Read(_easyBuffer, Segment.SIZE)) != -1;)
            {
                totalBytesRead += readCount;
                EmitCompleteSegments();
            }
            return totalBytesRead;
        }

        public BufferedSink WriteByte(int b)
        {
            if (_closed) throw new IllegalStateException("closed");
            _easyBuffer.WriteByte(b);
            return EmitCompleteSegments();
        }

        public BufferedSink WriteDecimalLong(long v)
        {
            if (_closed) throw new IllegalStateException("closed");
            _easyBuffer.WriteDecimalLong(v);
            return EmitCompleteSegments();
        }

        public BufferedSink WriteHexadecimalUnsignedLong(long v)
        {
            if (_closed) throw new IllegalStateException("closed");
            _easyBuffer.WriteHexadecimalUnsignedLong(v);
            return EmitCompleteSegments();
        }

        public BufferedSink WriteInt(int i)
        {
            if (_closed) throw new IllegalStateException("closed");
            _easyBuffer.WriteInt(i);
            return EmitCompleteSegments();
        }

        public BufferedSink WriteIntLe(int i)
        {
            if (_closed) throw new IllegalStateException("closed");
            _easyBuffer.WriteIntLe(i);
            return EmitCompleteSegments();
        }

        public BufferedSink WriteLong(long v)
        {
            if (_closed) throw new IllegalStateException("closed");
            _easyBuffer.WriteLong(v);
            return EmitCompleteSegments();
        }

        public BufferedSink WriteLongLe(long v)
        {
            if (_closed) throw new IllegalStateException("closed");
            _easyBuffer.WriteLongLe(v);
            return EmitCompleteSegments();
        }

        public BufferedSink WriteShort(int s)
        {
            if (_closed) throw new IllegalStateException("closed");
            _easyBuffer.WriteShort(s);
            return EmitCompleteSegments();
        }

        public BufferedSink WriteShortLe(int s)
        {
            if (_closed) throw new IllegalStateException("closed");
            _easyBuffer.WriteShortLe(s);
            return EmitCompleteSegments();
        }

        public BufferedSink WriteString(string @string, Encoding charset)
        {
            if (_closed) throw new IllegalStateException("closed");
            _easyBuffer.WriteString(@string, charset);
            return EmitCompleteSegments();
        }

        public BufferedSink WriteString(string @string, int beginIndex, int endIndex, Encoding charset)
        {
            if (_closed) throw new IllegalStateException("closed");
            _easyBuffer.WriteString(@string, beginIndex, endIndex, charset);
            return EmitCompleteSegments();
        }

        public BufferedSink WriteUtf8(string @string)
        {
            if (_closed) throw new IllegalStateException("closed");
            _easyBuffer.WriteUtf8(@string);
            return EmitCompleteSegments();
        }

        public BufferedSink WriteUtf8(string @string, int beginIndex, int endIndex)
        {
            if (_closed) throw new IllegalStateException("closed");
            _easyBuffer.WriteUtf8(@string, beginIndex, endIndex);
            return EmitCompleteSegments();
        }

        public BufferedSink WriteUtf8CodePoint(int codePoint)
        {
            if (_closed) throw new IllegalStateException("closed");
            _easyBuffer.WriteUtf8CodePoint(codePoint);
            return EmitCompleteSegments();
        }


    }
}
