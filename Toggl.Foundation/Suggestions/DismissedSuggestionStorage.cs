using System.Collections.Generic;
using Toggl.Foundation.Suggestions.Interfaces;
using Toggl.Multivac;

namespace Toggl.Foundation.Suggestions
{
    public sealed class DismissedSuggestionStorage : IDismissedSuggestionStorage
    {
        private struct HashableSuggestion
        {
            public string Description { get; }

            public long? ProjectId { get; }

            public long? TaskId { get; }

            public HashableSuggestion(Suggestion suggestion)
            {
                Description = suggestion.Description;
                ProjectId = suggestion.ProjectId;
                TaskId = suggestion.TaskId;
            }

            public override int GetHashCode()
                => HashCode.From(Description, ProjectId, TaskId);
        }

        private readonly HashSet<HashableSuggestion> dismissedSuggestions
            = new HashSet<HashableSuggestion>();

        public void StoreDismissedSuggestion(Suggestion suggestion)
            => dismissedSuggestions.Add(new HashableSuggestion(suggestion));

        public bool WasSuggestionDismissed(Suggestion suggestion)
            => dismissedSuggestions.Contains(new HashableSuggestion(suggestion));
    }
}
