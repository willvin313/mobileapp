using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Services;
using Toggl.Foundation.Sync;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.UI.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class NoWorkspaceViewModel : MvxViewModelResult<Unit>
    {
        private readonly ISyncManager syncManager;
        private readonly IAccessRestrictionStorage accessRestrictionStorage;
        private readonly IInteractorFactory interactorFactory;
        private readonly IMvxNavigationService navigationService;
        private readonly ISchedulerProvider schedulerProvider;
        private readonly IRxActionFactory rxActionFactory;

        public IObservable<bool> IsLoading { get; }
        public UIAction CreateWorkspaceWithDefaultName { get; }
        public UIAction TryAgain { get; }

        public NoWorkspaceViewModel(
            ISyncManager syncManager,
            IInteractorFactory interactorFactory,
            IMvxNavigationService navigationService,
            IAccessRestrictionStorage accessRestrictionStorage,
            ISchedulerProvider schedulerProvider,
            IRxActionFactory rxActionFactory)
        {
            Ensure.Argument.IsNotNull(syncManager, nameof(syncManager));
            Ensure.Argument.IsNotNull(accessRestrictionStorage, nameof(accessRestrictionStorage));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.syncManager = syncManager;
            this.accessRestrictionStorage = accessRestrictionStorage;
            this.navigationService = navigationService;
            this.interactorFactory = interactorFactory;
            this.rxActionFactory = rxActionFactory;

            CreateWorkspaceWithDefaultName = rxActionFactory.FromObservable(createWorkspaceWithDefaultName);
            TryAgain = rxActionFactory.FromAsync(tryAgain);
            IsLoading = Observable.CombineLatest(
                CreateWorkspaceWithDefaultName.Executing,
                TryAgain.Executing,
                CommonFunctions.Or);
        }

        private async Task tryAgain()
        {
            var anyWorkspaceIsAvailable = await syncManager.ForceFullSync()
                .Where(state => state == SyncState.Sleep)
                .SelectMany(_ => interactorFactory.GetAllWorkspaces().Execute())
                .Any(workspaces => workspaces.Any());

            if (anyWorkspaceIsAvailable)
            {
                close();
            }
        }

        private IObservable<Unit> createWorkspaceWithDefaultName()
            => interactorFactory.CreateDefaultWorkspace().Execute().Do(close);

        private void close()
        {
            accessRestrictionStorage.SetNoWorkspaceStateReached(false);
            navigationService.Close(this, Unit.Default);
        }
    }
}
