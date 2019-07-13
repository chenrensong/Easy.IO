using System;
using System.Collections.Generic;
using System.Text;

namespace Easy.IO
{
    public static class LongExtensions
    {
        public static int NumberOfTrailingZeros(this long i)
        {
            // HD, Figure 5-14
            int x, y;
            if (i == 0) return 64;
            int n = 63;
            y = (int)i; if (y != 0) { n = n - 32; x = y; } else x = (int)(i >>> 32);
            y = x << 16; if (y != 0) { n = n - 16; x = y; }
            y = x << 8; if (y != 0) { n = n - 8; x = y; }
            y = x << 4; if (y != 0) { n = n - 4; x = y; }
            y = x << 2; if (y != 0) { n = n - 2; x = y; }
            return n - ((x << 1) >> 31);
        }

        public static long HighestOneBit(this long i)
        {
            // HD, Figure 3-1
            i |= (i >> 1);
            i |= (i >> 2);
            i |= (i >> 4);
            i |= (i >> 8);
            i |= (i >> 16);
            i |= (i >> 32);
            return i - (i >> 1);
        }

    }
}
