using System;
using System.Collections.Generic;
using System.Text;

namespace Easy.IO
{
    public static class StringExtensions
    {
        internal static char charAt(this string str,int index)
        {
            return str[index];
        }

        internal static int length(this string str)
        {
            return str.Length;
        }
        internal static int codePointAt(this string str, int index)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }
            if (index >= str.Length)
            {
                return -1;
            }
            if (index < 0)
            {
                return -1;
            }
            int c = str[index];
            if ((c & 0xfc00) == 0xd800 && index + 1 < str.Length &&
                (str[index + 1] & 0xfc00) == 0xdc00)
            {
                // Get the Unicode code point for the surrogate pair
                return 0x10000 + ((c - 0xd800) << 10) + (str[index + 1] - 0xdc00);
            }
            return ((c & 0xf800) == 0xd800) ? 0xfffd : c;
        }
    }
}
