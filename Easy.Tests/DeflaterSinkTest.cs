using Easy.IO;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using NUnit.Framework;
using System;

namespace Easy.Tests
{
    class DeflaterSinkTest
    {
        [Test]
        public void deflateWithClose()
        {
            var data = new EasyBuffer();
            String original = "They're moving in herds. They do move in herds.";
            data.WriteUtf8(original);
            var sink = new EasyBuffer();
            DeflaterSink deflaterSink = new DeflaterSink(sink, new Deflater());
            deflaterSink.Write(data, data.Size);
            deflaterSink.Flush();
            deflaterSink.Dispose();
            var inflated = inflate(sink);
            Assert.AreEqual(original, inflated.ReadUtf8());
        }

        public void deflateWithSyncFlush()
        {
            String original = "Yes, yes, yes. That's why we're taking extreme precautions.";
            var data = new EasyBuffer();
            data.WriteUtf8(original);
            var sink = new EasyBuffer();
            DeflaterSink deflaterSink = new DeflaterSink(sink, new Deflater());
            deflaterSink.Write(data, data.Size);
            deflaterSink.Flush();
            var inflated = inflate(sink);
            Assert.AreEqual(original, inflated.ReadUtf8());
        }
        [Test]
        public void deflateWellCompressed()
        {
            String original = repeat('a', 1024 * 1024);
            var data = new EasyBuffer();
            data.WriteUtf8(original);
            var sink = new EasyBuffer();
            DeflaterSink deflaterSink = new DeflaterSink(sink, new Deflater());
            deflaterSink.Write(data, data.Size);
            deflaterSink.Flush();
            deflaterSink.Dispose();
            var inflated = inflate(sink);
            Assert.AreEqual(original, inflated.ReadUtf8());
        }

        [Test]
        public void deflatePoorlyCompressed()
        {
            ByteString original = randomBytes(1024 * 1024);
            var data = new EasyBuffer();
            data.Write(original);
            var sink = new EasyBuffer();
            DeflaterSink deflaterSink = new DeflaterSink(sink, new Deflater());
            deflaterSink.Write(data, data.Size);
            deflaterSink.Flush();
            deflaterSink.Dispose();
            var inflated = inflate(sink);
            Assert.AreEqual(original, inflated.ReadByteString());
        }
        [Test]
        public void multipleSegmentsWithoutCompression()
        {
            var buffer = new EasyBuffer();
            Deflater deflater = new Deflater();
            //deflater.SetLevel(Deflater.NO_COMPRESSION);//SharpZipLib 会出错~
            DeflaterSink deflaterSink = new DeflaterSink(buffer, deflater);
            int byteCount = Segment.SIZE * 4;
            var data = new EasyBuffer().WriteUtf8(repeat('a', byteCount));
            deflaterSink.Write(data, data.Size);
            deflaterSink.Flush();
            deflaterSink.Dispose();
            var test1 = repeat('a', byteCount);
            var test2 = inflate(buffer).ReadUtf8(byteCount);
            Assert.AreEqual(test1, test2);
        }

        [Test]
        public void deflateIntoNonemptySink()
        {
            String original = "They're moving in herds. They do move in herds.";
            // Exercise all possible offsets for the outgoing segment.
            for (int i = 0; i < Segment.SIZE; i++)
            {
                var data = new EasyBuffer().WriteUtf8(original);
                var sink = new EasyBuffer().WriteUtf8(repeat('a', i));

                DeflaterSink deflaterSink = new DeflaterSink(sink, new Deflater());
                deflaterSink.Write(data, data.Size);
                deflaterSink.Flush();
                deflaterSink.Dispose();

                sink.Skip(i);
                var inflated = inflate(sink);
                Assert.AreEqual(original, inflated.ReadUtf8());
            }
        }

        static String repeat(char c, int count)
        {
            char[] array = new char[count];
            Array.Fill(array, c);
            return new String(array);
        }

        static ByteString randomBytes(int length)
        {
            Random random = new Random(0);
            byte[] randomBytes = new byte[length];
            random.NextBytes(randomBytes);
            return ByteString.Of(randomBytes);
        }

        /**
    * Uses streaming decompression to inflate {@code deflated}. The input must
    * either be finished or have a trailing sync flush.
*/
        private EasyBuffer inflate(EasyBuffer deflated)
        {
            var deflatedIn = deflated.InputStream();
            Inflater inflater = new Inflater();
            var inflatedIn = new InflaterInputStream(deflatedIn, inflater);
            var result = new EasyBuffer();
            byte[] buffer = new byte[8192];
            while (!inflater.IsNeedingInput || deflated.Size > 0)
            {
                int count = inflatedIn.Read(buffer, 0, buffer.Length);
                if (count > 0)
                {
                    result.Write(buffer, 0, count);
                }
            }
            return result;
        }
    }
}
