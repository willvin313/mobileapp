using System;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Toggl.Foundation.MvvmCross.Themes;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Extensions;
using Toggl.Multivac;

namespace Toggl.Giskard.ViewHolders
{
    public sealed class WorkspaceSelectionViewHolder : BaseRecyclerViewHolder<SelectableWorkspaceViewModel>
    {
        public static WorkspaceSelectionViewHolder Create(View itemView)
            => new WorkspaceSelectionViewHolder(itemView);

        private View fadeView;
        private View separator;
        private ImageView checkedImage;
        private TextView workspaceName;

        public WorkspaceSelectionViewHolder(View itemView)
            : base(itemView)
        {
        }

        public WorkspaceSelectionViewHolder(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override void UpdateTheme(ITheme theme)
        {
            workspaceName.SetTextColor(theme.Text.ToNativeColor());
            ItemView.SetBackgroundColor(theme.Background.ToNativeColor());

            separator.SetBackgroundColor(theme.Separator.ToNativeColor());
            fadeView.Background = theme.Background.ToTransparentGradient();
        }

        protected override void InitializeViews()
        {
            fadeView = ItemView.FindViewById(Resource.Id.FadeView);
            separator = ItemView.FindViewById(Resource.Id.Separator);
            checkedImage = ItemView.FindViewById<ImageView>(Resource.Id.SettingsWorkspaceCellCheckedImageView);
            workspaceName = ItemView.FindViewById<TextView>(Resource.Id.SettingsWorkspaceCellWorkspaceNameTextView);
        }

        protected override void UpdateView()
        {
            workspaceName.Text = Item.WorkspaceName;
            checkedImage.Visibility = Item.Selected.ToVisibility(useGone: false);
        }
    }
}
