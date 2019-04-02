using System;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Interactors
{
    internal sealed class DeleteMultipleTimeEntriesInteractor : IInteractor<IObservable<Unit>>
    {
        private readonly long[] ids;
        private readonly IObservableDataSource<IThreadSafeTimeEntry, IDatabaseTimeEntry> dataSource;
        private readonly IInteractorFactory interactorFactory;

        public DeleteMultipleTimeEntriesInteractor(
            IObservableDataSource<IThreadSafeTimeEntry, IDatabaseTimeEntry> dataSource,
            IInteractorFactory interactorFactory,
            long[] ids)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));

            this.ids = ids;
            this.dataSource = dataSource;
            this.interactorFactory = interactorFactory;
        }

        public IObservable<Unit> Execute()
            => interactorFactory.GetMultipleTimeEntriesById(ids).Execute()
                .SelectMany(dataSource.DeleteAll)
                .SelectUnit();
    }
}
