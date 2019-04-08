﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Login;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.UI.Extensions;
using Toggl.Foundation.UI.Parameters;
using Toggl.Foundation.UI.Services;
using Toggl.Foundation.UI.Transformations;
using Toggl.Foundation.UI.ViewModels.Settings;
using Toggl.Foundation.Services;
using Toggl.Foundation.Sync;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave.Network;
using static Toggl.Multivac.Extensions.CommonFunctions;

namespace Toggl.Foundation.UI.ViewModels
{
    using WorkspaceToSelectableWorkspaceLambda = Func<IEnumerable<IThreadSafeWorkspace>, IList<SelectableWorkspaceViewModel>>;

    [Preserve(AllMembers = true)]
    public sealed class SettingsViewModel : MvxViewModel
    {
        private readonly ISubject<Unit> loggingOutSubject = new Subject<Unit>();
        private readonly ISubject<bool> isFeedbackSuccessViewShowing = new Subject<bool>();
        private readonly ISubject<bool> calendarPermissionGranted = new BehaviorSubject<bool>(false);
        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        private readonly ITogglDataSource dataSource;
        private readonly ISyncManager syncManager;
        private readonly IUserAccessManager userAccessManager;
        private readonly IDialogService dialogService;
        private readonly IUserPreferences userPreferences;
        private readonly IAnalyticsService analyticsService;
        private readonly IPlatformInfo platformInfo;
        private readonly IOnboardingStorage onboardingStorage;
        private readonly IInteractorFactory interactorFactory;
        private readonly IMvxNavigationService navigationService;
        private readonly IPrivateSharedStorageService privateSharedStorageService;
        private readonly IIntentDonationService intentDonationService;
        private readonly IStopwatchProvider stopwatchProvider;
        private readonly IRxActionFactory rxActionFactory;
        private readonly ISchedulerProvider schedulerProvider;
        private readonly IPermissionsService permissionsService;

        private bool isSyncing;
        private bool isLoggingOut;
        private IThreadSafeUser currentUser;
        private IThreadSafePreferences currentPreferences;
        private IStopwatch navigationFromMainViewModelStopwatch;

        public string Title { get; private set; } = Resources.Settings;
        public bool CalendarSettingsEnabled => onboardingStorage.CompletedCalendarOnboarding();
        public string Version => $"{platformInfo.Version} ({platformInfo.BuildNumber})";

        public IObservable<string> Name { get; }
        public IObservable<string> Email { get; }
        public IObservable<bool> IsSynced { get; }
        public IObservable<Unit> LoggingOut { get; }
        public IObservable<byte[]> UserAvatar { get; }
        public IObservable<string> DateFormat { get; }
        public IObservable<bool> IsRunningSync { get; }
        public IObservable<string> WorkspaceName { get; }
        public IObservable<string> DurationFormat { get; }
        public IObservable<string> BeginningOfWeek { get; }
        public IObservable<bool> IsManualModeEnabled { get; }
        public IObservable<bool> IsGroupingTimeEntries { get; }
        public IObservable<bool> AreRunningTimerNotificationsEnabled { get; }
        public IObservable<bool> AreStoppedTimerNotificationsEnabled { get; }
        public IObservable<bool> UseTwentyFourHourFormat { get; }
        public IObservable<IList<SelectableWorkspaceViewModel>> Workspaces { get; }
        public IObservable<bool> IsFeedbackSuccessViewShowing { get; }
        public IObservable<bool> IsCalendarSmartRemindersVisible { get; }
        public IObservable<string> CalendarSmartReminders { get; }

        public UIAction OpenCalendarSettings { get; }
        public UIAction OpenCalendarSmartReminders { get; }
        public UIAction OpenNotificationSettings { get; }
        public UIAction ToggleTwentyFourHourSettings { get; }
        public UIAction OpenHelpView { get; }
        public UIAction TryLogout { get; }
        public UIAction OpenAboutView { get; }
        public UIAction SubmitFeedback { get; }
        public UIAction SelectDateFormat { get; }
        public UIAction PickDefaultWorkspace { get; }
        public UIAction SelectDurationFormat { get; }
        public UIAction ToggleTimeEntriesGrouping { get; }
        public UIAction SelectBeginningOfWeek { get; }
        public UIAction Close { get; }

        public InputAction<SelectableWorkspaceViewModel> SelectDefaultWorkspace { get; }

        public SettingsViewModel(
            ITogglDataSource dataSource,
            ISyncManager syncManager,
            IPlatformInfo platformInfo,
            IDialogService dialogService,
            IUserPreferences userPreferences,
            IAnalyticsService analyticsService,
            IUserAccessManager userAccessManager,
            IInteractorFactory interactorFactory,
            IOnboardingStorage onboardingStorage,
            IMvxNavigationService navigationService,
            IPrivateSharedStorageService privateSharedStorageService,
            IIntentDonationService intentDonationService,
            IStopwatchProvider stopwatchProvider,
            IRxActionFactory rxActionFactory,
            IPermissionsService permissionsService,
            ISchedulerProvider schedulerProvider)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(syncManager, nameof(syncManager));
            Ensure.Argument.IsNotNull(platformInfo, nameof(platformInfo));
            Ensure.Argument.IsNotNull(dialogService, nameof(dialogService));
            Ensure.Argument.IsNotNull(userPreferences, nameof(userPreferences));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(userAccessManager, nameof(userAccessManager));
            Ensure.Argument.IsNotNull(privateSharedStorageService, nameof(privateSharedStorageService));
            Ensure.Argument.IsNotNull(intentDonationService, nameof(intentDonationService));
            Ensure.Argument.IsNotNull(stopwatchProvider, nameof(stopwatchProvider));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));
            Ensure.Argument.IsNotNull(permissionsService, nameof(permissionsService));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));

            this.dataSource = dataSource;
            this.syncManager = syncManager;
            this.platformInfo = platformInfo;
            this.dialogService = dialogService;
            this.userPreferences = userPreferences;
            this.rxActionFactory = rxActionFactory;
            this.analyticsService = analyticsService;
            this.interactorFactory = interactorFactory;
            this.navigationService = navigationService;
            this.userAccessManager = userAccessManager;
            this.onboardingStorage = onboardingStorage;
            this.stopwatchProvider = stopwatchProvider;
            this.intentDonationService = intentDonationService;
            this.privateSharedStorageService = privateSharedStorageService;
            this.rxActionFactory = rxActionFactory;
            this.schedulerProvider = schedulerProvider;
            this.permissionsService = permissionsService;

            IsSynced =
                syncManager.ProgressObservable
                    .SelectMany(checkSynced)
                    .AsDriver(schedulerProvider);

            IsRunningSync =
                syncManager.ProgressObservable
                    .Select(isRunningSync)
                    .AsDriver(schedulerProvider);

            Name =
                dataSource.User.Current
                    .Select(user => user.Fullname)
                    .DistinctUntilChanged()
                    .AsDriver(schedulerProvider);

            Email =
                dataSource.User.Current
                    .Select(user => user.Email.ToString())
                    .DistinctUntilChanged()
                    .AsDriver(schedulerProvider);

            IsManualModeEnabled = userPreferences.IsManualModeEnabledObservable
                .AsDriver(schedulerProvider);

            AreRunningTimerNotificationsEnabled = userPreferences.AreRunningTimerNotificationsEnabledObservable
                .AsDriver(schedulerProvider);

            AreStoppedTimerNotificationsEnabled = userPreferences.AreStoppedTimerNotificationsEnabledObservable
                .AsDriver(schedulerProvider);

            WorkspaceName =
                dataSource.User.Current
                    .DistinctUntilChanged(user => user.DefaultWorkspaceId)
                    .SelectMany(_ => interactorFactory.GetDefaultWorkspace()
                        .TrackException<InvalidOperationException, IThreadSafeWorkspace>("SettingsViewModel.constructor")
                        .Execute()
                    )
                    .Select(workspace => workspace.Name)
                    .AsDriver(schedulerProvider);

            BeginningOfWeek =
                dataSource.User.Current
                    .Select(user => user.BeginningOfWeek)
                    .DistinctUntilChanged()
                    .Select(beginningOfWeek => beginningOfWeek.ToString())
                    .AsDriver(schedulerProvider);

            DateFormat =
                dataSource.Preferences.Current
                    .Select(preferences => preferences.DateFormat.Localized)
                    .DistinctUntilChanged()
                    .AsDriver(schedulerProvider);

            DurationFormat =
                dataSource.Preferences.Current
                    .Select(preferences => preferences.DurationFormat)
                    .Select(DurationFormatToString.Convert)
                    .DistinctUntilChanged()
                    .AsDriver(schedulerProvider);

            UseTwentyFourHourFormat =
                dataSource.Preferences.Current
                    .Select(preferences => preferences.TimeOfDayFormat.IsTwentyFourHoursFormat)
                    .DistinctUntilChanged()
                    .AsDriver(schedulerProvider);

            IsGroupingTimeEntries =
                dataSource.Preferences.Current
                    .Select(preferences => preferences.CollapseTimeEntries)
                    .DistinctUntilChanged()
                    .AsDriver(false, schedulerProvider);
                    
            IsCalendarSmartRemindersVisible = calendarPermissionGranted.AsObservable()
                .CombineLatest(userPreferences.EnabledCalendars.Select(ids => ids.Any()), CommonFunctions.And);

            CalendarSmartReminders = userPreferences.CalendarNotificationsSettings()
                .Select(s => s.Title())
                .DistinctUntilChanged();

            UserAvatar =
                dataSource.User.Current
                    .Select(user => user.ImageUrl)
                    .DistinctUntilChanged()
                    .SelectMany(url => interactorFactory.GetUserAvatar(url).Execute())
                    .AsDriver(schedulerProvider)
                    .Where(avatar => avatar != null);

            Workspaces =
                dataSource.User.Current
                    .DistinctUntilChanged(user => user.DefaultWorkspaceId)
                    .SelectMany(user => interactorFactory
                        .GetAllWorkspaces()
                        .Execute()
                        .Select(selectableWorkspacesFromWorkspaces(user))
                    )
                    .AsDriver(schedulerProvider);

            LoggingOut = loggingOutSubject.AsObservable()
                .AsDriver(schedulerProvider);

            dataSource.User.Current
                .Subscribe(user => currentUser = user)
                .DisposedBy(disposeBag);

            dataSource.Preferences.Current
                .Subscribe(preferences => currentPreferences = preferences)
                .DisposedBy(disposeBag);

            IsRunningSync
                .Subscribe(isSyncing => this.isSyncing = isSyncing)
                .DisposedBy(disposeBag);

            IsFeedbackSuccessViewShowing = isFeedbackSuccessViewShowing.AsObservable()
                .AsDriver(schedulerProvider);

            OpenCalendarSettings = rxActionFactory.FromAsync(openCalendarSettings);
            OpenCalendarSmartReminders = rxActionFactory.FromAsync(openCalendarSmartReminders);
            OpenNotificationSettings = rxActionFactory.FromAsync(openNotificationSettings);
            ToggleTwentyFourHourSettings = rxActionFactory.FromAsync(toggleUseTwentyFourHourClock);
            OpenHelpView = rxActionFactory.FromAsync(openHelpView);
            TryLogout = rxActionFactory.FromAsync(tryLogout);
            OpenAboutView = rxActionFactory.FromAsync(openAboutView);
            SubmitFeedback = rxActionFactory.FromAsync(submitFeedback);
            SelectDateFormat = rxActionFactory.FromAsync(selectDateFormat);
            PickDefaultWorkspace = rxActionFactory.FromAsync(pickDefaultWorkspace);
            SelectDurationFormat = rxActionFactory.FromAsync(selectDurationFormat);
            SelectBeginningOfWeek = rxActionFactory.FromAsync(selectBeginningOfWeek);
            ToggleTimeEntriesGrouping = rxActionFactory.FromAsync(toggleTimeEntriesGrouping);
            SelectDefaultWorkspace = rxActionFactory.FromAsync<SelectableWorkspaceViewModel>(selectDefaultWorkspace);
            Close = rxActionFactory.FromAsync(close);
        }

        public override async Task Initialize()
        {
            await base.Initialize();
            await checkCalendarPermissions();
            navigationFromMainViewModelStopwatch = stopwatchProvider.Get(MeasuredOperation.OpenSettingsView);
            stopwatchProvider.Remove(MeasuredOperation.OpenStartView);
        }

        public override void ViewAppeared()
        {
            base.ViewAppeared();
            navigationFromMainViewModelStopwatch?.Stop();
            navigationFromMainViewModelStopwatch = null;
            checkCalendarPermissions();
        }

        public void CloseFeedbackSuccessView()
        {
            isFeedbackSuccessViewShowing.OnNext(false);
        }

        private Task selectDefaultWorkspace(SelectableWorkspaceViewModel workspace)
            => changeDefaultWorkspace(workspace.WorkspaceId);

        private async Task toggleUseTwentyFourHourClock()
        {
            var timeFormat = currentPreferences.TimeOfDayFormat.IsTwentyFourHoursFormat
                ? TimeFormat.TwelveHoursFormat
                : TimeFormat.TwentyFourHoursFormat;

            await updatePreferences(timeFormat: timeFormat);
        }

        public void ToggleManualMode()
        {
            if (userPreferences.IsManualModeEnabled)
            {
                userPreferences.EnableTimerMode();
            }
            else
            {
                userPreferences.EnableManualMode();
            }
        }

        public void ToggleRunningTimerNotifications()
        {
            var newState = !userPreferences.AreRunningTimerNotificationsEnabled;
            userPreferences.SetRunningTimerNotifications(newState);
        }

        public void ToggleStoppedTimerNotifications()
        {
            var newState = !userPreferences.AreStoppedTimerNotificationsEnabled;
            userPreferences.SetStoppedTimerNotifications(newState);
        }

        private Task openCalendarSettings()
            => navigationService.Navigate<CalendarSettingsViewModel>();

        private Task openCalendarSmartReminders()
            => navigationService.Navigate<UpcomingEventsNotificationSettingsViewModel, Unit>();

        private Task openNotificationSettings()
            => navigationService.Navigate<NotificationSettingsViewModel>();

        private IObservable<Unit> logout()
        {
            isLoggingOut = true;
            loggingOutSubject.OnNext(Unit.Default);

            return interactorFactory.Logout(LogoutSource.Settings)
                .Execute()
                .Do(_ => navigationService.Navigate<LoginViewModel>());
        }

        private IObservable<bool> isSynced()
            => dataSource.HasUnsyncedData().Select(Invert);

        private IObservable<bool> checkSynced(SyncProgress progress)
        {
            if (isLoggingOut || progress != SyncProgress.Synced)
                return Observable.Return(false);

            return isSynced();
        }

        private bool isRunningSync(SyncProgress progress)
            => isLoggingOut == false && progress == SyncProgress.Syncing;

        private async Task updatePreferences(
            New<DurationFormat> durationFormat = default(New<DurationFormat>),
            New<DateFormat> dateFormat = default(New<DateFormat>),
            New<TimeFormat> timeFormat = default(New<TimeFormat>),
            New<bool> collapseTimeEntries = default(New<bool>))
        {
            var preferencesDto = new EditPreferencesDTO
            {
                DurationFormat = durationFormat,
                DateFormat = dateFormat,
                TimeOfDayFormat = timeFormat,
                CollapseTimeEntries = collapseTimeEntries
            };

            await interactorFactory.UpdatePreferences(preferencesDto).Execute();
            syncManager.InitiatePushSync();
        }

        private async Task changeDefaultWorkspace(long selectedWorkspaceId)
        {
            if (selectedWorkspaceId == currentUser.DefaultWorkspaceId) return;

            await interactorFactory.UpdateDefaultWorkspace(selectedWorkspaceId).Execute();
            syncManager.InitiatePushSync();
        }

        private WorkspaceToSelectableWorkspaceLambda selectableWorkspacesFromWorkspaces(IThreadSafeUser user)
            => workspaces
                => workspaces
                    .Select(workspace => new SelectableWorkspaceViewModel(workspace, user.DefaultWorkspaceId == workspace.Id))
                    .ToList();

        private Task openHelpView() =>
            navigationService.Navigate<BrowserViewModel, BrowserParameters>(
                BrowserParameters.WithUrlAndTitle(platformInfo.HelpUrl, Resources.Help)
            );

        private async Task tryLogout()
        {
            var synced = !isSyncing && await isSynced();
            if (synced)
            {
                await logout();
                return;
            }

            var (title, message) = isSyncing
                ? (Resources.SettingsSyncInProgressTitle, Resources.SettingsSyncInProgressMessage)
                : (Resources.SettingsUnsyncedTitle, Resources.SettingsUnsyncedMessage);

            await dialogService
                .Confirm(title, message, Resources.SettingsDialogButtonSignOut, Resources.Cancel)
                .SelectMany(shouldLogout
                    => shouldLogout ? logout() : Observable.Return(Unit.Default));
        }

        private Task openAboutView()
            => navigationService.Navigate<AboutViewModel>();

        private async Task submitFeedback()
        {
            var sendFeedbackSucceed = await navigationService.Navigate<SendFeedbackViewModel, bool>();
            isFeedbackSuccessViewShowing.OnNext(sendFeedbackSucceed);
        }

        private async Task selectDateFormat()
        {
            var newDateFormat = await navigationService
                .Navigate<SelectDateFormatViewModel, DateFormat, DateFormat>(currentPreferences.DateFormat);

            if (currentPreferences.DateFormat == newDateFormat)
                return;

            await updatePreferences(dateFormat: newDateFormat);
        }

        private async Task pickDefaultWorkspace()
        {
            var defaultWorkspace = await interactorFactory.GetDefaultWorkspace()
                .TrackException<InvalidOperationException, IThreadSafeWorkspace>("SettingsViewModel.PickDefaultWorkspace")
                .Execute();

            var selectedWorkspaceId =
                await navigationService
                    .Navigate<SelectWorkspaceViewModel, long, long>(defaultWorkspace.Id);

            await changeDefaultWorkspace(selectedWorkspaceId);
        }

        private async Task toggleTimeEntriesGrouping()
        {
            var newValue = !currentPreferences.CollapseTimeEntries;
            analyticsService.GroupTimeEntriesSettingsChanged.Track(newValue);
            await updatePreferences(collapseTimeEntries: newValue);
        }

        private async Task selectDurationFormat()
        {
            var newDurationFormat = await navigationService
                .Navigate<SelectDurationFormatViewModel, DurationFormat, DurationFormat>(currentPreferences.DurationFormat);

            if (currentPreferences.DurationFormat == newDurationFormat)
                return;

            await updatePreferences(newDurationFormat);
        }

        private async Task selectBeginningOfWeek()
        {
            var newBeginningOfWeek = await navigationService
                .Navigate<SelectBeginningOfWeekViewModel, BeginningOfWeek, BeginningOfWeek>(currentUser
                    .BeginningOfWeek);

            if (currentUser.BeginningOfWeek == newBeginningOfWeek)
                return;

            await interactorFactory.UpdateUser(new EditUserDTO { BeginningOfWeek = newBeginningOfWeek }).Execute();
            syncManager.InitiatePushSync();
        }

        private async Task checkCalendarPermissions()
        {
            var authorized = await permissionsService.CalendarPermissionGranted;
            calendarPermissionGranted.OnNext(authorized);
        }
        
        private Task close() => navigationService.Close(this);
    }
}
