using Easy.IO;
using ICSharpCode.SharpZipLib.Checksum;
using Microsoft.VisualBasic.CompilerServices;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Easy.Tests
{
    class GzipSourceTest
    {
        [Test]
        public void gunzip()
        {
            var gzipped = new EasyBuffer();
            gzipped.Write(gzipHeader);
            gzipped.Write(deflated);
            gzipped.Write(gzipTrailer);
            assertGzipped(gzipped);
        }

        [Test]
        public void gunzip_withHCRC()
        {
            Crc32 hcrc = new Crc32();
            ByteString gzipHeader = gzipHeaderWithFlags((byte)0x02);
            hcrc.Update(gzipHeader.ToByteArray());

            var gzipped = new EasyBuffer();
            gzipped.Write(gzipHeader);
            gzipped.WriteShort(reverseBytesShort((short)hcrc.Value)); // little endian
            gzipped.Write(deflated);
            gzipped.Write(gzipTrailer);
            assertGzipped(gzipped);
        }
        [Test]
        public void gunzip_withExtra()
        {
            var gzipped = new EasyBuffer();
            gzipped.Write(gzipHeaderWithFlags((byte)0x04));
            gzipped.WriteShort(reverseBytesShort((short)7)); // little endian extra length
            gzipped.Write(Encoding.UTF8.GetBytes("blubber"), 0, 7);
            gzipped.Write(deflated);
            gzipped.Write(gzipTrailer);
            assertGzipped(gzipped);
        }
        [Test]
        public void gunzip_withName()
        {
            var gzipped = new EasyBuffer();
            gzipped.Write(gzipHeaderWithFlags((byte)0x08));
            gzipped.Write(Encoding.UTF8.GetBytes("foo.txt"), 0, 7);
            gzipped.WriteByte(0); // zero-terminated
            gzipped.Write(deflated);
            gzipped.Write(gzipTrailer);
            assertGzipped(gzipped);
        }
        [Test]
        public void gunzip_withComment()
        {
            var gzipped = new EasyBuffer();
            gzipped.Write(gzipHeaderWithFlags((byte)0x10));
            gzipped.Write(Encoding.UTF8.GetBytes("rubbish"), 0, 7);
            gzipped.WriteByte(0); // zero-terminated
            gzipped.Write(deflated);
            gzipped.Write(gzipTrailer);
            assertGzipped(gzipped);
        }

        [Test]
        public void gunzip_withAll()
        {
            var gzipped = new EasyBuffer();
            gzipped.Write(gzipHeaderWithFlags((byte)0x1c));
            gzipped.WriteShort(reverseBytesShort((short)7)); // little endian extra length
            gzipped.Write(Encoding.UTF8.GetBytes("blubber"), 0, 7);
            gzipped.Write(Encoding.UTF8.GetBytes("foo.txt"), 0, 7);
            gzipped.WriteByte(0); // zero-terminated
            gzipped.Write(Encoding.UTF8.GetBytes("rubbish"), 0, 7);
            gzipped.WriteByte(0); // zero-terminated
            gzipped.Write(deflated);
            gzipped.Write(gzipTrailer);
            assertGzipped(gzipped);
        }

        public static short reverseBytesShort(short s)
        {
            int i = s & 0xffff;
            int reversed = (i & 0xff00) >> 8
                | (i & 0x00ff) << 8;
            return (short)reversed;
        }

        private void assertGzipped(EasyBuffer gzipped)
        {
            var gunzipped = gunzip(gzipped);
            Assert.AreEqual("It's a UNIX system! I know this!", gunzipped.ReadUtf8());
        }


        private ByteString gzipHeaderWithFlags(byte flags)
        {
            byte[] result = gzipHeader.ToByteArray();
            result[3] = flags;
            return ByteString.Of(result);
        }

        private static ByteString gzipHeader = ByteString.DecodeHex("1f8b0800000000000000");

        // Deflated "It's a UNIX system! I know this!"
        private static ByteString deflated = ByteString.DecodeHex(
            "f32c512f56485408f5f38c5028ae2c2e49cd5554f054c8cecb2f5728c9c82c560400");

        private ByteString gzipTrailer = ByteString.DecodeHex(""
      + "8d8fad37" // Checksum of deflated.
      + "20000000" // 32 in little endian.
      );

        private EasyBuffer gunzip(EasyBuffer gzipped)
        {
            var result = new EasyBuffer();
            GzipSource source = new GzipSource(gzipped);
            while (source.Read(result, int.MaxValue) != -1)
            {
            }
            return result;
        }


    }
}
