using System;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Foundation.MvvmCross.ViewModels.Reports;
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar;
using Toggl.Foundation.MvvmCross.ViewModels.Selectable;
using Toggl.Foundation.MvvmCross.ViewModels.Settings;

namespace Toggl.Foundation.MvvmCross
{
    public sealed class TogglViewModelLocator : MvxDefaultViewModelLocator
    {
        private readonly FoundationUiDependencyContainer dependencyContainer;

        public TogglViewModelLocator(IMvxNavigationService navigationService, FoundationUiDependencyContainer dependencyContainer)
            : base(navigationService)
        {
            this.dependencyContainer = dependencyContainer;
        }

        public override IMvxViewModel Load(Type viewModelType, IMvxBundle parameterValues, IMvxBundle savedState)
        {
            var viewModel = findViewModel(viewModelType);

            RunViewModelLifecycle(viewModel, parameterValues, savedState);

            return viewModel;
        }

        public override IMvxViewModel<TParameter> Load<TParameter>(Type viewModelType, TParameter param, IMvxBundle parameterValues, IMvxBundle savedState)
        {
            var viewModel = findViewModel(viewModelType) as IMvxViewModel<TParameter>;

            RunViewModelLifecycle(viewModel, param, parameterValues, savedState);

            return viewModel;
        }

        private IMvxViewModel findViewModel(Type viewModelType)
        {
            if(viewModelType == typeof(BrowserViewModel))
                return new BrowserViewModel(dependencyContainer.NavigationService.Value);

Failed for ViewModel /Users/will/Documents/Projects/mobileapp/Toggl.Foundation.MvvmCross/ViewModels/EditDurationViewModel.cs
            if(viewModelType == typeof(EditProjectViewModel))
                return new EditProjectViewModel(dependencyContainer.DataSource.Value, dependencyContainer.DialogService.Value, dependencyContainer.InteractorFactory.Value, dependencyContainer.NavigationService.Value, dependencyContainer.StopwatchProvider.Value);

            if(viewModelType == typeof(EditTimeEntryViewModel))
                return new EditTimeEntryViewModel(dependencyContainer.TimeService.Value, dependencyContainer.DataSource.Value, dependencyContainer.InteractorFactory.Value, dependencyContainer.NavigationService.Value, dependencyContainer.OnboardingStorage.Value, dependencyContainer.DialogService.Value, dependencyContainer.AnalyticsService.Value, dependencyContainer.StopwatchProvider.Value);

            if(viewModelType == typeof(ForgotPasswordViewModel))
                return new ForgotPasswordViewModel(dependencyContainer.TimeService.Value, dependencyContainer.LoginManager, dependencyContainer.AnalyticsService.Value, dependencyContainer.NavigationService.Value, dependencyContainer.SchedulerProvider.Value);

            if(viewModelType == typeof(LoginViewModel))
                return new LoginViewModel(dependencyContainer.LoginManager, dependencyContainer.AnalyticsService.Value, dependencyContainer.OnboardingStorage.Value, dependencyContainer.ForkingNavigationService.Value, dependencyContainer.PasswordManagerService.Value, dependencyContainer.ErrorHandlingService.Value, dependencyContainer.LastTimeUsageStorage.Value, dependencyContainer.TimeService.Value, dependencyContainer.SchedulerProvider.Value);

            if(viewModelType == typeof(MainTabBarViewModel))
                return new MainTabBarViewModel(dependencyContainer.TimeService.Value, dependencyContainer.DataSource.Value, dependencyContainer.DialogService.Value, dependencyContainer.RatingService.Value, dependencyContainer.UserPreferences.Value, dependencyContainer.AnalyticsService.Value, dependencyContainer.BackgroundService.Value, dependencyContainer.InteractorFactory.Value, dependencyContainer.OnboardingStorage.Value, dependencyContainer.SchedulerProvider.Value, dependencyContainer.PermissionsService.Value, dependencyContainer.NavigationService.Value, dependencyContainer.RemoteConfigService.Value, dependencyContainer.SuggestionProviderContainer.Value, dependencyContainer.IntentDonationService.Value, dependencyContainer.AccessRestrictionStorage.Value, dependencyContainer.StopwatchProvider.Value);

            if(viewModelType == typeof(MainViewModel))
                return new MainViewModel(dependencyContainer.DataSource.Value, dependencyContainer.TimeService.Value, dependencyContainer.RatingService.Value, dependencyContainer.UserPreferences.Value, dependencyContainer.AnalyticsService.Value, dependencyContainer.OnboardingStorage.Value, dependencyContainer.InteractorFactory.Value, dependencyContainer.NavigationService.Value, dependencyContainer.RemoteConfigService.Value, dependencyContainer.SuggestionProviderContainer.Value, dependencyContainer.IntentDonationService.Value, dependencyContainer.AccessRestrictionStorage.Value, dependencyContainer.SchedulerProvider.Value, dependencyContainer.StopwatchProvider.Value);

            if(viewModelType == typeof(NoWorkspaceViewModel))
                return new NoWorkspaceViewModel(dependencyContainer.DataSource.Value, dependencyContainer.InteractorFactory.Value, dependencyContainer.NavigationService.Value, dependencyContainer.AccessRestrictionStorage.Value, dependencyContainer.SchedulerProvider.Value);

            if(viewModelType == typeof(OnboardingViewModel))
                return new OnboardingViewModel(dependencyContainer.NavigationService.Value, dependencyContainer.OnboardingStorage.Value, dependencyContainer.AnalyticsService.Value);

            if(viewModelType == typeof(OutdatedAppViewModel))
                return new OutdatedAppViewModel(dependencyContainer.BrowserService.Value);

            if(viewModelType == typeof(RatingViewModel))
                return new RatingViewModel(dependencyContainer.TimeService.Value, dependencyContainer.DataSource.Value, dependencyContainer.RatingService.Value, dependencyContainer.AnalyticsService.Value, dependencyContainer.OnboardingStorage.Value, dependencyContainer.NavigationService.Value, dependencyContainer.SchedulerProvider.Value);

            if(viewModelType == typeof(ReportsCalendarViewModel))
                return new ReportsCalendarViewModel(dependencyContainer.TimeService.Value, dependencyContainer.DialogService.Value, dependencyContainer.DataSource.Value, dependencyContainer.IntentDonationService.Value);

            if(viewModelType == typeof(SelectBeginningOfWeekViewModel))
                return new SelectBeginningOfWeekViewModel(dependencyContainer.NavigationService.Value);

            if(viewModelType == typeof(SelectClientViewModel))
                return new SelectClientViewModel(dependencyContainer.InteractorFactory.Value, dependencyContainer.NavigationService.Value, dependencyContainer.SchedulerProvider.Value);

            if(viewModelType == typeof(SelectColorViewModel))
                return new SelectColorViewModel(dependencyContainer.NavigationService.Value);

            if(viewModelType == typeof(SelectCountryViewModel))
                return new SelectCountryViewModel(dependencyContainer.NavigationService.Value);

            if(viewModelType == typeof(SelectDateFormatViewModel))
                return new SelectDateFormatViewModel(dependencyContainer.NavigationService.Value);

            if(viewModelType == typeof(SelectDateTimeViewModel))
                return new SelectDateTimeViewModel(dependencyContainer.NavigationService.Value);

            if(viewModelType == typeof(SelectDefaultWorkspaceViewModel))
                return new SelectDefaultWorkspaceViewModel(dependencyContainer.DataSource.Value, dependencyContainer.InteractorFactory.Value, dependencyContainer.NavigationService.Value, dependencyContainer.AccessRestrictionStorage.Value);

            if(viewModelType == typeof(SelectDurationFormatViewModel))
                return new SelectDurationFormatViewModel(dependencyContainer.NavigationService.Value);

            if(viewModelType == typeof(SelectProjectViewModel))
                return new SelectProjectViewModel(dependencyContainer.DataSource.Value, dependencyContainer.InteractorFactory.Value, dependencyContainer.NavigationService.Value, dependencyContainer.DialogService.Value, dependencyContainer.SchedulerProvider.Value, dependencyContainer.StopwatchProvider.Value);

            if(viewModelType == typeof(SelectTagsViewModel))
                return new SelectTagsViewModel(dependencyContainer.DataSource.Value, dependencyContainer.NavigationService.Value, dependencyContainer.InteractorFactory.Value, dependencyContainer.StopwatchProvider.Value);

            if(viewModelType == typeof(SelectTimeViewModel))
                return new SelectTimeViewModel(dependencyContainer.DataSource.Value, dependencyContainer.NavigationService.Value, dependencyContainer.InteractorFactory.Value, dependencyContainer.TimeService.Value);

Failed for ViewModel /Users/will/Documents/Projects/mobileapp/Toggl.Foundation.MvvmCross/ViewModels/SelectWorkspaceViewModel.cs
            if(viewModelType == typeof(SignupViewModel))
                return new SignupViewModel(dependencyContainer.ApiFactory.Value, dependencyContainer.LoginManager, dependencyContainer.AnalyticsService.Value, dependencyContainer.OnboardingStorage.Value, dependencyContainer.ForkingNavigationService.Value, dependencyContainer.ErrorHandlingService.Value, dependencyContainer.LastTimeUsageStorage.Value, dependencyContainer.TimeService.Value, dependencyContainer.SchedulerProvider.Value);

            if(viewModelType == typeof(StartTimeEntryViewModel))
                return new StartTimeEntryViewModel(dependencyContainer.TimeService.Value, dependencyContainer.DataSource.Value, dependencyContainer.DialogService.Value, dependencyContainer.UserPreferences.Value, dependencyContainer.OnboardingStorage.Value, dependencyContainer.InteractorFactory.Value, dependencyContainer.NavigationService.Value, dependencyContainer.AnalyticsService.Value, dependencyContainer.AutocompleteProvider.Value, dependencyContainer.SchedulerProvider.Value, dependencyContainer.IntentDonationService.Value, dependencyContainer.StopwatchProvider.Value);

            if(viewModelType == typeof(SuggestionsViewModel))
                return new SuggestionsViewModel(dependencyContainer.DataSource.Value, dependencyContainer.InteractorFactory.Value, dependencyContainer.OnboardingStorage.Value, dependencyContainer.SuggestionProviderContainer.Value, dependencyContainer.SchedulerProvider.Value);

            if(viewModelType == typeof(SyncFailuresViewModel))
                return new SyncFailuresViewModel(dependencyContainer.InteractorFactory.Value);

            if(viewModelType == typeof(TermsOfServiceViewModel))
                return new TermsOfServiceViewModel(dependencyContainer.BrowserService.Value);

            if(viewModelType == typeof(TimeEntriesViewModel))
                return new TimeEntriesViewModel(dependencyContainer.DataSource.Value, dependencyContainer..Value, dependencyContainer..Value, dependencyContainer..Value);

Failed for ViewModel /Users/will/Documents/Projects/mobileapp/Toggl.Foundation.MvvmCross/ViewModels/TimeEntryViewModel.cs
            if(viewModelType == typeof(TokenResetViewModel))
                return new TokenResetViewModel(dependencyContainer.LoginManager, dependencyContainer.DataSource.Value, dependencyContainer.DialogService.Value, dependencyContainer.ForkingNavigationService.Value, dependencyContainer.UserPreferences.Value, dependencyContainer.AnalyticsService.Value, dependencyContainer.SchedulerProvider.Value);

            if(viewModelType == typeof(CalendarPermissionDeniedViewModel))
                return new CalendarPermissionDeniedViewModel(dependencyContainer.PermissionsService.Value);

            if(viewModelType == typeof(CalendarViewModel))
                return new CalendarViewModel(dependencyContainer.DataSource.Value, dependencyContainer.TimeService.Value, dependencyContainer.DialogService.Value, dependencyContainer.UserPreferences.Value, dependencyContainer.AnalyticsService.Value, dependencyContainer.BackgroundService.Value, dependencyContainer.InteractorFactory.Value, dependencyContainer.OnboardingStorage.Value, dependencyContainer.SchedulerProvider.Value, dependencyContainer.PermissionsService.Value, dependencyContainer.NavigationService.Value, dependencyContainer.StopwatchProvider.Value);

            if(viewModelType == typeof(SelectUserCalendarsViewModel))
                return new SelectUserCalendarsViewModel(dependencyContainer.UserPreferences.Value, dependencyContainer.InteractorFactory.Value, dependencyContainer.NavigationService.Value);

Failed for ViewModel /Users/will/Documents/Projects/mobileapp/Toggl.Foundation.MvvmCross/ViewModels/Calendar/SelectUserCalendarsViewModelBase.cs
Failed for ViewModel /Users/will/Documents/Projects/mobileapp/Toggl.Foundation.MvvmCross/ViewModels/Reports/BarViewModel.cs
            if(viewModelType == typeof(ReportsViewModel))
                return new ReportsViewModel(dependencyContainer.DataSource.Value, dependencyContainer.TimeService.Value, dependencyContainer.NavigationService.Value, dependencyContainer.InteractorFactory.Value, dependencyContainer.AnalyticsService.Value, dependencyContainer.DialogService.Value, dependencyContainer.IntentDonationService.Value, dependencyContainer.SchedulerProvider.Value, dependencyContainer.StopwatchProvider.Value);

Failed for ViewModel /Users/will/Documents/Projects/mobileapp/Toggl.Foundation.MvvmCross/ViewModels/ReportsCalendar/ReportsCalendarDayViewModel.cs
Failed for ViewModel /Users/will/Documents/Projects/mobileapp/Toggl.Foundation.MvvmCross/ViewModels/ReportsCalendar/ReportsCalendarPageViewModel.cs
Failed for ViewModel /Users/will/Documents/Projects/mobileapp/Toggl.Foundation.MvvmCross/ViewModels/Selectable/SelectableBeginningOfWeekViewModel.cs
Failed for ViewModel /Users/will/Documents/Projects/mobileapp/Toggl.Foundation.MvvmCross/ViewModels/Selectable/SelectableClientViewModel.cs
Failed for ViewModel /Users/will/Documents/Projects/mobileapp/Toggl.Foundation.MvvmCross/ViewModels/Selectable/SelectableColorViewModel.cs
Failed for ViewModel /Users/will/Documents/Projects/mobileapp/Toggl.Foundation.MvvmCross/ViewModels/Selectable/SelectableCountryViewModel.cs
Failed for ViewModel /Users/will/Documents/Projects/mobileapp/Toggl.Foundation.MvvmCross/ViewModels/Selectable/SelectableDateFormatViewModel.cs
Failed for ViewModel /Users/will/Documents/Projects/mobileapp/Toggl.Foundation.MvvmCross/ViewModels/Selectable/SelectableDurationFormatViewModel.cs
Failed for ViewModel /Users/will/Documents/Projects/mobileapp/Toggl.Foundation.MvvmCross/ViewModels/Selectable/SelectableTagViewModel.cs
Failed for ViewModel /Users/will/Documents/Projects/mobileapp/Toggl.Foundation.MvvmCross/ViewModels/Selectable/SelectableUserCalendarViewModel.cs
Failed for ViewModel /Users/will/Documents/Projects/mobileapp/Toggl.Foundation.MvvmCross/ViewModels/Selectable/SelectableWorkspaceViewModel.cs
            if(viewModelType == typeof(AboutViewModel))
                return new AboutViewModel(dependencyContainer.NavigationService.Value);

            if(viewModelType == typeof(CalendarSettingsViewModel))
                return new CalendarSettingsViewModel(dependencyContainer.UserPreferences.Value, dependencyContainer.InteractorFactory.Value, dependencyContainer.PermissionsService.Value);

            if(viewModelType == typeof(LicensesViewModel))
                return new LicensesViewModel(dependencyContainer.LicenseProvider.Value);

            if(viewModelType == typeof(NotificationSettingsViewModel))
                return new NotificationSettingsViewModel(dependencyContainer.NavigationService.Value, dependencyContainer.BackgroundService.Value, dependencyContainer.PermissionsService.Value, dependencyContainer.UserPreferences.Value);

            if(viewModelType == typeof(SendFeedbackViewModel))
                return new SendFeedbackViewModel(dependencyContainer.NavigationService.Value, dependencyContainer.InteractorFactory.Value, dependencyContainer.DialogService.Value, dependencyContainer.SchedulerProvider.Value);

            if(viewModelType == typeof(SettingsViewModel))
                return new SettingsViewModel(dependencyContainer.DataSource.Value, dependencyContainer.PlatformInfo, dependencyContainer.LoginManager, dependencyContainer.DialogService.Value, dependencyContainer.UserPreferences.Value, dependencyContainer.FeedbackService.Value, dependencyContainer.AnalyticsService.Value, dependencyContainer.InteractorFactory.Value, dependencyContainer.OnboardingStorage.Value, dependencyContainer.NavigationService.Value, dependencyContainer.PrivateSharedStorageService.Value, dependencyContainer.IntentDonationService.Value, dependencyContainer.StopwatchProvider.Value);

            if(viewModelType == typeof(UpcomingEventsNotificationSettingsViewModel))
                return new UpcomingEventsNotificationSettingsViewModel(dependencyContainer.NavigationService.Value, dependencyContainer.UserPreferences.Value);


            return default(MvxViewModel);
        }
    }
}