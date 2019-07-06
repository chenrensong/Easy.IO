using Easy.IO;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Easy.Tests
{
    class GzipSinkTest
    {
        [Test]
        public void gzipGunzip()
        {
            var data = new EasyBuffer();
            String original = "It's a UNIX system! I know this!";
            data.WriteUtf8(original);
            var sink = new EasyBuffer();
            GzipSink gzipSink = new GzipSink(sink);
            gzipSink.Write(data, data.Size);
            gzipSink.Flush();
            gzipSink.Dispose();
            var inflated = gunzip(sink);
            var result = inflated.ReadUtf8();
            Assert.AreEqual(original, result);
        }

        private EasyBuffer gunzip(EasyBuffer gzipped)
        {
            var result = new EasyBuffer();
            GzipSource source = new GzipSource(gzipped);
            long count = 0;
            while ((count = source.Read(result, int.MaxValue)) != -1)
            {
            }
            return result;
        }
    }
}
