using Easy.IO;
using NUnit.Framework;
using System;
using System.Text;

namespace Easy.Tests
{
    public class Utf8Test
    {
        [Test]
        public void oneByteCharacters()
        {
            assertEncoded("00", 0x00); // Smallest 1-byte character.
            assertEncoded("20", ' ');
            assertEncoded("7e", '~');
            assertEncoded("7f", 0x7f); // Largest 1-byte character.
        }
        [Test]
        public void twoByteCharacters()
        {
            assertEncoded("c280", 0x0080); // Smallest 2-byte character.
            assertEncoded("c3bf", 0x00ff);
            assertEncoded("c480", 0x0100);
            assertEncoded("dfbf", 0x07ff); // Largest 2-byte character.
        }

        [Test]
        public void threeByteCharacters()
        {
            assertEncoded("e0a080", 0x0800); // Smallest 3-byte character.
            assertEncoded("e0bfbf", 0x0fff);
            assertEncoded("e18080", 0x1000);
            assertEncoded("e1bfbf", 0x1fff);
            assertEncoded("ed8080", 0xd000);
            assertEncoded("ed9fbf", 0xd7ff); // Largest character lower than the min surrogate.
            assertEncoded("ee8080", 0xe000); // Smallest character greater than the max surrogate.
            assertEncoded("eebfbf", 0xefff);
            assertEncoded("ef8080", 0xf000);
            assertEncoded("efbfbf", 0xffff); // Largest 3-byte character.
        }

        //[Test]
        //public void fourByteCharacters()
        //{
        //    assertEncoded("f0908080", 0x010000); // Smallest surrogate pair.
        //    assertEncoded("f48fbfbf", 0x10ffff); // Largest code point expressible by UTF-16.
        //}

        //[Test]
        //public void danglingHighSurrogate()
        //{
        //    assertStringEncoded("3f", "\ud800"); // "?"
        //}

        //[Test]
        //public void lowSurrogateWithoutHighSurrogate()
        //{
        //    assertStringEncoded("3f", "\udc00"); // "?"
        //}

        //[Test]
        //public void highSurrogateFollowedByNonSurrogate()
        //{
        //    assertStringEncoded("3f61", "\ud800\u0061"); // "?a": Following character is too low.
        //    assertStringEncoded("3fee8080", "\ud800\ue000"); // "?\ue000": Following character is too high.
        //}

        //[Test]
        //public void doubleLowSurrogate()
        //{
        //    assertStringEncoded("3f3f", "\udc00\udc00"); // "??"
        //}

        //[Test]
        //public void doubleHighSurrogate()
        //{
        //    assertStringEncoded("3f3f", "\ud800\ud800"); // "??"
        //}

        //[Test]
        //public void highSurrogateLowSurrogate()
        //{
        //    assertStringEncoded("3f3f", "\udc00\ud800"); // "??"
        //}

        private void assertEncoded(String hex, params int[] codePoints)
        {
            assertCodePointEncoded(hex, codePoints);
            assertCodePointDecoded(hex, codePoints);
            char[] ch = new char[codePoints.Length];
            for (int i = 0; i < ch.Length; i++)
            {
                ch[i] = (char)codePoints[i];
            }
            assertStringEncoded(hex, new String(ch, 0, ch.Length));
        }

        private void assertCodePointEncoded(String hex, params int[] codePoints)
        {
            var buffer = new EasyBuffer();
            foreach (var codePoint in codePoints)
            {
                buffer.WriteUtf8CodePoint(codePoint);
            }
            var b1 = buffer.ReadByteString();
            var b2 = ByteString.DecodeHex(hex);
            Assert.AreEqual(b1, b2);
        }

        private void assertCodePointDecoded(String hex, params int[] codePoints)
        {
            var buffer = new EasyBuffer().Write(ByteString.DecodeHex(hex));
            foreach (var codePoint in codePoints)
            {
                Assert.AreEqual(codePoint, buffer.ReadUtf8CodePoint());
            }
            Assert.True(buffer.Exhausted());
        }

        private void assertStringEncoded(String hex, String @string)
        {
            ByteString expectedUtf8 = ByteString.DecodeHex(hex);

            // Confirm our implementation matches those expectations.
            ByteString actualUtf8 = new EasyBuffer().WriteUtf8(@string).ReadByteString();
            Assert.AreEqual(expectedUtf8, actualUtf8);

            //// Confirm our expectations are consistent with the platform.
            //ByteString platformUtf8 = ByteString.Of(Encoding.UTF8.GetBytes(@string));
            //Assert.AreEqual(expectedUtf8, platformUtf8);

            // Confirm we are consistent when writing one code point at a time.
            var bufferUtf8 = new EasyBuffer();
            for (int i = 0; i < @string.Length;)
            {
                int c = @string.CodePointAt(i);
                bufferUtf8.WriteUtf8CodePoint(c);
                i += Character.CharCount(c);
            }
            Assert.AreEqual(expectedUtf8, bufferUtf8.ReadByteString());

            // Confirm we are consistent when measuring lengths.
            Assert.AreEqual(expectedUtf8.Size(), Utf8.Size(@string));
            Assert.AreEqual(expectedUtf8.Size(), Utf8.Size(@string, 0, @string.Length()));
        }
    }
}
