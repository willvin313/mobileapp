using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Suggestions;
using Toggl.Multivac;

namespace Toggl.Foundation.Interactors.Suggestions
{
    public sealed class GetSuggestionsInteractor : IInteractor<IObservable<IEnumerable<Suggestion>>>
    {
        private readonly int suggestionCount;
        private readonly ITogglDataSource dataSource;
        private readonly ITimeService timeService;

        public GetSuggestionsInteractor(int suggestionCount, ITogglDataSource dataSource, ITimeService timeService)
        {
            Ensure.Argument.IsInClosedRange(suggestionCount, 1, 9, nameof(suggestionCount));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));

            this.dataSource = dataSource;
            this.timeService = timeService;
            this.suggestionCount = suggestionCount;
        }

        public IObservable<IEnumerable<Suggestion>> Execute()
            => getSuggestionProviders()
                .Select(provider => provider.GetSuggestions())
                .Aggregate(Observable.Concat)
                .Take(suggestionCount)
                .ToList();

        private IReadOnlyList<ISuggestionProvider> getSuggestionProviders()
        {
            return new List<ISuggestionProvider>
            {
                new MostUsedTimeEntrySuggestionProvider(timeService, dataSource, suggestionCount)
            };
        }
    }
}
