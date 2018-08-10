using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using MvvmCross.Binding.BindingContext;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [NestedPresentation]
    public partial class ReportsCalendarViewController : ReactiveViewController<ReportsCalendarViewModel>, IUICollectionViewDelegate
    {
        private bool calendarInitialized;

        private UILabel[] headerLabels
            => new[] { DayHeader0, DayHeader1, DayHeader2, DayHeader3, DayHeader4, DayHeader5, DayHeader6 };

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
            this.BindVoid(ViewModel.ReloadCalendar, () =>
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
            headerLabels
                .Indexed()
                .ForEach((label, index) => label.Text = headers[index]);
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

