﻿using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.UI.Extensions;
using Toggl.Foundation.UI.Helper;
using Toggl.Foundation.UI.Parameters;
using Toggl.Foundation.UI.Transformations;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using static Toggl.Foundation.Helper.Constants;
using static Toggl.Foundation.UI.Helper.TemporalInconsistency;

namespace Toggl.Foundation.UI.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class EditDurationViewModel : MvxViewModel<EditDurationParameters, DurationParameter>
    {
        private readonly ITimeService timeService;
        private readonly IMvxNavigationService navigationService;
        private readonly IAnalyticsService analyticsService;

        private IDisposable runningTimeEntryDisposable;
        private DurationParameter defaultResult;
        private EditDurationEvent analyticsEvent;

        private ISubject<TemporalInconsistency> temporalInconsistencies = new Subject<TemporalInconsistency>();
        private BehaviorSubject<DateTimeOffset> startTime = new BehaviorSubject<DateTimeOffset>(default(DateTimeOffset));
        private BehaviorSubject<DateTimeOffset> stopTime = new BehaviorSubject<DateTimeOffset>(default(DateTimeOffset));
        private BehaviorSubject<EditMode> editMode = new BehaviorSubject<EditMode>(EditMode.None);
        private BehaviorSubject<bool> isRunning = new BehaviorSubject<bool>(false);

        private BehaviorSubject<DateTimeOffset> minimumDateTime = new BehaviorSubject<DateTimeOffset>(default(DateTimeOffset));
        private BehaviorSubject<DateTimeOffset> maximumDateTime = new BehaviorSubject<DateTimeOffset>(default(DateTimeOffset));

        public UIAction Save { get; }
        public UIAction Close { get; }
        public UIAction EditStartTime { get; }
        public UIAction EditStopTime { get; }
        public UIAction StopEditingTime { get; }
        public UIAction StopTimeEntry { get; }
        public InputAction<DateTimeOffset> ChangeStartTime { get; }
        public InputAction<DateTimeOffset> ChangeStopTime { get; }
        public InputAction<DateTimeOffset> ChangeActiveTime { get; }
        public InputAction<TimeSpan> ChangeDuration { get; }

        public IObservable<DateTimeOffset> StartTime { get; }
        public IObservable<DateTimeOffset> StopTime { get; }
        public IObservable<TimeSpan> Duration { get; }
        public IObservable<bool> IsEditingTime { get; }
        public IObservable<bool> IsEditingStartTime { get; }
        public IObservable<bool> IsEditingStopTime { get; }

        public IObservable<string> StartDateString { get; }
        public IObservable<string> StartTimeString { get; }
        public IObservable<string> StopDateString { get; }
        public IObservable<string> StopTimeString { get; }
        public IObservable<string> DurationString { get; }
        public IObservable<TimeFormat> TimeFormat { get; }
        public IObservable<bool> IsRunning { get; }

        public IObservable<DateTimeOffset> MinimumDateTime { get; }
        public IObservable<DateTimeOffset> MaximumDateTime { get; }
        public IObservable<TemporalInconsistency> TemporalInconsistencies => temporalInconsistencies.AsObservable();

        public IObservable<DateTimeOffset> MinimumStartTime { get; }
        public IObservable<DateTimeOffset> MaximumStartTime { get; }
        public IObservable<DateTimeOffset> MinimumStopTime { get; }
        public IObservable<DateTimeOffset> MaximumStopTime { get; }

        public bool IsDurationInitiallyFocused { get; private set; }

        public EditDurationViewModel(IMvxNavigationService navigationService, ITimeService timeService, ITogglDataSource dataSource, IAnalyticsService analyticsService, IRxActionFactory rxActionFactory, ISchedulerProvider schedulerProvider)
        {
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));

            this.timeService = timeService;
            this.navigationService = navigationService;
            this.analyticsService = analyticsService;

            Save = rxActionFactory.FromAsync(save);
            Close = rxActionFactory.FromAsync(close);
            EditStartTime = rxActionFactory.FromAction(editStartTime);
            EditStopTime = rxActionFactory.FromAction(editStopTime);
            StopEditingTime = rxActionFactory.FromAction(stopEditingTime);
            ChangeStartTime = rxActionFactory.FromAction<DateTimeOffset>(updateStartTime);
            ChangeStopTime = rxActionFactory.FromAction<DateTimeOffset>(updateStopTime);
            ChangeActiveTime = rxActionFactory.FromAction<DateTimeOffset>(changeActiveTime);
            ChangeDuration = rxActionFactory.FromAction<TimeSpan>(changeDuration);
            StopTimeEntry = rxActionFactory.FromAction(stopTimeEntry);

            var start = startTime.Where(v => v != default(DateTimeOffset));
            var stop = stopTime.Where(v => v != default(DateTimeOffset));
            var duration = Observable.CombineLatest(start, stop, (startValue, stopValue) => stopValue - startValue);

            StartTime = start.AsDriver(schedulerProvider);
            StopTime = stop.AsDriver(schedulerProvider);
            Duration = duration.AsDriver(schedulerProvider);

            IsEditingTime = editMode.Select(v => v != EditMode.None).AsDriver(schedulerProvider);
            IsEditingStartTime = editMode.Select(v => v == EditMode.StartTime).AsDriver(schedulerProvider);
            IsEditingStopTime = editMode.Select(v => v == EditMode.EndTime).AsDriver(schedulerProvider);

            var preferences = dataSource.Preferences.Current.ShareReplay();
            var dateFormat = preferences.Select(p => p.DateFormat);
            var timeFormat = preferences.Select(p => p.TimeOfDayFormat);
            var durationFormat = preferences.Select(p => p.DurationFormat);

            StartDateString = Observable.CombineLatest(start, dateFormat, toFormattedString)
                .AsDriver(schedulerProvider);
            StartTimeString = Observable.CombineLatest(start, timeFormat, toFormattedString)
                .AsDriver(schedulerProvider);
            StopDateString = Observable.CombineLatest(stop, dateFormat, toFormattedString)
                .AsDriver(schedulerProvider);
            StopTimeString = Observable.CombineLatest(stop, timeFormat, toFormattedString)
                .AsDriver(schedulerProvider);
            DurationString = Observable.CombineLatest(duration, durationFormat, toFormattedString)
                .AsDriver(schedulerProvider);
            TimeFormat = timeFormat.AsDriver(schedulerProvider);

            IsRunning = isRunning.AsDriver(schedulerProvider);

            MinimumDateTime = minimumDateTime.AsDriver(schedulerProvider);
            MaximumDateTime = maximumDateTime.AsDriver(schedulerProvider);

            MinimumStartTime = stopTime.Select(v => v.AddHours(-MaxTimeEntryDurationInHours)).AsDriver(schedulerProvider);
            MaximumStartTime = stopTime.AsDriver(schedulerProvider);
            MinimumStopTime = startTime.AsDriver(schedulerProvider);
            MaximumStopTime = startTime.Select(v => v.AddHours(MaxTimeEntryDurationInHours)).AsDriver(schedulerProvider);
        }

        private void updateStopTime(DateTimeOffset stopTime)
        {
            this.stopTime.OnNext(stopTime.RoundDownToMinute());
        }

        private void updateStartTime(DateTimeOffset startTime)
        {
            this.startTime.OnNext(startTime.RoundDownToMinute());
        }

        public override void Prepare(EditDurationParameters parameter)
        {
            defaultResult = parameter.DurationParam;
            isRunning.OnNext(defaultResult.Duration.HasValue == false);

            analyticsEvent = new EditDurationEvent(isRunning.Value,
                parameter.IsStartingNewEntry
                    ? EditDurationEvent.NavigationOrigin.Start
                    : EditDurationEvent.NavigationOrigin.Edit);

            if (isRunning.Value)
            {
                runningTimeEntryDisposable = timeService.CurrentDateTimeObservable
                   .Subscribe(currentTime => stopTime.OnNext(currentTime));
            }

            var start = parameter.DurationParam.Start;
            var stop = parameter.DurationParam.Duration.HasValue
                ? start + parameter.DurationParam.Duration.Value
                : timeService.CurrentDateTime;

            startTime.OnNext(start);
            stopTime.OnNext(stop);

            minimumDateTime.OnNext(start);
            maximumDateTime.OnNext(stop);
            IsDurationInitiallyFocused = parameter.IsDurationInitiallyFocused;
        }

        public void TimeEditedWithSource(EditTimeSource source)
        {
            analyticsEvent = analyticsEvent.UpdateWith(source);
        }

        private Task close()
        {
            analyticsEvent = analyticsEvent.With(result: EditDurationEvent.Result.Cancel);
            analyticsService.Track(analyticsEvent);
            return navigationService.Close(this, defaultResult);
        }

        private Task save()
        {
            analyticsEvent = analyticsEvent.With(result: EditDurationEvent.Result.Save);
            analyticsService.Track(analyticsEvent);
            var duration = stopTime.Value - startTime.Value;
            var result = DurationParameter.WithStartAndDuration(startTime.Value, isRunning.Value ? (TimeSpan?)null : duration);
            return navigationService.Close(this, result);
        }

        private void stopTimeEntry()
        {
            runningTimeEntryDisposable?.Dispose();
            stopTime.OnNext(timeService.CurrentDateTime);
            isRunning.OnNext(false);
            analyticsEvent = analyticsEvent.With(stoppedRunningEntry: true);
        }

        private void editStartTime()
        {
            if (editMode.Value == EditMode.StartTime)
            {
                editMode.OnNext(EditMode.None);
            }
            else
            {
                minimumDateTime.OnNext(stopTime.Value.AddHours(-MaxTimeEntryDurationInHours));
                maximumDateTime.OnNext(stopTime.Value);

                editMode.OnNext(EditMode.StartTime);
            }
        }

        private void editStopTime()
        {
            if (editMode.Value == EditMode.EndTime)
            {
                editMode.OnNext(EditMode.None);
            }
            else
            {
                minimumDateTime.OnNext(startTime.Value);
                maximumDateTime.OnNext(startTime.Value.AddHours(MaxTimeEntryDurationInHours));

                editMode.OnNext(EditMode.EndTime);
            }
        }

        private void stopEditingTime()
        {
            if (editMode.Value == EditMode.None)
            {
                return;
            }

            editMode.OnNext(EditMode.None);
        }

        private void changeActiveTime(DateTimeOffset newTime)
        {
            detectInconsistencies(newTime);
            var valueInRange = newTime.Clamp(minimumDateTime.Value, maximumDateTime.Value);

            switch (editMode.Value)
            {
                case EditMode.StartTime:
                    startTime.OnNext(valueInRange);
                    break;

                case EditMode.EndTime:
                    stopTime.OnNext(valueInRange);
                    break;
            }
        }

        private void detectInconsistencies(DateTimeOffset newTime)
        {
            switch (editMode.Value)
            {
                case EditMode.StartTime:
                    if (newTime < minimumDateTime.Value)
                    {
                        temporalInconsistencies.OnNext(DurationTooLong);
                    }

                    if (newTime > maximumDateTime.Value)
                    {
                        temporalInconsistencies.OnNext(isRunning.Value
                            ? StartTimeAfterCurrentTime
                            : StartTimeAfterStopTime);
                    }

                    break;

                case EditMode.EndTime:
                    if (newTime < minimumDateTime.Value)
                    {
                        temporalInconsistencies.OnNext(StopTimeBeforeStartTime);
                    }

                    if (newTime > maximumDateTime.Value)
                    {
                        temporalInconsistencies.OnNext(DurationTooLong);
                    }

                    break;
            }
        }

        private void changeDuration(TimeSpan changedDuration)
        {
            if (isRunning.Value)
                startTime.OnNext(timeService.CurrentDateTime - changedDuration);

            stopTime.OnNext(startTime.Value + changedDuration);
        }

        private string toFormattedString(DateTimeOffset dateTimeOffset, TimeFormat timeFormat)
        {
            return DateTimeToFormattedString.Convert(dateTimeOffset, timeFormat.Format);
        }

        private string toFormattedString(DateTimeOffset dateTimeOffset, DateFormat dateFormat)
        {
            return DateTimeToFormattedString.Convert(dateTimeOffset, dateFormat.Short);
        }

        private string toFormattedString(TimeSpan timeSpan, DurationFormat format)
        {
            return timeSpan.ToFormattedString(format);
        }

        public override void ViewDestroy(bool viewFinishing)
        {
            base.ViewDestroy(viewFinishing);
            runningTimeEntryDisposable?.Dispose();
        }

        private enum EditMode
        {
            None,
            StartTime,
            EndTime
        }
    }
}
