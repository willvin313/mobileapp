using System;
using System.Linq;
using Toggl.Tests.UI.Exceptions;
using Xamarin.UITest;
using Xamarin.UITest.Queries;

namespace Toggl.Tests.UI.Extensions
{
    public static partial class MainExtensions
    {
        public static void WaitForMainScreen(this IApp app)
        {
            var email = $"{Guid.NewGuid().ToString()}@toggl.space";

            app.WaitForSignUpScreen();

            app.Tap(SignUp.EmailText);
            app.EnterText(email);
            app.Tap(SignUp.PasswordText);
            app.EnterText("123456");
            app.SignUpSuccesfully();
        }

        public static void StartTimeEntryWithDescription(this IApp app, string description)
        {
            app.Tap(Main.StartTimeEntryButton);
            app.WaitForElement(StartTimeEntry.DoneButton);

            app.EnterText(description);
            app.Tap(StartTimeEntry.DoneButton);

            app.WaitForElement(Main.StopTimeEntryButton);
        }

        /// <summary>
        /// Starts a time entry.
        /// Sets description.
        /// Sets the project (creates it if necessary).
        /// Stops the time entry.
        /// </summary>
        public static void CreateTimeEntry(this IApp app, string description, string projectName = null)
        {
            app.Tap(Main.StartTimeEntryButton);
            app.WaitForElement(StartTimeEntry.DoneButton);
            app.EnterText(description);

            if (!string.IsNullOrEmpty(projectName))
            {
                app.EnterText($" @{projectName}");
                if (projectExists(projectName))
                    app.Tap(x => x.Text(projectName));
                else
                    createProject(projectName);
            }

            app.Tap(StartTimeEntry.DoneButton);
            app.Tap(Main.StopTimeEntryButton);

            bool projectExists(string name)
                => app.Query(x => x.Text(name)).Any();

            void createProject(string name)
            {
                app.TapCreateProject(projectName);
                app.WaitForElement(EditProject.CreateButton);
                app.Tap(EditProject.CreateButton);
            }
        }

        /// <summary>
        /// Starts a time entry. Creates a project. Cancels the time entry.
        /// </summary>
        public static void CreateProject(this IApp app, string projectName)
        {
            app.WaitForElement(Main.StartTimeEntryButton);
            app.Tap(Main.StartTimeEntryButton);
            app.WaitForElement(StartTimeEntry.DoneButton);
            app.EnterText($"@{projectName}");
            app.TapCreateProject(projectName);
            app.Tap(EditProject.CreateButton);
            app.Tap(StartTimeEntry.CloseButton);
            app.Tap(StartTimeEntry.DialogDiscard);
        }

        public static void TapOnTimeEntryWithDescription(this IApp app, string description)
        {
            var timeEntryCellSelector = queryForTimeEntryCell(description);
            app.WaitForElement(timeEntryCellSelector);
            app.Tap(timeEntryCellSelector);
        }

        public static void TapOnProjectWithName(this IApp app, string projectName)
        {
            Func<AppQuery, AppQuery> projectCellSelector = x => x.Marked(SelectProject.ProjectSuggestionRow).Descendant().Text(projectName);
            app.WaitForElement(projectCellSelector);
            app.Tap(projectCellSelector);
        }

        public static void SwipeEntryToDelete(this IApp app, string timeEntryDescription)
        {
            var timeEntryCellRect = RectForTimeEntryCell(app, timeEntryDescription);

            app.DragCoordinates(
                fromX: timeEntryCellRect.X + timeEntryCellRect.Width,
                fromY: timeEntryCellRect.CenterY,
                toX: timeEntryCellRect.X,
                toY: timeEntryCellRect.CenterY
            );

            app.WaitForNoElement(x => x.Text(timeEntryDescription));
        }

        public static void SwipeEntryToContinue(this IApp app, string timeEntryDescription)
        {
            var timeEntryCellRect = RectForTimeEntryCell(app, timeEntryDescription);

            app.DragCoordinates(
                fromX: timeEntryCellRect.X,
                fromY: timeEntryCellRect.CenterY,
                toX: timeEntryCellRect.X + timeEntryCellRect.Width,
                toY: timeEntryCellRect.CenterY
            );
        }

        public static void PullToRefresh(this IApp app)
        {
            app.WaitForNoElement(query => query.Text("Synced"));
            app.ScrollUp(Main.TimeEntriesCollection, ScrollStrategy.Gesture);
            app.WaitForNoElement(query => query.Text("Synced"));
        }

        public static void StopTimeEntry(this IApp app)
        {
            app.WaitForElement(Main.StopTimeEntryButton);
            app.Tap(Main.StopTimeEntryButton);
            app.WaitForNoElement(Main.StopTimeEntryButton);
        }

        public static void WaitForTimeEntryWithProject(this IApp app, string projectName)
            => app.WaitForElement(x => x.Marked(Main.TimeEntryRow).Descendant().Contains(projectName));

        public static AppRect RectForTimeEntryCell(this IApp app, string timeEntryDescription)
        {
            var timeEntryViews = app.Query(queryForTimeEntryCell(timeEntryDescription));
            if (timeEntryViews.Length == 0)
                return null;
            return timeEntryViews[0].Rect;
        }

        public static void AssertRunningTimeEntry(this IApp app, string description, string projectName = null)
        {
            Func<AppQuery, AppQuery> queryForItemsInCard = x => x.Marked(Main.CurrentTimeEntryCard).Descendant();
            Func<AppQuery, AppQuery> queryForDescriptionLabel = x => queryForItemsInCard(x).Text(description);
            Func<AppQuery, AppQuery> queryForProjectLabel = x => queryForItemsInCard(x).EndsWith(projectName); //Project name label starts with the dot icon
            var shouldCheckProject = !string.IsNullOrEmpty(projectName);

            app.WaitForElement(Main.CurrentTimeEntryCard);
            var theDescriptionIsCorrect = app.Query(queryForDescriptionLabel).Any();
            var theProjectIsCorrect = app.Query(queryForProjectLabel).Any();

            if (!theDescriptionIsCorrect)
                throw new NoRunningTimeEntryException($"There is no running time entry with description \"{description}\"");

            if (shouldCheckProject && !theProjectIsCorrect)
                throw new NoRunningTimeEntryException($"There is no running time entry with project \"{projectName}\"");
        }

        public static void AssertTimeEntryInTheLog(this IApp app, string description)
        {
            var timeEntryExists = app.Query(queryForTimeEntryCell(description)).Any();

            if (!timeEntryExists)
                throw new NoTimeEntryException($"Expected to find a time entry with description \"{description}\", but didn't find one");
        }

        public static void AssertNoTimeEntryInTheLog(this IApp app, string description)
        {
            var timeEntryExists = app.Query(queryForTimeEntryCell(description)).Any();

            if (timeEntryExists)
                throw new TimeEntryFoundException($"Expected to find no time entry with description \"{description}\", but found one");
        }

        private static Func<AppQuery, AppQuery> queryForTimeEntryCell(string timeEntryDescription)
            => x => x.Marked(Main.TimeEntryRow).Descendant().Text(timeEntryDescription);
    }
}
