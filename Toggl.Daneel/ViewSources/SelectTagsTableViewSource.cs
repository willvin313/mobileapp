﻿using Foundation;
using Toggl.Daneel.Cells;
using Toggl.Daneel.Views.Tag;
using Toggl.Foundation.UI.Collections;
using Toggl.Foundation.UI.ViewModels;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    using TagsSection = SectionModel<string, SelectableTagBaseViewModel>;

    public sealed class SelectTagsTableViewSource : BaseTableViewSource<TagsSection, string, SelectableTagBaseViewModel>
    {
        private const int rowHeight = 48;

        public SelectTagsTableViewSource(UITableView tableView)
        {
            tableView.RowHeight = rowHeight;
            tableView.RegisterNibForCellReuse(NewTagViewCell.Nib, NewTagViewCell.Identifier);
            tableView.RegisterNibForCellReuse(CreateTagViewCell.Nib, CreateTagViewCell.Identifier);
            tableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
            tableView.Source = this;
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var model = ModelAt(indexPath);
            var identifier = model is SelectableTagCreationViewModel ? CreateTagViewCell.Identifier : NewTagViewCell.Identifier;
            var cell = (BaseTableViewCell<SelectableTagBaseViewModel>)tableView.DequeueReusableCell(identifier);
            cell.Item = model;
            return cell;
        }
    }
}
