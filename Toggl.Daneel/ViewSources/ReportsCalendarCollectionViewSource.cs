using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Foundation;
using MvvmCross.Commands;
using MvvmCross.Platforms.Ios.Binding.Views;
using Toggl.Daneel.Cells.Reports;
using Toggl.Daneel.Views;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public sealed class ReportsCalendarCollectionViewSource : UICollectionViewSource
    {
        private const string cellIdentifier = nameof(ReportsCalendarViewCell);

        private readonly UICollectionView collectionView;
        private Action<CalendarDayViewModel> calendarDayTapped;

        private IImmutableList<CalendarPageViewModel> months = ImmutableList<CalendarPageViewModel>.Empty;
        public IImmutableList<CalendarPageViewModel> Months 
        {
            get => months;
            set
            {
                this.months = value ?? ImmutableList<CalendarPageViewModel>.Empty;
                collectionView.ReloadData();
            }
        }

        public ReportsCalendarCollectionViewSource(UICollectionView collectionView, Action<CalendarDayViewModel> calendarDayTapped) 
        {
            this.collectionView = collectionView;
            this.calendarDayTapped = calendarDayTapped;

            collectionView.RegisterNibForCell(ReportsCalendarViewCell.Nib, cellIdentifier);
        }

        public Action<IImmutableList<CalendarPageViewModel>> BindMonths()
            => months => Months = months;

        public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
        {
            var cell = collectionView.DequeueReusableCell(cellIdentifier, indexPath) as ReportsCalendarViewCell;
            cell.Item = Months[indexPath.Section].Days[(int)indexPath.Item];
            cell.CellTapped = calendarDayTapped;

            return cell;
        }

        public override nint NumberOfSections(UICollectionView collectionView)
            => Months.Count;

        public override nint GetItemsCount(UICollectionView collectionView, nint section)
            => Months[(int)section].Days.Count;
    }
}
