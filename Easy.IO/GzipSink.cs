using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Zip.Compression;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Easy.IO
{
    public class GzipSink : Sink
    {

        /** Sink into which the GZIP format is written. */
        private BufferedSink sink;

        /** The deflater used to compress the body. */
        private Deflater deflater;

        /**
         * The deflater sink takes care of moving data between decompressed source and
         * compressed sink buffers.
         */
        private DeflaterSink deflaterSink;

        private bool closed;

        /** Checksum calculated for the compressed body. */
        private Crc32 crc = new Crc32();

        public GzipSink(Sink sink)
        {
            if (sink == null) throw new ArgumentException("sink == null");
            this.deflater = new Deflater(Deflater.DEFAULT_COMPRESSION, true /* No wrap */);
            this.sink = EasyIO.Buffer(sink);
            this.deflaterSink = new DeflaterSink(this.sink, deflater);
            writeHeader();
        }

        public void Dispose()
        {
            if (closed) return;
            try
            {
                deflaterSink.Dispose();
            }
            catch
            {

            }
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
            deflaterSink.Flush();
            writeFooter();
        }

        public Timeout Timeout()
        {
            return sink.Timeout();
        }

        public void Write(EasyBuffer source, long byteCount)
        {
            if (byteCount < 0) throw new ArgumentException("byteCount < 0: " + byteCount);
            if (byteCount == 0) return;

            updateCrc(source, byteCount);
            deflaterSink.Write(source, byteCount);
        }


        private void writeHeader()
        {
            // Write the Gzip header directly into the buffer for the sink to avoid handling IOException.
            var buffer = this.sink.Buffer();
            buffer.WriteShort(0x1f8b); // Two-byte Gzip ID.
            buffer.WriteByte(0x08); // 8 == Deflate compression method.
            buffer.WriteByte(0x00); // No flags.
            buffer.WriteInt(0x00); // No modification time.
            buffer.WriteByte(0x00); // No extra flags.
            buffer.WriteByte(0x00); // No OS.
        }

        private void writeFooter()
        {
            sink.WriteIntLe((int)crc.Value); // CRC of original data.
            sink.WriteIntLe((int)deflater.TotalIn); // Length of original data.
        }

        /** Updates the CRC with the given bytes. */
        private void updateCrc(EasyBuffer buffer, long byteCount)
        {
            for (Segment head = buffer.Head; byteCount > 0; head = head.Next)
            {
                int segmentLength = (int)Math.Min(byteCount, head.Limit - head.Pos);
                var newData = head.Data.Copy(head.Pos, segmentLength);
                crc.Update(newData);
                byteCount -= segmentLength;
            }
        }
    }
}
