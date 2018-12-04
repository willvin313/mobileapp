using FluentAssertions;
using Toggl.Foundation.Suggestions;
using Toggl.Foundation.Suggestions.Interfaces;
using Xunit;

namespace Toggl.Foundation.Tests.Suggestions
{
    public sealed class DismissedSuggestionStorageTests
    {
        public sealed class TheWasSuggestionDismissedMethod
        {
            private readonly IDismissedSuggestionStorage dismissedSuggestionStorage = new DismissedSuggestionStorage();

            [Theory, LogIfTooSlow]
            [InlineData("", null, null)]
            [InlineData("", 12, null)]
            [InlineData("", 12, 13)]
            [InlineData("Some time entry", null, null)]
            [InlineData("Some time entry", 12, null)]
            [InlineData("Some time entry", 12, 13)]
            public void ReturnsFalseForSuggestionsThatHaveNotBeenDismissed(
                string description, long? projectId, long? taskId)
            {
                var suggestion = getSuggestion(description, projectId, taskId);

                dismissedSuggestionStorage
                    .WasSuggestionDismissed(suggestion)
                    .Should()
                    .BeFalse();
            }

            [Theory, LogIfTooSlow]
            [InlineData("", null, null)]
            [InlineData("", 12, null)]
            [InlineData("", 12, 13)]
            [InlineData("Some time entry", null, null)]
            [InlineData("Some time entry", 12, null)]
            [InlineData("Some time entry", 12, 13)]
            public void ReturnsTrueForSuggestionsThatHaveBeenDismissed(
               string description, long? projectId, long? taskId)
            {
                var suggestion = getSuggestion(description, projectId, taskId);
                dismissedSuggestionStorage.StoreDismissedSuggestion(suggestion);

                dismissedSuggestionStorage
                    .WasSuggestionDismissed(suggestion)
                    .Should()
                    .BeTrue();
            }

            private Suggestion getSuggestion(string description, long? projectId, long? taskId)
            {
                var timeEntry = new Mocks.MockTimeEntry
                {
                    Description = description,
                    ProjectId = projectId,
                    TaskId = taskId
                };

                return new Suggestion(timeEntry);
            }
        }
    }
}
