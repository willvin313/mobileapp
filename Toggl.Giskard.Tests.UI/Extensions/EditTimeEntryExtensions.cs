using Xamarin.UITest;

namespace Toggl.Tests.UI.Extensions
{
    public static class EditTimeEntryExtensions
    {
        public static void ConfirmEditTimeEntry(this IApp app)
        {
            app.Tap(EditTimeEntry.Confirm);

            //On Android the confirm button sometimes need to be tapped twice.
            //Once to finish editing the description and once more to close the edit view
            if (app.Query(EditTimeEntry.Confirm).Length > 0)
                app.Tap(EditTimeEntry.Confirm);

            app.WaitForNoElement(EditTimeEntry.Confirm);
        }
    }
}
