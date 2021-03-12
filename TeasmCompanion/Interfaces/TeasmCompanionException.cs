using System;
using System.Runtime.Serialization;

namespace TeasmCompanion.Interfaces
{
    public class TeasmCompanionException : Exception
    {
        public TeasmCompanionException()
        {
        }

        public TeasmCompanionException(string message) : base(message)
        {
        }

        public TeasmCompanionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected TeasmCompanionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
