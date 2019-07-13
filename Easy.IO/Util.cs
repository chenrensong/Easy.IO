using System;
using System.Collections.Generic;
using System.Text;

namespace Easy.IO
{
    //1.15.0
    class Util
    {
        /** A cheap and type-safe constant for the UTF-8 Charset. */
        public static Encoding UTF_8 = Encoding.UTF8;

        private Util()
        {
        }

        public static void CheckOffsetAndCount(long size, long offset, long byteCount)
        {
            if ((offset | byteCount) < 0 || offset > size || size - offset < byteCount)
            {
                throw new IndexOutOfRangeException(
                    string.Format("size=%s offset=%s byteCount=%s", size, offset, byteCount));
            }
        }

        public static short ReverseBytesShort(short s)
        {
            int i = s & 0xffff;
            int reversed = (i & 0xff00) >> 8
                | (i & 0x00ff) << 8;
            return (short)reversed;
        }

        public static int ReverseBytesInt(int i)
        {
            return (int)ReverseBytesInt((uint)i);
        }

        public static uint ReverseBytesInt(uint i)
        {
            return (i & 0xff000000) >> 24
                | (i & 0x00ff0000) >> 8
                | (i & 0x0000ff00) << 8
                | (i & 0x000000ff) << 24;
        }

        public static long ReverseBytesLong(long i)
        {
            return (long)ReverseBytesLong((ulong)i);
        }

        public static ulong ReverseBytesLong(ulong v)
        {
            return (v & 0xff00000000000000L) >> 56
                | (v & 0x00ff000000000000L) >> 40
                | (v & 0x0000ff0000000000L) >> 24
                | (v & 0x000000ff00000000L) >> 8
                | (v & 0x00000000ff000000L) << 8
                | (v & 0x0000000000ff0000L) << 24
                | (v & 0x000000000000ff00L) << 40
                | (v & 0x00000000000000ffL) << 56;
        }

        public static bool ArrayRangeEquals(
            byte[] a, int aOffset, byte[] b, int bOffset, int byteCount)
        {
            for (int i = 0; i < byteCount; i++)
            {
                if (a[i + aOffset] != b[i + bOffset]) return false;
            }
            return true;
        }
    }
}
