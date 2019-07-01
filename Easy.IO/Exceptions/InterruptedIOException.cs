using System;
using System.Runtime.Serialization;

namespace Easy.IO
{
    [Serializable]
    internal class InterruptedIOException : Exception
    {
        public InterruptedIOException()
        {
        }

        public InterruptedIOException(string message) : base(message)
        {
        }

        public InterruptedIOException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InterruptedIOException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}