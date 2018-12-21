using System;
namespace Toggl.Foundation.Exceptions
{
    public sealed class SuggestionProviderException : Exception
    {
        public SuggestionProviderException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
