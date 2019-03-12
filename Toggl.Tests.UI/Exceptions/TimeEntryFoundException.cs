using System;
namespace Toggl.Tests.UI.Exceptions
{
    public sealed class TimeEntryFoundException : Exception
    {
        public TimeEntryFoundException(string message) : base(message)
        {
        }
    }
}
