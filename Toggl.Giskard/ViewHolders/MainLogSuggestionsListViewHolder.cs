using System;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Foundation.UI.ViewModels;
using Toggl.Foundation.Suggestions;
using Toggl.Giskard.Adapters;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.ViewHelpers;
using Toggl.Multivac.Extensions;
using TogglResources = Toggl.Foundation.Resources;

namespace Toggl.Giskard.ViewHolders
{
    public class MainLogSuggestionsListViewHolder : RecyclerView.ViewHolder
    {
        private SuggestionsViewModel suggestionsViewModel;

        private TextView hintTextView;
        private TextView indicatorTextView;
        private RecyclerView suggestionsRecyclerView;
        private MainSuggestionsRecyclerAdapter mainSuggestionsRecyclerAdapter;

        public CompositeDisposable DisposeBag { get; } = new CompositeDisposable();

        public MainLogSuggestionsListViewHolder(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public MainLogSuggestionsListViewHolder(View itemView, SuggestionsViewModel suggestionsViewModel) : base(itemView)
        {
            this.suggestionsViewModel = suggestionsViewModel;

            hintTextView = ItemView.FindViewById<TextView>(Resource.Id.SuggestionsHintTextView);
            indicatorTextView = ItemView.FindViewById<TextView>(Resource.Id.SuggestionsIndicatorTextView);
            suggestionsRecyclerView = ItemView.FindViewById<RecyclerView>(Resource.Id.SuggestionsRecyclerView);

            suggestionsRecyclerView.SetLayoutManager(new LinearLayoutManager(ItemView.Context, LinearLayoutManager.Horizontal, false));
            var snapMargin = 16.DpToPixels(ItemView.Context);
            var snapHelper = new SuggestionsRecyclerViewSnapHelper(snapMargin);
            snapHelper.AttachToRecyclerView(suggestionsRecyclerView);


            mainSuggestionsRecyclerAdapter = new MainSuggestionsRecyclerAdapter();

            mainSuggestionsRecyclerAdapter.SuggestionTaps
                .Subscribe(suggestionsViewModel.StartTimeEntry.Inputs)
                .DisposedBy(DisposeBag);

            suggestionsRecyclerView.SetAdapter(mainSuggestionsRecyclerAdapter);

            suggestionsViewModel.Suggestions
                .Subscribe(onSuggestionsChanged)
                .DisposedBy(DisposeBag);

            suggestionsViewModel.IsEmpty
                .Invert()
                .Subscribe(updateViewVisibility)
                .DisposedBy(DisposeBag);

            var suggestionCount = suggestionsViewModel.Suggestions.Select(s => s.Length);
            var currentIndexAndSuggestionCount =
                Observable.CombineLatest(snapHelper.CurrentIndexObservable, suggestionCount,
                    (currIndx, count) => (currIndx, count));

            currentIndexAndSuggestionCount.Do(tuple =>
            {
                var currIdx = tuple.Item1;
                var count = tuple.Item2;
                onCurrentSuggestionIndexChanged(count, currIdx);
            })
            .Subscribe()
            .DisposedBy(DisposeBag);
        }

        private void updateViewVisibility(bool visible)
        {
            if (visible)
            {
                hintTextView.Visibility = ViewStates.Visible;
                suggestionsRecyclerView.Visibility = ViewStates.Visible;
            }
            else
            {
                hintTextView.Visibility = ViewStates.Gone;
                indicatorTextView.Visibility = ViewStates.Gone;
                suggestionsRecyclerView.Visibility = ViewStates.Gone;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            DisposeBag.Dispose();
        }

        private void onSuggestionsChanged(Suggestion[] suggestions)
        {
            mainSuggestionsRecyclerAdapter.UpdateDataset(suggestions.ToImmutableList());
        }

        private void onCurrentSuggestionIndexChanged(int numberOfSuggestions, int currentIndex)
        {
            switch (numberOfSuggestions)
            {
                case 0:
                    return;

                case 1:
                    hintTextView.Text = TogglResources.WorkingOnThis;
                    indicatorTextView.Visibility = ViewStates.Gone;
                    break;

                default:
                    var indicatorText = $"{currentIndex} {TogglResources.Of.ToUpper()} {numberOfSuggestions}";
                    hintTextView.Text = TogglResources.WorkingOnThese;
                    indicatorTextView.Visibility = ViewStates.Visible;
                    indicatorTextView.Text = indicatorText;
                    break;
            }
        }
    }
}
