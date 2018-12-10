using System;
using System.Collections.Generic;
using System.Reactive;
using Toggl.Foundation.Interactors.Changes;
using Toggl.Foundation.Models.Interfaces;

namespace Toggl.Foundation.Interactors
{
    public sealed partial class InteractorFactory : IInteractorFactory
    {
        public IInteractor<IObservable<Unit>> ObserveWorkspaceOrTimeEntriesChanges()
            => new ObserveWorkspaceOrTimeEntriesChangesInteractor(this);
    }
}
