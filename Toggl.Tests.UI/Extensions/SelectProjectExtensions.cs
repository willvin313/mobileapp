using Xamarin.UITest;

namespace Toggl.Tests.UI.Extensions
{
    public static class SelectProjectExtensions
    {
        public static void TapNoProjectCell(this IApp app)
        {
            app.Tap("No Project");
        }
    }
}
