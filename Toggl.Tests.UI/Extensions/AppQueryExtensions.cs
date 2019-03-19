using Xamarin.UITest.Queries;

namespace Toggl.Tests.UI.Extensions
{
    public static partial class AppQueryExtensions
    {
        public static AppQuery Contains(this AppQuery appQuery, string text)
            => appQuery.Raw($"* {{text CONTAINS '{text}'}}");

        public static AppQuery StartsWith(this AppQuery appQuery, string text)
            => appQuery.Raw($"* {{text BEGINSWITH '{text}'}}");

        public static AppQuery EndsWith(this AppQuery appQuery, string text)
            => appQuery.Raw($"* {{text ENDSWITH '{text}'}}");
    }
}
