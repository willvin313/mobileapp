using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using CoreGraphics;
using MvvmCross.Binding.BindingContext;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [NestedPresentation]
    public partial class ReportsCalendarViewController : ReactiveViewController<ReportsCalendarViewModel>, IUICollectionViewDelegate
    {
        private bool calendarInitialized;

        public ReportsCalendarViewController()
            : base(nameof(ReportsCalendarViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var calendarCollectionViewSource = new ReportsCalendarCollectionViewSource(CalendarCollectionView, ViewModel.CalendarDayTapped);
            var calendarCollectionViewLayout = new ReportsCalendarCollectionViewLayout();

            CalendarCollectionView.DataSource = calendarCollectionViewSource;
            CalendarCollectionView.CollectionViewLayout = calendarCollectionViewLayout;

            var quickSelectCollectionViewSource = new ReportsCalendarQuickSelectCollectionViewSource(QuickSelectCollectionView, ViewModel.QuickSelect);
            QuickSelectCollectionView.Source = quickSelectCollectionViewSource;

            //Updates
            this.Bind(ViewModel.ReloadCalendar, range =>
            {
                CalendarCollectionView.ReloadData();
                QuickSelectCollectionView.ReloadData();
            });

            //Calendar collection view
            this.Bind(ViewModel.Months, calendarCollectionViewSource.BindMonths());

            //Quick select collection view
            this.Bind(ViewModel.QuickSelectShortcuts, quickSelectCollectionViewSource.BindShortcuts());

            //Text
            this.Bind(ViewModel.DayHeaders, setHeaders);
            this.Bind(ViewModel.CurrentYear, CurrentYearLabel.BindText());
            this.Bind(ViewModel.CurrentMonthName, CurrentMonthLabel.BindText());
        }

        private void setHeaders(IImmutableList<string> headers)
        {
            DayHeader0.Text = headers[0];
            DayHeader1.Text = headers[1];
            DayHeader2.Text = headers[2];
            DayHeader3.Text = headers[3];
            DayHeader4.Text = headers[4];
            DayHeader5.Text = headers[5];
            DayHeader6.Text = headers[6];
        }

        public override void DidMoveToParentViewController(UIViewController parent)
        {
            base.DidMoveToParentViewController(parent);

            //The constraint isn't available before DidMoveToParentViewController
            var heightConstraint = View.Superview.Constraints
                .Single(c => c.FirstAttribute == NSLayoutAttribute.Height);

            var additionalHeight = View.Bounds.Height - CalendarCollectionView.Bounds.Height;

            this.Bind(ViewModel.RowsInCurrentMonth.Select(contraintHeight), heightConstraint.BindAnimatedConstant());

            nfloat contraintHeight(int rowCount)
                => rowCount * ReportsCalendarCollectionViewLayout.CellHeight + additionalHeight;
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            if (calendarInitialized) return;

            //This binding needs the calendar to be in it's final size to work properly
            this.Bind(ViewModel.CurrentPage, CalendarCollectionView.BindCurrentPage());
            this.Bind(CalendarCollectionView.CurrentPage(), ViewModel.OnCurrentPageChanged);
            calendarInitialized = true;
        }
    }
}

