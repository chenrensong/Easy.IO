using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Easy.IO
{
    public class RealBufferedSink : BufferedSink
    {
        private EasyBuffer easyBuffer = new EasyBuffer();
        private Sink sink;
        private bool closed = false;

        public RealBufferedSink(Sink sink)
        {
            this.sink = sink;
        }

        public EasyBuffer Buffer()
        {
            return easyBuffer;
        }

        public void Dispose()
        {
            if (closed) return;
            try
            {
                if (easyBuffer.Size > 0)
                {
                    sink.Write(easyBuffer, easyBuffer.Size);
                }
            }
            catch (Exception ex)
            {

            }

            try
            {
                sink.Dispose();
            }
            catch (Exception ex)
            {

            }
            closed = true;
        }

        public BufferedSink Emit()
        {
            if (closed) throw new IllegalStateException("closed");
            long byteCount = easyBuffer.Size;
            if (byteCount > 0) sink.Write(easyBuffer, byteCount);
            return this;
        }

        public BufferedSink EmitCompleteSegments()
        {
            if (closed) throw new IllegalStateException("closed");
            long byteCount = easyBuffer.completeSegmentByteCount();
            if (byteCount > 0) sink.Write(easyBuffer, byteCount);
            return this;
        }

        public void Flush()
        {
            if (closed) throw new IllegalStateException("closed");
            if (easyBuffer.Size > 0)
            {
                sink.Write(easyBuffer, easyBuffer.Size);
            }
            sink.Flush();
        }

        public Stream OutputStream()
        {
            throw new NotImplementedException();
        }

        public Timeout Timeout()
        {
            return sink.Timeout();
        }

        public BufferedSink Write(ByteString byteString)
        {
            if (closed) throw new IllegalStateException("closed");
            easyBuffer.Write(byteString);
            return EmitCompleteSegments();
        }

        public BufferedSink Write(byte[] source)
        {
            if (closed) throw new IllegalStateException("closed");
            easyBuffer.Write(source);
            return EmitCompleteSegments();
        }

        public BufferedSink Write(byte[] source, int offset, int byteCount)
        {
            if (closed) throw new IllegalStateException("closed");
            easyBuffer.Write(source, offset, byteCount);
            return EmitCompleteSegments();
        }

        public BufferedSink Write(Source source, long byteCount)
        {
            while (byteCount > 0)
            {
                long read = source.Read(easyBuffer, byteCount);
                if (read == -1) throw new EOFException();
                byteCount -= read;
                EmitCompleteSegments();
            }
            return this;
        }

        public void Write(EasyBuffer source, long byteCount)
        {
            if (closed) throw new IllegalStateException("closed");
            easyBuffer.Write(source, byteCount);
            EmitCompleteSegments();
        }

        public long WriteAll(Source source)
        {
            if (source == null) throw new IllegalArgumentException("source == null");
            long totalBytesRead = 0;
            for (long readCount; (readCount = source.Read(easyBuffer, Segment.SIZE)) != -1;)
            {
                totalBytesRead += readCount;
                EmitCompleteSegments();
            }
            return totalBytesRead;
        }

        public BufferedSink WriteByte(int b)
        {
            if (closed) throw new IllegalStateException("closed");
            easyBuffer.WriteByte(b);
            return EmitCompleteSegments();
        }

        public BufferedSink WriteDecimalLong(long v)
        {
            if (closed) throw new IllegalStateException("closed");
            easyBuffer.WriteDecimalLong(v);
            return EmitCompleteSegments();
        }

        public BufferedSink WriteHexadecimalUnsignedLong(long v)
        {
            if (closed) throw new IllegalStateException("closed");
            easyBuffer.WriteHexadecimalUnsignedLong(v);
            return EmitCompleteSegments();
        }

        public BufferedSink WriteInt(int i)
        {
            if (closed) throw new IllegalStateException("closed");
            easyBuffer.WriteInt(i);
            return EmitCompleteSegments();
        }

        public BufferedSink WriteIntLe(int i)
        {
            if (closed) throw new IllegalStateException("closed");
            easyBuffer.WriteIntLe(i);
            return EmitCompleteSegments();
        }

        public BufferedSink WriteLong(long v)
        {
            if (closed) throw new IllegalStateException("closed");
            easyBuffer.WriteLong(v);
            return EmitCompleteSegments();
        }

        public BufferedSink WriteLongLe(long v)
        {
            if (closed) throw new IllegalStateException("closed");
            easyBuffer.WriteLongLe(v);
            return EmitCompleteSegments();
        }

        public BufferedSink WriteShort(int s)
        {
            if (closed) throw new IllegalStateException("closed");
            easyBuffer.WriteShort(s);
            return EmitCompleteSegments();
        }

        public BufferedSink WriteShortLe(int s)
        {
            if (closed) throw new IllegalStateException("closed");
            easyBuffer.WriteShortLe(s);
            return EmitCompleteSegments();
        }

        public BufferedSink WriteString(string @string, Encoding charset)
        {
            if (closed) throw new IllegalStateException("closed");
            easyBuffer.WriteString(@string, charset);
            return EmitCompleteSegments();
        }

        public BufferedSink WriteString(string @string, int beginIndex, int endIndex, Encoding charset)
        {
            if (closed) throw new IllegalStateException("closed");
            easyBuffer.WriteString(@string, beginIndex, endIndex, charset);
            return EmitCompleteSegments();
        }

        public BufferedSink WriteUtf8(string @string)
        {
            if (closed) throw new IllegalStateException("closed");
            easyBuffer.WriteUtf8(@string);
            return EmitCompleteSegments();
        }

        public BufferedSink WriteUtf8(string @string, int beginIndex, int endIndex)
        {
            if (closed) throw new IllegalStateException("closed");
            easyBuffer.WriteUtf8(@string, beginIndex, endIndex);
            return EmitCompleteSegments();
        }

        public BufferedSink WriteUtf8CodePoint(int codePoint)
        {
            if (closed) throw new IllegalStateException("closed");
            easyBuffer.WriteUtf8CodePoint(codePoint);
            return EmitCompleteSegments();
        }


    }
}
