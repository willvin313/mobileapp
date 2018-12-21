using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Interactors.Suggestions;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Multivac.Models;
using Xunit;

namespace Toggl.Foundation.Tests.Interactors.Suggestions
{
    public sealed class GetSuggestionsInteractorTests
    {
        public sealed class TheConstructor : BaseInteractorTests
        {
            private readonly IInteractor<IObservable<IThreadSafeWorkspace>> defaultWorkspaceInteractor = Substitute.For<IInteractor<IObservable<IThreadSafeWorkspace>>>();

            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useStopWatchProvider,
                bool useDataSource,
                bool useTimeService,
                bool useCalendarService,
                bool useAnalyticsService,
                bool useDefaultWorkspaceInteractor)
            {
                Action createInstance = () => new GetSuggestionsInteractor(
                    3,
                    useStopWatchProvider ? StopwatchProvider : null,
                    useDataSource ? DataSource : null,
                    useTimeService ? TimeService : null,
                    useCalendarService ? CalendarService : null,
                    useAnalyticsService ? AnalyticsService : null,
                    useDefaultWorkspaceInteractor ? defaultWorkspaceInteractor : null);

                createInstance.Should().Throw<ArgumentNullException>();
            }

            [Theory, LogIfTooSlow]
            [InlineData(0)]
            [InlineData(-1)]
            [InlineData(10)]
            [InlineData(-100)]
            [InlineData(256)]
            public void ThrowsIfTheCountIsOutOfRange(int count)
            {
                Action createInstance = () => new GetSuggestionsInteractor(
                        count,
                        StopwatchProvider,
                        DataSource,
                        TimeService,
                        CalendarService,
                        AnalyticsService,
                        defaultWorkspaceInteractor);

                createInstance.Should().Throw<ArgumentException>();
            }
        }

        public sealed class TheExecuteMethod : BaseInteractorTests
        {
            private GetSuggestionsInteractor interactor;
            private IThreadSafeWorkspace defaultWorkspace = new MockWorkspace { Id = 12, IsInaccessible = false };

            public TheExecuteMethod()
            {
                var defaultWorkspaceInteractor = Substitute.For<IInteractor<IObservable<IThreadSafeWorkspace>>>();
                defaultWorkspaceInteractor.Execute().Returns(Observable.Return(defaultWorkspace));
                interactor = new GetSuggestionsInteractor(3, StopwatchProvider, DataSource, TimeService, CalendarService, AnalyticsService, defaultWorkspaceInteractor);
            }


            [Theory, LogIfTooSlow]
            [InlineData(10, 30, 40)]
            [InlineData(2, 3, 4)]
            [InlineData(38, 1, 2)]
            public async Task OrdersSuggestionsByCertainty(params int[] timeEntryOccurences)
            {
                var now = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero);
                TimeService.CurrentDateTime.Returns(now);
                var timeEntries = Enumerable.Range(0, timeEntryOccurences.Length)
                    .SelectMany(i => getTimeEntries(i.ToString(), timeEntryOccurences[i], now.AddHours(-i)));
                var timeEntriesObservable = Observable.Return(new List<IThreadSafeTimeEntry>(timeEntries));
                DataSource.TimeEntries.GetAll().Returns(timeEntriesObservable);

                var suggestions = await interactor.Execute();

                suggestions.Should().NotBeEmpty();
                suggestions.Should().BeInDescendingOrder(suggestion => suggestion.Certainty);
            }

            private IEnumerable<IThreadSafeTimeEntry> getTimeEntries(string description, int count, DateTimeOffset start)
            {
                return Enumerable.Range(0, count)
                    .Select(id => new MockTimeEntry { Description = description, Start = start, Workspace = defaultWorkspace });
            }
        }
    }
}
