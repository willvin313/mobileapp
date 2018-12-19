using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Suggestions;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Xunit;
using TimeEntry = Toggl.Foundation.Models.TimeEntry;

namespace Toggl.Foundation.Tests.Suggestions
{
    public sealed class MostUsedTimeEntrySuggestionProviderTests
    {
        public abstract class MostUsedTimeEntrySuggestionProviderTest
        {
            protected const int NumberOfSuggestions = 7;

            protected MostUsedTimeEntrySuggestionProvider Provider { get; }
            protected ITimeService TimeService { get; } = Substitute.For<ITimeService>();
            protected ITogglDataSource DataSource { get; } = Substitute.For<ITogglDataSource>();

            protected DateTimeOffset Now { get; } = new DateTimeOffset(2017, 03, 24, 12, 34, 56, TimeSpan.Zero);

            protected MostUsedTimeEntrySuggestionProviderTest()
            {
                Provider = new MostUsedTimeEntrySuggestionProvider(TimeService, DataSource, NumberOfSuggestions);

                TimeService.CurrentDateTime.Returns(_ => Now);
            }
        }

        public sealed class TheConstructor : MostUsedTimeEntrySuggestionProviderTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(bool useDataSource, bool useTimeService)
            {
                var dataSource = useDataSource ? DataSource : null;
                var timeService = useTimeService ? TimeService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new MostUsedTimeEntrySuggestionProvider(timeService, dataSource, NumberOfSuggestions);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheGetSuggestionsMethod : MostUsedTimeEntrySuggestionProviderTest
        {
            private IEnumerable<IThreadSafeTimeEntry> getTimeEntries(params int[] numberOfRepetitions)
            {
                var workspace = new MockWorkspace { Id = 12 };
                var timeEntryPrototype = new MockTimeEntry()
                {
                    Id = 21,
                    UserId = 10,
                    WorkspaceId = workspace.Id,
                    Workspace = workspace,
                    At = Now,
                    Start = Now
                };

                return Enumerable.Range(0, numberOfRepetitions.Length)
                .SelectMany(index => Enumerable
                    .Range(0, numberOfRepetitions[index])
                    .Select(_ => new MockTimeEntry(timeEntryPrototype) { Description = $"te{index}" }));
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsEmptyObservableIfThereAreNoTimeEntries()
            {
                DataSource.TimeEntries
                        .GetAll()
                        .Returns(Observable.Empty<IEnumerable<IThreadSafeTimeEntry>>());

                var suggestions = await Provider.GetSuggestions().ToList();

                suggestions.Should().HaveCount(0);
            }

            [Property(StartSize = 1, EndSize = 10, MaxTest = 10)]
            public void ReturnsUpToNSuggestionsWhereNIsTheNumberUsedWhenConstructingTheProvider(
                NonNegativeInt numberOfSuggestions)
            {
                var provider = new MostUsedTimeEntrySuggestionProvider(TimeService, DataSource, numberOfSuggestions.Get);

                var timeEntries = getTimeEntries(2, 2, 2, 3, 3, 4, 5, 5, 6, 6, 7, 7, 7, 8, 8, 9);

                DataSource.TimeEntries
                        .GetAll()
                        .Returns(Observable.Return(timeEntries));

                var suggestions = provider.GetSuggestions().ToList().Wait();

                suggestions.Should().HaveCount(numberOfSuggestions.Get);
            }

            [Fact, LogIfTooSlow]
            public async Task SortsTheSuggestionsByUsage()
            {
                var timeEntries = getTimeEntries(5, 3, 2, 5, 4, 4, 5, 4, 3);
                var expectedDescriptions = new[] { 0, 3, 6, 4, 5, 7, 1 }.Select(i => $"te{i}");
              
                DataSource.TimeEntries
                        .GetAll()
                        .Returns(Observable.Return(timeEntries));
                
                var suggestions = await Provider.GetSuggestions().ToList();

                suggestions.Should().OnlyContain(suggestion => expectedDescriptions.Contains(suggestion.Description));
            }
        }
    }
}
