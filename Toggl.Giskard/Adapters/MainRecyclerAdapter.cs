using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.Support.V7.Widget;
using Android.Views;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.UI.Collections;
using Toggl.Foundation.UI.ViewModels;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.ViewHolders;
using Toggl.Multivac.Extensions;
using Toggl.Foundation;
using Toggl.Foundation.UI.ViewModels.TimeEntriesLog;
using Toggl.Giskard.ViewHelpers;
using System.Reactive.Disposables;
using Toggl.Foundation.UI.ViewModels.TimeEntriesLog.Identity;
using Toggl.Foundation.Analytics;

namespace Toggl.Giskard.Adapters
{
    public class MainRecyclerAdapter : ReactiveSectionedRecyclerAdapter<LogItemViewModel, TimeEntryViewData, DaySummaryViewModel, DaySummaryViewModel, MainLogCellViewHolder, MainLogSectionViewHolder, IMainLogKey>
    {
        public const int SuggestionViewType = 2;
        public const int UserFeedbackViewType = 3;

        private readonly ITimeService timeService;

        private bool isRatingViewVisible = false;

        public IObservable<GroupId> ToggleGroupExpansion
            => toggleGroupExpansionSubject.AsObservable();

        public IObservable<LogItemViewModel> TimeEntryTaps
            => timeEntryTappedSubject.Select(item => item.ViewModel).AsObservable();

        public IObservable<(LogItemViewModel LogItem, ContinueTimeEntryMode ContinueMode)> ContinueTimeEntry
            => continueTimeEntrySubject.AsObservable();

        public IObservable<LogItemViewModel> DeleteTimeEntrySubject
            => deleteTimeEntrySubject.AsObservable();

        public SuggestionsViewModel SuggestionsViewModel { get; set; }
        public RatingViewModel RatingViewModel { get; set; }

        public IStopwatchProvider StopwatchProvider { get; set; }

        private readonly Subject<GroupId> toggleGroupExpansionSubject = new Subject<GroupId>();
        private readonly Subject<TimeEntryViewData> timeEntryTappedSubject = new Subject<TimeEntryViewData>();
        private readonly Subject<(LogItemViewModel, ContinueTimeEntryMode)> continueTimeEntrySubject = new Subject<(LogItemViewModel, ContinueTimeEntryMode)>();
        private readonly Subject<LogItemViewModel> deleteTimeEntrySubject = new Subject<LogItemViewModel>();

        public MainRecyclerAdapter(ITimeService timeService)
        {
            this.timeService = timeService;
        }

        public void ContinueTimeEntryBySwiping(int position)
        {
            var continuedTimeEntry = GetItemAt(position);
            NotifyItemChanged(position);

            var continueMode = continuedTimeEntry.IsTimeEntryGroupHeader
                ? ContinueTimeEntryMode.TimeEntriesGroupSwipe
                : ContinueTimeEntryMode.SingleTimeEntrySwipe;

            continueTimeEntrySubject.OnNext((continuedTimeEntry, continueMode));
        }

        public void DeleteTimeEntry(int position)
        {
            var deletedTimeEntry = GetItemAt(position);
            deleteTimeEntrySubject.OnNext(deletedTimeEntry);
        }

        public override int HeaderOffset => isRatingViewVisible ? 2 : 1;

        protected override bool TryBindCustomViewType(RecyclerView.ViewHolder holder, int position)
        {
            return holder is MainLogSuggestionsListViewHolder
                || holder is MainLogUserFeedbackViewHolder;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            if (viewType == SuggestionViewType)
            {
                var mainLogSuggestionsStopwatch = StopwatchProvider.Create(MeasuredOperation.CreateMainLogSuggestionsViewHolder);
                mainLogSuggestionsStopwatch.Start();
                var suggestionsView = LayoutInflater.FromContext(parent.Context).Inflate(Resource.Layout.MainSuggestions, parent, false);
                var mainLogSuggestionsListViewHolder = new MainLogSuggestionsListViewHolder(suggestionsView, SuggestionsViewModel);
                mainLogSuggestionsStopwatch.Stop();
                return mainLogSuggestionsListViewHolder;
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
            if (position == 0)
                return SuggestionViewType;

            if (isRatingViewVisible && position == 1)
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
                ContinueButtonTappedSubject = continueTimeEntrySubject,
                ToggleGroupExpansionSubject = toggleGroupExpansionSubject
            };

            mainLogCellStopwatch.Stop();
            return mainLogCellViewHolder;
        }

        protected override IMainLogKey IdFor(LogItemViewModel item)
            => item.Identity;

        protected override IMainLogKey IdForSection(DaySummaryViewModel section)
            => section.Identity;

        protected override TimeEntryViewData Wrap(LogItemViewModel item)
            => new TimeEntryViewData(item);

        protected override DaySummaryViewModel Wrap(DaySummaryViewModel section)
            => section;

        protected override bool AreItemContentsTheSame(LogItemViewModel item1, LogItemViewModel item2)
            => item1 == item2;

        protected override bool AreSectionsRepresentationsTheSame(
            DaySummaryViewModel oneHeader,
            DaySummaryViewModel otherHeader,
            IReadOnlyList<LogItemViewModel> one,
            IReadOnlyList<LogItemViewModel> other)
        {
            return oneHeader.Title == otherHeader.Title && one.ContainsExactlyAll(other);
        }
    }
}
