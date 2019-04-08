﻿using System;
using System.Collections.Immutable;
using Foundation;
using Toggl.Daneel.Cells;
using Toggl.Daneel.Cells.Calendar;
using Toggl.Foundation.UI.Collections;
using Toggl.Foundation.UI.ViewModels.Calendar;
using Toggl.Foundation.UI.ViewModels.Selectable;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    using CalendarSectionModel = SectionModel<UserCalendarSourceViewModel, SelectableUserCalendarViewModel>;
    public sealed class SelectUserCalendarsTableViewSource : BaseTableViewSource<CalendarSectionModel, UserCalendarSourceViewModel, SelectableUserCalendarViewModel>
    {
        private const int rowHeight = 48;
        private const int headerHeight = 48;

        public UIColor SectionHeaderBackgroundColor { get; set; } = UIColor.White;

        public SelectUserCalendarsTableViewSource(UITableView tableView)
            : base(ImmutableList<SectionModel<UserCalendarSourceViewModel, SelectableUserCalendarViewModel>>.Empty)
        {
            tableView.RegisterNibForCellReuse(SelectableUserCalendarViewCell.Nib, SelectableUserCalendarViewCell.Identifier);
            tableView.RegisterNibForHeaderFooterViewReuse(UserCalendarListHeaderViewCell.Nib, UserCalendarListHeaderViewCell.Identifier);
            tableView.SectionHeaderHeight = headerHeight;
            tableView.RowHeight = rowHeight;
        }

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            base.RowSelected(tableView, indexPath);

            var cell = (SelectableUserCalendarViewCell)tableView.CellAt(indexPath);
            cell.ToggleSwitch();

            tableView.DeselectRow(indexPath, true);
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var cell = (BaseTableViewCell<SelectableUserCalendarViewModel>)tableView.DequeueReusableCell(
                SelectableUserCalendarViewCell.Identifier, indexPath);
            cell.Item = ModelAt(indexPath);
            return cell;
        }

        public override UIView GetViewForHeader(UITableView tableView, nint section)
        {
            var header = tableView.DequeueReusableHeaderFooterView(UserCalendarListHeaderViewCell.Identifier) as UserCalendarListHeaderViewCell;
            header.Item = HeaderOf(section);
            header.ContentView.BackgroundColor = SectionHeaderBackgroundColor;
            return header;
        }
    }
}
