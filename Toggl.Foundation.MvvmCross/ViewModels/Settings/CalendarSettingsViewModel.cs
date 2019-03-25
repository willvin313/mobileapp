using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.MvvmCross.ViewModels.Settings
{
    [Preserve(AllMembers = true)]
    public sealed class CalendarSettingsViewModel : SelectUserCalendarsViewModelBase
    {
        private readonly IPermissionsService permissionsService;
        private readonly IRxActionFactory rxActionFactory;

        private bool calendarListVisible = false;
        private ISubject<bool> calendarListVisibleSubject = new BehaviorSubject<bool>(false);

        public bool PermissionGranted { get; private set; }
        public IObservable<bool> CalendarListVisible { get; }

        public UIAction RequestAccess { get; }
        public UIAction TogglCalendarIntegration { get; }

        public CalendarSettingsViewModel(
            IUserPreferences userPreferences,
            IInteractorFactory interactorFactory,
            IMvxNavigationService navigationService,
            IRxActionFactory rxActionFactory,
            IPermissionsService permissionsService)
            : base(userPreferences, interactorFactory, navigationService, rxActionFactory)
        {
            Ensure.Argument.IsNotNull(permissionsService, nameof(permissionsService));

            this.permissionsService = permissionsService;

            RequestAccess = rxActionFactory.FromAction(requestAccess);
            TogglCalendarIntegration = rxActionFactory.FromAsync(togglCalendarIntegration);

            SelectCalendar
                .Elements
                .Subscribe(onCalendarSelected);

            CalendarListVisible = calendarListVisibleSubject.AsObservable().DistinctUntilChanged();
        }

        public async override Task Initialize()
        {
            PermissionGranted = await permissionsService.CalendarPermissionGranted;

            if (!PermissionGranted)
            {
                UserPreferences.SetEnabledCalendars();
            }

            await base.Initialize();

            calendarListVisible = PermissionGranted && SelectedCalendarIds.Any();
            calendarListVisibleSubject.OnNext(calendarListVisible);
        }

        protected override async Task OnClose()
        {
            UserPreferences.SetEnabledCalendars(InitialSelectedCalendarIds.ToArray());
            await base.OnClose();
        }

        protected override async Task OnDone()
        {
            if (!calendarListVisible)
                SelectedCalendarIds.Clear();

            UserPreferences.SetEnabledCalendars(SelectedCalendarIds.ToArray());
            await base.OnDone();
        }

        private void requestAccess()
        {
            permissionsService.OpenAppSettings();
        }

        private void onCalendarSelected()
        {
            UserPreferences.SetEnabledCalendars(SelectedCalendarIds.ToArray());
        }

        private async Task togglCalendarIntegration()
        {
            if (calendarListVisible)
            {
                calendarListVisible = false;
            }
            else
            {
                var authorized = await permissionsService.CalendarPermissionGranted;
                if (!authorized)
                {
                    authorized = await permissionsService.RequestCalendarAuthorization();
                    if (!authorized)
                        await NavigationService.Navigate<CalendarPermissionDeniedViewModel, Unit>();

                    calendarListVisible = await permissionsService.CalendarPermissionGranted;
                    ReloadCalendars();
                }
                else
                {
                    calendarListVisible = true;
                }
            }
            calendarListVisibleSubject.OnNext(calendarListVisible);
        }
    }
}
