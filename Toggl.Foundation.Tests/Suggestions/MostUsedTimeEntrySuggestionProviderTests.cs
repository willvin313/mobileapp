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
                var builder = TimeEntry.Builder.Create(21)
                    .SetUserId(10)
                    .SetWorkspaceId(12)
                    .SetAt(Now)
                    .SetStart(Now);

                return Enumerable.Range(0, numberOfRepetitions.Length)
                    .SelectMany(index => Enumerable
                        .Range(0, numberOfRepetitions[index])
                        .Select(_ => builder
                            .SetDescription($"te{index}")
                            .Build()));
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsEmptyObservableIfThereAreNoTimeEntries()
            {
                DataSource.TimeEntries
                        .GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
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
                        .GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
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
                        .GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                        .Returns(Observable.Return(timeEntries));
                
                var suggestions = await Provider.GetSuggestions().ToList();

                suggestions.Should().OnlyContain(suggestion => expectedDescriptions.Contains(suggestion.Description));
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotReturnTimeEntriesWithoutDescription()
            {
                var builder = TimeEntry.Builder.Create(12)
                                       .SetUserId(9)
                                       .SetWorkspaceId(2)
                                       .SetAt(Now)
                                       .SetStart(Now)
                                       .SetDescription("");
                var emptyTimeEntries = Enumerable.Range(20, 0)
                    .Select(_ => builder.Build());
                var timeEntries = new List<IThreadSafeTimeEntry>(emptyTimeEntries);
                timeEntries.AddRange(getTimeEntries(1, 2, 3, 4, 5));
                DataSource.TimeEntries
                        .GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                        .Returns(Observable.Return(timeEntries));

                var suggestions = await Provider.GetSuggestions().ToList();

                suggestions.Should().OnlyContain(
                    suggestion => !string.IsNullOrEmpty(suggestion.Description)
                );
            }
        }
    }
}
