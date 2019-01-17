using System;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MvvmCross.Plugin.Color.Platforms.Android;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Views;

namespace Toggl.Giskard.ViewHolders
{
    public sealed class ColorSelectionViewHolder : BaseRecyclerViewHolder<SelectableColorViewModel>
    {
        public static ColorSelectionViewHolder Create(View itemView)
            => new ColorSelectionViewHolder(itemView);

        private CircleView colorCircle;
        private ImageView selectionCircle;

        public ColorSelectionViewHolder(View itemView)
            : base(itemView)
        {
        }

        public ColorSelectionViewHolder(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override void InitializeViews()
        {
            colorCircle = ItemView.FindViewById<CircleView>(Resource.Id.EditProjectColorCircle);
            selectionCircle = ItemView.FindViewById<ImageView>(Resource.Id.EditProjectColorSelectionCircle);
        }

        protected override void UpdateView()
        {
            colorCircle.SetCircleColor(Item.Color.ToNativeColor());
            selectionCircle.Visibility = Item.Selected.ToVisibility(useGone: false);
        }
    }
}
