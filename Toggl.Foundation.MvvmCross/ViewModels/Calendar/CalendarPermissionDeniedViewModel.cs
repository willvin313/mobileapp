using System.Reactive;
using MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.ViewModels.Calendar
{
    [Preserve(AllMembers = true)]
    public sealed class CalendarPermissionDeniedViewModel : MvxViewModelResult<Unit>
    {
        private readonly IPermissionsService permissionsService;

        public UIAction EnableAccess { get; }

        public CalendarPermissionDeniedViewModel(IPermissionsService permissionsService)
        {
            Ensure.Argument.IsNotNull(permissionsService, nameof(permissionsService));

            this.permissionsService = permissionsService;

            EnableAccess = UIAction.FromAction(enableAccess);
        }

        private void enableAccess()
        {
            permissionsService.OpenAppSettings();
        }
    }
}
