using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.Support.V7.Widget;
using Android.Views;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.ViewHolders;
using Toggl.Multivac.Extensions;
using Toggl.Foundation;
using Toggl.Giskard.ViewHelpers;
using System.Collections.Immutable;
using Toggl.Foundation.Suggestions;
using Android.Support.V7.Util;

namespace Toggl.Giskard.Adapters
{
    public class MainRecyclerAdapter : ReactiveSectionedRecyclerAdapter<TimeEntryViewModel, TimeEntryViewData, TimeEntryCollectionViewModel, MainLogCellViewHolder, MainLogSectionViewHolder>
    {
        public const int SuggestionViewType = 2;
        public const int UserFeedbackViewType = 3;
        public const int SuggestionHeaderViewType = 4;

        private readonly ITimeService timeService;

        private bool isRatingViewVisible = false;

        public IObservable<TimeEntryViewModel> TimeEntryTaps
            => timeEntryTappedSubject.Select(item => item.TimeEntryViewModel).AsObservable();

        public IObservable<TimeEntryViewModel> ContinueTimeEntrySubject
            => continueTimeEntrySubject.AsObservable();

        public IObservable<TimeEntryViewModel> DeleteTimeEntrySubject
            => deleteTimeEntrySubject.AsObservable();

        public IObservable<Suggestion> SuggestionTappedObservable
            => suggestionTappedSubject.AsObservable();

        public SuggestionsViewModel SuggestionsViewModel { get; set; }
        public RatingViewModel RatingViewModel { get; set; }

        public IStopwatchProvider StopwatchProvider { get; set; }

        private Subject<TimeEntryViewData> timeEntryTappedSubject = new Subject<TimeEntryViewData>();
        private Subject<TimeEntryViewModel> continueTimeEntrySubject = new Subject<TimeEntryViewModel>();
        private Subject<TimeEntryViewModel> deleteTimeEntrySubject = new Subject<TimeEntryViewModel>();
        private Subject<Suggestion> suggestionTappedSubject = new Subject<Suggestion>();

        private IImmutableList<Suggestion> currentSuggestions;

        private IDisposable suggestionsSubscription;

        private bool haveSuggestions => currentSuggestions?.Any() ?? false;

        public MainRecyclerAdapter(
            ObservableGroupedOrderedCollection<TimeEntryViewModel> items,
            ITimeService timeService,
            SuggestionsViewModel suggestionsViewModel)
            : base(items)
        {
            this.timeService = timeService;

            SuggestionsViewModel = suggestionsViewModel;

            suggestionsSubscription = SuggestionsViewModel.Suggestions.Subscribe(suggestions =>
            {
                lock(CollectionUpdateLock)
                {
                    var diffCallback = new SuggestionsDiffCallback(currentSuggestions, suggestions);
                    currentSuggestions = suggestions;
                    var diff = DiffUtil.CalculateDiff(diffCallback);
                    diff.DispatchUpdatesTo(this);
                }
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            suggestionsSubscription.Dispose();
        }

        public void ContinueTimeEntry(int position)
        {
            var continuedTimeEntry = getItemAt(position);
            NotifyItemChanged(position);
            continueTimeEntrySubject.OnNext(continuedTimeEntry);
        }

        public void DeleteTimeEntry(int position)
        {
            var deletedTimeEntry = getItemAt(position);
            deleteTimeEntrySubject.OnNext(deletedTimeEntry);
        }

        public override int HeaderOffset
        {
            get
            {
                var headerOffset = currentSuggestions?.Count ?? 0;

                //+1 for the suggestion header
                if (haveSuggestions)
                    headerOffset++;

                if (isRatingViewVisible)
                    headerOffset++;
                return headerOffset;
            }
        }

        protected override bool TryBindCustomViewType(RecyclerView.ViewHolder holder, int position)
        {
            if (holder is MainLogSuggestionItemViewHolder suggestionviewHolder)
            {
                suggestionviewHolder.Item = currentSuggestions[position - 1];
                return true;
            }

            return holder is MainLogUserFeedbackViewHolder
                || holder is MainSuggestionsHeaderViewHolder;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            if (viewType == SuggestionHeaderViewType)
            {
                var view = LayoutInflater.FromContext(parent.Context).Inflate(Resource.Layout.MainSuggestionsHeader, parent, false);
                return new MainSuggestionsHeaderViewHolder(view);
            }

            if (viewType == SuggestionViewType)
            {
                var view = LayoutInflater.FromContext(parent.Context).Inflate(Resource.Layout.MainSuggestionsCard, parent, false);
                var suggestionViewHolder = new MainLogSuggestionItemViewHolder(view);
                suggestionViewHolder.TappedSubject = suggestionTappedSubject;
                return suggestionViewHolder;
            }

            if (viewType == UserFeedbackViewType)
            {
                var suggestionsView = LayoutInflater.FromContext(parent.Context).Inflate(Resource.Layout.MainUserFeedbackCard, parent, false);
                var userFeedbackViewHolder = new MainLogUserFeedbackViewHolder(suggestionsView, RatingViewModel);
                return userFeedbackViewHolder;
            }

            return base.OnCreateViewHolder(parent, viewType);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (holder is MainLogSectionViewHolder mainLogHeader)
                mainLogHeader.Now = timeService.CurrentDateTime;

            var stopwatchForViewHolder = createStopwatchFor(holder);
            stopwatchForViewHolder?.Start();
            base.OnBindViewHolder(holder, position);
            stopwatchForViewHolder?.Stop();
        }

        private IStopwatch createStopwatchFor(RecyclerView.ViewHolder holder)
        {
            switch (holder)
            {
                case MainLogCellViewHolder _:
                    return StopwatchProvider.MaybeCreateStopwatch(MeasuredOperation.BindMainLogItemVH, probability: 0.1F);

                case MainLogSectionViewHolder _:
                    return StopwatchProvider.MaybeCreateStopwatch(MeasuredOperation.BindMainLogSectionVH, probability: 0.5F);

                default:
                    return StopwatchProvider.Create(MeasuredOperation.BindMainLogSuggestionsVH);
            }
        }

        public override int GetItemViewType(int position)
        {
            var suggestionCount = currentSuggestions?.Count ?? 0;
            if (haveSuggestions)
            {
                if (position == 0)
                    return SuggestionHeaderViewType;

                if (position <= suggestionCount)
                    return SuggestionViewType;
            }

            if (isRatingViewVisible && position == suggestionCount + 1)
                return UserFeedbackViewType;

            return base.GetItemViewType(position);
        }

        protected override MainLogSectionViewHolder CreateHeaderViewHolder(ViewGroup parent)
        {
            var mainLogSectionStopwatch = StopwatchProvider.Create(MeasuredOperation.CreateMainLogSectionViewHolder);
            mainLogSectionStopwatch.Start();
            var mainLogSectionViewHolder = new MainLogSectionViewHolder(LayoutInflater.FromContext(parent.Context)
                .Inflate(Resource.Layout.MainLogHeader, parent, false));
            mainLogSectionViewHolder.Now = timeService.CurrentDateTime;
            mainLogSectionStopwatch.Stop();
            return mainLogSectionViewHolder;
        }

        public void SetupRatingViewVisibility(bool isVisible)
        {
            if (isRatingViewVisible == isVisible)
                return;

            isRatingViewVisible = isVisible;
            NotifyDataSetChanged();
        }

        protected override MainLogCellViewHolder CreateItemViewHolder(ViewGroup parent)
        {
            var mainLogCellStopwatch = StopwatchProvider.Create(MeasuredOperation.CreateMainLogItemViewHolder);
            mainLogCellStopwatch.Start();
            var mainLogCellViewHolder = new MainLogCellViewHolder(LayoutInflater.FromContext(parent.Context).Inflate(Resource.Layout.MainLogCell, parent, false))
            {
                TappedSubject = timeEntryTappedSubject,
                ContinueButtonTappedSubject = continueTimeEntrySubject
            };
            mainLogCellStopwatch.Stop();
            return mainLogCellViewHolder;
        }

        protected override long IdFor(TimeEntryViewModel item)
            => item.Id;

        protected override long IdForSection(IReadOnlyList<TimeEntryViewModel> section)
            => section.First().StartTime.Date.GetHashCode();

        protected override TimeEntryViewData Wrap(TimeEntryViewModel item)
            => new TimeEntryViewData(item);

        protected override TimeEntryCollectionViewModel Wrap(IReadOnlyList<TimeEntryViewModel> section)
            => new TimeEntryCollectionViewModel(section);

        protected override bool AreItemContentsTheSame(TimeEntryViewModel item1, TimeEntryViewModel item2)
            => item1 == item2;

        protected override bool AreSectionsRepresentationsTheSame(IReadOnlyList<TimeEntryViewModel> one, IReadOnlyList<TimeEntryViewModel> other)
        {
            var oneFirst = one.FirstOrDefault()?.StartTime.Date;
            var otherFirst = other.FirstOrDefault()?.StartTime.Date;
            return (oneFirst != null || otherFirst != null)
                   && oneFirst == otherFirst
                   && one.ContainsExactlyAll(other);
        }

        private sealed class SuggestionsDiffCallback : DiffUtil.Callback
        {
            private readonly IImmutableList<Suggestion> oldSuggestions;
            private readonly IImmutableList<Suggestion> newSuggestions;

            //To account for suggestion header
            private const int headerOffset = 1;

            public override int NewListSize
            {
                get
                {
                    var suggestionCount = newSuggestions.Count;
                    if (suggestionCount > 0)
                        suggestionCount++;
                    return suggestionCount;
                }
            }

            public override int OldListSize
            {
                get
                {
                    if (oldSuggestions == null)
                        return 0;
                    return oldSuggestions.Count + headerOffset;
                }
            }

            public SuggestionsDiffCallback(IImmutableList<Suggestion> oldSuggestions, IImmutableList<Suggestion> newSuggestions)
            {
                this.oldSuggestions = oldSuggestions;
                this.newSuggestions = newSuggestions;
            }

            public override bool AreContentsTheSame(int oldItemPosition, int newItemPosition)
            {
                if (oldItemPosition < headerOffset || newItemPosition < headerOffset)
                {
                    return oldItemPosition == newItemPosition;
                }

                var oldItem = oldSuggestions[oldItemPosition - headerOffset];
                var newItem = newSuggestions[newItemPosition - headerOffset];

                //Compare just the fields that we display in UI
                return oldItem.Description == newItem.Description
                    && oldItem.ProjectName == newItem.ProjectName
                    && oldItem.TaskName == newItem.TaskName
                    && oldItem.ClientName == newItem.ClientName
                    && oldItem.ProjectColor == newItem.ProjectColor;
            }

            public override bool AreItemsTheSame(int oldItemPosition, int newItemPosition)
            {
                if (oldItemPosition < headerOffset || newItemPosition < headerOffset)
                {
                    return oldItemPosition == newItemPosition;
                }

                var oldItem = oldSuggestions[oldItemPosition - headerOffset];
                var newItem = newSuggestions[newItemPosition - headerOffset];

                return oldItem.Equals(newItem);
            }
        }
    }
}