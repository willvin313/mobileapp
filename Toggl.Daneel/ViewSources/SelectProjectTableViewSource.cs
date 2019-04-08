﻿using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Foundation;
using Toggl.Daneel.Views.EntityCreation;
using Toggl.Daneel.Views.StartTimeEntry;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.UI.Collections;
using Toggl.Foundation.UI.Helper;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    using ProjectSection = SectionModel<string, AutocompleteSuggestion>;

    public sealed class SelectProjectTableViewSource : BaseTableViewSource<ProjectSection, string, AutocompleteSuggestion>
    {
        private const int headerHeight = 40;

        public bool UseGrouping { get; set; }

        private readonly ISubject<ProjectSuggestion> toggleTaskSuggestionsSubject = new Subject<ProjectSuggestion>();
        public IObservable<ProjectSuggestion> ToggleTaskSuggestions => toggleTaskSuggestionsSubject.AsObservable();

        public void RegisterViewCells(UITableView tableView)
        {
            tableView.RegisterNibForCellReuse(ReactiveProjectSuggestionViewCell.Nib, ReactiveProjectSuggestionViewCell.Key);
            tableView.RegisterNibForCellReuse(ReactiveTaskSuggestionViewCell.Nib, ReactiveTaskSuggestionViewCell.Key);
            tableView.RegisterNibForCellReuse(CreateEntityViewCell.Nib, CreateEntityViewCell.Key);
            tableView.RegisterNibForHeaderFooterViewReuse(ReactiveWorkspaceHeaderViewCell.Nib, ReactiveWorkspaceHeaderViewCell.Key);
        }

        public override nfloat GetHeightForHeader(UITableView tableView, nint section)
        {
            if (!UseGrouping)
                return 0;

            var header = Sections[(int)section].Header;
            return header == null ? 0 : headerHeight;
        }

        public override UIView GetViewForHeader(UITableView tableView, nint section)
        {
            var header = HeaderOf(section);

            if (string.IsNullOrEmpty(header))
                return null;

            var headerCell = (ReactiveWorkspaceHeaderViewCell)tableView.DequeueReusableHeaderFooterView(ReactiveWorkspaceHeaderViewCell.Key);
            headerCell.WorkspaceName = header;
            return headerCell;
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var autocompleteSuggestion = ModelAt(indexPath);

            switch (autocompleteSuggestion)
            {
                case ProjectSuggestion projectSuggestion:
                    var projectCell = (ReactiveProjectSuggestionViewCell)tableView.DequeueReusableCell(ReactiveProjectSuggestionViewCell.Key, indexPath);
                    projectCell.Item = projectSuggestion;
                    projectCell.ToggleTaskSuggestions.Subscribe(toggleTaskSuggestionsSubject).DisposedBy(projectCell.DisposeBag);
                    updateSeparatorVisibility(tableView, projectCell, indexPath);
                    return projectCell;

                case TaskSuggestion taskSuggestion:
                    var taskCell = (ReactiveTaskSuggestionViewCell)tableView.DequeueReusableCell(ReactiveTaskSuggestionViewCell.Key, indexPath);
                    taskCell.Item = taskSuggestion;
                    return taskCell;

                case CreateEntitySuggestion createEntitySuggestion:
                    var createEntityCell = (CreateEntityViewCell)tableView.DequeueReusableCell(CreateEntityViewCell.Key, indexPath);
                    createEntityCell.Item = createEntitySuggestion;
                    return createEntityCell;

                default:
                    throw new Exception("Unexpected item type encountered");
            }
        }

        private void updateSeparatorVisibility(UITableView tableView, ReactiveProjectSuggestionViewCell cell, NSIndexPath indexPath)
        {
            var previousItemPath = NSIndexPath.FromItemSection(indexPath.Item - 1, indexPath.Section);
            var previous = ModelAtOrDefault(previousItemPath);
            var previousIsTask = previous is TaskSuggestion;
            cell.TopSeparatorHidden = !previousIsTask;

            var nextItemPath = NSIndexPath.FromItemSection(indexPath.Item + 1, indexPath.Section);
            var next = ModelAtOrDefault(nextItemPath);
            var isLastItemInSection = next == null;
            var isLastSection = indexPath.Section == tableView.NumberOfSections() - 1;
            cell.BottomSeparatorHidden = isLastItemInSection && !isLastSection;
        }
    }
}
