using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.MvvmCross.ViewModels.Calendar
{
    [Preserve(AllMembers = true)]
    public sealed class SelectUserCalendarsViewModel : SelectUserCalendarsViewModelBase
    {
        public SelectUserCalendarsViewModel(
            IUserPreferences userPreferences,
            IInteractorFactory interactorFactory,
            IMvxNavigationService navigationService,
            IRxActionFactory rxActionFactory
            )
            : base(userPreferences, interactorFactory, navigationService, rxActionFactory)
        {
        }
    }
}
