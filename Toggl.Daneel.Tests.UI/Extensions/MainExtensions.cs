using Xamarin.UITest;

namespace Toggl.Tests.UI.Extensions
{
    public static partial class MainExtensions
    {
        public static void TapNthCellInCollection(this IApp app, int index)
        {
            app.Tap(query => query.Class("UITableViewCell").Child(index));
        }

        public static void OpenEditView(this IApp app)
        {
            app.TapNthCellInCollection(0);
            app.WaitForElement(EditTimeEntry.EditTags);
        }

        public static void TapSnackBarButton(this IApp app, string buttonText)
        {
            app.Tap(x => x.Marked(Misc.SnackBar).Descendant().Text(buttonText));
        }
    }
}
