using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.Experiments;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.UI.Collections;
using Toggl.Foundation.UI.Extensions;
using Toggl.Foundation.UI.Parameters;
using Toggl.Foundation.UI.ViewModels;
using Toggl.Foundation.UI.ViewModels.Reports;
using Toggl.Foundation.UI.ViewModels.TimeEntriesLog;
using Toggl.Foundation.UI.ViewModels.TimeEntriesLog.Identity;
using Toggl.Foundation.Services;
using Toggl.Foundation.Suggestions;
using Toggl.Foundation.Sync;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Settings;

[assembly: MvxNavigation(typeof(MainViewModel), ApplicationUrls.Main.Regex)]
namespace Toggl.Foundation.UI.ViewModels
{
    using MainLogSection = AnimatableSectionModel<DaySummaryViewModel, LogItemViewModel, IMainLogKey>;

    [Preserve(AllMembers = true)]
    public sealed class MainViewModel : MvxViewModel
    {
        private const int ratingViewTimeout = 5;
        private const double throttlePeriodInSeconds = 0.1;

        private bool isEditViewOpen;
        private string urlNavigationAction;
        private bool noWorkspaceViewPresented;
        private bool hasStopButtonEverBeenUsed;
        private bool noDefaultWorkspaceViewPresented;
        private bool shouldHideRatingViewIfStillVisible = false;
        private object isEditViewOpenLock = new object();

        private readonly ITogglDataSource dataSource;
        private readonly ISyncManager syncManager;
        private readonly IUserPreferences userPreferences;
        private readonly IAnalyticsService analyticsService;
        private readonly IOnboardingStorage onboardingStorage;
        private readonly IInteractorFactory interactorFactory;
        private readonly IStopwatchProvider stopwatchProvider;
        private readonly IMvxNavigationService navigationService;
        private readonly IIntentDonationService intentDonationService;
        private readonly IAccessRestrictionStorage accessRestrictionStorage;
        private readonly IRxActionFactory rxActionFactory;
        private readonly ISchedulerProvider schedulerProvider;

        private readonly RatingViewExperiment ratingViewExperiment;
        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        private readonly ISubject<Unit> hideRatingView = new Subject<Unit>();

        public IObservable<bool> LogEmpty { get; }
        public IObservable<int> TimeEntriesCount { get; }
        public IObservable<bool> IsInManualMode { get; private set; }
        public IObservable<string> ElapsedTime { get; private set; }
        public IObservable<bool> IsTimeEntryRunning { get; private set; }
        public IObservable<int> NumberOfSyncFailures { get; private set; }
        public IObservable<bool> ShouldShowEmptyState { get; private set; }
        public IObservable<bool> ShouldShowWelcomeBack { get; private set; }
        public IObservable<Unit> ShouldReloadTimeEntryLog { get; private set; }
        public IObservable<SyncProgress> SyncProgressState { get; private set; }
        public IObservable<bool> ShouldShowRunningTimeEntryNotification { get; private set; }
        public IObservable<bool> ShouldShowStoppedTimeEntryNotification { get; private set; }
        public IObservable<IThreadSafeTimeEntry> CurrentRunningTimeEntry { get; private set; }
        public IObservable<bool> ShouldShowRatingView { get; private set; }

        public IObservable<IEnumerable<MainLogSection>> TimeEntries => TimeEntriesViewModel.TimeEntries
            .Throttle(TimeSpan.FromSeconds(throttlePeriodInSeconds))
            .AsDriver(Enumerable.Empty<MainLogSection>(), schedulerProvider);

        public RatingViewModel RatingViewModel { get; }
        public SuggestionsViewModel SuggestionsViewModel { get; }
        public IOnboardingStorage OnboardingStorage => onboardingStorage;

        public new IMvxNavigationService NavigationService => navigationService;

        public UIAction Refresh { get; private set; }
        public UIAction OpenReports { get; private set; }
        public UIAction OpenSettings { get; private set; }
        public UIAction OpenSyncFailures { get; private set; }
        public InputAction<bool> StartTimeEntry { get; private set; }
        public InputAction<(long[], EditTimeEntryOrigin)> SelectTimeEntry { get; private set; }
        public InputAction<TimeEntryStopOrigin> StopTimeEntry { get; private set; }
        public InputAction<(long, ContinueTimeEntryMode)> ContinueTimeEntry { get; private set; }

        public ITimeService TimeService { get; }

        public TimeEntriesViewModel TimeEntriesViewModel { get; }

        public MainViewModel(
            ITogglDataSource dataSource,
            ISyncManager syncManager,
            ITimeService timeService,
            IRatingService ratingService,
            IUserPreferences userPreferences,
            IAnalyticsService analyticsService,
            IOnboardingStorage onboardingStorage,
            IInteractorFactory interactorFactory,
            IMvxNavigationService navigationService,
            IRemoteConfigService remoteConfigService,
            ISuggestionProviderContainer suggestionProviders,
            IIntentDonationService intentDonationService,
            IAccessRestrictionStorage accessRestrictionStorage,
            ISchedulerProvider schedulerProvider,
            IStopwatchProvider stopwatchProvider,
            IRxActionFactory rxActionFactory)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(syncManager, nameof(syncManager));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(ratingService, nameof(ratingService));
            Ensure.Argument.IsNotNull(userPreferences, nameof(userPreferences));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(stopwatchProvider, nameof(stopwatchProvider));
            Ensure.Argument.IsNotNull(remoteConfigService, nameof(remoteConfigService));
            Ensure.Argument.IsNotNull(suggestionProviders, nameof(suggestionProviders));
            Ensure.Argument.IsNotNull(intentDonationService, nameof(intentDonationService));
            Ensure.Argument.IsNotNull(accessRestrictionStorage, nameof(accessRestrictionStorage));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.dataSource = dataSource;
            this.syncManager = syncManager;
            this.userPreferences = userPreferences;
            this.analyticsService = analyticsService;
            this.interactorFactory = interactorFactory;
            this.navigationService = navigationService;
            this.onboardingStorage = onboardingStorage;
            this.schedulerProvider = schedulerProvider;
            this.intentDonationService = intentDonationService;
            this.accessRestrictionStorage = accessRestrictionStorage;
            this.stopwatchProvider = stopwatchProvider;
            this.rxActionFactory = rxActionFactory;

            TimeService = timeService;

            SuggestionsViewModel = new SuggestionsViewModel(dataSource, interactorFactory, onboardingStorage, suggestionProviders, schedulerProvider, rxActionFactory);
            RatingViewModel = new RatingViewModel(timeService, dataSource, ratingService, analyticsService, onboardingStorage, navigationService, schedulerProvider, rxActionFactory);
            TimeEntriesViewModel = new TimeEntriesViewModel(dataSource, syncManager, interactorFactory, analyticsService, schedulerProvider, rxActionFactory, timeService);

            LogEmpty = TimeEntriesViewModel.Empty.AsDriver(schedulerProvider);
            TimeEntriesCount = TimeEntriesViewModel.Count.AsDriver(schedulerProvider);

            ratingViewExperiment = new RatingViewExperiment(timeService, dataSource, onboardingStorage, remoteConfigService);
        }

        public void Init(string action, string description)
        {
            urlNavigationAction = action;

            if (!string.IsNullOrEmpty(description))
            {
                interactorFactory.GetDefaultWorkspace()
                    .TrackException<InvalidOperationException, IThreadSafeWorkspace>("MainViewModel.Init")
                    .Execute()
                    .SelectMany(workspace => interactorFactory
                        .CreateTimeEntry(description.AsTimeEntryPrototype(TimeService.CurrentDateTime, workspace.Id), TimeEntryStartOrigin.Timer)
                        .Execute())
                    .Subscribe()
                    .DisposedBy(disposeBag);
            }
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            await SuggestionsViewModel.Initialize();
            await RatingViewModel.Initialize();

            SyncProgressState = syncManager.ProgressObservable
                .AsDriver(schedulerProvider);

            var isWelcome = onboardingStorage.IsNewUser;

            var noTimeEntries = Observable
                .CombineLatest(TimeEntriesViewModel.Empty, SuggestionsViewModel.IsEmpty,
                    (isTimeEntryEmpty, isSuggestionEmpty) => isTimeEntryEmpty && isSuggestionEmpty)
                .DistinctUntilChanged();

            ShouldShowEmptyState = ObservableAddons.CombineLatestAll(
                    isWelcome,
                    noTimeEntries
                )
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            ShouldShowWelcomeBack = ObservableAddons.CombineLatestAll(
                    isWelcome.Select(b => !b),
                    noTimeEntries
                )
                .StartWith(false)
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            IsInManualMode = userPreferences
                .IsManualModeEnabledObservable
                .AsDriver(schedulerProvider);

            ShouldShowRunningTimeEntryNotification = userPreferences.AreRunningTimerNotificationsEnabledObservable;
            ShouldShowStoppedTimeEntryNotification = userPreferences.AreStoppedTimerNotificationsEnabledObservable;

            CurrentRunningTimeEntry = dataSource
                .TimeEntries
                .CurrentlyRunningTimeEntry
                .AsDriver(schedulerProvider);

            IsTimeEntryRunning = CurrentRunningTimeEntry
                .Select(te => te != null)
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);

            var durationObservable = dataSource
                .Preferences
                .Current
                .Select(preferences => preferences.DurationFormat);

            ElapsedTime = TimeService
                .CurrentDateTimeObservable
                .CombineLatest(CurrentRunningTimeEntry, (now, te) => (now - te?.Start) ?? TimeSpan.Zero)
                .CombineLatest(durationObservable, (duration, format) => duration.ToFormattedString(format))
                .AsDriver(schedulerProvider);

            NumberOfSyncFailures = interactorFactory
                .GetItemsThatFailedToSync()
                .Execute()
                .Select(i => i.Count())
                .AsDriver(schedulerProvider);

            ShouldReloadTimeEntryLog = Observable.Merge(
                TimeService.MidnightObservable.SelectUnit(),
                TimeService.SignificantTimeChangeObservable.SelectUnit())
                .AsDriver(schedulerProvider);

            Refresh = rxActionFactory.FromAsync(refresh);
            OpenReports = rxActionFactory.FromAsync(openReports);
            OpenSettings = rxActionFactory.FromAsync(openSettings);
            OpenSyncFailures = rxActionFactory.FromAsync(openSyncFailures);
            SelectTimeEntry = rxActionFactory.FromAsync<(long[], EditTimeEntryOrigin)>(timeEntrySelected);
            ContinueTimeEntry = rxActionFactory.FromObservable<(long, ContinueTimeEntryMode)>(continueTimeEntry);
            StartTimeEntry = rxActionFactory.FromAsync<bool>(startTimeEntry, IsTimeEntryRunning.Invert());
            StopTimeEntry = rxActionFactory.FromAsync<TimeEntryStopOrigin>(stopTimeEntry, IsTimeEntryRunning);

            switch (urlNavigationAction)
            {
                case ApplicationUrls.Main.Action.Continue:
                    await continueMostRecentEntry();
                    break;

                case ApplicationUrls.Main.Action.Stop:
                    await stopTimeEntry(TimeEntryStopOrigin.Deeplink);
                    break;

                case ApplicationUrls.Main.Action.StopFromSiri:
                    await stopTimeEntry(TimeEntryStopOrigin.Siri);
                    break;
            }

            ShouldShowRatingView = Observable.Merge(
                    ratingViewExperiment.RatingViewShouldBeVisible,
                    RatingViewModel.HideRatingView.SelectValue(false),
                    hideRatingView.AsObservable().SelectValue(false)
                )
                .Select(canPresentRating)
                .DistinctUntilChanged()
                .Do(trackRatingViewPresentation)
                .AsDriver(schedulerProvider);

            onboardingStorage.StopButtonWasTappedBefore
                             .Subscribe(hasBeen => hasStopButtonEverBeenUsed = hasBeen)
                             .DisposedBy(disposeBag);

            interactorFactory.GetDefaultWorkspace()
                .TrackException<InvalidOperationException, IThreadSafeWorkspace>("MainViewModel.Initialize")
                .Execute()
                .Subscribe(intentDonationService.SetDefaultShortcutSuggestions)
                .DisposedBy(disposeBag);
        }

        public void Track(ITrackableEvent e)
        {
            analyticsService.Track(e);
        }

        private bool canPresentRating(bool shouldBeVisible)
        {
            if (!shouldBeVisible) return false;

            var wasShownMoreThanOnce = onboardingStorage.NumberOfTimesRatingViewWasShown() > 1;
            if (wasShownMoreThanOnce) return false;

            var lastOutcome = onboardingStorage.RatingViewOutcome();
            if (lastOutcome != null)
            {
                var thereIsInteractionFormLastTime = lastOutcome != RatingViewOutcome.NoInteraction;
                if (thereIsInteractionFormLastTime) return false;
            }

            var lastOutcomeTime = onboardingStorage.RatingViewOutcomeTime();
            if (lastOutcomeTime != null)
            {
                var oneDayHasNotPassedSinceLastTime = lastOutcomeTime + TimeSpan.FromHours(24) > TimeService.CurrentDateTime;
                if (oneDayHasNotPassedSinceLastTime && !wasShownMoreThanOnce) return false;
            }

            return true;
        }

        private void trackRatingViewPresentation(bool shouldBeVisible)
        {
            if (!shouldBeVisible)
                return;

            analyticsService.RatingViewWasShown.Track();
            onboardingStorage.SetDidShowRatingView();
            onboardingStorage.SetRatingViewOutcome(RatingViewOutcome.NoInteraction, TimeService.CurrentDateTime);

            TimeService.RunAfterDelay(TimeSpan.FromMinutes(ratingViewTimeout), () =>
            {
                shouldHideRatingViewIfStillVisible = true;
                hideRatingView.OnNext(Unit.Default);
            });
        }

        private async Task continueMostRecentEntry()
        {
            await interactorFactory.ContinueMostRecentTimeEntry().Execute();
        }

        public override void ViewDisappeared()
        {
            base.ViewDisappeared();
            viewDisappearedAsync();
        }

        private async Task viewDisappearedAsync()
        {
            await TimeEntriesViewModel.FinalizeDelayDeleteTimeEntryIfNeeded();
        }

        public override void ViewAppearing()
        {
            base.ViewAppearing();
            ViewAppearingAsync();
        }

        internal async Task ViewAppearingAsync()
        {
            hideRatingViewIfStillVisibleAfterDelay();
            await handleNoWorkspaceState();
            handleNoDefaultWorkspaceState();
        }

        private void hideRatingViewIfStillVisibleAfterDelay()
        {
            if (shouldHideRatingViewIfStillVisible)
            {
                shouldHideRatingViewIfStillVisible = false;
                hideRatingView.OnNext(Unit.Default);
            }
        }

        private async Task handleNoWorkspaceState()
        {
            if (accessRestrictionStorage.HasNoWorkspace() && !noWorkspaceViewPresented)
            {
                noWorkspaceViewPresented = true;
                await navigationService.Navigate<NoWorkspaceViewModel, Unit>();
                noWorkspaceViewPresented = false;
            }
        }

        private async Task handleNoDefaultWorkspaceState()
        {
            if (accessRestrictionStorage.HasNoDefaultWorkspace() && !noDefaultWorkspaceViewPresented)
            {
                noDefaultWorkspaceViewPresented = true;
                await navigationService.Navigate<SelectDefaultWorkspaceViewModel, Unit>();
                noDefaultWorkspaceViewPresented = false;
            }
        }

        private Task openSettings()
        {
            var settingsStopwatch = stopwatchProvider.CreateAndStore(MeasuredOperation.OpenSettingsView);
            settingsStopwatch.Start();
            return navigate<SettingsViewModel>();
        }

        private Task openReports()
        {
            var openReportsStopwatch = stopwatchProvider.CreateAndStore(MeasuredOperation.OpenReportsFromGiskard);
            openReportsStopwatch.Start();
            return navigate<ReportsViewModel>();
        }

        private Task openSyncFailures()
            => navigate<SyncFailuresViewModel>();

        private Task startTimeEntry(bool useDefaultMode)
        {
            var initializeInManualMode = useDefaultMode == userPreferences.IsManualModeEnabled;

            OnboardingStorage.StartButtonWasTapped();
            var startTimeEntryStopwatch = stopwatchProvider.CreateAndStore(MeasuredOperation.OpenStartView);
            startTimeEntryStopwatch.Start();

            if (hasStopButtonEverBeenUsed)
                onboardingStorage.SetNavigatedAwayFromMainViewAfterStopButton();

            var parameter = initializeInManualMode
                ? StartTimeEntryParameters.ForManualMode(TimeService.CurrentDateTime)
                : StartTimeEntryParameters.ForTimerMode(TimeService.CurrentDateTime);

            return navigate<StartTimeEntryViewModel, StartTimeEntryParameters>(parameter);
        }

        private IObservable<Unit> continueTimeEntry((long, ContinueTimeEntryMode) continueInfo)
        {
            var (timeEntryId, continueMode) = continueInfo;
            return interactorFactory.GetTimeEntryById(timeEntryId).Execute()
                .Select(timeEntry => timeEntry.AsTimeEntryPrototype())
                .SelectMany(prototype =>
                    interactorFactory.ContinueTimeEntry(prototype, continueMode).Execute())
                .Do(_ => onboardingStorage.SetTimeEntryContinued())
                .SelectUnit();
        }

        private async Task timeEntrySelected((long[], EditTimeEntryOrigin) timeEntrySelection)
        {
            if (isEditViewOpen)
                return;

            var (timeEntryIds, origin) = timeEntrySelection;

            onboardingStorage.TimeEntryWasTapped();

            lock (isEditViewOpenLock)
            {
                isEditViewOpen = true;
            }

            var editTimeEntryStopwatch = stopwatchProvider.CreateAndStore(MeasuredOperation.EditTimeEntryFromMainLog);
            editTimeEntryStopwatch.Start();

            analyticsService.EditViewOpened.Track(origin);
            await navigate<EditTimeEntryViewModel, long[]>(timeEntryIds);

            lock (isEditViewOpenLock)
            {
                isEditViewOpen = false;
            }
        }

        private async Task refresh()
        {
            await syncManager.ForceFullSync();
        }

        private async Task stopTimeEntry(TimeEntryStopOrigin origin)
        {
            OnboardingStorage.StopButtonWasTapped();

            await interactorFactory
                .StopTimeEntry(TimeService.CurrentDateTime, origin)
                .Execute()
                .Do(_ => intentDonationService.DonateStopCurrentTimeEntry())
                .Do(syncManager.InitiatePushSync);
        }

        private Task navigate<TModel, TParameters>(TParameters value)
            where TModel : IMvxViewModel<TParameters>
        {
            if (hasStopButtonEverBeenUsed)
                onboardingStorage.SetNavigatedAwayFromMainViewAfterStopButton();

            return navigationService.Navigate<TModel, TParameters>(value);
        }

        private Task navigate<TModel>()
            where TModel : IMvxViewModel
        {
            if (hasStopButtonEverBeenUsed)
                onboardingStorage.SetNavigatedAwayFromMainViewAfterStopButton();

            return navigationService.Navigate<TModel>();
        }
    }
}
