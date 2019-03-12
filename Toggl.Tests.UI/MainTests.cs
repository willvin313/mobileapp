using NUnit.Framework;
using Xamarin.UITest;
using static Toggl.Tests.UI.Extensions.MainExtensions;

namespace Toggl.Tests.UI
{
    [TestFixture]
    public sealed class MainTests
    {
        private const string validEmail = "susancalvin@psychohistorian.museum";

        private IApp app;

        [SetUp]
        public void BeforeEachTest()
        {
            app = Configuration.GetApp();

            app.WaitForMainScreen();
        }

        [Test]
        public void TappingTheStopButtonStopsTheRunningTimeEntry()
        {
            app.StartTimeEntryWithDescription("Testing the Toggl app");

            app.Tap(Main.StopTimeEntryButton);
            app.WaitForElement(Main.StartTimeEntryButton);
        }

        [Test]
        public void SwipingAnEntryLeftDeletesIt()
        {
            var description = "This is a test";
            app.StartTimeEntryWithDescription(description);
            app.StopTimeEntry();

            app.SwipeEntryToDelete(description);

            app.AssertNoTimeEntryInTheLog(description);
        }

        [Test, IgnoreOnAndroid]
        public void SwipingAnEntryLeftAndTappingDeleteButtonDeletesTheEntry()
        {
            var description = "This is a test";
            app.StartTimeEntryWithDescription(description);
            app.StopTimeEntry();
            var timeEntryCellRect = app.RectForTimeEntryCell(description);

            app.DragCoordinates(
                fromX: timeEntryCellRect.CenterX,
                fromY: timeEntryCellRect.CenterY,
                toX: timeEntryCellRect.X - 100,
                toY: timeEntryCellRect.CenterY
            );
            app.WaitForElement(x => x.Text("Delete"));
            app.Tap(x => x.Text("Delete"));

            app.AssertNoTimeEntryInTheLog(description);
        }

        [Test]
        public void TappingTheUndoButtonBringsBackTheDeletedTimeEntry()
        {
            var description = "some time entry";
            app.CreateTimeEntry(description);
            app.SwipeEntryToDelete(description);

            app.AssertNoTimeEntryInTheLog(description);

            app.TapSnackBarButton("UNDO");

            app.AssertTimeEntryInTheLog(description);
        }
    }
}
