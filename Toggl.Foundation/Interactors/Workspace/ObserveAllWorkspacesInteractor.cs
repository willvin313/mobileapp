using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;

namespace Toggl.Foundation.Interactors
{
    internal sealed class ObserveAllWorkspacesInteractor : IInteractor<IObservable<IEnumerable<IThreadSafeWorkspace>>>
    {
        private readonly ITogglDataSource dataSource;

        public ObserveAllWorkspacesInteractor(ITogglDataSource dataSource)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));

            this.dataSource = dataSource;
        }

        public IObservable<IEnumerable<IThreadSafeWorkspace>> Execute()
            => dataSource.Workspaces.ItemsChanged()
                .StartWith(Unit.Default)
                .SelectMany(_ => dataSource.Workspaces.GetAll())
                .DistinctUntilChanged();
    }
}
