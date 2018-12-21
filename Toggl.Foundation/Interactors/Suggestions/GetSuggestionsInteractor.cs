using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Services;
using Toggl.Foundation.Suggestions;
using Toggl.Multivac;

namespace Toggl.Foundation.Interactors.Suggestions
{
    public sealed class GetSuggestionsInteractor : IInteractor<IObservable<IEnumerable<Suggestion>>>
    {
        private readonly int suggestionCount;

        private readonly IStopwatchProvider stopwatchProvider;
        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly ICalendarService calendarService;
        private readonly IAnalyticsService analyticsService;
        private readonly IInteractor<IObservable<IThreadSafeWorkspace>> defaultWorkspaceInteractor;

        public GetSuggestionsInteractor(
            int suggestionCount,
            IStopwatchProvider stopwatchProvider,
            ITogglDataSource dataSource,
            ITimeService timeService,
            ICalendarService calendarService,
            IAnalyticsService analyticsService,
            IInteractor<IObservable<IThreadSafeWorkspace>> defaultWorkspaceInteractor)
        {
            Ensure.Argument.IsInClosedRange(suggestionCount, 1, 9, nameof(suggestionCount));
            Ensure.Argument.IsNotNull(stopwatchProvider, nameof(stopwatchProvider));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(calendarService, nameof(calendarService));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(defaultWorkspaceInteractor, nameof(defaultWorkspaceInteractor));

            this.stopwatchProvider = stopwatchProvider;
            this.dataSource = dataSource;
            this.timeService = timeService;
            this.suggestionCount = suggestionCount;
            this.calendarService = calendarService;
            this.analyticsService = analyticsService;
            this.defaultWorkspaceInteractor = defaultWorkspaceInteractor;
        }

        public IObservable<IEnumerable<Suggestion>> Execute()
            => getSuggestionProviders()
                .Select(getSuggestionsWithExceptionTracking)
                .Aggregate(Observable.Concat)
                .ToList()
                .Select(balancedSuggestions)
                .Select(suggestions => suggestions
                    .OrderByDescending(suggestion => suggestion.Certainty)
                    .Take(suggestionCount));

        private IReadOnlyList<ISuggestionProvider> getSuggestionProviders()
        {
            return new List<ISuggestionProvider>
            {
                new RandomForestSuggestionProvider(stopwatchProvider, dataSource, timeService, suggestionCount),
                new MostUsedTimeEntrySuggestionProvider(timeService, dataSource, suggestionCount),
                new CalendarSuggestionProvider(timeService, calendarService, defaultWorkspaceInteractor)
            };
        }

        private IList<Suggestion> balancedSuggestions(IList<Suggestion> suggestions)
        {
            if (!suggestions.Any())
                return suggestions;

            var overallAverageCertainty = suggestions.Average(suggestion => suggestion.Certainty);

            var averageCertaintiesPerProvider = suggestions
                .GroupBy(suggestion => suggestion.ProviderType)
                .ToDictionary(
                    keySelector: grouping => grouping.First().ProviderType,
                    elementSelector: grouping => grouping.Average(s => s.Certainty));

            var results = new List<Suggestion>();
            foreach (var suggestion in suggestions)
            {
                var exponent = System.Math.Log(overallAverageCertainty, averageCertaintiesPerProvider[suggestion.ProviderType]);
                var newCertainty = (float)System.Math.Pow(suggestion.Certainty, exponent);
                results.Add(new Suggestion(suggestion, newCertainty));
            }
            return results;
        }

        private IObservable<Suggestion> getSuggestionsWithExceptionTracking(ISuggestionProvider provider)
        {
            return provider.GetSuggestions()
                .Catch((Exception exception) => {
                    var providerException = new SuggestionProviderException($"{provider.GetType()} threw an exception", exception);
                    analyticsService.Track(providerException, providerException.Message);
                    return Observable.Empty<Suggestion>();
                });
        }
    }
}
