using System;
using Android.Graphics.Drawables;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MvvmCross.Plugin.Color.Platforms.Android;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Extensions;

namespace Toggl.Giskard.ViewHolders
{
    public sealed class QuickSelectShortcutViewHolder : BaseRecyclerViewHolder<QuickSelectShortcut>
    {
        public static QuickSelectShortcutViewHolder Create(View itemView)
            => new QuickSelectShortcutViewHolder(itemView);

        private TextView shortcutTextView;

        public QuickSelectShortcutViewHolder(View itemView)
            : base(itemView)
        {
        }

        public QuickSelectShortcutViewHolder(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override void InitializeViews()
        {
            shortcutTextView = ItemView.FindViewById<TextView>(Resource.Id.ReportsCalendarShortcutTextView);
        }

        protected override void UpdateView()
        {
            shortcutTextView.Text = Item.Title;
            shortcutTextView.SetTextColor(Item.TitleColor.ToNativeColor());

            if (shortcutTextView.Background is GradientDrawable drawable)
            {
                drawable.SetColor(Item.BackgroundColor.ToNativeColor().ToArgb());
                drawable.InvalidateSelf();
            }
        }
    }
}
