using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Models;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Sync;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;

namespace Toggl.Foundation.Interactors
{
    internal class UpdateMultipleTimeEntriesInteractor : IInteractor<IObservable<IEnumerable<IThreadSafeTimeEntry>>>
    {
        private readonly EditTimeEntryDto[] timeEntriesDtos;
        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IInteractorFactory interactorFactory;
        private readonly IStopwatchProvider stopwatchProvider;
        private readonly ISyncManager syncManager;

        public UpdateMultipleTimeEntriesInteractor(
            ITimeService timeService,
            ITogglDataSource dataSource,
            IStopwatchProvider stopwatchProvider,
            IInteractorFactory interactorFactory,
            ISyncManager syncManager,
            EditTimeEntryDto[] timeEntriesDtos)
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(stopwatchProvider, nameof(stopwatchProvider));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(timeEntriesDtos, nameof(timeEntriesDtos));
            Ensure.Argument.IsNotNull(syncManager, nameof(syncManager));

            this.timeEntriesDtos = timeEntriesDtos;
            this.timeService = timeService;
            this.dataSource = dataSource;
            this.interactorFactory = interactorFactory;
            this.stopwatchProvider = stopwatchProvider;
            this.syncManager = syncManager;
        }

        public IObservable<IEnumerable<IThreadSafeTimeEntry>> Execute()
        {
            var updateTimeMeasurement = stopwatchProvider.Create(MeasuredOperation.UpdateTimeEntriesGroup);
            updateTimeMeasurement.Start();

            var dtosMap = timeEntriesDtos.ToDictionary(dto => dto.Id);

            var ids = dtosMap.Keys.ToArray();

            return interactorFactory.GetMultipleTimeEntriesById(ids)
                .Execute()
                .Select(timeEntries => timeEntries.Select(timeEntry => createUpdatedTimeEntry(timeEntry, dtosMap[timeEntry.Id])))
                .SelectMany(dataSource.TimeEntries.BatchUpdate)
                .UnwrapUpdatedThreadSafeEntities()
                .Do(syncManager.InitiatePushSync)
                .Do(_ => updateTimeMeasurement.Stop());
        }

        private TimeEntry createUpdatedTimeEntry(IThreadSafeTimeEntry timeEntry, EditTimeEntryDto dto)
            => TimeEntry.Builder.Create(dto.Id)
                .SetDescription(dto.Description)
                .SetDuration(dto.StopTime.HasValue ? (long?)(dto.StopTime.Value - dto.StartTime).TotalSeconds : null)
                .SetTagIds(dto.TagIds)
                .SetStart(dto.StartTime)
                .SetTaskId(dto.TaskId)
                .SetBillable(dto.Billable)
                .SetProjectId(dto.ProjectId)
                .SetWorkspaceId(dto.WorkspaceId)
                .SetUserId(timeEntry.UserId)
                .SetIsDeleted(timeEntry.IsDeleted)
                .SetServerDeletedAt(timeEntry.ServerDeletedAt)
                .SetAt(timeService.CurrentDateTime)
                .SetSyncStatus(SyncStatus.SyncNeeded)
                .Build();
    }
}
