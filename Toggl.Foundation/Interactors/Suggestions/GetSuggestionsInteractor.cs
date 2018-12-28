using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.Suggestions;
using Toggl.Multivac;

namespace Toggl.Foundation.Interactors.Suggestions
{
    public sealed class GetSuggestionsInteractor : IInteractor<IObservable<IEnumerable<Suggestion>>>
    {
        private readonly int suggestionCount;
        private readonly IAnalyticsService analyticsService;
        private readonly IInteractor<IEnumerable<ISuggestionProvider>> suggestionProvidersInteractor;

        public GetSuggestionsInteractor(
            int suggestionCount,
            IAnalyticsService analyticsService,
            IInteractor<IEnumerable<ISuggestionProvider>> suggestionProvidersInteractor)
        {
            Ensure.Argument.IsInClosedRange(suggestionCount, 1, 9, nameof(suggestionCount));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(suggestionProvidersInteractor, nameof(suggestionProvidersInteractor));

            this.suggestionCount = suggestionCount;
            this.analyticsService = analyticsService;
            this.suggestionProvidersInteractor = suggestionProvidersInteractor;
        }

        public IObservable<IEnumerable<Suggestion>> Execute()
            => suggestionProvidersInteractor
                .Execute()
                .Select(getSuggestionsWithExceptionTracking)
                .Aggregate(Observable.Concat)
                .ToList()
                .Select(balancedSuggestions)
                .Select(suggestions => suggestions
                    .OrderByDescending(suggestion => suggestion.Certainty)
                    .Take(suggestionCount));

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
