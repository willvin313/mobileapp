using System;
using System.Collections.Immutable;
using CoreGraphics;
using Foundation;
using Toggl.Daneel.Cells.Reports;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public sealed class ReportsCalendarQuickSelectCollectionViewSource : UICollectionViewSource, IUICollectionViewDelegateFlowLayout
    {
        private const int cellWidth = 96;
        private const int cellHeight = 32;
        private const string cellIdentifier = nameof(ReportsCalendarQuickSelectViewCell);

        private readonly UICollectionView collectionView;
        private readonly Action<QuickSelectShortcut> shortcutTapped;

        private IImmutableList<QuickSelectShortcut> shortcuts = ImmutableList<QuickSelectShortcut>.Empty;
        public IImmutableList<QuickSelectShortcut> Shortcuts
        {
            get => shortcuts;
            set 
            {
                shortcuts = value ?? ImmutableList<QuickSelectShortcut>.Empty;
                collectionView.ReloadData();
            }
        }

        public ReportsCalendarQuickSelectCollectionViewSource(UICollectionView collectionView, Action<QuickSelectShortcut> shortcutTapped) 
        {
            Ensure.Argument.IsNotNull(collectionView, nameof(collectionView));

            this.collectionView = collectionView;
            this.shortcutTapped = shortcutTapped;

            collectionView.RegisterNibForCell(ReportsCalendarQuickSelectViewCell.Nib, cellIdentifier);
        }

        public Action<IImmutableList<QuickSelectShortcut>> BindShortcuts()
            => shortcuts => Shortcuts = shortcuts;

        public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
        {
            var cell = collectionView.DequeueReusableCell(cellIdentifier, indexPath) as ReportsCalendarQuickSelectViewCell;
            cell.Item = shortcuts[indexPath.Row];
            return cell;
        }

        public override void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath)
        {
            shortcutTapped?.Invoke(shortcuts[indexPath.Row]);
        }

        public override nint GetItemsCount(UICollectionView collectionView, nint section)
            => shortcuts.Count;

        public override nint NumberOfSections(UICollectionView collectionView) => 1;

        [Export("collectionView:layout:sizeForItemAtIndexPath:")]
        public CGSize GetSizeForItem(UICollectionView collectionView, UICollectionViewLayout layout, NSIndexPath indexPath)
            => new CGSize(cellWidth, cellHeight);
    }
}
