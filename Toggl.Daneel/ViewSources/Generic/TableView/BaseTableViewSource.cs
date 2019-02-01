using System;
using System.Collections.Immutable;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public abstract class BaseTableViewSource<TModel> : UITableViewSource
    {
        public delegate UITableViewCell CellConfiguration(UITableView tableView, NSIndexPath indexPath, TModel model);
        public delegate UIView HeaderConfiguration(UITableView tableView, int section);

        private readonly CellConfiguration configureCell;

        protected IImmutableList<IImmutableList<TModel>> Sections { get; set; }

        public EventHandler<TModel> OnItemTapped { get; set; }
        public EventHandler<CGPoint> OnScrolled { get; set; }

        public HeaderConfiguration ConfigureHeader { get; set; }

        public BaseTableViewSource(CellConfiguration configureCell, IImmutableList<IImmutableList<TModel>> sections)
        {
            this.Sections = sections;
            this.configureCell = configureCell;
        }

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            tableView.DeselectRow(indexPath, true);
            OnItemTapped?.Invoke(this, Sections[indexPath.Section][indexPath.Row]);
        }

        public override void Scrolled(UIScrollView scrollView)
        {
            OnScrolled?.Invoke(this, scrollView.ContentOffset);
        }

        public override nint NumberOfSections(UITableView tableView)
            => Sections.Count;

        public override nint RowsInSection(UITableView tableview, nint section)
            => Sections[(int)section].Count;

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
            => configureCell(tableView, indexPath, Sections[indexPath.Section][indexPath.Row]);

        public override UIView GetViewForHeader(UITableView tableView, nint section)
            => ConfigureHeader == null ? null : ConfigureHeader(tableView, (int)section);
    }
}
