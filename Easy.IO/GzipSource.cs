using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Zip.Compression;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Easy.IO
{
    public class GzipSource : Source
    {
        private static byte FHCRC = 1;
        private static byte FEXTRA = 2;
        private static byte FNAME = 3;
        private static byte FCOMMENT = 4;

        private static byte SECTION_HEADER = 0;
        private static byte SECTION_BODY = 1;
        private static byte SECTION_TRAILER = 2;
        private static byte SECTION_DONE = 3;

        /** The current section. Always progresses forward. */
        private int section = SECTION_HEADER;

        /**
         * Our source should yield a GZIP header (which we consume directly), followed
         * by deflated bytes (which we consume via an InflaterSource), followed by a
         * GZIP trailer (which we also consume directly).
         */
        private BufferedSource source;

        /** The inflater used to decompress the deflated body. */
        private Inflater inflater;

        /**
         * The inflater source takes care of moving data between compressed source and
         * decompressed sink buffers.
         */
        private InflaterSource inflaterSource;

        /** Checksum used to check both the GZIP header and decompressed body. */
        private Crc32 crc = new Crc32();

        public GzipSource(Source source)
        {
            if (source == null) throw new IllegalArgumentException("source == null");
            this.inflater = new Inflater(true);
            this.source = EasyIO.Buffer(source);
            this.inflaterSource = new InflaterSource(this.source, inflater);
        }

        public void Dispose()
        {
            inflaterSource.Dispose();
        }

        public long Read(EasyBuffer sink, long byteCount)
        {
            if (byteCount < 0) throw new IllegalArgumentException("byteCount < 0: " + byteCount);
            if (byteCount == 0) return 0;

            // If we haven't consumed the header, we must consume it before anything else.
            if (section == SECTION_HEADER)
            {
                consumeHeader();
                section = SECTION_BODY;
            }

            // Attempt to read at least a byte of the body. If we do, we're done.
            if (section == SECTION_BODY)
            {
                long offset = sink.Size;
                long result = inflaterSource.Read(sink, byteCount);
                if (result > 0)//!=-1
                {
                    updateCrc(sink, offset, result);
                    return result;
                }
                section = SECTION_TRAILER;
            }

            // The body is exhausted; time to read the trailer. We always consume the
            // trailer before returning a -1 exhausted result; that way if you read to
            // the end of a GzipSource you guarantee that the CRC has been checked.
            if (section == SECTION_TRAILER)
            {
                consumeTrailer();
                section = SECTION_DONE;

                // Gzip streams self-terminate: they return -1 before their underlying
                // source returns -1. Here we attempt to force the underlying stream to
                // return -1 which may trigger it to release its resources. If it doesn't
                // return -1, then our Gzip data finished prematurely!
                if (!source.exhausted())
                {
                    throw new Exception("gzip finished without exhausting source");
                }
            }

            return -1;
        }

        public Timeout Timeout()
        {
            return source.Timeout();
        }

        private void consumeHeader()
        {
            // Read the 10-byte header. We peek at the flags byte first so we know if we
            // need to CRC the entire header. Then we read the magic ID1ID2 sequence.
            // We can skip everything else in the first 10 bytes.
            // +---+---+---+---+---+---+---+---+---+---+
            // |ID1|ID2|CM |FLG|     MTIME     |XFL|OS | (more-->)
            // +---+---+---+---+---+---+---+---+---+---+
            source.Require(10);
            byte flags = source.Buffer().GetByte(3);
            var fhcrc = ((flags >> FHCRC) & 1) == 1;
            if (fhcrc) updateCrc(source.Buffer(), 0, 10);

            short id1id2 = source.ReadShort();
            checkEqual("ID1ID2", (short)0x1f8b, id1id2);
            source.Skip(8);

            // Skip optional extra fields.
            // +---+---+=================================+
            // | XLEN  |...XLEN bytes of "extra field"...| (more-->)
            // +---+---+=================================+
            if (((flags >> FEXTRA) & 1) == 1)
            {
                source.Require(2);
                if (fhcrc) updateCrc(source.Buffer(), 0, 2);
                int xlen = source.Buffer().ReadShortLe();
                source.Require(xlen);
                if (fhcrc) updateCrc(source.Buffer(), 0, xlen);
                source.Skip(xlen);
            }

            // Skip an optional 0-terminated name.
            // +=========================================+
            // |...original file name, zero-terminated...| (more-->)
            // +=========================================+
            if (((flags >> FNAME) & 1) == 1)
            {
                long index = source.IndexOf((byte)0);
                if (index == -1) throw new EOFException();
                if (fhcrc) updateCrc(source.Buffer(), 0, index + 1);
                source.Skip(index + 1);
            }

            // Skip an optional 0-terminated comment.
            // +===================================+
            // |...file comment, zero-terminated...| (more-->)
            // +===================================+
            if (((flags >> FCOMMENT) & 1) == 1)
            {
                long index = source.IndexOf((byte)0);
                if (index == -1) throw new EOFException();
                if (fhcrc) updateCrc(source.Buffer(), 0, index + 1);
                source.Skip(index + 1);
            }

            // Confirm the optional header CRC.
            // +---+---+
            // | CRC16 |
            // +---+---+
            if (fhcrc)
            {
                checkEqual("FHCRC", source.ReadShortLe(), (short)crc.Value);
                crc.Reset();
            }
        }

        private void consumeTrailer()
        {
            // Read the eight-byte trailer. Confirm the body's CRC and size.
            // +---+---+---+---+---+---+---+---+
            // |     CRC32     |     ISIZE     |
            // +---+---+---+---+---+---+---+---+
            checkEqual("CRC", source.ReadIntLe(), (int)crc.Value);
            checkEqual("ISIZE", source.ReadIntLe(), (int)inflater.TotalOut);
        }

        /** Updates the CRC with the given bytes. */
        private void updateCrc(EasyBuffer buffer, long offset, long byteCount)
        {
            // Skip segments that we aren't checksumming.
            Segment s = buffer.Head;
            for (; offset >= (s.Limit - s.Pos); s = s.Next)
            {
                offset -= (s.Limit - s.Pos);
            }

            // Checksum one segment at a time.
            for (; byteCount > 0; s = s.Next)
            {
                int pos = (int)(s.Pos + offset);
                int toUpdate = (int)Math.Min(s.Limit - pos, byteCount);
                var newData = s.Data.Copy(pos, toUpdate);
                crc.Update(newData);
                byteCount -= toUpdate;
                offset = 0;
            }
        }

        private void checkEqual(String name, int expected, int actual)
        {
            if (actual != expected)
            {
                throw new Exception(String.Format(
                    "%s: actual 0x%08x != expected 0x%08x", name, actual, expected));
            }
        }
    }
}
