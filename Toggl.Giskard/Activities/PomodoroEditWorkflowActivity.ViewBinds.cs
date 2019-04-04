using Android.Views;
using Android.Widget;
using MvvmCross.Droid.Support.V7.AppCompat;
using Android.Support.V7.Widget;
using Toggl.Foundation.MvvmCross.ViewModels;
using static Toggl.Giskard.Resource.Id;
using Toggl.Giskard.Views.Pomodoro;

namespace Toggl.Giskard.Activities
{
    public sealed partial class PomodoroEditWorkflowActivity : ReactiveActivity<PomodoroEditWorkflowViewModel>
    {
        private SeekBar durationSeekBar;
        private PomodoroWorkflowView workflowView;
        private EditText nameEditText;

        private View confirmButton;
        private View closeButton;

        private TextView workflowDurationTextView;
        private TextView segmentDurationTextView;

        private View addSegmentButton;
        private View deleteSegmentButton;

        private View workTypeButton;
        private View restTypeButton;

        protected override void InitializeViews()
        {
            durationSeekBar = FindViewById<SeekBar>(DurationSeekBar);
            workflowView = FindViewById<PomodoroWorkflowView>(WorkflowView);

            nameEditText = FindViewById<EditText>(NameEditText);

            closeButton = FindViewById(CloseButton);
            confirmButton = FindViewById(ConfirmButton);

            workflowDurationTextView = FindViewById<TextView>(WorkflowDuration);
            segmentDurationTextView = FindViewById<TextView>(SegmentDuration);

            addSegmentButton = FindViewById(AddSegmentButton);
            deleteSegmentButton = FindViewById(DeleteSegmentButton);

            workTypeButton = FindViewById(WorkTypeButton);
            restTypeButton = FindViewById(RestTypeButton);
        }
    }
}
