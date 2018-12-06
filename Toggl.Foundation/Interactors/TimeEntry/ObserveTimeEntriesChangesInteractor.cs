using System;
using System.Reactive;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Extensions;
using Toggl.Multivac;

namespace Toggl.Foundation.Interactors
{
    public class ObserveTimeEntriesChangesInteractor : IInteractor<IObservable<Unit>>
    {
            private readonly ITogglDataSource dataSource;

            public ObserveTimeEntriesChangesInteractor(ITogglDataSource dataSource)
            {
                Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));

                this.dataSource = dataSource;
            }

            public IObservable<Unit> Execute()
                => dataSource.TimeEntries.ItemsChanged();
        }
}
