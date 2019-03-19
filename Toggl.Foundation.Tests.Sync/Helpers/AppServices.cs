using System.Reactive.Concurrency;
using MvvmCross.Navigation;
using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Foundation.Sync;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave;
using IStopwatchProvider = Toggl.Foundation.Diagnostics.IStopwatchProvider;

namespace Toggl.Foundation.Tests.Sync.Helpers
{
    public sealed class AppServices
    {
        private readonly ISyncErrorHandlingService syncErrorHandlingService;

        public IScheduler Scheduler { get; }

        public ITimeService TimeService { get; }

        public IAccessRestrictionStorage AccessRestrictionStorageSubsitute { get; } =
            Substitute.For<IAccessRestrictionStorage>();

        public IMvxNavigationService NavigationServiceSubstitute { get; } =
            Substitute.For<IMvxNavigationService>();

        public IAnalyticsService AnalyticsServiceSubstitute { get; } = Substitute.For<IAnalyticsService>();

        public ILastTimeUsageStorage LastTimeUsageStorageSubstitute { get; } = Substitute.For<ILastTimeUsageStorage>();

        public IStopwatchProvider StopwatchProvider { get; } = Substitute.For<IStopwatchProvider>();

        public ISyncManager SyncManager { get; }

        public IAutomaticSyncingService AutomaticSyncingService { get; } = Substitute.For<IAutomaticSyncingService>();

        public AppServices(ITogglApi api, ITogglDatabase database)
        {
            Scheduler = System.Reactive.Concurrency.Scheduler.Default;
            TimeService = new TimeService(Scheduler);

            var errorHandlingService = new ErrorHandlingService(NavigationServiceSubstitute, AccessRestrictionStorageSubsitute);
            syncErrorHandlingService = new SyncErrorHandlingService(errorHandlingService);

            var dataSource = new TogglDataSource(
                database,
                TimeService,
                AnalyticsServiceSubstitute);

            SyncManager = TogglSyncManager.CreateSyncManager(
                database,
                api,
                dataSource,
                TimeService,
                AnalyticsServiceSubstitute,
                LastTimeUsageStorageSubstitute,
                Scheduler,
                StopwatchProvider,
                AutomaticSyncingService);

            syncErrorHandlingService.HandleErrorsOf(SyncManager);
        }
    }
}
