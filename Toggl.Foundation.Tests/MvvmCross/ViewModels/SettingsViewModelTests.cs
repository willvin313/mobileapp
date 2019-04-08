﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.UI.Parameters;
using Toggl.Foundation.UI.ViewModels;
using Toggl.Foundation.UI.ViewModels.Settings;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Foundation.Tests.TestExtensions;
using Toggl.Multivac;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SettingsViewModelTests
    {
        public abstract class SettingsViewModelTest : BaseViewModelTests<SettingsViewModel>
        {
            protected ISubject<IThreadSafeUser> UserSubject;
            protected ISubject<SyncProgress> ProgressSubject;
            protected ISubject<IThreadSafePreferences> PreferencesSubject;

            protected override SettingsViewModel CreateViewModel()
            {
                UserSubject = new Subject<IThreadSafeUser>();
                ProgressSubject = new Subject<SyncProgress>();
                PreferencesSubject = new Subject<IThreadSafePreferences>();

                DataSource.User.Current.Returns(UserSubject.AsObservable());
                DataSource.Preferences.Current.Returns(PreferencesSubject.AsObservable());
                SyncManager.ProgressObservable.Returns(ProgressSubject.AsObservable());

                UserSubject.OnNext(new MockUser());
                PreferencesSubject.OnNext(new MockPreferences());

                SetupObservables();

                return new SettingsViewModel(
                    DataSource,
                    SyncManager,
                    PlatformInfo,
                    DialogService,
                    UserPreferences,
                    AnalyticsService,
                    UserAccessManager,
                    InteractorFactory,
                    OnboardingStorage,
                    NavigationService,
                    PrivateSharedStorageService,
                    IntentDonationService,
                    StopwatchProvider,
                    RxActionFactory,
                    PermissionsService,
                    SchedulerProvider);
            }

            protected virtual void SetupObservables()
            {
            }
        }

        public sealed class TheConstructor : SettingsViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useDataSource,
                bool useSyncManager,
                bool useUserAccessManager,
                bool useDialogService,
                bool useUserPreferences,
                bool useAnalyticsService,
                bool useInteractorFactory,
                bool useplatformInfo,
                bool useOnboardingStorage,
                bool useNavigationService,
                bool usePrivateSharedStorageService,
                bool useIntentDonationService,
                bool useStopwatchProvider,
                bool useRxActionFactory,
                bool usePermissionsService,
                bool useSchedulerProvider)
            {
                var dataSource = useDataSource ? DataSource : null;
                var syncManager = useSyncManager ? SyncManager : null;
                var platformInfo = useplatformInfo ? PlatformInfo : null;
                var dialogService = useDialogService ? DialogService : null;
                var userPreferences = useUserPreferences ? UserPreferences : null;
                var analyticsService = useAnalyticsService ? AnalyticsService : null;
                var userAccessManager = useUserAccessManager ? UserAccessManager : null;
                var onboardingStorage = useOnboardingStorage ? OnboardingStorage : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var stopwatchProvider = useStopwatchProvider ? StopwatchProvider : null;
                var intentDonationService = useIntentDonationService ? IntentDonationService : null;
                var privateSharedStorageService = usePrivateSharedStorageService ? PrivateSharedStorageService : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;
                var permissionsService = usePermissionsService ? PermissionsService : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new SettingsViewModel(
                        dataSource,
                        syncManager,
                        platformInfo,
                        dialogService,
                        userPreferences,
                        analyticsService,
                        userAccessManager,
                        interactorFactory,
                        onboardingStorage,
                        navigationService,
                        privateSharedStorageService,
                        intentDonationService,
                        stopwatchProvider,
                        rxActionFactory,
                        permissionsService,
                        schedulerProvider);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheFlags : SettingsViewModelTest
        {
            [Property]
            public void DoesNotEverSetBothIsRunningSyncAndIsSyncedBothToTrue(NonEmptyArray<SyncProgress> statuses)
            {
                DataSource.HasUnsyncedData().Returns(Observable.Return(false));
                var syncedObserver = TestScheduler.CreateObserver<bool>();
                var syncingObserver = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();
                viewModel.IsSynced.Subscribe(syncedObserver);
                viewModel.IsRunningSync.Subscribe(syncingObserver);

                foreach (var state in statuses.Get)
                {
                    syncedObserver.Messages.Clear();
                    syncingObserver.Messages.Clear();

                    ProgressSubject.OnNext(state);
                    TestScheduler.Start();
                    TestScheduler.Stop();

                    var isSynced = syncedObserver.SingleEmittedValue();
                    var isRunningSync = syncingObserver.SingleEmittedValue();

                    (isRunningSync && isSynced).Should().BeFalse();
                }
            }

            [Property]
            public void EmitTheAppropriateIsRunningSyncValues(NonEmptyArray<SyncProgress> statuses)
            {
                DataSource.HasUnsyncedData().Returns(Observable.Return(false));
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                viewModel.IsRunningSync.Subscribe(observer);

                foreach (var state in statuses.Get)
                {
                    observer.Messages.Clear();

                    ProgressSubject.OnNext(state);
                    TestScheduler.Start();
                    TestScheduler.Stop();

                    var isRunningSync = observer.SingleEmittedValue();
                    isRunningSync.Should().Be(state == SyncProgress.Syncing);
                }
            }

            [Property]
            public void EmitTheAppropriateIsSyncedValues(NonEmptyArray<SyncProgress> statuses)
            {
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                viewModel.IsSynced.Subscribe(observer);

                foreach (var state in statuses.Get)
                {
                    if (state == SyncProgress.Unknown)
                        continue;

                    observer.Messages.Clear();

                    ProgressSubject.OnNext(state);
                    TestScheduler.Start();
                    TestScheduler.Stop();

                    var isSynced = observer.SingleEmittedValue();
                    isSynced.Should().Be(state == SyncProgress.Synced);
                }
            }
        }

        public sealed class TheEmailObservable : SettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void EmitsWheneverTheUserEmailChanges()
            {
                var observer = TestScheduler.CreateObserver<string>();
                ViewModel.Email.Subscribe(observer);

                UserSubject.OnNext(new MockUser { Email = Email.From("newmail@mail.com") });
                UserSubject.OnNext(new MockUser { Email = Email.From("newmail@mail.com") });
                UserSubject.OnNext(new MockUser { Email = Email.From("differentmail@mail.com") });
                TestScheduler.Start();

                observer.Messages.Count.Should().Be(2);
            }
        }

        public sealed class TheUserAvatarObservable : SettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void EmitsWheneverTheUserImageChanges()
            {
                var observer = TestScheduler.CreateObserver<byte[]>();
                ViewModel.UserAvatar.Subscribe(observer);

                UserSubject.OnNext(new MockUser { ImageUrl = "http://toggl.com/image.jpg" });
                UserSubject.OnNext(new MockUser { ImageUrl = "http://toggl.com/image.jpg" });
                UserSubject.OnNext(new MockUser { ImageUrl = "http://toggl.com/image2.jpg" });
                TestScheduler.Start();

                observer.Messages.Count.Should().Be(2);
            }

            [Fact, LogIfTooSlow]
            public void CallsTheImageDownloadInteractor()
            {
                var observer = TestScheduler.CreateObserver<byte[]>();
                ViewModel.UserAvatar.Subscribe(observer);

                UserSubject.OnNext(new MockUser { ImageUrl = "http://toggl.com/image.jpg" });
                UserSubject.OnNext(new MockUser { ImageUrl = "http://toggl.com/image.jpg" });
                UserSubject.OnNext(new MockUser { ImageUrl = "http://toggl.com/image2.jpg" });
                TestScheduler.Start();

                InteractorFactory.Received(2).GetUserAvatar(Arg.Any<string>());
            }
        }

        public sealed class TheTryLogoutMethod : SettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task EmitsOneIsLoggingOutEvent()
            {
                var observer = TestScheduler.CreateObserver<Unit>();
                ViewModel.LoggingOut.Subscribe(observer);

                doNotShowConfirmationDialog();

                ViewModel.TryLogout.Execute();
                TestScheduler.Start();

                observer.Messages.Single();
            }

            [Fact, LogIfTooSlow]
            public async Task ExecutesTheLogoutInteractor()
            {
                doNotShowConfirmationDialog();

                TestScheduler.Start();
                ViewModel.TryLogout.Execute();

                await InteractorFactory.Received().Logout(LogoutSource.Settings).Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheLoginScreen()
            {
                doNotShowConfirmationDialog();

                TestScheduler.Start();
                ViewModel.TryLogout.Execute();

                await NavigationService.Received().Navigate<LoginViewModel>();
            }

            [Fact, LogIfTooSlow]
            public async Task ChecksIfThereAreUnsyncedDataWhenTheSyncProcessFinishes()
            {
                ProgressSubject.OnNext(SyncProgress.Synced);

                TestScheduler.Start();
                ViewModel.TryLogout.Execute();

                await DataSource.Received().HasUnsyncedData();
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotShowConfirmationDialogWhenTheAppIsInSync()
            {
                doNotShowConfirmationDialog();

                TestScheduler.Start();
                ViewModel.TryLogout.Execute();

                await DialogService.DidNotReceiveWithAnyArgs().Confirm("", "", "", "");
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsConfirmationDialogWhenThereIsNothingToPushButSyncIsRunning()
            {
                DataSource.HasUnsyncedData().Returns(Observable.Return(false));
                ProgressSubject.OnNext(SyncProgress.Syncing);

                TestScheduler.Start();
                ViewModel.TryLogout.Execute();

                await DialogService.ReceivedWithAnyArgs().Confirm("", "", "", "");
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsConfirmationDialogWhenThereIsSomethingToPushButSyncIsNotRunning()
            {
                DataSource.HasUnsyncedData().Returns(Observable.Return(true));
                ProgressSubject.OnNext(SyncProgress.Syncing);

                TestScheduler.Start();
                ViewModel.TryLogout.Execute();

                await DialogService.ReceivedWithAnyArgs().Confirm("", "", "", "");
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotProceedWithLogoutWhenUserClicksCancelButtonInTheDialog()
            {
                ProgressSubject.OnNext(SyncProgress.Syncing);
                DialogService.Confirm(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>()).Returns(Observable.Return(false));

                TestScheduler.Start();
                ViewModel.TryLogout.Execute();

                InteractorFactory.DidNotReceive().Logout(Arg.Any<LogoutSource>());
                await NavigationService.DidNotReceive().Navigate<LoginViewModel>();
            }

            [Fact, LogIfTooSlow]
            public async Task ProceedsWithLogoutWhenUserClicksSignOutButtonInTheDialog()
            {
                ProgressSubject.OnNext(SyncProgress.Syncing);
                DialogService.Confirm(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>()).Returns(Observable.Return(true));

                TestScheduler.Start();
                ViewModel.TryLogout.Execute();

                await InteractorFactory.Received().Logout(LogoutSource.Settings).Execute();
                await NavigationService.Received().Navigate<LoginViewModel>();
            }

            private void doNotShowConfirmationDialog()
            {
                DataSource.HasUnsyncedData().Returns(Observable.Return(false));
                ProgressSubject.OnNext(SyncProgress.Synced);
            }
        }

        public sealed class ThePickDefaultWorkspaceMethod : SettingsViewModelTest
        {
            private const long workspaceId = 10;
            private const long defaultWorkspaceId = 11;
            private const string workspaceName = "My custom workspace";
            private readonly IThreadSafeWorkspace workspace;
            private readonly IThreadSafeWorkspace defaultWorkspace = Substitute.For<IThreadSafeWorkspace>();

            protected override void SetupObservables()
            {
                DataSource.User.Current.Returns(Observable.Return(new MockUser()));
            }

            public ThePickDefaultWorkspaceMethod()
            {
                defaultWorkspace = new MockWorkspace { Id = defaultWorkspaceId };
                workspace = new MockWorkspace { Id = workspaceId, Name = workspaceName };

                UserSubject.OnNext(new MockUser());

                InteractorFactory.GetDefaultWorkspace().Execute()
                    .Returns(Observable.Return(defaultWorkspace));

                InteractorFactory.GetWorkspaceById(workspaceId).Execute()
                    .Returns(Observable.Return(workspace));

                ViewModel.Prepare();
            }

            [Fact, LogIfTooSlow]
            public async Task CallsTheSelectWorkspaceViewModel()
            {
                ViewModel.PickDefaultWorkspace.Execute();
                TestScheduler.Start();

                await NavigationService.Received()
                    .Navigate<SelectWorkspaceViewModel, long, long>(Arg.Any<long>());
            }

            [Fact, LogIfTooSlow]
            public async Task UpdatesTheUserWithTheReceivedWorspace()
            {
                NavigationService
                    .Navigate<SelectWorkspaceViewModel, long, long>(Arg.Any<long>())
                    .Returns(Task.FromResult(workspaceId));

                ViewModel.PickDefaultWorkspace.Execute();
                TestScheduler.Start();

                await InteractorFactory
                    .Received()
                    .UpdateDefaultWorkspace(Arg.Is(workspaceId))
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task StartsTheSyncAlgorithm()
            {
                NavigationService
                    .Navigate<SelectWorkspaceViewModel, long, long>(Arg.Any<long>())
                    .Returns(Task.FromResult(workspaceId));

                ViewModel.PickDefaultWorkspace.Execute();
                TestScheduler.Start();

                SyncManager.Received().PushSync();
            }
        }

        public sealed class TheToggleManualModeMethod : SettingsViewModelTest
        {
            public TheToggleManualModeMethod()
            {
                PreferencesSubject.OnNext(new MockPreferences());
            }

            [Fact, LogIfTooSlow]
            public void CallsEnableTimerModeIfCurrentlyInManualMode()
            {
                UserPreferences.IsManualModeEnabled.Returns(true);

                ViewModel.ToggleManualMode();

                UserPreferences.Received().EnableTimerMode();
            }

            [Fact, LogIfTooSlow]
            public void CallsEnableManualModeIfCurrentlyInTimerMode()
            {
                UserPreferences.IsManualModeEnabled.Returns(false);

                ViewModel.ToggleManualMode();

                UserPreferences.Received().EnableManualMode();
            }
        }

        public sealed class TheOpenHelpViewMethod : SettingsViewModelTest
        {
            [Property]
            public void NavigatesToBrowserViewModelWithUrlFromplatformInfo(
                NonEmptyString nonEmptyString)
            {
                var helpUrl = nonEmptyString.Get;
                PlatformInfo.HelpUrl.Returns(helpUrl);

                ViewModel.OpenHelpView.Execute();
                TestScheduler.Start();

                NavigationService.Received().Navigate<BrowserViewModel, BrowserParameters>(
                    Arg.Is<BrowserParameters>(parameter => parameter.Url == helpUrl));
            }

            [Fact, LogIfTooSlow]
            public void NavigatesToBrowserViewModelWithHelpTitle()
            {
                ViewModel.OpenHelpView.Execute();

                NavigationService.Received().Navigate<BrowserViewModel, BrowserParameters>(
                    Arg.Is<BrowserParameters>(parameter => parameter.Title == Resources.Help));
            }
        }

        public sealed class TheVersionProperty : SettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void ShouldBeConstructedFromVersionAndBuildNumber()
            {
                const string version = "1.0";
                PlatformInfo.Version.Returns(version);

                ViewModel.Version.Should().Be($"{version} ({PlatformInfo.BuildNumber})");
            }
        }

        public sealed class TheToggleTimeEntriesGroupingAction : SettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task UpdatesTheStoredPreferences()
            {
                var oldValue = false;
                var newValue = true;
                var preferences = new MockPreferences { CollapseTimeEntries = oldValue };
                PreferencesSubject.OnNext(preferences);

                ViewModel.ToggleTimeEntriesGrouping.Execute();
                TestScheduler.Start();

                await InteractorFactory
                    .Received()
                    .UpdatePreferences(Arg.Is<EditPreferencesDTO>(dto => dto.CollapseTimeEntries.Equals(New<bool>.Value(newValue))))
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task UpdatesTheCollapseTimeEntriesProperty()
            {
                var oldValue = false;
                var newValue = true;
                var oldPreferences = new MockPreferences { CollapseTimeEntries = oldValue };
                var newPreferences = new MockPreferences { CollapseTimeEntries = newValue };
                PreferencesSubject.OnNext(oldPreferences);
                InteractorFactory.UpdatePreferences(Arg.Any<EditPreferencesDTO>())
                    .Execute()
                    .Returns(Observable.Return(newPreferences));

                ViewModel.ToggleTimeEntriesGrouping.Execute();
                TestScheduler.Start();

                await InteractorFactory
                    .Received()
                    .UpdatePreferences(Arg.Is<EditPreferencesDTO>(dto => dto.CollapseTimeEntries.ValueOr(oldValue) == newValue))
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task InitiatesPushSync()
            {
                var oldValue = false;
                var preferences = new MockPreferences { CollapseTimeEntries = oldValue };
                PreferencesSubject.OnNext(preferences);

                ViewModel.ToggleTimeEntriesGrouping.Execute();
                TestScheduler.Start();

                await SyncManager.Received().PushSync();
            }

            [Property, LogIfTooSlow]
            public void TracksEventWhenToggled(bool initialState)
            {
                var analyticsEvent = Substitute.For<IAnalyticsEvent<bool>>();
                AnalyticsService.GroupTimeEntriesSettingsChanged.Returns(analyticsEvent);
                var preferences = new MockPreferences { CollapseTimeEntries = initialState };
                var expectedState = !initialState;

                PreferencesSubject.OnNext(preferences);
                ViewModel.ToggleTimeEntriesGrouping.Execute();
                TestScheduler.Start();

                analyticsEvent.Received().Track(expectedState);
            }
        }

        public sealed class TheSelectDateFormatMethod : SettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task NavigatesToSelectDateFormatViewModelPassingCurrentDateFormat()
            {
                var dateFormat = DateFormat.FromLocalizedDateFormat("MM-DD-YYYY");
                var preferences = new MockPreferences { DateFormat = dateFormat };
                PreferencesSubject.OnNext(preferences);

                ViewModel.SelectDateFormat.Execute();
                TestScheduler.Start();

                await NavigationService
                    .Received()
                    .Navigate<SelectDateFormatViewModel, DateFormat, DateFormat>(dateFormat);
            }

            [Fact, LogIfTooSlow]
            public async Task UpdatesTheStoredPreferences()
            {
                var oldDateFormat = DateFormat.FromLocalizedDateFormat("MM-DD-YYYY");
                var newDateFormat = DateFormat.FromLocalizedDateFormat("DD.MM.YYYY");
                var preferences = new MockPreferences { DateFormat = oldDateFormat };
                PreferencesSubject.OnNext(preferences);
                NavigationService
                    .Navigate<SelectDateFormatViewModel, DateFormat, DateFormat>(Arg.Any<DateFormat>())
                    .Returns(Task.FromResult(newDateFormat));

                ViewModel.SelectDateFormat.Execute();
                TestScheduler.Start();

                await InteractorFactory
                    .Received()
                    .UpdatePreferences(Arg.Is<EditPreferencesDTO>(dto => dto.DateFormat.Equals(New<DateFormat>.Value(newDateFormat))))
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task UpdatesTheDateFormatProperty()
            {
                var oldDateFormat = DateFormat.FromLocalizedDateFormat("MM-DD-YYYY");
                var newDateFormat = DateFormat.FromLocalizedDateFormat("DD.MM.YYYY");
                var oldPreferences = new MockPreferences { DateFormat = oldDateFormat };
                var newPreferences = new MockPreferences { DateFormat = newDateFormat };
                PreferencesSubject.OnNext(oldPreferences);
                NavigationService
                    .Navigate<SelectDateFormatViewModel, DateFormat, DateFormat>(Arg.Any<DateFormat>())
                    .Returns(Task.FromResult(newDateFormat));
                InteractorFactory.UpdatePreferences(Arg.Any<EditPreferencesDTO>())
                    .Execute()
                    .Returns(Observable.Return(newPreferences));

                ViewModel.SelectDateFormat.Execute();
                TestScheduler.Start();

                await InteractorFactory
                    .Received()
                    .UpdatePreferences(Arg.Is<EditPreferencesDTO>(dto => dto.DateFormat.ValueOr(oldDateFormat) == newDateFormat))
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task InitiatesPushSync()
            {
                var oldDateFormat = DateFormat.FromLocalizedDateFormat("MM-DD-YYYY");
                var newDateFormat = DateFormat.FromLocalizedDateFormat("DD.MM.YYYY");
                var preferences = new MockPreferences { DateFormat = oldDateFormat };
                PreferencesSubject.OnNext(preferences);
                NavigationService
                    .Navigate<SelectDateFormatViewModel, DateFormat, DateFormat>(Arg.Any<DateFormat>())
                    .Returns(Task.FromResult(newDateFormat));

                ViewModel.SelectDateFormat.Execute();
                TestScheduler.Start();

                SyncManager.Received().PushSync();
            }
        }

        public sealed class TheToggleUseTwentyFourHourClockMethod : SettingsViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public async Task ChangesTheValueOfTheUseTwentyFourHourHourClock(bool originalValue)
            {
                var timeFormat = originalValue ? TimeFormat.TwentyFourHoursFormat : TimeFormat.TwelveHoursFormat;
                PreferencesSubject.OnNext(new MockPreferences { TimeOfDayFormat = timeFormat });

                ViewModel.ToggleTwentyFourHourSettings.Execute();
                TestScheduler.Start();

                await InteractorFactory
                    .Received()
                    .UpdatePreferences(Arg.Is<EditPreferencesDTO>(
                        dto => dto.TimeOfDayFormat.ValueOr(default(TimeFormat)).IsTwentyFourHoursFormat != originalValue)
                    ).Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task InitiatesPushSync()
            {
                var preferences = new MockPreferences();
                PreferencesSubject.OnNext(preferences);
                var observable = Observable.Return(preferences);
                InteractorFactory.UpdatePreferences(Arg.Any<EditPreferencesDTO>()).Execute().Returns(observable);

                ViewModel.ToggleTwentyFourHourSettings.Execute();
                TestScheduler.Start();

                SyncManager.Received().PushSync();
            }
        }

        public sealed class TheSelectDurationFormatMethod : SettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task NavigatesToSelectDurationFormatViewModelPassingCurrentDurationFormat()
            {
                var durationFormat = DurationFormat.Improved;
                var preferences = new MockPreferences { DurationFormat = durationFormat };
                PreferencesSubject.OnNext(preferences);

                ViewModel.SelectDurationFormat.Execute();
                TestScheduler.Start();

                await NavigationService
                    .Received()
                    .Navigate<SelectDurationFormatViewModel, DurationFormat, DurationFormat>(durationFormat);
            }

            [Fact, LogIfTooSlow]
            public async Task UpdatesTheStoredPreferences()
            {
                var oldDurationFormat = DurationFormat.Decimal;
                var newDurationFormat = DurationFormat.Improved;
                var preferences = new MockPreferences { DurationFormat = oldDurationFormat };
                PreferencesSubject.OnNext(preferences);
                NavigationService
                    .Navigate<SelectDurationFormatViewModel, DurationFormat, DurationFormat>(Arg.Any<DurationFormat>())
                    .Returns(Task.FromResult(newDurationFormat));

                ViewModel.SelectDurationFormat.Execute();
                TestScheduler.Start();

                await InteractorFactory
                    .Received()
                    .UpdatePreferences(Arg.Is<EditPreferencesDTO>(dto => dto.DurationFormat.Equals(New<DurationFormat>.Value(newDurationFormat))))
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task SelectDurationFormatCommandCallsPushSync()
            {
                var oldDurationFormat = DurationFormat.Decimal;
                var newDurationFormat = DurationFormat.Improved;
                var preferences = new MockPreferences { DurationFormat = oldDurationFormat };
                PreferencesSubject.OnNext(preferences);
                NavigationService
                    .Navigate<SelectDurationFormatViewModel, DurationFormat, DurationFormat>(Arg.Any<DurationFormat>())
                    .Returns(Task.FromResult(newDurationFormat));

                ViewModel.SelectDurationFormat.Execute();
                TestScheduler.Start();

                await SyncManager.Received().PushSync();
            }

            [Fact, LogIfTooSlow]
            public async Task UpdatesTheDurationFormatProperty()
            {
                var oldDurationFormat = DurationFormat.Decimal;
                var newDurationFormat = DurationFormat.Improved;
                var oldPreferences = new MockPreferences { DurationFormat = oldDurationFormat };
                var newPreferences = new MockPreferences { DurationFormat = newDurationFormat };
                PreferencesSubject.OnNext(oldPreferences);
                NavigationService
                    .Navigate<SelectDurationFormatViewModel, DurationFormat, DurationFormat>(Arg.Any<DurationFormat>())
                    .Returns(Task.FromResult(newDurationFormat));
                InteractorFactory
                    .UpdatePreferences(Arg.Any<EditPreferencesDTO>())
                    .Execute()
                    .Returns(Observable.Return(newPreferences));

                ViewModel.SelectDurationFormat.Execute();
                TestScheduler.Start();

                await InteractorFactory
                    .UpdatePreferences(Arg.Is<EditPreferencesDTO>(dto => dto.DurationFormat.ValueOr(oldDurationFormat) == newDurationFormat))
                    .Received()
                    .Execute();
            }
        }

        public sealed class TheSelectBeginningOfWeekMethod : SettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task NavigatesToSelectBeginningOfWeekViewModelPassingCurrentBeginningOfWeek()
            {
                var beginningOfWeek = BeginningOfWeek.Friday;
                var user = new MockUser { BeginningOfWeek = beginningOfWeek };
                UserSubject.OnNext(user);

                ViewModel.SelectBeginningOfWeek.Execute();
                TestScheduler.Start();

                await NavigationService
                    .Received()
                    .Navigate<SelectBeginningOfWeekViewModel, BeginningOfWeek, BeginningOfWeek>(beginningOfWeek);
            }

            [Fact, LogIfTooSlow]
            public async Task UpdatesTheStoredPreferences()
            {
                var oldBeginningOfWeek = BeginningOfWeek.Tuesday;
                var newBeginningOfWeek = BeginningOfWeek.Sunday;

                var user = Substitute.For<IThreadSafeUser>();
                user.BeginningOfWeek.Returns(oldBeginningOfWeek);
                UserSubject.OnNext(user);
                NavigationService
                    .Navigate<SelectBeginningOfWeekViewModel, BeginningOfWeek, BeginningOfWeek>(Arg.Any<BeginningOfWeek>())
                    .Returns(Task.FromResult(newBeginningOfWeek));

                ViewModel.SelectBeginningOfWeek.Execute();
                TestScheduler.Start();

                await InteractorFactory
                    .Received()
                    .UpdateUser(Arg.Is<EditUserDTO>(dto => dto.BeginningOfWeek == newBeginningOfWeek))
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task SelectBeginningOfWeekCommandCallsPushSync()
            {
                var oldBeginningOfWeek = BeginningOfWeek.Tuesday;
                var newBeginningOfWeek = BeginningOfWeek.Sunday;
                var user = new MockUser { BeginningOfWeek = oldBeginningOfWeek };
                UserSubject.OnNext(user);
                NavigationService
                    .Navigate<SelectBeginningOfWeekViewModel, BeginningOfWeek, BeginningOfWeek>(Arg.Any<BeginningOfWeek>())
                    .Returns(Task.FromResult(newBeginningOfWeek));

                ViewModel.SelectBeginningOfWeek.Execute();
                TestScheduler.Start();

                await SyncManager.Received().PushSync();
            }

            [Fact, LogIfTooSlow]
            public async Task UpdatesTheBeginningOfWeekProperty()
            {
                var oldBeginningOfWeek = BeginningOfWeek.Tuesday;
                var newBeginningOfWeek = BeginningOfWeek.Sunday;
                var oldUser = new MockUser { BeginningOfWeek = oldBeginningOfWeek };
                var newUser = new MockUser { BeginningOfWeek = newBeginningOfWeek };
                UserSubject.OnNext(oldUser);
                NavigationService
                    .Navigate<SelectBeginningOfWeekViewModel, BeginningOfWeek, BeginningOfWeek>(Arg.Any<BeginningOfWeek>())
                    .Returns(Task.FromResult(newBeginningOfWeek));
                InteractorFactory
                    .UpdateUser(Arg.Any<EditUserDTO>())
                    .Execute()
                    .Returns(Observable.Return(newUser));

                ViewModel.SelectBeginningOfWeek.Execute();
                TestScheduler.Start();

                await InteractorFactory.UpdateUser(
                    Arg.Is<EditUserDTO>(dto => dto.BeginningOfWeek == newBeginningOfWeek
                )).Received().Execute();
            }
        }

        public sealed class TheOpenAboutViewMethod : SettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheAboutPage()
            {
                ViewModel.OpenAboutView.Execute();

                NavigationService.Received().Navigate<AboutViewModel>();
            }
        }

        public sealed class TheIsFeedBackSuccessViewShowingProperty : SettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void EmitsTrueWhenTapOnTheView()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                viewModel.IsFeedbackSuccessViewShowing.StartWith(true).Subscribe(observer);
                viewModel.CloseFeedbackSuccessView();
                TestScheduler.Start();

                observer.LastEmittedValue().Should().BeFalse();
            }
        }

        public sealed class TheOpenCalendarSettingsAction : SettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task NavigatesToCalendarSettingsViewModel()
            {
                ViewModel.OpenCalendarSettings.Execute(Unit.Default);

                NavigationService.Received().Navigate<CalendarSettingsViewModel>();
            }
        }

        public sealed class TheIsSmartRemindersVisibleProperty : SettingsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task EmitsTrueWhenCalendarPermissionsAreGrantedAndCalendarsAreSelected()
            {
                PermissionsService.CalendarPermissionGranted.Returns(Observable.Return(true));
                UserPreferences.EnabledCalendars.Returns(Observable.Return(new List<string>() { "1" }));

                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                await viewModel.Initialize();
                TestScheduler.Start();

                viewModel.IsCalendarSmartRemindersVisible.Subscribe(observer);

                observer.Messages.First().Value.Value.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public async Task EmitsFalseWhenCalendarPermissionsAreNotGranted()
            {
                PermissionsService.CalendarPermissionGranted.Returns(Observable.Return(false));
                UserPreferences.EnabledCalendars.Returns(Observable.Return(new List<string>() { "1" }));

                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                await viewModel.Initialize();
                TestScheduler.Start();

                viewModel.IsCalendarSmartRemindersVisible.Subscribe(observer);

                observer.Messages.First().Value.Value.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task EmitsFalseWhenNoCalendarsAreSelected()
            {
                PermissionsService.CalendarPermissionGranted.Returns(Observable.Return(true));
                UserPreferences.EnabledCalendars.Returns(Observable.Return(new List<string>()));

                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                await viewModel.Initialize();
                TestScheduler.Start();

                viewModel.IsCalendarSmartRemindersVisible.Subscribe(observer);

                observer.Messages.First().Value.Value.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task EmitsAgainWhenCalendarPermissionsChangeAfterViewAppears()
            {
                PermissionsService.CalendarPermissionGranted.Returns(Observable.Return(false), Observable.Return(true));
                UserPreferences.EnabledCalendars.Returns(Observable.Return(new List<string>() { "1" }));

                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                await viewModel.Initialize();
                TestScheduler.Start();

                viewModel.IsCalendarSmartRemindersVisible.Subscribe(observer);

                viewModel.ViewAppeared();

                var messages = observer.Messages.Select(msg => msg.Value.Value);

                messages.First().Should().BeFalse();
                messages.Last().Should().BeTrue();
            }
        }
    }
}
