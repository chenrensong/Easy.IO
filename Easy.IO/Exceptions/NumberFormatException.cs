using System;
using System.Runtime.Serialization;

namespace Easy.IO
{
    [Serializable]
    internal class NumberFormatException : Exception
    {
        private object p;

        public NumberFormatException()
        {
        }

        public NumberFormatException(object p)
        {
            this.p = p;
        }

        public NumberFormatException(string message) : base(message)
        {
        }

        public NumberFormatException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NumberFormatException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}