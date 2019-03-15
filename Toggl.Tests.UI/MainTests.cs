using System;
using NUnit.Framework;
using Xamarin.UITest;
using Xamarin.UITest.Queries;
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
            var projectName = "Fuking project";
            app.CreateTimeEntry(description, projectName);
            app.SwipeEntryToDelete(description);

            app.AssertNoTimeEntryInTheLog(description, projectName);

            app.TapSnackBarButton("UNDO");

            app.AssertTimeEntryInTheLog(description, projectName);
        }

        [Test]
        public void TappingTheContinueButtonOnATimeEntryContinuesTheTimeEntry()
        {
            var timeEntryDescription = "This is a time entry";
            app.CreateTimeEntry(timeEntryDescription);
            Func<AppQuery, AppQuery> queryForContinueButton
                = x => x.Marked(Main.TimeEntryRow)
                    .Descendant()
                    .Text(timeEntryDescription)
                    .Parent()
                    .Marked(Main.TimeEntryRow)
                    .Descendant()
                    .Marked(Main.TimeEntryRowContinueButton);

            app.Tap(queryForContinueButton);

            app.AssertRunningTimeEntry(timeEntryDescription);
        }

        [Test]
        public void SwipingATimeEntryRightContinuesIt()
        {
            var timeEntryDescription = "No, this is Patrick!";
            var projectName = "Some project";
            app.CreateTimeEntry(timeEntryDescription, projectName);

            app.SwipeEntryToContinue(timeEntryDescription);

            app.AssertRunningTimeEntry(timeEntryDescription, projectName);
        }

        [Test, IgnoreOnAndroid]
        public void SwipingATimeEntryRightAndTappingTheRevealedContinueButtonContinuesTheTimeEntry()
        {
            var description = "This was a triupmh";
            app.CreateTimeEntry(description);
            var timeEntryCellRect = app.RectForTimeEntryCell(description);

            app.DragCoordinates(
                fromX: timeEntryCellRect.X,
                fromY: timeEntryCellRect.CenterY,
                toX: timeEntryCellRect.X + 100,
                toY: timeEntryCellRect.CenterY
            );
            app.WaitForElement(x => x.Text("Continue"));
            app.Tap(x => x.Text("Continue"));

            app.AssertRunningTimeEntry(description);
        }
    }
}
