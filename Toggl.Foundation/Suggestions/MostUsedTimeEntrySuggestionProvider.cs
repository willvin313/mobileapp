using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources;
using Toggl.Multivac;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Suggestions
{
    public sealed class MostUsedTimeEntrySuggestionProvider : ISuggestionProvider
    {
        private const int daysToQuery = 42;
        private static readonly TimeSpan thresholdPeriod = TimeSpan.FromDays(daysToQuery);

        private readonly ITimeService timeService;
        private readonly int maxNumberOfSuggestions;
        private readonly ITogglDataSource dataSource;

        public MostUsedTimeEntrySuggestionProvider(
            ITimeService timeService,
            ITogglDataSource dataSource,
            int maxNumberOfSuggestions)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));

            this.dataSource = dataSource;
            this.timeService = timeService;
            this.maxNumberOfSuggestions = maxNumberOfSuggestions;
        }

        public IObservable<Suggestion> GetSuggestions()
            => dataSource
                .TimeEntries
                .GetAll()
                .SelectMany(createSuggestions);

        private IEnumerable<Suggestion> createSuggestions(IEnumerable<IDatabaseTimeEntry> timeEntries)
        {
            var countOfAllTimeEntries = timeEntries.Count();

            return timeEntries
                .Where(isSuitableForSuggestion)
                .GroupBy(te => new { te.Description, te.ProjectId, te.TaskId })
                .OrderByDescending(g => g.Count())
                .Select(grouping =>
                    new Suggestion(
                        grouping.First(),
                        calculateCertainty(grouping.Count(), countOfAllTimeEntries),
                        SuggestionProviderType.MostUsedTimeEntries))
                .Take(maxNumberOfSuggestions);
        }

        private bool isSuitableForSuggestion(IDatabaseTimeEntry timeEntry)
            => calculateDelta(timeEntry) <= thresholdPeriod
               && isActive(timeEntry);

        private TimeSpan calculateDelta(IDatabaseTimeEntry timeEntry)
            => timeService.CurrentDateTime - timeEntry.Start;

        private bool isActive(IDatabaseTimeEntry timeEntry)
            => timeEntry.IsDeleted == false
               && !timeEntry.IsInaccessible
               && (timeEntry.Project?.Active ?? true);

        private float calculateCertainty(int occurences, int totalCount)
            => (float)occurences / totalCount;
    }
}
