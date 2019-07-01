using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Easy.IO
{
    public class EasyStream : Stream
    {
        private EasyBuffer _easyBuffer;
        public EasyStream(EasyBuffer easyBuffer)
        {
            _easyBuffer = easyBuffer;
        }

        public override bool CanRead => true;

        public override bool CanSeek => throw new NotImplementedException();

        public override bool CanWrite => true;

        public override long Length => _easyBuffer.Size;

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush()
        {
            _easyBuffer.Flush();
        }

        public int Read()
        {
            if (Length <= 0)
            {
                return -1;
            }
            return _easyBuffer.ReadByte() & 0xff;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {

            return _easyBuffer.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _easyBuffer.Write(buffer, offset, count);
        }
    }

}
