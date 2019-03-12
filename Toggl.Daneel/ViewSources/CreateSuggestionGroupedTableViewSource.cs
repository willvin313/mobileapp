using System;
using System.Linq;
using Foundation;
using MvvmCross.Commands;
using Toggl.Daneel.Views;
using Toggl.Daneel.Views.EntityCreation;
using Toggl.Foundation.MvvmCross.Collections;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public abstract class CreateSuggestionGroupedTableViewSource<T>
        : GroupedCollectionTableViewSource<WorkspaceGroupedCollection<T>, T>
        where T : class
    {
        protected const string CreateEntityCellIdentifier = nameof(CreateEntityViewCell);

        public string Text { get; set; }

        private bool suggestCreation;
        public bool SuggestCreation
        {
            get => suggestCreation;
            set
            {
                if (suggestCreation == value) return;

                suggestCreation = value;
                ReloadTableData();
            }
        }

        public IMvxCommand CreateCommand { get; set; }

        protected CreateSuggestionGroupedTableViewSource(UITableView tableView, string cellIdentifier, string headerCellIdentifier)
            : base(tableView, cellIdentifier, headerCellIdentifier)
        {
            tableView.RegisterNibForCellReuse(CreateEntityViewCell.Nib, CreateEntityCellIdentifier);
        }

        public override UIView GetViewForHeader(UITableView tableView, nint section)
        {
            if (!SuggestCreation) return base.GetViewForHeader(tableView, section);

            var actualSection = (int)section;
            if (actualSection == 0) return null;

            return base.GetViewForHeader(tableView, actualSection - 1);
        }

        public override nint NumberOfSections(UITableView tableView)
            => base.NumberOfSections(tableView) + (SuggestCreation ? 1 : 0);

        public override nint RowsInSection(UITableView tableview, nint section)
        {
            if (!SuggestCreation)
                return base.RowsInSection(tableview, section);

            if (section == 0) return 1;

            return base.RowsInSection(tableview, section - 1);
        }

        protected override object GetItemAt(NSIndexPath indexPath)
        {
            if (!SuggestCreation) return base.GetItemAt(indexPath);

            if (indexPath.Section == 0)
                return GetCreateSuggestionItem();

            return GroupedItems.ElementAtOrDefault(indexPath.Section - 1)?.ElementAtOrDefault((int)indexPath.Item);
        }

        protected abstract object GetCreateSuggestionItem();

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            if (SuggestCreation && indexPath.Section == 0 && indexPath.Row == 0)
            {
                CreateCommand.Execute();
                return;
            }

            base.RowSelected(tableView, indexPath);
        }

        public override nfloat GetHeightForHeader(UITableView tableView, nint section)
            => SuggestCreation && section == 0 ? 0 : 40;
    }
}
