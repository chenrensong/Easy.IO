using System;
using System.Collections.Generic;
using System.Text;

namespace Easy.IO
{
    public class Base64
    {
        private Base64()
        {
        }

        public static byte[] decode(string @in)
        {
            // Ignore trailing '=' padding and whitespace from the input.
            int limit = @in.Length;
            for (; limit > 0; limit--)
            {
                char c = @in[limit - 1];
                if (c != '=' && c != '\n' && c != '\r' && c != ' ' && c != '\t')
                {
                    break;
                }
            }

            // If the input includes whitespace, this output array will be longer than necessary.
            byte[] @out = new byte[(int)(limit * 6L / 8L)];
            int outCount = 0;
            int inCount = 0;

            int word = 0;
            for (int pos = 0; pos < limit; pos++)
            {
                char c = @in[pos];

                int bits;
                if (c >= 'A' && c <= 'Z')
                {
                    // char ASCII value
                    //  A    65    0
                    //  Z    90    25 (ASCII - 65)
                    bits = c - 65;
                }
                else if (c >= 'a' && c <= 'z')
                {
                    // char ASCII value
                    //  a    97    26
                    //  z    122   51 (ASCII - 71)
                    bits = c - 71;
                }
                else if (c >= '0' && c <= '9')
                {
                    // char ASCII value
                    //  0    48    52
                    //  9    57    61 (ASCII + 4)
                    bits = c + 4;
                }
                else if (c == '+' || c == '-')
                {
                    bits = 62;
                }
                else if (c == '/' || c == '_')
                {
                    bits = 63;
                }
                else if (c == '\n' || c == '\r' || c == ' ' || c == '\t')
                {
                    continue;
                }
                else
                {
                    return null;
                }

                // Append this char's 6 bits to the word.
                word = (word << 6) | (byte)bits;

                // For every 4 chars Of input, we accumulate 24 bits Of output. Emit 3 bytes.
                inCount++;
                if (inCount % 4 == 0)
                {
                    @out[outCount++] = (byte)(word >> 16);
                    @out[outCount++] = (byte)(word >> 8);
                    @out[outCount++] = (byte)word;
                }
            }

            int lastWordChars = inCount % 4;
            if (lastWordChars == 1)
            {
                // We read 1 char followed by "===". But 6 bits is a truncated byte! Fail.
                return null;
            }
            else if (lastWordChars == 2)
            {
                // We read 2 chars followed by "==". Emit 1 byte with 8 Of those 12 bits.
                word = word << 12;
                @out[outCount++] = (byte)(word >> 16);
            }
            else if (lastWordChars == 3)
            {
                // We read 3 chars, followed by "=". Emit 2 bytes for 16 Of those 18 bits.
                word = word << 6;
                @out[outCount++] = (byte)(word >> 16);
                @out[outCount++] = (byte)(word >> 8);
            }

            // If we sized our out array perfectly, we're done.
            if (outCount == @out.Length) return @out;

            // Copy the decoded bytes to a new, right-sized array.
            byte[] prefix = new byte[outCount];
            Array.Copy(@out, 0, prefix, 0, outCount);
            return prefix;
        }

        private static byte[] MAP = new byte[] {
        (byte) 'A', (byte)'B', (byte)'C', (byte)'D', (byte)'E', (byte)'F', (byte)'G', (byte)'H', (byte)'I', (byte)'J', (byte)'K', (byte)'L', (byte)'M', (byte)'N', (byte)'O', (byte)'P', (byte)'Q', (byte)'R', (byte)'S',
        (byte)  'T', (byte)'U', (byte)'V', (byte)'W', (byte)'X', (byte)'Y', (byte)'Z', (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', (byte)'f', (byte)'g', (byte)'h', (byte)'i', (byte)'j', (byte)'k', (byte)'l',
        (byte)  'm', (byte)'n', (byte)'o', (byte)'p', (byte)'q', (byte)'r', (byte)'s', (byte)'t', (byte)'u', (byte)'v', (byte)'w', (byte)'x', (byte)'y', (byte)'z', (byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4',
        (byte) '5', (byte)'6', (byte)'7', (byte)'8', (byte)'9', (byte)'+', (byte)'/'
      };

        private static byte[] URL_MAP = new byte[] {
        (byte)  'A', (byte)'B', (byte)'C', (byte)'D', (byte)'E', (byte)'F', (byte)'G', (byte)'H', (byte)'I', (byte)'J', (byte)'K', (byte)'L', (byte)'M', (byte)'N', (byte)'O', (byte)'P', (byte)'Q', (byte)'R', (byte)'S',
        (byte)  'T', (byte)'U', (byte)'V', (byte)'W', (byte)'X', (byte)'Y', (byte)'Z', (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', (byte)'f', (byte)'g', (byte)'h', (byte)'i', (byte)'j', (byte)'k', (byte)'l',
        (byte)  'm', (byte)'n', (byte)'o', (byte)'p', (byte)'q', (byte)'r', (byte)'s', (byte)'t', (byte)'u', (byte)'v', (byte)'w', (byte)'x', (byte)'y', (byte)'z', (byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4',
        (byte)  '5', (byte)'6', (byte)'7', (byte)'8', (byte)'9', (byte)'-', (byte)'_'
      };

        public static string encode(byte[] @in)
        {
            return encode(@in, MAP);
        }

        public static string encodeUrl(byte[] @in)
        {
            return encode(@in, URL_MAP);
        }

        public static string encode(byte[] @in, byte[] map)
        {
            int Length = (@in.Length + 2) / 3 * 4;
            byte[] @out = new byte[Length];
            int index = 0, end = @in.Length - @in.Length % 3;
            for (int i = 0; i < end; i += 3)
            {
                @out[index++] = map[(@in[i] & 0xff) >> 2];
                @out[index++] = map[((@in[i] & 0x03) << 4) | ((@in[i + 1] & 0xff) >> 4)];
                @out[index++] = map[((@in[i + 1] & 0x0f) << 2) | ((@in[i + 2] & 0xff) >> 6)];
                @out[index++] = map[(@in[i + 2] & 0x3f)];
            }
            switch (@in.Length % 3)
            {
                case 1:
                    @out[index++] = map[(@in[end] & 0xff) >> 2];
                    @out[index++] = map[(@in[end] & 0x03) << 4];
                    @out[index++] = (byte)'=';
                    @out[index++] = (byte)'=';
                    break;
                case 2:
                    @out[index++] = map[(@in[end] & 0xff) >> 2];
                    @out[index++] = map[((@in[end] & 0x03) << 4) | ((@in[end + 1] & 0xff) >> 4)];
                    @out[index++] = map[((@in[end + 1] & 0x0f) << 2)];
                    @out[index++] = (byte)'=';
                    break;
            }
            try
            {
                return Encoding.GetEncoding("US-ASCII").GetString(@out);
            }
            catch (Exception e)
            {
                throw new AssertionException("US-ASCII is Error", e);
            }
        }
    }
}
