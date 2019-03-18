using System;
namespace Toggl.Tests.UI.Exceptions
{
    public sealed class NoRunningTimeEntryException : Exception
    {
        public NoRunningTimeEntryException(string message) : base(message)
        {
        }
    }
}
