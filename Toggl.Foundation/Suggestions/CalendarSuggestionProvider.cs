using System;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Foundation.Calendar;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Math = System.Math;

namespace Toggl.Foundation.Suggestions
{
    public sealed class CalendarSuggestionProvider : ISuggestionProvider
    {
        private readonly ITimeService timeService;
        private readonly ICalendarService calendarService;
        private readonly IInteractor<IObservable<IThreadSafeWorkspace>> defaultWorkspaceInteractor;

        private readonly TimeSpan lookBackTimeSpan = TimeSpan.FromHours(1);
        private readonly TimeSpan lookAheadTimeSpan = TimeSpan.FromHours(1);

        public CalendarSuggestionProvider(
            ITimeService timeService,
            ICalendarService calendarService,
            IInteractor<IObservable<IThreadSafeWorkspace>> defaultWorkspaceInteractor)
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(calendarService, nameof(calendarService));
            Ensure.Argument.IsNotNull(defaultWorkspaceInteractor, nameof(defaultWorkspaceInteractor));

            this.timeService = timeService;
            this.calendarService = calendarService;
            this.defaultWorkspaceInteractor = defaultWorkspaceInteractor;
        }

        public IObservable<Suggestion> GetSuggestions()
        {
            var now = timeService.CurrentDateTime;
            var startOfRange = now - lookBackTimeSpan;
            var endOfRange = now + lookAheadTimeSpan;

            var eventsObservable = calendarService
                .GetEventsInRange(startOfRange, endOfRange)
                .SelectMany(events => events.Where(eventHasDescription));

            return defaultWorkspaceInteractor.Execute()
                .CombineLatest(
                    eventsObservable,
                    (workspace, calendarItem) => suggestionFromEvent(calendarItem, workspace.Id))
                .Catch((NotAuthorizedException _) => Observable.Empty<Suggestion>());
        }

        private Suggestion suggestionFromEvent(CalendarItem calendarItem, long workspaceId)
            => new Suggestion(calendarItem, workspaceId, calculateCertainty(calendarItem));

        private bool eventHasDescription(CalendarItem calendarItem)
            => !string.IsNullOrEmpty(calendarItem.Description);

        private float calculateCertainty(CalendarItem calendarItem)
        {
            var now = timeService.CurrentDateTime;
            var delta = (now - calendarItem.StartTime).TotalMinutes;

            //Delta has to be bigger than 1, so 1 / delta < 1
            if (delta < 1)
                delta++;

            //If the event is in the past, it is rated lower than an upcoming event with the same delta
            if (calendarItem.StartTime < now)
                delta++;

            return 1 / (float)Math.Abs(delta);
        }
    }
}
