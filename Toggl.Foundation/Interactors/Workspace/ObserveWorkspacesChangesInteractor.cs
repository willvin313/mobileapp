using System;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Extensions;
using Toggl.Multivac;

namespace Toggl.Foundation.Interactors
{
    internal sealed class ObserveWorkspacesChangesInteractor : IInteractor<IObservable<Unit>>
    {
        private readonly ITogglDataSource dataSource;

        public ObserveWorkspacesChangesInteractor(ITogglDataSource dataSource)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));

            this.dataSource = dataSource;
        }

        public IObservable<Unit> Execute()
            => dataSource.Workspaces.ItemsChanged();
    }
}
