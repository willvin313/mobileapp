using System;
using System.Linq;
using System.Reactive.Linq;
using MvvmCross.ViewModels;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models.Reports;
using static System.Math;

namespace Toggl.Foundation.MvvmCross.ViewModels.Reports
{
    public sealed class ReportsBarChartViewModel : MvxViewModel
    {
        private const int maximumLabeledNumberOfDays = 7;

        private const int roundToMultiplesOf = 2;

        private IDisposable reportsDisposable;

        public IObservable<BarViewModel[]> Bars { get; }

        public IObservable<int> MaximumHoursPerBar { get; }

        public IObservable<DateTimeOffset[]> HorizontalLegend { get; }

        public IObservable<DateFormat> DateFormat { get; }

        private readonly DateFormat defaultDateFormat = Multivac.DateFormat.FromLocalizedDateFormat("mm/dd");

        public ReportsBarChartViewModel(
            ISchedulerProvider schedulerProvider,
            ISingletonDataSource<IThreadSafePreferences> preferencesDataSource,
            IObservable<ITimeEntriesTotals> reports)
        {
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(preferencesDataSource, nameof(preferencesDataSource));
            Ensure.Argument.IsNotNull(reports, nameof(reports));

            DateFormat = preferencesDataSource.Current
                .Select(preferences => preferences.DateFormat)
                .AsDriver(onErrorJustReturn: defaultDateFormat, schedulerProvider: schedulerProvider);

            var finalReports = reports.Replay(1);
            reportsDisposable = finalReports.Connect();

            Bars = finalReports.Select(bars)
                .AsDriver(onErrorJustReturn: Array.Empty<BarViewModel>(), schedulerProvider: schedulerProvider);

            MaximumHoursPerBar = finalReports.Select(upperHoursLimit)
                .AsDriver(onErrorJustReturn: 0, schedulerProvider: schedulerProvider);

            HorizontalLegend = finalReports.Select(weeklyLegend)
                .AsDriver(onErrorJustReturn: null, schedulerProvider: schedulerProvider);
        }

        public override void ViewDestroy(bool viewFinishing = true)
        {
            base.ViewDestroy(viewFinishing);

            if (!viewFinishing) return;

            reportsDisposable.Dispose();
        }

        private BarViewModel[] bars(ITimeEntriesTotals report)
        {
            var upperLimit = upperHoursLimit(report);
            return report.Groups.Select(normalizedBar(upperLimit)).ToArray();
        }

        private int upperHoursLimit(ITimeEntriesTotals report)
        {
            var maximumTotalTrackedTimePerGroup = report.Groups.Max(group => group.Total);
            var rounded = (int)Ceiling(maximumTotalTrackedTimePerGroup.TotalHours / roundToMultiplesOf) * roundToMultiplesOf;
            return Max(roundToMultiplesOf, rounded);
        }

        private Func<ITimeEntriesTotalsGroup, BarViewModel> normalizedBar(double maxHours)
            => group =>
            {
                var billableHours = group.Billable.TotalHours;
                var nonBillableHours = group.Total.TotalHours - billableHours;
                return new BarViewModel(billableHours / maxHours, nonBillableHours / maxHours);
            };

        private DateTimeOffset[] weeklyLegend(ITimeEntriesTotals report)
            => report.Groups.Length <= maximumLabeledNumberOfDays && report.Resolution == Resolution.Day
                ? daysRange(report.Groups.Length, report.StartDate)
                : null;

        private DateTimeOffset[] daysRange(int numberOfDays, DateTimeOffset startDate)
            => Enumerable.Range(0, numberOfDays)
                .Select(i => startDate.AddDays(i))
                .ToArray();
    }
}
