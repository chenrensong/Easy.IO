using Easy.IO;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Easy.Tests
{
    class EasyIOTest
    {
        [Test]
        public void readWriteFile()
        {
            string tempFile = Path.GetTempFileName();
            BufferedSink sink = EasyIO.Buffer(EasyIO.Sink(tempFile));
            sink.WriteUtf8("Hello, easy.io file!");
            sink.Dispose();
            Assert.True(File.Exists(tempFile));
            var allText = File.ReadAllText(tempFile);
            Assert.AreEqual(20, allText.Length);
            BufferedSource source = EasyIO.Buffer(EasyIO.Source(tempFile));
            Assert.AreEqual("Hello, easy.io file!", source.ReadUtf8());
            source.Dispose();
        }
    }
}
