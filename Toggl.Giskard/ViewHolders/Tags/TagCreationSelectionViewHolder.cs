using System;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Themes;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Extensions;

namespace Toggl.Giskard.ViewHolders
{
    public sealed class TagCreationSelectionViewHolder : BaseRecyclerViewHolder<SelectableTagBaseViewModel>
    {
        private TextView creationTextView;
        private View separator;
        private View fadeView;
        public TagCreationSelectionViewHolder(View itemView) : base(itemView)
        {
        }

        public TagCreationSelectionViewHolder(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership)
        {
        }

        protected override void InitializeViews()
        {
            creationTextView = ItemView.FindViewById<TextView>(Resource.Id.CreationLabel);
            separator = ItemView.FindViewById(Resource.Id.Separator);
            fadeView = ItemView.FindViewById(Resource.Id.FadeView);
        }

        protected override void UpdateView()
        {
            creationTextView.Text = $"{Resources.CreateTag} \"{Item.Name.Trim()}\"";
        }

        protected override void UpdateTheme(ITheme theme)
        {
            base.UpdateTheme(theme);
            ItemView.SetBackgroundColor(theme.CellBackground.ToNativeColor());
            creationTextView.SetTextColor(theme.Text.ToNativeColor());
            separator.SetBackgroundColor(theme.Separator.ToNativeColor());
            fadeView.Background = theme.CellBackground.ToTransparentGradient();
        }
    }
}
