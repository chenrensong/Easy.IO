using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Easy.IO
{
    public interface BufferedSink : BufferedSink<BufferedSink>
    {

    }

    public interface BufferedSink<T> : Sink
    {
        /** Returns this sink's internal buffer. */
        EasyBuffer Buffer();

        T Write(ByteString byteString);

        /**
         * Like {@link OutputStream#write(byte[])}, this writes a complete byte array to
         * this sink.
         */
        T Write(byte[] source);

        /**
         * Like {@link OutputStream#write(byte[], int, int)}, this writes {@code byteCount}
         * bytes Of {@code source}, starting at {@code offset}.
         */
        T Write(byte[] source, int offset, int byteCount);

        /**
         * Removes all bytes from {@code source} and appends them to this sink. Returns the
         * number Of bytes read which will be 0 if {@code source} is exhausted.
         */
        long WriteAll(Source source);

        /** Removes {@code byteCount} bytes from {@code source} and appends them to this sink. */
        T Write(Source source, long byteCount);

        /**
         * Encodes {@code string} in UTF-8 and writes it to this sink. <pre>{@code
         *
         *   Buffer buffer = new Buffer();
         *   buffer.writeUtf8("Uh uh uh!");
         *   buffer.writeByte(' ');
         *   buffer.writeUtf8("You didn't say the magic word!");
         *
         *   assertEquals("Uh uh uh! You didn't say the magic word!", buffer.readUtf8());
         * }</pre>
         */
        T WriteUtf8(string @string);

        /**
         * Encodes the characters at {@code beginIndex} up to {@code endIndex} from {@code string} in
         * UTF-8 and writes it to this sink. <pre>{@code
         *
         *   Buffer buffer = new Buffer();
         *   buffer.writeUtf8("I'm a hacker!\n", 6, 12);
         *   buffer.writeByte(' ');
         *   buffer.writeUtf8("That's what I said: you're a nerd.\n", 29, 33);
         *   buffer.writeByte(' ');
         *   buffer.writeUtf8("I prefer to be called a hacker!\n", 24, 31);
         *
         *   assertEquals("hacker nerd hacker!", buffer.readUtf8());
         * }</pre>
         */
        T WriteUtf8(string @string, int beginIndex, int endIndex);

        /** Encodes {@code codePoint} in UTF-8 and writes it to this sink. */
        T WriteUtf8CodePoint(int codePoint);

        /** Encodes {@code string} in {@code charset} and writes it to this sink. */
        T WriteString(string @string, Encoding charset);

        /**
         * Encodes the characters at {@code beginIndex} up to {@code endIndex} from {@code string} in
         * {@code charset} and writes it to this sink.
         */
        T WriteString(string @string, int beginIndex, int endIndex, Encoding charset)
     ;

        /** Writes a byte to this sink. */
        T WriteByte(int b);

        /**
         * Writes a big-endian short to this sink using two bytes. <pre>{@code
         *
         *   Buffer buffer = new Buffer();
         *   buffer.writeShort(32767);
         *   buffer.writeShort(15);
         *
         *   assertEquals(4, buffer.size());
         *   assertEquals((byte) 0x7f, buffer.readByte());
         *   assertEquals((byte) 0xff, buffer.readByte());
         *   assertEquals((byte) 0x00, buffer.readByte());
         *   assertEquals((byte) 0x0f, buffer.readByte());
         *   assertEquals(0, buffer.size());
         * }</pre>
         */
        T WriteShort(int s);

        /**
         * Writes a little-endian short to this sink using two bytes. <pre>{@code
         *
         *   Buffer buffer = new Buffer();
         *   buffer.writeShortLe(32767);
         *   buffer.writeShortLe(15);
         *
         *   assertEquals(4, buffer.size());
         *   assertEquals((byte) 0xff, buffer.readByte());
         *   assertEquals((byte) 0x7f, buffer.readByte());
         *   assertEquals((byte) 0x0f, buffer.readByte());
         *   assertEquals((byte) 0x00, buffer.readByte());
         *   assertEquals(0, buffer.size());
         * }</pre>
         */
        T WriteShortLe(int s);

        /**
         * Writes a big-endian int to this sink using four bytes. <pre>{@code
         *
         *   Buffer buffer = new Buffer();
         *   buffer.writeInt(2147483647);
         *   buffer.writeInt(15);
         *
         *   assertEquals(8, buffer.size());
         *   assertEquals((byte) 0x7f, buffer.readByte());
         *   assertEquals((byte) 0xff, buffer.readByte());
         *   assertEquals((byte) 0xff, buffer.readByte());
         *   assertEquals((byte) 0xff, buffer.readByte());
         *   assertEquals((byte) 0x00, buffer.readByte());
         *   assertEquals((byte) 0x00, buffer.readByte());
         *   assertEquals((byte) 0x00, buffer.readByte());
         *   assertEquals((byte) 0x0f, buffer.readByte());
         *   assertEquals(0, buffer.size());
         * }</pre>
         */
        T WriteInt(int i);

        /**
         * Writes a little-endian int to this sink using four bytes.  <pre>{@code
         *
         *   Buffer buffer = new Buffer();
         *   buffer.writeIntLe(2147483647);
         *   buffer.writeIntLe(15);
         *
         *   assertEquals(8, buffer.size());
         *   assertEquals((byte) 0xff, buffer.readByte());
         *   assertEquals((byte) 0xff, buffer.readByte());
         *   assertEquals((byte) 0xff, buffer.readByte());
         *   assertEquals((byte) 0x7f, buffer.readByte());
         *   assertEquals((byte) 0x0f, buffer.readByte());
         *   assertEquals((byte) 0x00, buffer.readByte());
         *   assertEquals((byte) 0x00, buffer.readByte());
         *   assertEquals((byte) 0x00, buffer.readByte());
         *   assertEquals(0, buffer.size());
         * }</pre>
         */
        T WriteIntLe(int i);

        /**
         * Writes a big-endian long to this sink using eight bytes. <pre>{@code
         *
         *   Buffer buffer = new Buffer();
         *   buffer.writeLong(9223372036854775807L);
         *   buffer.writeLong(15);
         *
         *   assertEquals(16, buffer.size());
         *   assertEquals((byte) 0x7f, buffer.readByte());
         *   assertEquals((byte) 0xff, buffer.readByte());
         *   assertEquals((byte) 0xff, buffer.readByte());
         *   assertEquals((byte) 0xff, buffer.readByte());
         *   assertEquals((byte) 0xff, buffer.readByte());
         *   assertEquals((byte) 0xff, buffer.readByte());
         *   assertEquals((byte) 0xff, buffer.readByte());
         *   assertEquals((byte) 0xff, buffer.readByte());
         *   assertEquals((byte) 0x00, buffer.readByte());
         *   assertEquals((byte) 0x00, buffer.readByte());
         *   assertEquals((byte) 0x00, buffer.readByte());
         *   assertEquals((byte) 0x00, buffer.readByte());
         *   assertEquals((byte) 0x00, buffer.readByte());
         *   assertEquals((byte) 0x00, buffer.readByte());
         *   assertEquals((byte) 0x00, buffer.readByte());
         *   assertEquals((byte) 0x0f, buffer.readByte());
         *   assertEquals(0, buffer.size());
         * }</pre>
         */
        T WriteLong(long v);

        /**
         * Writes a little-endian long to this sink using eight bytes. <pre>{@code
         *
         *   Buffer buffer = new Buffer();
         *   buffer.writeLongLe(9223372036854775807L);
         *   buffer.writeLongLe(15);
         *
         *   assertEquals(16, buffer.size());
         *   assertEquals((byte) 0xff, buffer.readByte());
         *   assertEquals((byte) 0xff, buffer.readByte());
         *   assertEquals((byte) 0xff, buffer.readByte());
         *   assertEquals((byte) 0xff, buffer.readByte());
         *   assertEquals((byte) 0xff, buffer.readByte());
         *   assertEquals((byte) 0xff, buffer.readByte());
         *   assertEquals((byte) 0xff, buffer.readByte());
         *   assertEquals((byte) 0x7f, buffer.readByte());
         *   assertEquals((byte) 0x0f, buffer.readByte());
         *   assertEquals((byte) 0x00, buffer.readByte());
         *   assertEquals((byte) 0x00, buffer.readByte());
         *   assertEquals((byte) 0x00, buffer.readByte());
         *   assertEquals((byte) 0x00, buffer.readByte());
         *   assertEquals((byte) 0x00, buffer.readByte());
         *   assertEquals((byte) 0x00, buffer.readByte());
         *   assertEquals((byte) 0x00, buffer.readByte());
         *   assertEquals(0, buffer.size());
         * }</pre>
         */
        T WriteLongLe(long v);

        /**
         * Writes a long to this sink in signed decimal form (i.e., as a string in base 10). <pre>{@code
         *
         *   Buffer buffer = new Buffer();
         *   buffer.writeDecimalLong(8675309L);
         *   buffer.writeByte(' ');
         *   buffer.writeDecimalLong(-123L);
         *   buffer.writeByte(' ');
         *   buffer.writeDecimalLong(1L);
         *
         *   assertEquals("8675309 -123 1", buffer.readUtf8());
         * }</pre>
         */
        T WriteDecimalLong(long v);

        /**
         * Writes a long to this sink in hexadecimal form (i.e., as a string in base 16). <pre>{@code
         *
         *   Buffer buffer = new Buffer();
         *   buffer.writeHexadecimalUnsignedLong(65535L);
         *   buffer.writeByte(' ');
         *   buffer.writeHexadecimalUnsignedLong(0xcafebabeL);
         *   buffer.writeByte(' ');
         *   buffer.writeHexadecimalUnsignedLong(0x10L);
         *
         *   assertEquals("ffff cafebabe 10", buffer.readUtf8());
         * }</pre>
         */
        T WriteHexadecimalUnsignedLong(long v);

        /**
         * Writes all buffered data to the underlying sink, if one exists. Then that sink is recursively
         * flushed which pushes data as far as possible towards its ultimate destination. Typically that
         * destination is a network socket or file. <pre>{@code
         *
         *    T  b0 = new Buffer();
         *    T  b1 = Okio.buffer(b0);
         *    T  b2 = Okio.buffer(b1);
         *
         *   b2.writeUtf8("hello");
         *   assertEquals(5, b2.buffer().size());
         *   assertEquals(0, b1.buffer().size());
         *   assertEquals(0, b0.buffer().size());
         *
         *   b2.flush();
         *   assertEquals(0, b2.buffer().size());
         *   assertEquals(0, b1.buffer().size());
         *   assertEquals(5, b0.buffer().size());
         * }</pre>
         */
        //void flush();

        /**
         * Writes all buffered data to the underlying sink, if one exists. Like {@link #flush}, but
         * weaker. Call this before this buffered sink goes out Of scope so that its data can reach its
         * destination. <pre>{@code
         *
         *    T  b0 = new Buffer();
         *    T  b1 = Okio.buffer(b0);
         *    T  b2 = Okio.buffer(b1);
         *
         *   b2.writeUtf8("hello");
         *   assertEquals(5, b2.buffer().size());
         *   assertEquals(0, b1.buffer().size());
         *   assertEquals(0, b0.buffer().size());
         *
         *   b2.emit();
         *   assertEquals(0, b2.buffer().size());
         *   assertEquals(5, b1.buffer().size());
         *   assertEquals(0, b0.buffer().size());
         *
         *   b1.emit();
         *   assertEquals(0, b2.buffer().size());
         *   assertEquals(0, b1.buffer().size());
         *   assertEquals(5, b0.buffer().size());
         * }</pre>
         */
        T Emit();

        /**
         * Writes complete segments to the underlying sink, if one exists. Like {@link #flush}, but
         * weaker. Use this to limit the memory held in the buffer to a single segment. Typically
         * application code will not need to call this: it is only necessary when application code writes
         * directly to this {@linkplain #buffer() sink's buffer}. <pre>{@code
         *
         *    T  b0 = new Buffer();
         *    T  b1 = Okio.buffer(b0);
         *    T  b2 = Okio.buffer(b1);
         *
         *   b2.buffer().write(new byte[20_000]);
         *   assertEquals(20_000, b2.buffer().size());
         *   assertEquals(     0, b1.buffer().size());
         *   assertEquals(     0, b0.buffer().size());
         *
         *   b2.emitCompleteSegments();
         *   assertEquals( 3_616, b2.buffer().size());
         *   assertEquals(     0, b1.buffer().size());
         *   assertEquals(16_384, b0.buffer().size()); // This example assumes 8192 byte segments.
         * }</pre>
         */
        T EmitCompleteSegments();

        /** Returns an output stream that writes to this sink. */
        Stream OutputStream();
    }
}
