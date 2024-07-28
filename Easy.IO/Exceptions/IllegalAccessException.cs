using System;
using System.Runtime.Serialization;

namespace Easy.IO
{
    [Serializable]
    internal class IllegalAccessException : Exception
    {
        public IllegalAccessException()
        {
        }

        public IllegalAccessException(string message) : base(message)
        {
        }

        public IllegalAccessException(string message, Exception innerException) : base(message, innerException)
        {
        }

    }
}