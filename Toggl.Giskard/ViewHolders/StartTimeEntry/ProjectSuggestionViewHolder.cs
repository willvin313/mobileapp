using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Extensions.Reactive;
using static Toggl.Giskard.Resource.Id;

namespace Toggl.Giskard.ViewHolders
{
    public class ProjectSuggestionViewHolder : SuggestionRecyclerViewHolder<ProjectSuggestion>
    {
        private View caret;
        private View toggleTasksButton;

        private TextView taskLabel;
        private TextView projectLabel;
        private TextView taskCountLabel;
        private TextView clientNameLabel;

        private IDisposable toggleTasksDisposable;
        private readonly ISubject<ProjectSuggestion> toggleTasksSubject;
        
        public ProjectSuggestionViewHolder(View itemView, ISubject<ProjectSuggestion> toggleTasksSubject)
            : base(itemView)
        {
            this.toggleTasksSubject = toggleTasksSubject;
        }

        protected override void InitializeViews()
        {
            caret = ItemView.FindViewById(Caret);
            taskLabel = ItemView.FindViewById<TextView>(TaskLabel);
            toggleTasksButton = ItemView.FindViewById(ToggleTasksButton);
            projectLabel = ItemView.FindViewById<TextView>(ProjectLabel);
            taskCountLabel = ItemView.FindViewById<TextView>(TaskCountLabel);
            clientNameLabel = ItemView.FindViewById<TextView>(ClientNameLabel);

            toggleTasksDisposable = Observable.Merge(caret.Rx().Tap(), taskLabel.Rx().Tap())
                .Select(_ => Suggestion)
                .Subscribe(toggleTasksSubject);
        }

        protected override void UpdateView()
        {
            var projectColor = Color.ParseColor(Suggestion.ProjectColor);
            projectLabel.Text = Suggestion.ProjectName;
            projectLabel.SetTextColor(projectColor);

            clientNameLabel.Text = Suggestion.ClientName ?? "";
            clientNameLabel.Visibility = string.IsNullOrEmpty(Suggestion.ClientName) ? ViewStates.Gone : ViewStates.Visible;

            taskCountLabel.Text = Suggestion.FormattedNumberOfTasks();

            var caretAngle = Suggestion.TasksVisible ? 180.0f : 0.0f;
            caret.Visibility = Suggestion.HasTasks.ToVisibility();
            caret.Animate().SetDuration(1).Rotation(caretAngle);

            toggleTasksButton.Visibility = Suggestion.HasTasks.ToVisibility();
        }
    }
}
