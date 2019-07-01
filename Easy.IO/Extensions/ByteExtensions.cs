using System;
using System.Collections.Generic;
using System.Text;

namespace Easy.IO
{
    public static class ByteExtensions
    {
        public static byte[] Copy(this byte[] bytes)
        {
            byte[] newBytes = new byte[bytes.Length];
            bytes.CopyTo(newBytes, 0);
            return newBytes;
        }
    }
}
