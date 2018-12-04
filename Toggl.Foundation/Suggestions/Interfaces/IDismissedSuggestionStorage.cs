namespace Toggl.Foundation.Suggestions.Interfaces
{
    public interface IDismissedSuggestionStorage
    {
        void StoreDismissedSuggestion(Suggestion suggestion);
        bool WasSuggestionDismissed(Suggestion suggestion);
    }
}
