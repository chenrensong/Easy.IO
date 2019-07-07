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
        public void readWriteFileCommon()
        {
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "Hello, easy.io file!");
            Assert.True(File.Exists(tempFile));
            var allText = File.ReadAllText(tempFile);
            Assert.AreEqual(20, allText.Length);
            Assert.AreEqual("Hello, easy.io file!", File.ReadAllText(tempFile));
        }

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

        [Test]
        public void appendFile()
        {
            string tempFile = Path.GetTempFileName();
            BufferedSink sink = EasyIO.Buffer(EasyIO.Sink(tempFile, FileMode.Append));
            sink.WriteUtf8("Hello, ");
            sink.Dispose();
            Assert.True(File.Exists(tempFile));
            Assert.AreEqual(7, File.ReadAllText(tempFile).Length);

            sink = EasyIO.Buffer(EasyIO.Sink(tempFile, FileMode.Append));
            sink.WriteUtf8("easy.io file!");
            sink.Dispose();
            Assert.AreEqual(20, File.ReadAllText(tempFile).Length);

            BufferedSource source = EasyIO.Buffer(EasyIO.Source(tempFile));
            Assert.AreEqual("Hello, easy.io file!", source.ReadUtf8());
            source.Dispose();
        }

      
    }
}
