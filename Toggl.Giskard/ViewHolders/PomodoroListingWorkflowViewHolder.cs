using System;
using System.Linq;
using System.Reactive.Subjects;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Toggl.Multivac.Extensions;
using Toggl.Giskard.Views.EditDuration;
using Toggl.Giskard.Views.Pomodoro;
using Toggl.Foundation.Models.Pomodoro;

namespace Toggl.Giskard.ViewHolders
{
    public sealed class PomodoroListingWorkflowViewHolder : BaseRecyclerViewHolder<PomodoroWorkflow>
    {
        private TextView nameLabel;

        private LinearLayout startButton;
        private View startButtonIcon;
        private TextView startButtonLabel;
        private TextView totalDurationLabel;
        private PomodoroWorkflowView workflowView;

        private Subject<PomodoroWorkflow> workflowStartedSubject;

        public PomodoroListingWorkflowViewHolder(View itemView, Subject<PomodoroWorkflow> workflowStartedSubject) : base(itemView)
        {
            this.workflowStartedSubject = workflowStartedSubject;
        }

        public PomodoroListingWorkflowViewHolder(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership)
        {
        }

        protected override void InitializeViews()
        {
            nameLabel = ItemView.FindViewById<TextView>(Resource.Id.WorkflowNameLabel);

            startButton = ItemView.FindViewById<LinearLayout>(Resource.Id.StartButton);
            startButtonIcon = ItemView.FindViewById<View>(Resource.Id.StartButtonIcon);
            startButtonLabel = ItemView.FindViewById<TextView>(Resource.Id.StartButtonLabel);

            totalDurationLabel = ItemView.FindViewById<TextView>(Resource.Id.TotalDurationLabel);

            workflowView = ItemView.FindViewById<PomodoroWorkflowView>(Resource.Id.WorkflowView);

            startButton.Click += onStartButton;
        }

        private void onStartButton(object sender, EventArgs e)
        {
            workflowStartedSubject.OnNext(Item);
        }

        protected override void UpdateView()
        {
            nameLabel.Text = Item.Name;
            totalDurationLabel.Text = Item.Items.Sum(item => item.Duration).AsDurationString();
            workflowView.Update(Item.Items);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            startButton.Click -= onStartButton;
        }
    }
}
