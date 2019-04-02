using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave.ApiClients;
using Toggl.Ultrawave.Network;

namespace Toggl.Foundation.Interactors.Settings
{
    public class SendFeedbackInteractor : IInteractor<IObservable<Unit>>
    {
        public const string LastLogin = "Last login";
        public const string PhoneModel = "Phone model";
        public const string ManualModeIsOn = "Manual mode is on";
        public const string AppNameAndVersion = "App name and version";
        public const string OperatingSystem = "Platform and OS version";
        public const string LastSyncAttempt = "Time of last attempted sync";
        public const string DeviceTime = "Device's time when sending this feedback";
        public const string LastSuccessfulSync = "Time of last successful full sync";
        public const string NumberOfUnsyncedTimeEntries = "Number of unsynced time entries";
        public const string NumberOfWorkspaces = "Number of workspaces available to the user";
        public const string NumberOfUnsyncableTimeEntries = "Number of unsyncable time entries";
        public const string NumberOfTimeEntries = "Number of time entries in our database in total";

        private readonly string message;
        private readonly ITimeService timeService;
        private readonly IFeedbackApi feedbackApi;
        private readonly IPlatformInfo platformInfo;
        private readonly IUserPreferences userPreferences;
        private readonly ILastTimeUsageStorage lastTimeUsageStorage;
        private readonly ISingletonDataSource<IThreadSafeUser> userDataSource;
        private readonly IDataSource<IThreadSafeWorkspace, IDatabaseWorkspace> workspacesDataSource;
        private readonly IDataSource<IThreadSafeTimeEntry, IDatabaseTimeEntry> timeEntriesDataSource;

        public SendFeedbackInteractor(
            IFeedbackApi feedbackApi,
            ISingletonDataSource<IThreadSafeUser> userDataSource,
            IDataSource<IThreadSafeWorkspace, IDatabaseWorkspace> workspacesDataSource,
            IDataSource<IThreadSafeTimeEntry, IDatabaseTimeEntry> timeEntriesDataSource,
            IPlatformInfo platformInfo,
            IUserPreferences userPreferences,
            ILastTimeUsageStorage lastTimeUsageStorage,
            ITimeService timeService,
            string message)
        {
            Ensure.Argument.IsNotNull(message, nameof(message));
            Ensure.Argument.IsNotNull(feedbackApi, nameof(feedbackApi));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(platformInfo, nameof(platformInfo));
            Ensure.Argument.IsNotNull(userDataSource, nameof(userDataSource));
            Ensure.Argument.IsNotNull(userPreferences, nameof(userPreferences));
            Ensure.Argument.IsNotNull(lastTimeUsageStorage, nameof(lastTimeUsageStorage));
            Ensure.Argument.IsNotNull(workspacesDataSource, nameof(workspacesDataSource));
            Ensure.Argument.IsNotNull(timeEntriesDataSource, nameof(timeEntriesDataSource));

            this.message = message;
            this.feedbackApi = feedbackApi;
            this.timeService = timeService;
            this.platformInfo = platformInfo;
            this.userDataSource = userDataSource;
            this.userPreferences = userPreferences;
            this.workspacesDataSource = workspacesDataSource;
            this.timeEntriesDataSource = timeEntriesDataSource;
            this.lastTimeUsageStorage = lastTimeUsageStorage;
        }

        private IObservable<int> workspacesCount
            => workspacesDataSource
                .GetAll()
                .Select(list => list.Count());

        private IObservable<int> timeEntriesCount
            => timeEntriesDataSource
                .GetAll()
                .Select(list => list.Count());

        private IObservable<int> unsyncedTimeEntriesCount
            => timeEntriesDataSource
                .GetAll(te => te.SyncStatus == SyncStatus.SyncNeeded)
                .Select(list => list.Count());

        private IObservable<int> unsyncabeTimeEntriesCount
            => timeEntriesDataSource
                .GetAll(te => te.SyncStatus == SyncStatus.SyncFailed)
                .Select(list => list.Count());

        public IObservable<Unit> Execute()
            => workspacesCount.Zip(
                    timeEntriesCount,
                    unsyncedTimeEntriesCount,
                    unsyncabeTimeEntriesCount,
                    combineData)
                .SelectMany(data =>
                    userDataSource.Get().SelectMany(user =>
                        feedbackApi.Send(user.Email, message, data)));

        private Dictionary<string, string> combineData(
            int workspaces,
            int timeEntries,
            int unsyncedTimeEntries,
            int unsyncableTimeEntriesCount)
            => new Dictionary<string, string>
            {
                [PhoneModel] = platformInfo.PhoneModel,
                [OperatingSystem] = platformInfo.OperatingSystem,
                [AppNameAndVersion] = $"{platformInfo.Platform}/{platformInfo.Version}",
                [NumberOfWorkspaces] = workspaces.ToString(),
                [NumberOfTimeEntries] = timeEntries.ToString(),
                [NumberOfUnsyncedTimeEntries] = unsyncedTimeEntries.ToString(),
                [NumberOfUnsyncableTimeEntries] = unsyncableTimeEntriesCount.ToString(),
                [LastSyncAttempt] = lastTimeUsageStorage.LastSyncAttempt?.ToString() ?? "never",
                [LastSuccessfulSync] = lastTimeUsageStorage.LastSuccessfulSync?.ToString() ?? "never",
                [DeviceTime] = timeService.CurrentDateTime.ToString(),
                [ManualModeIsOn] = userPreferences.IsManualModeEnabled ? "yes" : "no",
                [LastLogin] = lastTimeUsageStorage.LastLogin?.ToString() ?? "never"
            };
    }
}
