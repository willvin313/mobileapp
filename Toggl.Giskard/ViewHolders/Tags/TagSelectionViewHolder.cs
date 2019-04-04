using System;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Toggl.Foundation.MvvmCross.Themes;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Extensions;

namespace Toggl.Giskard.ViewHolders
{
    public sealed class TagSelectionViewHolder : BaseRecyclerViewHolder<SelectableTagBaseViewModel>
    {
        private ImageView selectedImageView;
        private TextView nameTextView;
        private View separator;
        private View fadeView;
        private View selectedIndicatorBackground;

        public TagSelectionViewHolder(View itemView) : base(itemView)
        {
        }

        public TagSelectionViewHolder(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership)
        {
        }

        protected override void InitializeViews()
        {
            selectedImageView = ItemView.FindViewById<ImageView>(Resource.Id.SelectedImageView);
            nameTextView = ItemView.FindViewById<TextView>(Resource.Id.NameTextView);
            separator = ItemView.FindViewById(Resource.Id.Separator);
            fadeView = ItemView.FindViewById(Resource.Id.FadeView);
            selectedIndicatorBackground = ItemView.FindViewById(Resource.Id.SelectedIndicatorBackground);
        }

        protected override void UpdateView()
        {
            nameTextView.Text = Item.Name;
            selectedImageView.Visibility = Item.Selected ? ViewStates.Visible : ViewStates.Gone;
        }

        protected override void UpdateTheme(ITheme theme)
        {
            base.UpdateTheme(theme);
            ItemView.SetBackgroundColor(theme.CellBackground.ToNativeColor());
            selectedIndicatorBackground.SetBackgroundColor(theme.CellBackground.ToNativeColor());
            nameTextView.SetTextColor(theme.Text.ToNativeColor());
            separator.SetBackgroundColor(theme.Separator.ToNativeColor());
            fadeView.Background = theme.CellBackground.ToTransparentGradient();
        }
    }
}
