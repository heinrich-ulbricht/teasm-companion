using System;
#nullable enable

namespace TeasmCompanion.Interfaces
{
    public class CannotLockException : Exception
    {
        public CannotLockException() : base()
        {
        }

        public CannotLockException(string? message) : base(message)
        {
        }
        public CannotLockException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
