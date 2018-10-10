using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Foundation.MvvmCross.ViewModels.Reports;
using Toggl.Foundation.Services;
using Toggl.Foundation.Suggestions;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class MainTabBarViewModel : MvxViewModel
    {
        private readonly IRemoteConfigService remoteConfigService;

        private readonly MainViewModel mainViewModel;
        private readonly ReportsViewModel reportsViewModel;
        private readonly CalendarViewModel calendarViewModel;

        public IEnumerable<MvxViewModel> Tabs { get; private set; }

        public MainTabBarViewModel(
            ITimeService timeService,
            ITogglDataSource dataSource,
            IDialogService dialogService,
            IRatingService ratingService,
            IUserPreferences userPreferences,
            IAnalyticsService analyticsService,
            IBackgroundService backgroundService,
            IInteractorFactory interactorFactory,
            IOnboardingStorage onboardingStorage,
            ISchedulerProvider schedulerProvider,
            IPermissionsService permissionsService,
            IMvxNavigationService navigationService,
            IRemoteConfigService remoteConfigService,
            ISuggestionProviderContainer suggestionProviders,
            IIntentDonationService intentDonationService,
            IAccessRestrictionStorage accessRestrictionStorage)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(dialogService, nameof(dialogService));
            Ensure.Argument.IsNotNull(ratingService, nameof(ratingService));
            Ensure.Argument.IsNotNull(userPreferences, nameof(userPreferences));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(backgroundService, nameof(backgroundService));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(permissionsService, nameof(permissionsService));
            Ensure.Argument.IsNotNull(remoteConfigService, nameof(remoteConfigService));
            Ensure.Argument.IsNotNull(suggestionProviders, nameof(suggestionProviders));
            Ensure.Argument.IsNotNull(accessRestrictionStorage, nameof(accessRestrictionStorage));
            Ensure.Argument.IsNotNull(intentDonationService, nameof(intentDonationService));
            Ensure.Argument.IsNotNull(dialogService, nameof(dialogService));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));

            this.remoteConfigService = remoteConfigService;

            mainViewModel = new MainViewModel(
                dataSource,
                timeService,
                ratingService,
                userPreferences,
                analyticsService,
                onboardingStorage,
                interactorFactory,
                navigationService,
                remoteConfigService,
                suggestionProviders,
                intentDonationService,
                accessRestrictionStorage,
                schedulerProvider);

            reportsViewModel = new ReportsViewModel(
                dataSource,
                timeService,
                navigationService,
                interactorFactory,
                analyticsService,
                dialogService,
                intentDonationService,
                schedulerProvider);

            calendarViewModel = new CalendarViewModel(
                dataSource,
                timeService,
                dialogService,
                userPreferences,
                analyticsService,
                backgroundService,
                interactorFactory,
                onboardingStorage,
                schedulerProvider,
                permissionsService,
                navigationService);
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            var isCalendarEnabled = await remoteConfigService.IsCalendarFeatureEnabled;

            Tabs = getViewModels(isCalendarEnabled);

            await Tabs
                .Select(vm => vm.Initialize())
                .Apply(Task.WhenAll);
        }

        private IEnumerable<MvxViewModel> getViewModels(bool isCalendarEnabled)
        {
            yield return mainViewModel;
            yield return reportsViewModel;

            if (isCalendarEnabled)
            {
                yield return calendarViewModel;
            }
        }
    }
}
