using Android.Views;
using Android.Widget;
using Toggl.Giskard.Views;

namespace Toggl.Giskard.Activities
{
    public sealed partial class EditProjectActivity
    {
        private View errorText;
        private View editWorkspace;
        private View editClientView;
        private View toggleIsPrivateView;
        private View editProjectColorArrow;
        private Switch isPrivateSwitch;
        private TextView createProjectButton;
        private TextView workspaceNameLabel;
        private TextView clientNameTextView;
        private TextView editProjectProjectName;
        private CircleView editProjectColorCircle;

        protected override void InitializeViews()
        {
            errorText = FindViewById(Resource.Id.ErrorText);
            editWorkspace = FindViewById(Resource.Id.EditWorkspace);
            editClientView = FindViewById(Resource.Id.EditClientView);
            toggleIsPrivateView = FindViewById(Resource.Id.ToggleIsPrivateView);
            isPrivateSwitch = FindViewById<Switch>(Resource.Id.IsPrivateSwitch);
            editProjectColorArrow = FindViewById(Resource.Id.EditProjectColorArrow);
            clientNameTextView = FindViewById<TextView>(Resource.Id.ClientNameTextView);
            workspaceNameLabel = FindViewById<TextView>(Resource.Id.WorkspaceNameLabel);
            createProjectButton = FindViewById<TextView>(Resource.Id.CreateProjectButton);
            editProjectProjectName = FindViewById<TextView>(Resource.Id.EditProjectProjectName);
            editProjectColorCircle = FindViewById<CircleView>(Resource.Id.EditProjectColorCircle);
        }
    }
}
