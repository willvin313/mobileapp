using System;
using System.Reactive.Linq;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using MvvmCross;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Giskard.Adapters.Calendar;
using Toggl.Giskard.Views.Calendar;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Multivac.Extensions;
using System.Linq;
using Toggl.Foundation.Calendar;
using System.Reactive;
using Toggl.Multivac;

namespace Toggl.Giskard.Fragments
{
    public partial class CalendarFragment : ReactiveFragment<CalendarViewModel>
    {
        private CalendarLayoutManager calendarLayoutManager;
        private ITimeService timeService;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.CalendarFragment, container, false);
            InitializeViews(view);

            timeService = Mvx.Resolve<ITimeService>();
            var schedulerProvider = Mvx.Resolve<ISchedulerProvider>();
            calendarLayoutManager = new CalendarLayoutManager();
            calendarRecyclerView.SetLayoutManager(calendarLayoutManager);
            var displayMetrics = new DisplayMetrics();
            Activity.WindowManager.DefaultDisplay.GetMetrics(displayMetrics);
            var calendarAdapter = new CalendarAdapter(view.Context, timeService, displayMetrics.WidthPixels);
            calendarRecyclerView.SetTimeService(timeService);
            calendarRecyclerView.SetAdapter(calendarAdapter);

            timeService
                .CurrentDateTimeObservable
                .DistinctUntilChanged(offset => offset.Day)
                .ObserveOn(schedulerProvider.MainScheduler)
                .Subscribe(configureHeaderDate)
                .DisposedBy(DisposeBag);

            ViewModel.HasCalendarsLinked.SelectUnit()
                .Merge(ViewModel.CalendarItems.CollectionChange.SelectUnit())
                .SelectMany(ViewModel.HasCalendarsLinked)
                .Subscribe(hasCalendarsLinked =>
                {
                    calendarAdapter.UpdateItems(ViewModel.CalendarItems, hasCalendarsLinked);
                    calendarRecyclerView.SetHasTwoColumns(hasCalendarsLinked);
                })
                .DisposedBy(DisposeBag);

            ViewModel.HasCalendarsLinked
                .Subscribe(headerCalendarEventsView.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.CalendarItems.CollectionChange
                .SelectUnit()
                .StartWith(Unit.Default)
                .Select(_ => calculateCalendarEventsCount())
                .ObserveOn(schedulerProvider.MainScheduler)
                .Subscribe(updateCalendarEventsCount)
                .DisposedBy(DisposeBag);

            ViewModel.TimeTrackedToday
                .Subscribe(headerTimeEntriesTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.HasCalendarsLinked
                .Invert()
                .Subscribe(headerLinkCalendarsButton.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            headerLinkCalendarsButton.Rx().Tap()
                .Subscribe(ViewModel.LinkCalendars.Inputs)
                .DisposedBy(DisposeBag);

            calendarAdapter.CalendarItemTappedObservable
                .Subscribe(ViewModel.OnItemTapped.Inputs)
                .DisposedBy(DisposeBag);

            calendarRecyclerView.EmptySpansTouchedObservable
                .Where(_ => !calendarAdapter.NeedsToClearItemInEditMode())
                .Subscribe(ViewModel.CreateTimeEntryAtOffset.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.ShouldShowOnboarding
                .Subscribe(onboardingVisibilityChanged)
                .DisposedBy(DisposeBag);

            return view;
        }

        private void onboardingVisibilityChanged(bool visible)
        {
            if (visible)
            {
                if (onboardingView == null)
                {
                    initializeOnboardingView();
                }

                onboardingView.Visibility = ViewStates.Visible;
            }
            else if (onboardingView != null)
            {
                onboardingView.Visibility = ViewStates.Gone;
            }
        }

        private void initializeOnboardingView()
        {
            onboardingView = onboardingViewStub.Inflate();
            getStartedButton = onboardingView.FindViewById<Button>(Resource.Id.CalendarOnboardingGetStartedButton);
            skipButton = onboardingView.FindViewById<TextView>(Resource.Id.CalendarOnboardingSkipButton);

            getStartedButton.Rx().Tap()
                .Subscribe(ViewModel.GetStarted.Inputs)
                .DisposedBy(DisposeBag);

            skipButton.Rx().Tap()
                .Subscribe(ViewModel.SkipOnboarding.Inputs)
                .DisposedBy(DisposeBag);
        }

        private void updateCalendarEventsCount(int count)
        {
            var text = Context.GetString(Resource.String.TotalEvents, count.ToString());
            headerCalendarEventsTextView.Text = text;
        }

        private int calculateCalendarEventsCount()
        {
            return ViewModel
                .CalendarItems
                .SelectMany(group => group.Where(item => item.Source == CalendarItemSource.Calendar))
                .Count();
        }

        private void configureHeaderDate(DateTimeOffset offset)
        {
            var day = offset.Day.ToString();
            headerDayTextView.Text = day;
            headerWeekdayTextView.Text = offset.ToString("ddd");
        }
    }
}
