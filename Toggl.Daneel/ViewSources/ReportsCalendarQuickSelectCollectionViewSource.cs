using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreGraphics;
using Foundation;
using Toggl.Daneel.Views.Reports;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels.ReportsCalendar.QuickSelectShortcuts;
using Toggl.Multivac;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public sealed class ReportsCalendarQuickSelectCollectionViewSource
        : UICollectionViewSource, IUICollectionViewDelegateFlowLayout
    {
        private const int cellWidth = 96;
        private const int cellHeight = 32;
        private const string cellIdentifier = nameof(ReportsCalendarQuickSelectViewCell);

        private readonly UICollectionView collectionView;
        private readonly ISubject<ReportsCalendarBaseQuickSelectShortcut> shortcutTaps = new Subject<ReportsCalendarBaseQuickSelectShortcut>();

        private ReportsDateRangeParameter currentDateRange;
        private IList<ReportsCalendarBaseQuickSelectShortcut> shortcuts = new List<ReportsCalendarBaseQuickSelectShortcut>();
        
        public IObservable<ReportsCalendarBaseQuickSelectShortcut> ShortcutTaps { get; }
        
        public ReportsCalendarQuickSelectCollectionViewSource(UICollectionView collectionView)
        {
            Ensure.Argument.IsNotNull(collectionView, nameof(collectionView));
            collectionView.RegisterNibForCell(ReportsCalendarQuickSelectViewCell.Nib, cellIdentifier);
            ShortcutTaps = shortcutTaps.AsObservable();
            this.collectionView = collectionView;
        }
        
        public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
        {
            var item = shortcuts[indexPath.Row] as ReportsCalendarBaseQuickSelectShortcut;
            var cell = collectionView.DequeueReusableCell(cellIdentifier, indexPath) as ReportsCalendarQuickSelectViewCell;

            cell.UpdateSelectedDateRange(currentDateRange);
            cell.Item = item;

            return cell;
        }

        public override nint NumberOfSections(UICollectionView collectionView) => 1;

        public override nint GetItemsCount(UICollectionView collectionView, nint section) => shortcuts.Count;

        [Export("collectionView:layout:sizeForItemAtIndexPath:")]
        public CGSize GetSizeForItem(UICollectionView collectionView, UICollectionViewLayout layout, NSIndexPath indexPath)
        {
            return new CGSize(cellWidth, cellHeight);
        }

        public override void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath)
        {
            shortcutTaps.OnNext(shortcuts[indexPath.Row]);
        }

        public void UpdateShortcuts(IList<ReportsCalendarBaseQuickSelectShortcut> newShortcuts)
        {
            shortcuts = newShortcuts;
            collectionView.ReloadData();
        }

        public void UpdateSelection(ReportsDateRangeParameter dateRange)
        {
            currentDateRange = dateRange;
            collectionView.ReloadData();
        }
    }
}
