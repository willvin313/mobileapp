using Xamarin.UITest;

namespace Toggl.Tests.UI.Extensions
{
    public static class EditTimeEntryExtensions
    {
        public static void ConfirmEditTimeEntry(this IApp app)
        {
            app.Tap(EditTimeEntry.Confirm);

            app.WaitForNoElement(EditTimeEntry.Confirm);
        }
    }
}
