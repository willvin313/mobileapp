using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Calendar;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Services;
using Toggl.Foundation.Suggestions;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.Mocks;
using Xunit;

namespace Toggl.Foundation.Tests.Suggestions
{
    public sealed class CalendarSuggestionProviderTests
    {
        public abstract class CalendarSuggestionProviderTest
        {
            protected CalendarSuggestionProvider Provider { get; }

            protected ITimeService TimeService { get; } = Substitute.For<ITimeService>();
            protected ICalendarService CalendarService { get; } = Substitute.For<ICalendarService>();
            protected IInteractor<IObservable<IThreadSafeWorkspace>> DefaultWorkspaceInteractor { get; } = Substitute.For<IInteractor<IObservable<IThreadSafeWorkspace>>>();

            public CalendarSuggestionProviderTest()
            {
                Provider = new CalendarSuggestionProvider(TimeService, CalendarService, DefaultWorkspaceInteractor);
            }
        }

        public sealed class TheConstructor : CalendarSuggestionProviderTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useTimeService,
                bool useCalendarService,
                bool useDefaultWorkspaceInteractor)
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new CalendarSuggestionProvider(
                        useTimeService ? TimeService : null,
                        useCalendarService ? CalendarService : null,
                        useDefaultWorkspaceInteractor ? DefaultWorkspaceInteractor : null
                    );

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheGetSuggestionsMethod : CalendarSuggestionProviderTest
        {
            [Fact, LogIfTooSlow]
            public async Task ReturnsSuggestionsFromEventsOneHourInThePastAndOneHourInTheFuture()
            {
                var now = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero);
                TimeService.CurrentDateTime.Returns(now);
                var tenMinutes = TimeSpan.FromMinutes(10);
                var events = Enumerable.Range(1, 5)
                    .Select(id => new CalendarItem(
                        id.ToString(),
                        CalendarItemSource.Calendar,
                        now - tenMinutes * id,
                        tenMinutes,
                        id.ToString(),
                        CalendarIconKind.None));
                CalendarService
                    .GetEventsInRange(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                    .Returns(Observable.Return(events));

                var suggestions = await Provider.GetSuggestions().ToList();

                await CalendarService.Received().GetEventsInRange(now.AddHours(-1), now.AddHours(1));
                suggestions.Should().HaveCount(events.Count())
                    .And.OnlyContain(suggestion => events.Any(@event => @event.Description == suggestion.Description));
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsSuggestionsForTheDefaultWorkspace()
            {
                var defaultWorkspace = new MockWorkspace(10);
                var now = new DateTimeOffset(2020, 10, 9, 8, 7, 6, TimeSpan.Zero);
                DefaultWorkspaceInteractor.Execute().Returns(Observable.Return(defaultWorkspace));
                TimeService.CurrentDateTime.Returns(now);
                var tenMinutes = TimeSpan.FromMinutes(10);
                var events = Enumerable.Range(1, 5)
                    .Select(id => new CalendarItem(
                        id.ToString(),
                        CalendarItemSource.Calendar,
                        now - tenMinutes * id,
                        tenMinutes,
                        id.ToString(),
                        CalendarIconKind.None));
                CalendarService
                    .GetEventsInRange(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                    .Returns(Observable.Return(events));

                var suggestions = await Provider.GetSuggestions().ToList();

                suggestions.Should().OnlyContain(suggestion => suggestion.WorkspaceId == defaultWorkspace.Id);
            }
        }
    }
}
