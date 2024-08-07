﻿using System;
using System.Runtime.Serialization;

namespace Easy.IO
{
    [Serializable]
    internal class IllegalStateException : Exception
    {
        public IllegalStateException()
        {
        }

        public IllegalStateException(string message) : base(message)
        {
        }

        public IllegalStateException(string message, Exception innerException) : base(message, innerException)
        {
        }

    }
}