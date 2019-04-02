using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [NestedPresentation]
    public partial class ReportsCalendarViewController : ReactiveViewController<ReportsCalendarViewModel>, IUICollectionViewDelegate
    {
        private bool calendarInitialized;
        private List<ReportsCalendarPageViewModel> pendingMonthsUpdate;
        private ReportsCalendarCollectionViewSource calendarCollectionViewSource;

        public ReportsCalendarViewController()
            : base(nameof(ReportsCalendarViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            calendarCollectionViewSource = new ReportsCalendarCollectionViewSource(CalendarCollectionView);
            var calendarCollectionViewLayout = new ReportsCalendarCollectionViewLayout();
            CalendarCollectionView.Delegate = calendarCollectionViewSource;
            CalendarCollectionView.DataSource = calendarCollectionViewSource;
            CalendarCollectionView.CollectionViewLayout = calendarCollectionViewLayout;

            var quickSelectCollectionViewSource = new ReportsCalendarQuickSelectCollectionViewSource(QuickSelectCollectionView);
            QuickSelectCollectionView.Source = quickSelectCollectionViewSource;

            ViewModel.DayHeadersObservable
                .Subscribe(setupDayHeaders)
                .DisposedBy(DisposeBag);

            ViewModel.MonthsObservable
                .Subscribe(calendarCollectionViewSource.UpdateMonths)
                .DisposedBy(DisposeBag);

            calendarCollectionViewSource.DayTaps
                .Subscribe(ViewModel.SelectDay.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.HighlightedDateRangeObservable
                .Subscribe(calendarCollectionViewSource.UpdateSelection)
                .DisposedBy(DisposeBag);

            ViewModel.CurrentMonthObservable
                .Select(month => month.Year.ToString())
                .Subscribe(CurrentYearLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.CurrentMonthObservable
                .Select(month => month.Month)
                .Select(CultureInfo.GetCultureInfo("en-US").DateTimeFormat.GetMonthName)
                .Subscribe(CurrentMonthLabel.Rx().Text())
                .DisposedBy(DisposeBag);

            ViewModel.SelectedDateRangeObservable
                .Subscribe(quickSelectCollectionViewSource.UpdateSelection)
                .DisposedBy(DisposeBag);

            quickSelectCollectionViewSource.ShortcutTaps
                .Subscribe(ViewModel.SelectShortcut.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.QuickSelectShortcutsObservable
                .Subscribe(quickSelectCollectionViewSource.UpdateShortcuts)
                .DisposedBy(DisposeBag);
        }

        public override void DidMoveToParentViewController(UIViewController parent)
        {
            base.DidMoveToParentViewController(parent);

            //The constraint isn't available before DidMoveToParentViewController

            var rowHeight = ReportsCalendarCollectionViewLayout.CellHeight;
            var additionalHeight = View.Bounds.Height - CalendarCollectionView.Bounds.Height;

            var heightConstraint = View
                .Superview
                .Constraints
                .Single(c => c.FirstAttribute == NSLayoutAttribute.Height);

            ViewModel.RowsInCurrentMonthObservable
                .Select(rows => rows * rowHeight + additionalHeight)
                .Subscribe(heightConstraint.Rx().ConstantAnimated())
                .DisposedBy(DisposeBag);
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            if (calendarInitialized) return;

            calendarCollectionViewSource.CurrentPageNotScrollingObservable
                .Subscribe(ViewModel.SetCurrentPage)
                .DisposedBy(DisposeBag);

            calendarCollectionViewSource.CurrentPageWhileScrollingObservable
                .Subscribe(ViewModel.UpdateMonth)
                .DisposedBy(DisposeBag);

            ViewModel.CurrentPageObservable
                .Subscribe(CalendarCollectionView.Rx().CurrentPageObserver())
                .DisposedBy(DisposeBag);

            calendarInitialized = true;
        }

        private void setupDayHeaders(IReadOnlyList<string> dayHeaders)
        {
            DayHeader0.Text = dayHeaders[0];
            DayHeader1.Text = dayHeaders[1];
            DayHeader2.Text = dayHeaders[2];
            DayHeader3.Text = dayHeaders[3];
            DayHeader4.Text = dayHeaders[4];
            DayHeader5.Text = dayHeaders[5];
            DayHeader6.Text = dayHeaders[6];
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
            {
                DisposeBag.Dispose();
            }
        }
    }
}

