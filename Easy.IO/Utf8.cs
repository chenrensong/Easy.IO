using System;
using System.Collections.Generic;
using System.Text;

namespace Easy.IO
{
    public class Utf8
    {
        public static long Size(string @string)
        {
            return Size(@string, 0, @string.Length);
        }

        /**
         * Returns the number Of bytes used to encode the slice Of {@code string} as UTF-8 when using
         * {@link BufferedSink#writeUtf8(string, int, int)}.
         */
        public static long Size(string @string, int beginIndex, int endIndex)
        {
            if (@string == null)
            {
                throw new IllegalArgumentException("string == null");
            }
            if (beginIndex < 0)
            {
                throw new IllegalArgumentException("beginIndex < 0: " + beginIndex);
            }
            if (endIndex < beginIndex)
            {
                throw new IllegalArgumentException("endIndex < beginIndex: " + endIndex + " < " + beginIndex);
            }
            if (endIndex > @string.Length)
            {
                throw new IllegalArgumentException(
                    "endIndex > string.length: " + endIndex + " > " + @string.Length);
            }

            long result = 0;
            for (int i = beginIndex; i < endIndex;)
            {
                int c = @string[i];

                if (c < 0x80)
                {
                    // A 7-bit character with 1 byte.
                    result++;
                    i++;

                }
                else if (c < 0x800)
                {
                    // An 11-bit character with 2 bytes.
                    result += 2;
                    i++;

                }
                else if (c < 0xd800 || c > 0xdfff)
                {
                    // A 16-bit character with 3 bytes.
                    result += 3;
                    i++;

                }
                else
                {
                    int low = i + 1 < endIndex ? @string[i + 1] : 0;
                    if (c > 0xdbff || low < 0xdc00 || low > 0xdfff)
                    {
                        // A malformed surrogate, which yields '?'.
                        result++;
                        i++;

                    }
                    else
                    {
                        // A 21-bit character with 4 bytes.
                        result += 4;
                        i += 2;
                    }
                }
            }
            return result;
        }
    }
}
