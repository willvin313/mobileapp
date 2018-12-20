using System;
using System.Reactive.Disposables;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.Widget;
using MvvmCross.Droid.Support.V7.AppCompat;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels.Reports;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Giskard.ViewHelpers;
using Toggl.Giskard.Views;
using Toggl.Multivac.Extensions;
using Observable = System.Reactive.Linq.Observable;

namespace Toggl.Giskard.Activities
{
    [MvxActivityPresentation]
    [Activity(Theme = "@style/AppTheme",
        ScreenOrientation = ScreenOrientation.Portrait,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed partial class ReportsActivity : MvxAppCompatActivity<ReportsViewModel>
    {
        private ReportsRecyclerView reportsRecyclerView;
        private ReportsLinearLayout reportsMainContainer;

        public CompositeDisposable DisposeBag { get; } = new CompositeDisposable();

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.ReportsActivity);
            OverridePendingTransition(Resource.Animation.abc_slide_in_right, Resource.Animation.abc_fade_out);

            reportsRecyclerView = FindViewById<ReportsRecyclerView>(Resource.Id.ReportsActivityRecyclerView);
            reportsMainContainer = FindViewById<ReportsLinearLayout>(Resource.Id.ReportsActivityMainContainer);
            reportsMainContainer.CalendarContainer = FindViewById(Resource.Id.ReportsCalendarContainer);

            initializeViews();

            selectWorkspaceFAB.Rx().Tap()
                .Subscribe(ViewModel.SelectWorkspace.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.WorkspaceNameObservable
                .Subscribe(workspaceName.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            Observable.CombineLatest(
                    ViewModel.StartDate,
                    ViewModel.EndDate,
                    ViewModel.WorkspaceHasBillableFeatureEnabled,
                    ViewModel.BarChartViewModel.DateFormat,
                    ViewModel.BarChartViewModel.Bars,
                    ViewModel.BarChartViewModel.MaximumHoursPerBar,
                    ViewModel.BarChartViewModel.HorizontalLegend,
                    (startDate, endDate, workspaceIsBillable, dateFormat, bars, maximumHoursPerBar, horizontalLegend) => new BarChartData(startDate, endDate, workspaceIsBillable, dateFormat, bars, maximumHoursPerBar, horizontalLegend))
                .Subscribe(reportsRecyclerView.ReportsRecyclerAdapter.UpdateBarChart)
                .DisposedBy(DisposeBag);

            setupToolbar();
        }

        public override void OnEnterAnimationComplete()
        {
            base.OnEnterAnimationComplete();
            ViewModel.StopNavigationFromMainLogStopwatch();
        }

        public override void Finish()
        {
            base.Finish();
            OverridePendingTransition(Resource.Animation.abc_fade_in, Resource.Animation.abc_slide_out_right);
        }

        private void setupToolbar()
        {
            var toolbar = FindViewById<Toolbar>(Resource.Id.Toolbar);
            toolbar.Title = "";
            SetSupportActionBar(toolbar);

            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);

            toolbar.NavigationClick += onNavigateBack;
        }

        private void onNavigateBack(object sender, Toolbar.NavigationClickEventArgs e)
        {
            Finish();
        }

        internal void ToggleCalendarState(bool forceHide)
        {
            reportsMainContainer.ToggleCalendar(forceHide);
        }

        internal void RecalculateCalendarHeight()
        {
            reportsMainContainer.RecalculateCalendarHeight();
        }
    }
}
