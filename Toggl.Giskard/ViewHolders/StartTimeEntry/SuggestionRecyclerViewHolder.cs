using System;
using Android.Runtime;
using Android.Views;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.MvvmCross.Themes;
using Toggl.Giskard.Extensions;

namespace Toggl.Giskard.ViewHolders
{

    public abstract class SuggestionRecyclerViewHolder<TSuggestion> : BaseRecyclerViewHolder<AutocompleteSuggestion>
        where TSuggestion : AutocompleteSuggestion
    {

        private View fadeView;
        private View separator;

        protected SuggestionRecyclerViewHolder(View itemView)
            : base(itemView)
        {
        }

        protected SuggestionRecyclerViewHolder(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override void InitializeViews()
        {
            fadeView = ItemView.FindViewById(Resource.Id.FadeView);
            separator = ItemView.FindViewById(Resource.Id.Separator);
        }

        public TSuggestion Suggestion => Item as TSuggestion;

        protected override void UpdateTheme(ITheme theme)
        {
            ItemView.SetBackgroundColor(theme.CellBackground.ToNativeColor());
            separator?.SetBackgroundColor(theme.Separator.ToNativeColor());
            if (fadeView == null) return;
            fadeView.Background = theme.CellBackground.ToTransparentGradient();
        }
    }
}
