using System;
using System.Collections.Generic;
using System.Text;

namespace Easy.IO
{
    public interface Sink : IDisposable
    {
        /** Removes {@code byteCount} bytes from {@code source} and appends them to this. */
        void Write(EasyBuffer source, long byteCount);

        /** Pushes all buffered bytes to their final destination. */
        void Flush();

        /** Returns the timeout for this sink. */
        Timeout Timeout();
    }
}
