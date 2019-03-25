using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.ViewModels.Calendar
{
    [Preserve(AllMembers = true)]
    public sealed class CalendarPermissionDeniedViewModel : MvxViewModelResult<Unit>
    {
        private readonly IMvxNavigationService navigationService;
        private readonly IPermissionsService permissionsService;
        private readonly IRxActionFactory rxActionFactory;

        public UIAction EnableAccess { get; }
        public UIAction Close { get; }

        public CalendarPermissionDeniedViewModel(IMvxNavigationService navigationService, IPermissionsService permissionsService, IRxActionFactory rxActionFactory)
        {
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(permissionsService, nameof(permissionsService));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.navigationService = navigationService;
            this.permissionsService = permissionsService;

            EnableAccess = rxActionFactory.FromAction(enableAccess);
            Close = rxActionFactory.FromAsync(close);
        }

        private Task close()
            => navigationService.Close(this, Unit.Default);

        public override void ViewAppeared()
        {
            base.ViewAppeared();
            closeIfPermissionIsGranted();
        }

        private void enableAccess()
        {
            permissionsService.OpenAppSettings();
        }

        private async Task closeIfPermissionIsGranted()
        {
            var authorized = await permissionsService.CalendarPermissionGranted;
            if (authorized)
                navigationService.Close(this, Unit.Default);
        }
    }
}
