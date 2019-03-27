using System;
using System.Collections.Generic;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.Interactors.AutocompleteSuggestions;

namespace Toggl.Foundation.Interactors
{
    public partial class InteractorFactory
    {
        public IInteractor<IObservable<IEnumerable<AutocompleteSuggestion>>> GetAutocompleteSuggestions(QueryInfo queryInfo)
            => new GetAutocompleteSuggestions(this, queryInfo);

        public IInteractor<IObservable<IEnumerable<AutocompleteSuggestion>>> GetTimeEntriesAutocompleteSuggestions(IList<string> wordsToQuery)
            => new GetTimeEntriesAutocompleteSuggestions(dataSource.TimeEntries, wordsToQuery);

        public IInteractor<IObservable<IEnumerable<AutocompleteSuggestion>>> GetTagsAutocompleteSuggestions(IList<string> wordsToQuery)
            => new GetTagsAutocompleteSuggestions(dataSource.Tags, wordsToQuery);

        public IInteractor<IObservable<IEnumerable<AutocompleteSuggestion>>> GetProjectsAutocompleteSuggestions(IList<string> wordsToQuery)
            => new GetProjectsAutocompleteSuggestions(dataSource.Projects, wordsToQuery);
    }
}
