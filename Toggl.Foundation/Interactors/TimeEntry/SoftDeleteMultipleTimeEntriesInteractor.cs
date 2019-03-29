using System;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models;
using Toggl.Foundation.Models.Interfaces;
using Toggl.PrimeRadiant.Models;
using System.Reactive;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Interactors.Generic;
using System.Reactive.Linq;
using System.Linq;
using Toggl.Multivac.Extensions;
using Toggl.Foundation.Extensions;

namespace Toggl.Foundation.Interactors
{
    public class SoftDeleteMultipleTimeEntriesInteractor : IInteractor<IObservable<Unit>>
    {
        private readonly IInteractorFactory interactorFactory;
        private readonly IDataSource<IThreadSafeTimeEntry, IDatabaseTimeEntry> dataSource;
        private readonly ISyncManager syncManager;
        private readonly long[] ids;

        public SoftDeleteMultipleTimeEntriesInteractor(
            IDataSource<IThreadSafeTimeEntry, IDatabaseTimeEntry> dataSource,
            ISyncManager syncManager,
            IInteractorFactory interactorFactory,
            long[] ids)
        {
            this.interactorFactory = interactorFactory;
            this.dataSource = dataSource;
            this.syncManager = syncManager;
            this.ids = ids;
        }

        public IObservable<Unit> Execute()
            => interactorFactory.GetMultipleTimeEntriesById(ids)
                .Execute()
                .Select(timeEntries => timeEntries.Select(TimeEntry.DirtyDeleted))
                .SelectMany(dataSource.BatchUpdate)
                .SingleAsync()
                .Do(syncManager.InitiatePushSync)
                .SelectUnit();
    }
}
