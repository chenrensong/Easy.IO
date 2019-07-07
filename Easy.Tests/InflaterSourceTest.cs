using Easy.IO;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;

namespace Easy.Tests
{
    class InflaterSourceTest
    {
        [Test]
        public void inflate()
        {
            var deflated = decodeBase64("eJxzz09RyEjNKVAoLdZRKE9VL0pVyMxTKMlIVchIzEspVshPU0jNS8/MS00tK"
        + "tYDAF6CD5s=");
            var inflated = inflate(deflated);
            Assert.AreEqual("God help us, we're in the hands of engineers.", inflated.ReadUtf8());
        }

        [Test]
        public void inflateWellCompressed()
        {
            var deflated = decodeBase64("eJztwTEBAAAAwqCs61/CEL5AAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
        + "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAB8B"
        + "tFeWvE=\n");
            String original = repeat('a', 1024 * 1024);
            var inflated = inflate(deflated);
            Assert.AreEqual(original, inflated.ReadUtf8());
        }

        [Test]
        public void inflatePoorlyCompressed()
        {
            ByteString original = randomBytes(1024 * 1024);
            var deflated = deflate(original);
            var inflated = inflate(deflated);
            Assert.AreEqual(original, inflated.ReadByteString());
        }
        [Test]
        public void inflateIntoNonemptySink()
        {
            for (int i = 0; i < Segment.SIZE; i++)
            {
                var inflated = new EasyBuffer().WriteUtf8(repeat('a', i));
                var deflated = decodeBase64(
                    "eJxzz09RyEjNKVAoLdZRKE9VL0pVyMxTKMlIVchIzEspVshPU0jNS8/MS00tKtYDAF6CD5s=");
                InflaterSource source = new InflaterSource(deflated, new Inflater());
                while (source.Read(inflated, int.MaxValue) > 0)
                {
                }
                inflated.Skip(i);
                Assert.AreEqual("God help us, we're in the hands of engineers.", inflated.ReadUtf8());
            }
        }
        [Test]
        public void inflateSingleByte()
        {
            var inflated = new EasyBuffer();
            var deflated = decodeBase64(
                "eJxzz09RyEjNKVAoLdZRKE9VL0pVyMxTKMlIVchIzEspVshPU0jNS8/MS00tKtYDAF6CD5s=");
            InflaterSource source = new InflaterSource(deflated, new Inflater());
            source.Read(inflated, 1);
            source.Dispose();
            Assert.AreEqual("G", inflated.ReadUtf8());
            Assert.AreEqual(0, inflated.Size);
        }

        [Test]
        public void inflateByteCount()
        {
            var inflated = new EasyBuffer();
            var deflated = decodeBase64(
            "eJxzz09RyEjNKVAoLdZRKE9VL0pVyMxTKMlIVchIzEspVshPU0jNS8/MS00tKtYDAF6CD5s=");
            InflaterSource source = new InflaterSource(deflated, new Inflater());
            source.Read(inflated, 11);
            source.Dispose();
            Assert.AreEqual("God help us", inflated.ReadUtf8());
            Assert.AreEqual(0, inflated.Size);
        }

        static ByteString randomBytes(int length)
        {
            Random random = new Random(0);
            byte[] randomBytes = new byte[length];
            random.NextBytes(randomBytes);
            return ByteString.Of(randomBytes);
        }

        static String repeat(char c, int count)
        {
            char[] array = new char[count];
            Array.Fill(array, c);
            return new String(array);
        }

        private EasyBuffer decodeBase64(String s)
        {
            return new EasyBuffer().Write(ByteString.DecodeBase64(s));
        }

        /** Use DeflaterOutputStream to deflate source. */
        private EasyBuffer deflate(ByteString source)
        {
            var result = new EasyBuffer();
            Sink sink = EasyIO.Sink(new DeflaterOutputStream(result.OutputStream()));
            sink.Write(new EasyBuffer().Write(source), source.Size());
            sink.Dispose();
            return result;
        }

        /** Returns a new buffer containing the inflated contents of {@code deflated}. */
        private EasyBuffer inflate(EasyBuffer deflated)
        {
            var result = new EasyBuffer();
            InflaterSource source = new InflaterSource(deflated, new Inflater());
            while (source.Read(result, int.MaxValue) != -1)
            {
            }
            return result;
        }
    }
}
