using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Easy.IO
{
    static class SystemEx
    {
        public static long NanoTime()
        {
            return (long)(Stopwatch.GetTimestamp() / (Stopwatch.Frequency / 1000000000.0));
        }
    }
}
