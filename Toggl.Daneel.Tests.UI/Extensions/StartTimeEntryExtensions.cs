using Xamarin.UITest;

namespace Toggl.Tests.UI.Extensions
{
    public static partial class StartTimeEntryExtensions
    {
        public static void TapCreateTag(this IApp app, string tagName)
        {
            app.TapNthCellInCollection(0);
        }

        public static void TapCreateProject(this IApp app, string projectName)
        {
            var cellText = $"Create project \"{projectName}\"";
            app.Tap(x => x.Contains(cellText));
        }

        public static void TapSelectTag(this IApp app, string tagName)
        {
            app.TapNthCellInCollection(0);
        }

        public static void TapSelectProject(this IApp app, string projectName)
        {
            app.TapNthCellInCollection(0);
        }

        public static void TapCreateClient(this IApp app, string clientName)
        {
            app.TapNthCellInCollection(0);
        }

        public static void TapSelectClient(this IApp app, string clientName)
        {
            app.Tap(x => x.Marked(Client.ClientViewCell).Descendant().Text(clientName));
        }

        public static void EnterManualTimeEntryDuration(this IApp app, string duration)
        {
            app.Tap(StartTimeEntry.DurationLabel);
            app.EnterText(duration);
        }
    }
}
