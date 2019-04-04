using System;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Toggl.Foundation.MvvmCross.Themes;
using Toggl.Giskard.Extensions;

namespace Toggl.Giskard.ViewHolders
{
    public sealed class SimpleTextViewHolder<T> : BaseRecyclerViewHolder<T>
    {
        private TextView textView;
        private View fadeView;
        private View separator;
        private readonly int textViewResourceId;
        private readonly Func<T, string> transformFunction;

        public SimpleTextViewHolder(View itemView, int textViewResourceId, Func<T, string> transformFunction)
            : base(itemView)
        {
            this.textViewResourceId = textViewResourceId;
            this.transformFunction = transformFunction;
        }

        public SimpleTextViewHolder(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override void InitializeViews()
        {
            textView = ItemView.FindViewById<TextView>(textViewResourceId);
            fadeView = ItemView.FindViewById(Resource.Id.FadeView);
            separator = ItemView.FindViewById(Resource.Id.Separator);
        }

        protected override void UpdateView()
        {
            textView.Text = transformFunction(Item);
        }

        protected override void UpdateTheme(ITheme theme)
        {
            ItemView.SetBackgroundColor(theme.CellBackground.ToNativeColor());
            separator?.SetBackgroundColor(theme.Separator.ToNativeColor());
            if (fadeView == null) return;
            fadeView.Background = theme.CellBackground.ToTransparentGradient();
        }
    }
}
