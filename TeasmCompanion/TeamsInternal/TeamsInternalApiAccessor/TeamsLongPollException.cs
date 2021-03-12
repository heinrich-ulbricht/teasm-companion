using System;
using System.Runtime.Serialization;

#nullable enable

namespace TeasmCompanion.TeamsInternal.TeamsInternalApiAccessor
{
    public class TeamsLongPollException : Exception
    {
        public TeamsLongPollException()
        {
        }

        public TeamsLongPollException(string message) : base(message)
        {
        }

        public TeamsLongPollException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected TeamsLongPollException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
