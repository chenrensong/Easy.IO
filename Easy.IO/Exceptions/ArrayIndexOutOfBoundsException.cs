using System;
using System.Runtime.Serialization;

namespace Easy.IO
{
    [Serializable]
    internal class ArrayIndexOutOfBoundsException : Exception
    {
        public ArrayIndexOutOfBoundsException()
        {
        }

        public ArrayIndexOutOfBoundsException(string message) : base(message)
        {
        }

        public ArrayIndexOutOfBoundsException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ArrayIndexOutOfBoundsException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}