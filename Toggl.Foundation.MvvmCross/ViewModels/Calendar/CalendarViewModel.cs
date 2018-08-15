﻿using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.Calendar;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.MvvmCross.ViewModels.Calendar
{
    [Preserve(AllMembers = true)]
    public sealed class CalendarViewModel : MvxViewModel
    {
        private readonly ITogglDataSource dataSource;
        private readonly ITimeService timeService;
        private readonly IInteractorFactory interactorFactory;
        private readonly IOnboardingStorage onboardingStorage;
        private readonly IPermissionsService permissionsService;
        private readonly IMvxNavigationService navigationService;

        public IObservable<bool> ShouldShowOnboarding { get; }

        public IObservable<TimeFormat> TimeOfDayFormat { get; }

        public IObservable<DateTime> Date { get; }

        public UIAction GetStartedAction { get; }

        public InputAction<CalendarItem> OnItemTapped { get; }

        public InputAction<(DateTimeOffset, TimeSpan)> OnDurationSelected { get; }

        public ObservableGroupedOrderedCollection<CalendarItem> CalendarItems { get; }

        public CalendarViewModel(
            ITogglDataSource dataSource,
            ITimeService timeService,
            IInteractorFactory interactorFactory,
            IOnboardingStorage onboardingStorage,
            IPermissionsService permissionsService,
            IMvxNavigationService navigationService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(permissionsService, nameof(permissionsService));

            this.dataSource = dataSource;
            this.timeService = timeService;
            this.interactorFactory = interactorFactory;
            this.onboardingStorage = onboardingStorage;
            this.navigationService = navigationService;
            this.permissionsService = permissionsService;

            ShouldShowOnboarding = Observable
                .Return(!onboardingStorage.CompletedCalendarOnboarding());

            TimeOfDayFormat = dataSource
                .Preferences
                .Current
                .Select(preferences => preferences.TimeOfDayFormat);

            Date = Observable.Return(timeService.CurrentDateTime.Date);

            GetStartedAction = new UIAction(getStarted);

            OnItemTapped = new InputAction<CalendarItem>(onItemTapped);

            OnDurationSelected = new InputAction<(DateTimeOffset StartTime, TimeSpan Duration)>(
                tuple => onDurationSelected(tuple.StartTime, tuple.Duration)
            );

            CalendarItems = new ObservableGroupedOrderedCollection<CalendarItem>(
                indexKey: item => item.StartTime,
                orderingKey: item => item.StartTime,
                groupingKey: _ => 0);
        }

        public override async Task Initialize()
        {
            var today = timeService.CurrentDateTime.Date;
            await fetchCalendarItems(today);
        }

        private IObservable<Unit> getStarted()
            => permissionsService
                .RequestCalendarAuthorization()
                .Do(handlePermissionRequestResult)
                .SelectUnit();

        private void handlePermissionRequestResult(bool permissionGranted)
        {
            if (permissionGranted)
                Console.WriteLine("Great success");
            else
                navigationService.Navigate<CalendarPermissionDeniedViewModel>();
        }

        private IObservable<Unit> onItemTapped(CalendarItem calendarItem)
            => Observable.FromAsync(async () =>
            {
                switch (calendarItem.Source)
                {
                    case CalendarItemSource.TimeEntry when calendarItem.TimeEntryId.HasValue:
                        await navigationService.Navigate<EditTimeEntryViewModel, long>(calendarItem.TimeEntryId.Value);
                        break;

                    case CalendarItemSource.Calendar:
                        var workspace = await interactorFactory.GetDefaultWorkspace().Execute();
                        var prototype = calendarItem.AsTimeEntryPrototype(workspace.Id);
                        var timeEntry = await interactorFactory.CreateTimeEntry(prototype).Execute();
                        await navigationService.Navigate<EditTimeEntryViewModel, long>(timeEntry.Id);
                        break;
                }

                await fetchCalendarItems(timeService.CurrentDateTime.Date);

            });

        private IObservable<Unit> onDurationSelected(DateTimeOffset startTime, TimeSpan duration)
            => Observable.FromAsync(async () =>
            {
                var workspace = await interactorFactory.GetDefaultWorkspace().Execute();
                var prototype = duration.AsTimeEntryPrototype(startTime, workspace.Id);
                await interactorFactory.CreateTimeEntry(prototype).Execute();

                await fetchCalendarItems(timeService.CurrentDateTime.Date);
            });

        private async Task fetchCalendarItems(DateTime date)
        {
            var calendarItems = await interactorFactory.GetCalendarItemsForDate(date).Execute();
            CalendarItems.ReplaceWith(calendarItems);
        }
    }
}