using System;
using System.Collections.Generic;
using Toggl.Foundation.Interactors.Suggestions;
using Toggl.Foundation.Suggestions;

namespace Toggl.Foundation.Interactors
{
    public sealed partial class InteractorFactory : IInteractorFactory
    {
        public IInteractor<IObservable<IEnumerable<Suggestion>>> GetSuggestions(int count)
            => new GetSuggestionsInteractor(
                count, 
                analyticsService, 
                GetSuggestionProviders(count)
            );

        public IInteractor<IEnumerable<ISuggestionProvider>> GetSuggestionProviders(int count)
            => new GetSuggestionProvidersInteractor(
                count, 
                stopwatchProvider, 
                dataSource, 
                timeService, 
                calendarService, 
                GetDefaultWorkspace()
            );
    }
}
