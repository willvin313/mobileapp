using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MvvmCross.ViewModels;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models;
using Toggl.Multivac;

namespace Toggl.Foundation.UI.ViewModels
{
    [Preserve(AllMembers = true)]
    public class SyncFailuresViewModel : MvxViewModel
    {
        public IImmutableList<SyncFailureItem> SyncFailures { get; private set; }

        private readonly IInteractorFactory interactorFactory;

        public SyncFailuresViewModel(IInteractorFactory interactorFactory)
        {
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));

            this.interactorFactory = interactorFactory;
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            var syncFailures = await interactorFactory
                .GetItemsThatFailedToSync()
                .Execute()
                .FirstAsync();

            SyncFailures = syncFailures.ToImmutableList();
        }
    }
}
