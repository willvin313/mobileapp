using NUnit.Framework;
using Toggl.Tests.UI.Extensions;
using Xamarin.UITest;

namespace Toggl.Tests.UI
{
    [TestFixture]
    public sealed class EditTimeEntryTests
    {
        private IApp app;

        [SetUp]
        public void BeforeEachTest()
        {
            app = Configuration.GetApp();

            app.WaitForMainScreen();
        }

        [Test]
        public void UpdatesTheCorrectCellWhenTheDescriptionIsChanged()
        {
            var initialDescription = "This is a time entry";
            var appendedText = " (and stuff)";
            var newDescription = initialDescription + appendedText;

            app.StartTimeEntryWithDescription(initialDescription);
            app.StopTimeEntry();

            app.TapOnTimeEntryWithDescription(initialDescription);

            app.WaitForElement(EditTimeEntry.EditDescription);
            app.Tap(EditTimeEntry.EditDescription);
            app.EnterText(appendedText);
            app.DismissKeyboard();

            app.ConfirmEditTimeEntry();

            app.WaitForNoElement(EditTimeEntry.Confirm);

            app.WaitForNoElement(x => x.Text(initialDescription));
            app.WaitForElement(x => x.Text(newDescription));
        }

        [Test]
        public void AssigningANewProjectToATimeEntryWithoutAProject()
        {
            var timeEntryDescription = "Does not matter";
            var projectName = "this is a project";
            app.StartTimeEntryWithDescription(timeEntryDescription);
            app.StopTimeEntry();
            app.TapOnTimeEntryWithDescription(timeEntryDescription);

            app.WaitForElement(EditTimeEntry.EditProject);
            app.Tap(EditTimeEntry.EditProject);

            app.WaitForElement(SelectProject.ProjectNameTextField);
            app.Tap(SelectProject.ProjectNameTextField);
            app.EnterText(projectName);
            app.TapCreateProject(projectName);

            app.WaitForElement(EditProject.CreateButton);
            app.Tap(EditProject.CreateButton);

            app.WaitForElement(EditTimeEntry.Confirm);
            app.Tap(EditTimeEntry.Confirm);

            app.WaitForTimeEntryWithProject(projectName);
        }

        [Test]
        public void AssigningANewProjectToATimeEntryWithAProject()
        {
            var timeEntryDescription = "Something";
            var projectName = "This is a project";
            var newProjectName = "This is a different project";
            app.CreateTimeEntry(timeEntryDescription, projectName);

            app.TapOnTimeEntryWithDescription(timeEntryDescription);

            app.WaitForElement(EditTimeEntry.EditProject);
            app.Tap(EditTimeEntry.EditProject);

            app.WaitForElement(SelectProject.ProjectNameTextField);
            app.EnterText(newProjectName);
            app.TapCreateProject(newProjectName);
            app.Tap(EditProject.CreateButton);
            app.Tap(EditTimeEntry.Confirm);

            app.WaitForTimeEntryWithProject(newProjectName);
        }

        [Test]
        public void AssigningAnExistingProjectToATimeEntryWithoutAProject()
        {
            var projectName = "This is an existing project";
            var timeEntryDescription = "This is a time entry";
            app.CreateProject(projectName);
            app.CreateTimeEntry(timeEntryDescription);

            app.TapOnTimeEntryWithDescription(timeEntryDescription);

            app.WaitForElement(EditTimeEntry.EditProject);
            app.Tap(EditTimeEntry.EditProject);

            app.EnterText(projectName);
            app.TapOnProjectWithName(projectName);

            app.Tap(EditTimeEntry.Confirm);

            app.WaitForTimeEntryWithProject(projectName);
        }

        [Test]
        public void AssigningAnExistingProjectToATimeEntryWithAProject()
        {
            var timeEntryDescription = "Listening to human music";
            var assignedProjectName = "Work";
            var newProjectName = "No work";

            app.CreateProject(newProjectName);
            app.CreateTimeEntry(timeEntryDescription, assignedProjectName);

            app.TapOnTimeEntryWithDescription(timeEntryDescription);

            app.WaitForElement(EditTimeEntry.EditProject);
            app.Tap(EditTimeEntry.EditProject);

            app.WaitForElement(SelectProject.ProjectNameTextField);
            app.Tap(SelectProject.ProjectNameTextField);
            app.EnterText(newProjectName);

            app.TapOnProjectWithName(newProjectName);

            app.Tap(EditTimeEntry.Confirm);

            app.WaitForTimeEntryWithProject(newProjectName);
        }
    }
}
