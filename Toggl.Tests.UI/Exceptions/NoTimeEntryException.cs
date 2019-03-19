using System;

namespace Toggl.Tests.UI.Exceptions
{
    public sealed class NoTimeEntryException : Exception
    {
        public NoTimeEntryException(string message) : base(message)
        {
        }
    }
}
