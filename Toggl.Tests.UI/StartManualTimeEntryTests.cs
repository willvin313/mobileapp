using NUnit.Framework;
using Toggl.Tests.UI.Extensions;
using Xamarin.UITest;

namespace Toggl.Tests.UI
{
    [TestFixture]
    public sealed class StartManualTimeEntryTests
    {
        private IApp app;

        [SetUp]
        public void BeforeEachTest()
        {
            app = Configuration.GetApp();

            app.WaitForMainScreen();
            app.TouchAndHold(Main.StartTimeEntryButton);
            app.WaitForElement(StartTimeEntry.DoneButton);
        }

        [Test]
        public void TappingTheDoneButtonCreatesANewTimeEntry()
        {
            app.Tap(StartTimeEntry.DoneButton);

            app.WaitForNoElement(Main.StopTimeEntryButton); // a manual te should not be running
            app.WaitForElement(Main.StartTimeEntryButton);
        }

        [Test]
        public void TheCreatedTimeEntryHasCorrectDefaultDurationSet()
        {
            app.Tap(StartTimeEntry.DoneButton);

            app.WaitForElement("0:30:00");
        }

        [Test]
        public void TheDurationIsSetCorrectly()
        {
            app.EnterManualTimeEntryDuration("1206");
            app.Tap(StartTimeEntry.DoneButton);
            app.WaitForElement("12:06:00");
        }
    }
}
