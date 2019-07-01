using System;
using System.Runtime.Serialization;

namespace Easy.IO
{
    [Serializable]
    internal class EOFException : Exception
    {
        public EOFException()
        {
        }

        public EOFException(string message) : base(message)
        {
        }

        public EOFException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected EOFException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}