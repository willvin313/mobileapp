using System;
using MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Foundation.MvvmCross.ViewModels.Reports;
using Toggl.Foundation.MvvmCross.ViewModels.Settings;

namespace Toggl.Foundation.MvvmCross
{
    public sealed class TogglViewModelLocator : MvxDefaultViewModelLocator
    {
        private readonly UiDependencyContainer dependencyContainer;

        public TogglViewModelLocator(UiDependencyContainer dependencyContainer)
            : base(dependencyContainer.NavigationService.Value)
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
            if (viewModelType == typeof(BrowserViewModel))
                return new BrowserViewModel(
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.RxActionFactory.Value);

            if (viewModelType == typeof(EditDurationViewModel))
                return new EditDurationViewModel(
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.TimeService.Value,
                    dependencyContainer.DataSource.Value,
                    dependencyContainer.AnalyticsService.Value,
                    dependencyContainer.RxActionFactory.Value,
                    dependencyContainer.SchedulerProvider.Value);

            if (viewModelType == typeof(EditProjectViewModel))
                return new EditProjectViewModel(
                    dependencyContainer.DataSource.Value,
                    dependencyContainer.DialogService.Value,
                    dependencyContainer.RxActionFactory.Value,
                    dependencyContainer.InteractorFactory.Value,
                    dependencyContainer.SchedulerProvider.Value,
                    dependencyContainer.StopwatchProvider.Value,
                    dependencyContainer.NavigationService.Value);

            if (viewModelType == typeof(ForgotPasswordViewModel))
                return new ForgotPasswordViewModel(
                    dependencyContainer.TimeService.Value,
                    dependencyContainer.UserAccessManager,
                    dependencyContainer.AnalyticsService.Value,
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.RxActionFactory.Value);

            if (viewModelType == typeof(LoginViewModel))
                return new LoginViewModel(
                    dependencyContainer.UserAccessManager,
                    dependencyContainer.AnalyticsService.Value,
                    dependencyContainer.OnboardingStorage.Value,
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.PasswordManagerService.Value,
                    dependencyContainer.ErrorHandlingService.Value,
                    dependencyContainer.LastTimeUsageStorage.Value,
                    dependencyContainer.TimeService.Value,
                    dependencyContainer.SchedulerProvider.Value,
                    dependencyContainer.RxActionFactory.Value);

            if (viewModelType == typeof(MainTabBarViewModel))
                return new MainTabBarViewModel(
                    dependencyContainer.TimeService.Value,
                    dependencyContainer.DataSource.Value,
                    dependencyContainer.SyncManager.Value,
                    dependencyContainer.DialogService.Value,
                    dependencyContainer.RatingService.Value,
                    dependencyContainer.UserPreferences.Value,
                    dependencyContainer.AnalyticsService.Value,
                    dependencyContainer.BackgroundService.Value,
                    dependencyContainer.InteractorFactory.Value,
                    dependencyContainer.OnboardingStorage.Value,
                    dependencyContainer.SchedulerProvider.Value,
                    dependencyContainer.PermissionsService.Value,
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.RemoteConfigService.Value,
                    dependencyContainer.SuggestionProviderContainer.Value,
                    dependencyContainer.IntentDonationService.Value,
                    dependencyContainer.AccessRestrictionStorage.Value,
                    dependencyContainer.StopwatchProvider.Value,
                    dependencyContainer.RxActionFactory.Value,
                    dependencyContainer.UserAccessManager,
                    dependencyContainer.PrivateSharedStorageService.Value,
                    dependencyContainer.PlatformInfo.Value);

            if (viewModelType == typeof(MainViewModel))
                return new MainViewModel(
                    dependencyContainer.DataSource.Value,
                    dependencyContainer.SyncManager.Value,
                    dependencyContainer.TimeService.Value,
                    dependencyContainer.RatingService.Value,
                    dependencyContainer.UserPreferences.Value,
                    dependencyContainer.AnalyticsService.Value,
                    dependencyContainer.OnboardingStorage.Value,
                    dependencyContainer.InteractorFactory.Value,
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.RemoteConfigService.Value,
                    dependencyContainer.SuggestionProviderContainer.Value,
                    dependencyContainer.IntentDonationService.Value,
                    dependencyContainer.AccessRestrictionStorage.Value,
                    dependencyContainer.SchedulerProvider.Value,
                    dependencyContainer.StopwatchProvider.Value,
                    dependencyContainer.RxActionFactory.Value);

            if (viewModelType == typeof(NoWorkspaceViewModel))
                return new NoWorkspaceViewModel(
                    dependencyContainer.SyncManager.Value,
                    dependencyContainer.InteractorFactory.Value,
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.AccessRestrictionStorage.Value,
                    dependencyContainer.SchedulerProvider.Value,
                    dependencyContainer.RxActionFactory.Value);

            if (viewModelType == typeof(OnboardingViewModel))
                return new OnboardingViewModel(
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.OnboardingStorage.Value,
                    dependencyContainer.AnalyticsService.Value,
                    dependencyContainer.RxActionFactory.Value,
                    dependencyContainer.SchedulerProvider.Value);

            if (viewModelType == typeof(OutdatedAppViewModel))
                return new OutdatedAppViewModel(
                    dependencyContainer.BrowserService.Value,
                    dependencyContainer.RxActionFactory.Value);

            if (viewModelType == typeof(RatingViewModel))
                return new RatingViewModel(
                    dependencyContainer.TimeService.Value,
                    dependencyContainer.DataSource.Value,
                    dependencyContainer.RatingService.Value,
                    dependencyContainer.AnalyticsService.Value,
                    dependencyContainer.OnboardingStorage.Value,
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.SchedulerProvider.Value,
                    dependencyContainer.RxActionFactory.Value);

            if (viewModelType == typeof(ReportsCalendarViewModel))
                return new ReportsCalendarViewModel(
                    dependencyContainer.TimeService.Value,
                    dependencyContainer.DialogService.Value,
                    dependencyContainer.DataSource.Value,
                    dependencyContainer.IntentDonationService.Value,
                    dependencyContainer.RxActionFactory.Value);

            if (viewModelType == typeof(SelectBeginningOfWeekViewModel))
                return new SelectBeginningOfWeekViewModel(
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.RxActionFactory.Value);

            if (viewModelType == typeof(SelectClientViewModel))
                return new SelectClientViewModel(
                    dependencyContainer.InteractorFactory.Value,
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.SchedulerProvider.Value,
                    dependencyContainer.RxActionFactory.Value);

            if (viewModelType == typeof(SelectColorViewModel))
                return new SelectColorViewModel(
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.RxActionFactory.Value);

            if (viewModelType == typeof(SelectCountryViewModel))
                return new SelectCountryViewModel(
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.RxActionFactory.Value);

            if (viewModelType == typeof(SelectDateFormatViewModel))
                return new SelectDateFormatViewModel(
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.RxActionFactory.Value);

            if (viewModelType == typeof(SelectDateTimeViewModel))
                return new SelectDateTimeViewModel(
                    dependencyContainer.NavigationService.Value);

            if (viewModelType == typeof(SelectDefaultWorkspaceViewModel))
                return new SelectDefaultWorkspaceViewModel(
                    dependencyContainer.DataSource.Value,
                    dependencyContainer.InteractorFactory.Value,
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.AccessRestrictionStorage.Value,
                    dependencyContainer.RxActionFactory.Value);

            if (viewModelType == typeof(SelectDurationFormatViewModel))
                return new SelectDurationFormatViewModel(
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.RxActionFactory.Value);

            if (viewModelType == typeof(SelectProjectViewModel))
                return new SelectProjectViewModel(
                    dependencyContainer.DataSource.Value,
                    dependencyContainer.RxActionFactory.Value,
                    dependencyContainer.InteractorFactory.Value,
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.DialogService.Value,
                    dependencyContainer.SchedulerProvider.Value,
                    dependencyContainer.StopwatchProvider.Value);

            if (viewModelType == typeof(SelectTagsViewModel))
                return new SelectTagsViewModel(
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.StopwatchProvider.Value,
                    dependencyContainer.InteractorFactory.Value,
                    dependencyContainer.SchedulerProvider.Value,
                    dependencyContainer.RxActionFactory.Value);

            if (viewModelType == typeof(SelectWorkspaceViewModel))
                return new SelectWorkspaceViewModel(
                    dependencyContainer.InteractorFactory.Value,
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.RxActionFactory.Value);

            if (viewModelType == typeof(SignupViewModel))
                return new SignupViewModel(
                    dependencyContainer.ApiFactory.Value,
                    dependencyContainer.UserAccessManager,
                    dependencyContainer.AnalyticsService.Value,
                    dependencyContainer.OnboardingStorage.Value,
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.ErrorHandlingService.Value,
                    dependencyContainer.LastTimeUsageStorage.Value,
                    dependencyContainer.TimeService.Value,
                    dependencyContainer.SchedulerProvider.Value,
                    dependencyContainer.RxActionFactory.Value,
                    dependencyContainer.PlatformInfo.Value);

            if (viewModelType == typeof(SuggestionsViewModel))
                return new SuggestionsViewModel(
                    dependencyContainer.DataSource.Value,
                    dependencyContainer.InteractorFactory.Value,
                    dependencyContainer.OnboardingStorage.Value,
                    dependencyContainer.SuggestionProviderContainer.Value,
                    dependencyContainer.SchedulerProvider.Value,
                    dependencyContainer.RxActionFactory.Value);

            if (viewModelType == typeof(SyncFailuresViewModel))
                return new SyncFailuresViewModel(
                    dependencyContainer.InteractorFactory.Value);

            if (viewModelType == typeof(TermsOfServiceViewModel))
                return new TermsOfServiceViewModel(
                    dependencyContainer.BrowserService.Value,
                    dependencyContainer.RxActionFactory.Value);

            if (viewModelType == typeof(TokenResetViewModel))
                return new TokenResetViewModel(
                    dependencyContainer.UserAccessManager,
                    dependencyContainer.DataSource.Value,
                    dependencyContainer.DialogService.Value,
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.UserPreferences.Value,
                    dependencyContainer.AnalyticsService.Value,
                    dependencyContainer.SchedulerProvider.Value,
                    dependencyContainer.RxActionFactory.Value,
                    dependencyContainer.InteractorFactory.Value);

            if (viewModelType == typeof(CalendarPermissionDeniedViewModel))
                return new CalendarPermissionDeniedViewModel(
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.PermissionsService.Value,
                    dependencyContainer.RxActionFactory.Value);

            if (viewModelType == typeof(CalendarViewModel))
                return new CalendarViewModel(
                    dependencyContainer.DataSource.Value,
                    dependencyContainer.TimeService.Value,
                    dependencyContainer.DialogService.Value,
                    dependencyContainer.UserPreferences.Value,
                    dependencyContainer.AnalyticsService.Value,
                    dependencyContainer.BackgroundService.Value,
                    dependencyContainer.InteractorFactory.Value,
                    dependencyContainer.OnboardingStorage.Value,
                    dependencyContainer.SchedulerProvider.Value,
                    dependencyContainer.PermissionsService.Value,
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.StopwatchProvider.Value,
                    dependencyContainer.RxActionFactory.Value);

            if (viewModelType == typeof(SelectUserCalendarsViewModel))
                return new SelectUserCalendarsViewModel(
                    dependencyContainer.UserPreferences.Value,
                    dependencyContainer.InteractorFactory.Value,
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.RxActionFactory.Value);

            if (viewModelType == typeof(ReportsViewModel))
                return new ReportsViewModel(
                    dependencyContainer.DataSource.Value,
                    dependencyContainer.TimeService.Value,
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.InteractorFactory.Value,
                    dependencyContainer.AnalyticsService.Value,
                    dependencyContainer.DialogService.Value,
                    dependencyContainer.IntentDonationService.Value,
                    dependencyContainer.SchedulerProvider.Value,
                    dependencyContainer.StopwatchProvider.Value,
                    dependencyContainer.RxActionFactory.Value);

            if (viewModelType == typeof(AboutViewModel))
                return new AboutViewModel(
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.RxActionFactory.Value);

            if (viewModelType == typeof(CalendarSettingsViewModel))
                return new CalendarSettingsViewModel(
                    dependencyContainer.UserPreferences.Value,
                    dependencyContainer.InteractorFactory.Value,
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.RxActionFactory.Value,
                    dependencyContainer.PermissionsService.Value);

            if (viewModelType == typeof(LicensesViewModel))
                return new LicensesViewModel(
                    dependencyContainer.LicenseProvider.Value);

            if (viewModelType == typeof(NotificationSettingsViewModel))
                return new NotificationSettingsViewModel(
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.BackgroundService.Value,
                    dependencyContainer.PermissionsService.Value,
                    dependencyContainer.UserPreferences.Value,
                    dependencyContainer.SchedulerProvider.Value,
                    dependencyContainer.RxActionFactory.Value);

            if (viewModelType == typeof(SendFeedbackViewModel))
                return new SendFeedbackViewModel(
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.InteractorFactory.Value,
                    dependencyContainer.DialogService.Value,
                    dependencyContainer.SchedulerProvider.Value,
                    dependencyContainer.RxActionFactory.Value);

            if (viewModelType == typeof(SettingsViewModel))
                return new SettingsViewModel(
                    dependencyContainer.DataSource.Value,
                    dependencyContainer.SyncManager.Value,
                    dependencyContainer.PlatformInfo.Value,
                    dependencyContainer.DialogService.Value,
                    dependencyContainer.UserPreferences.Value,
                    dependencyContainer.AnalyticsService.Value,
                    dependencyContainer.UserAccessManager,
                    dependencyContainer.InteractorFactory.Value,
                    dependencyContainer.OnboardingStorage.Value,
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.PrivateSharedStorageService.Value,
                    dependencyContainer.IntentDonationService.Value,
                    dependencyContainer.StopwatchProvider.Value,
                    dependencyContainer.RxActionFactory.Value,
                    dependencyContainer.PermissionsService.Value,
                    dependencyContainer.SchedulerProvider.Value);

            if (viewModelType == typeof(UpcomingEventsNotificationSettingsViewModel))
                return new UpcomingEventsNotificationSettingsViewModel(
                    dependencyContainer.NavigationService.Value,
                    dependencyContainer.UserPreferences.Value,
                    dependencyContainer.RxActionFactory.Value);


            throw new InvalidOperationException($"Trying to locate ViewModel {viewModelType.Name} failed.");
        }
    }
}
