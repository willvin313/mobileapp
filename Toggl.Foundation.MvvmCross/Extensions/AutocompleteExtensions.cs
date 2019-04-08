using System.Collections.Generic;
using System.Linq;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.UI.Collections;
using Toggl.Foundation.Autocomplete;
using System.Collections.Immutable;
using Toggl.Foundation.Autocomplete.Span;

namespace Toggl.Foundation.UI.Extensions
{
    using WorkspaceGroupedSuggestionsCollection = WorkspaceGroupedCollection<AutocompleteSuggestion>;
    using WorkspaceSuggestionsGrouping = IGrouping<(long workspaceId, string workspaceName), AutocompleteSuggestion>;

    public static class AutocompleteExtensions
    {
        public static IEnumerable<WorkspaceGroupedSuggestionsCollection> GroupByWorkspace(
            this IEnumerable<AutocompleteSuggestion> suggestions)
            => suggestions
                .Where(suggestion => !string.IsNullOrEmpty(suggestion.WorkspaceName))
                .GroupBy(suggestion => (suggestion.WorkspaceId, suggestion.WorkspaceName))
                .Select(createWorkspaceGroupedCollection);

        public static IEnumerable<WorkspaceGroupedSuggestionsCollection> GroupByWorkspaceAddingNoProject(
            this IEnumerable<AutocompleteSuggestion> suggestions)
            => suggestions
                .GroupByWorkspace()
                .Select(withNoProject);

        public static IEnumerable<WorkspaceGroupedSuggestionsCollection> OrderByDefaultWorkspaceAndName(
            this IEnumerable<WorkspaceGroupedSuggestionsCollection> suggestions, long defaultWorkSpaceId)
        {
            return suggestions
                .OrderByDescending(suggestion => suggestion.WorkspaceId == defaultWorkSpaceId)
                .ThenBy(s => s.WorkspaceName);
        }

        private static WorkspaceGroupedSuggestionsCollection withNoProject(WorkspaceGroupedSuggestionsCollection collection)
        {
            collection.Insert(0, ProjectSuggestion.NoProject(collection.WorkspaceId, collection.WorkspaceName));
            return collection;
        }

        private static WorkspaceGroupedSuggestionsCollection createWorkspaceGroupedCollection(WorkspaceSuggestionsGrouping grouping)
            => new WorkspaceGroupedSuggestionsCollection(grouping.Key.workspaceName, grouping.Key.workspaceId, grouping);

        public static TextFieldInfo FromQuerySymbolSuggestion(
            this TextFieldInfo textFieldInfo,
            QuerySymbolSuggestion querySymbolSuggestion)
        {
            var result = textFieldInfo.AddQuerySymbol(querySymbolSuggestion.Symbol);
            return result;
        }

        public static TextFieldInfo FromTimeEntrySuggestion(
            this TextFieldInfo textFieldInfo,
            TimeEntrySuggestion timeEntrySuggestion)
        {
            var builder = ImmutableList.CreateBuilder<ISpan>();
            builder.Add(new TextSpan(timeEntrySuggestion.Description));

            if (timeEntrySuggestion.HasProject)
            {
                var projectSpan = new ProjectSpan(
                    timeEntrySuggestion.ProjectId.Value,
                    timeEntrySuggestion.ProjectName,
                    timeEntrySuggestion.ProjectColor,
                    timeEntrySuggestion.TaskId,
                    timeEntrySuggestion.TaskName
                );

                builder.Add(projectSpan);
            }

            builder.Add(new QueryTextSpan());

            return TextFieldInfo
                .Empty(timeEntrySuggestion.WorkspaceId)
                .ReplaceSpans(builder.ToImmutable());
        }

        public static TextFieldInfo FromProjectSuggestion(
            this TextFieldInfo textFieldInfo,
            ProjectSuggestion projectSuggestion)
        {
            var result = textFieldInfo.WithProject(
                projectSuggestion.WorkspaceId,
                projectSuggestion.ProjectId,
                projectSuggestion.ProjectName,
                projectSuggestion.ProjectColor,
                null,
                null
            );

            return result;
        }

        public static TextFieldInfo FromTaskSuggestion(
            this TextFieldInfo textFieldInfo,
            TaskSuggestion taskSuggestion)
        {
            var result = textFieldInfo.WithProject(
                taskSuggestion.WorkspaceId,
                taskSuggestion.ProjectId,
                taskSuggestion.ProjectName,
                taskSuggestion.ProjectColor,
                taskSuggestion.TaskId,
                taskSuggestion.Name
            );

            return result;
        }

        public static TextFieldInfo FromTagSuggestion(
            this TextFieldInfo textFieldInfo,
            TagSuggestion tagSuggestion)
        {
            var result = textFieldInfo.AddTag(tagSuggestion.TagId, tagSuggestion.Name);
            return result;
        }

        public static IEnumerable<ISpan> CollapseTextSpans(this IEnumerable<ISpan> spans)
        {
            var collapsed = new List<ISpan>();

            TextSpan previousTextSpan = null;

            foreach (var span in spans)
            {
                if (span is TextSpan == false && previousTextSpan != null)
                {
                    collapsed.Add(previousTextSpan);
                    previousTextSpan = null;
                }

                if (span is TextSpan textSpan)
                {
                    previousTextSpan = previousTextSpan == null
                        ? textSpan
                        : mergeTextSpans(previousTextSpan, textSpan);
                }
                else
                {
                    collapsed.Add(span);
                }
            }

            if (previousTextSpan != null)
            {
                collapsed.Add(previousTextSpan);
            }

            return collapsed;
        }

        private static TextSpan mergeTextSpans(TextSpan first, TextSpan second)
        {
            if (first is QueryTextSpan firstQuerySpan)
            {
                return new QueryTextSpan(first.Text + second.Text, firstQuerySpan.CursorPosition);
            }

            if (second is QueryTextSpan secondQuerySpan)
            {
                return new QueryTextSpan(first.Text + second.Text, first.Text.Length + secondQuerySpan.CursorPosition);
            }

            return new TextSpan(first.Text + second.Text);
        }
    }
}
