using System;
using System.Collections.Generic;
using System.Text;

namespace Easy.IO
{
    public interface Source : IDisposable
    {
        /// <summary>
        ///Removes at least 1, and up to {@code byteCount} bytes from this and appends
        ///them to { @code sink }. Returns the number Of bytes read, or -1 if this
        ///source is exhausted.
        /// </summary>
        /// <param name="sink"></param>
        /// <param name="byteCount"></param>
        /// <returns></returns>
        long Read(EasyBuffer sink, long byteCount);

        /// <summary>
        /// Returns the timeout for this source.
        /// </summary>
        /// <returns></returns>
        Timeout Timeout();


    }
}
