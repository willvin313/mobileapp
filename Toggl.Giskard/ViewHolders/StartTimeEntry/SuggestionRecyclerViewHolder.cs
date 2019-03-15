using System;
using Android.Runtime;
using Android.Views;
using Toggl.Foundation.Autocomplete.Suggestions;

namespace Toggl.Giskard.ViewHolders
{

    public abstract class SuggestionRecyclerViewHolder<TSuggestion> : BaseRecyclerViewHolder<AutocompleteSuggestion>
        where TSuggestion : AutocompleteSuggestion
    {
        protected SuggestionRecyclerViewHolder(View itemView)
            : base(itemView)
        {
        }

        protected SuggestionRecyclerViewHolder(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        public TSuggestion Suggestion => Item as TSuggestion;
    }
}
