using Xamarin.UITest;

namespace Toggl.Tests.UI.Extensions
{
    public static partial class MainExtensions
    {
        public static void OpenEditView(this IApp app)
        {
            app.Tap("MainLogContentView");
        }

        public static void TapSnackBarButton(this IApp app, string buttonText)
        {
            app.Tap(x => x.Marked(Misc.SnackbarAction).Text(buttonText));
        }
    }
}
