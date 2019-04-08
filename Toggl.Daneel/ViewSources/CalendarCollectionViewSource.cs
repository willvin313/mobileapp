﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using CoreGraphics;
using Foundation;
using MvvmCross.Platforms.Ios.Binding.Views;
using Toggl.Daneel.Cells.Calendar;
using Toggl.Daneel.Views.Calendar;
using Toggl.Foundation;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Calendar;
using Toggl.Foundation.UI.Calendar;
using Toggl.Foundation.UI.Collections;
using Toggl.Foundation.UI.Extensions;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using UIKit;
using FoundationResources = Toggl.Foundation.Resources;

namespace Toggl.Daneel.ViewSources
{
    public sealed class CalendarCollectionViewSource : MvxCollectionViewSource, ICalendarCollectionViewLayoutDataSource
    {
        private static readonly string twelveHoursFormat = Resources.TwelveHoursFormat;
        private static readonly string twentyFourHoursFormat = Resources.TwentyFourHoursFormat;
        private static readonly string editingTwelveHoursFormat = Resources.EditingTwelveHoursFormat;
        private static readonly string editingTwentyFourHoursFormat = Resources.EditingTwentyFourHoursFormat;

        private readonly string itemReuseIdentifier = nameof(CalendarItemView);
        private readonly string hourReuseIdentifier = nameof(HourSupplementaryView);
        private readonly string editingHourReuseIdentifier = nameof(HourSupplementaryView);
        private readonly string currentTimeReuseIdentifier = nameof(CurrentTimeSupplementaryView);

        private readonly ITimeService timeService;
        private readonly IObservable<TimeFormat> timeOfDayFormatObservable;
        private readonly ObservableGroupedOrderedCollection<CalendarItem> collection;

        private IList<CalendarItem> calendarItems;
        private IList<CalendarItemLayoutAttributes> layoutAttributes;
        private TimeFormat timeOfDayFormat = TimeFormat.TwelveHoursFormat;
        private DateTime date;
        private NSIndexPath editingItemIndexPath;
        private NSIndexPath runningTimeEntryIndexPath;

        private readonly CompositeDisposable disposeBag = new CompositeDisposable();
        private readonly ISubject<CalendarItem> itemTappedSubject = new Subject<CalendarItem>();

        private CalendarCollectionViewLayout layout => CollectionView.CollectionViewLayout as CalendarCollectionViewLayout;
        private CalendarLayoutCalculator layoutCalculator;

        public bool IsEditing { get; private set; }

        public IObservable<CalendarItem> ItemTapped => itemTappedSubject.AsObservable();

        public CalendarCollectionViewSource(
            ITimeService timeService,
            UICollectionView collectionView,
            IObservable<TimeFormat> timeOfDayFormat,
            ObservableGroupedOrderedCollection<CalendarItem> collection)
            : base(collectionView)
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(timeOfDayFormat, nameof(timeOfDayFormat));
            Ensure.Argument.IsNotNull(collection, nameof(collection));
            this.timeService = timeService;
            this.timeOfDayFormatObservable = timeOfDayFormat;
            this.collection = collection;

            layoutCalculator = new CalendarLayoutCalculator(timeService);
            calendarItems = new List<CalendarItem>();
            layoutAttributes = new List<CalendarItemLayoutAttributes>();

            registerCells();

            timeOfDayFormat
                .Subscribe(timeOfDayFormatChanged)
                .DisposedBy(disposeBag);

            collection
                .CollectionChange
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(_ => onCollectionChanges())
                .DisposedBy(disposeBag);

            timeService
                .MidnightObservable
                .Subscribe(dateChanged)
                .DisposedBy(disposeBag);

            timeService
                .CurrentDateTimeObservable
                .DistinctUntilChanged(offset => offset.Minute)
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(_ => updateLayoutAttributesIfNeeded())
                .DisposedBy(disposeBag);

            onCollectionChanges();
        }

        protected override UICollectionViewCell GetOrCreateCellFor(UICollectionView collectionView, NSIndexPath indexPath, object item)
        {
            var cell = collectionView.DequeueReusableCell(itemReuseIdentifier, indexPath) as CalendarItemView;
            cell.Layout = layout;
            cell.Item = calendarItems[(int)indexPath.Item];
            cell.IsEditing = IsEditing && indexPath == editingItemIndexPath;
            return cell;
        }

        public override nint GetItemsCount(UICollectionView collectionView, nint section)
            => calendarItems.Count;

        public override void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath)
        {
            var item = calendarItems[indexPath.Row];
            itemTappedSubject.OnNext(item);
        }

        public override UICollectionReusableView GetViewForSupplementaryElement(UICollectionView collectionView, NSString elementKind, NSIndexPath indexPath)
        {
            if (elementKind == CalendarCollectionViewLayout.HourSupplementaryViewKind)
            {
                var reusableView = collectionView.DequeueReusableSupplementaryView(elementKind, hourReuseIdentifier, indexPath) as HourSupplementaryView;
                var hour = date.AddHours((int)indexPath.Item);
                reusableView.SetLabel(hour.ToString(supplementaryHourFormat(), CultureInfo.InvariantCulture));
                return reusableView;
            }
            else if (elementKind == CalendarCollectionViewLayout.EditingHourSupplementaryViewKind)
            {
                var reusableView = collectionView.DequeueReusableSupplementaryView(elementKind, editingHourReuseIdentifier, indexPath) as EditingHourSupplementaryView;
                var attrs = layoutAttributes[(int)editingItemIndexPath.Item];
                var hour = (int)indexPath.Item == 0 ? attrs.StartTime.ToLocalTime() : attrs.EndTime.ToLocalTime();
                reusableView.SetLabel(hour.ToString(editingHourFormat(), CultureInfo.InvariantCulture));
                return reusableView;
            }

            return collectionView.DequeueReusableSupplementaryView(elementKind, currentTimeReuseIdentifier, indexPath);
        }

        public IEnumerable<NSIndexPath> IndexPathsOfCalendarItemsBetweenHours(int minHour, int maxHour)
        {
            if (calendarItems.None())
            {
                return Enumerable.Empty<NSIndexPath>();
            }

            var now = timeService.CurrentDateTime;
            var indices = calendarItems
                .Select((value, index) => new { value, index })
                .Where(t => t.value.StartTime.ToLocalTime().Hour >= minHour || t.value.EndTime(now).ToLocalTime().Hour <= maxHour)
                .Select(t => t.index);

            return indices.Select(index => NSIndexPath.FromItemSection(index, 0));
        }

        public CalendarItemLayoutAttributes LayoutAttributesForItemAtIndexPath(NSIndexPath indexPath)
            => layoutAttributes[(int)indexPath.Item];

        public NSIndexPath IndexPathForEditingItem()
            => editingItemIndexPath;

        public NSIndexPath IndexPathForRunningTimeEntry()
            => runningTimeEntryIndexPath;

        public CalendarItem? CalendarItemAtPoint(CGPoint point)
        {
            var indexPath = CollectionView.IndexPathForItemAtPoint(point);
            if (indexPath != null && indexPath.Item < calendarItems.Count)
            {
                return calendarItems[(int)indexPath.Item];
            }
            return null;
        }

        public List<DateTimeOffset> AllItemsStartAndEndTime()
        {
            var startTimes = calendarItems.Select(item => item.StartTime).Distinct();
            var endTimes = calendarItems.Where(item => item.EndTime.HasValue).Select(item => (DateTimeOffset)item.EndTime).Distinct();
            return startTimes.Concat(endTimes).ToList();
        }

        public void StartEditing()
        {
            IsEditing = true;
            layout.IsEditing = true;
        }

        public void StartEditing(NSIndexPath indexPath)
        {
            IsEditing = true;
            editingItemIndexPath = indexPath;
            layout.IsEditing = true;
            CollectionView.ReloadItems(new NSIndexPath[] { indexPath });
        }

        public void StopEditing()
        {
            IsEditing = false;
            layout.IsEditing = false;
            layoutAttributes = calculateLayoutAttributes();
            layout.InvalidateLayoutForVisibleItems();
        }

        public NSIndexPath InsertItemView(DateTimeOffset startTime, TimeSpan duration)
        {
            if (!IsEditing)
                throw new InvalidOperationException("Set IsEditing before calling insert/update/remove");

            editingItemIndexPath = insertCalendarItem(startTime, duration);
            CollectionView.InsertItems(new NSIndexPath[] { editingItemIndexPath });
            return editingItemIndexPath;
        }

        public NSIndexPath UpdateItemView(NSIndexPath indexPath, DateTimeOffset startTime, TimeSpan duration)
        {
            if (!IsEditing)
                throw new InvalidOperationException("Set IsEditing before calling insert/update/remove");

            editingItemIndexPath = updateCalendarItem(indexPath, startTime, duration);

            updateEditingHours();
            layout.InvalidateLayoutForVisibleItems();

            return editingItemIndexPath;
        }

        public void RemoveItemView(NSIndexPath indexPath)
        {
            if (!IsEditing)
                throw new InvalidOperationException("Set IsEditing before calling insert/update/remove");

            removeCalendarItem(indexPath);
            CollectionView.DeleteItems(new NSIndexPath[] { indexPath });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            disposeBag.Dispose();
        }

        private void timeOfDayFormatChanged(TimeFormat timeFormat)
        {
            timeOfDayFormat = timeFormat;
            CollectionView.ReloadData();
        }

        private void onCollectionChanges()
        {
            calendarItems = collection.IsEmpty ? new List<CalendarItem>() : collection[0].ToList();
            layoutAttributes = calculateLayoutAttributes();

            var index = calendarItems.IndexOf(item => item.Duration == null);
            runningTimeEntryIndexPath = index >= 0 ? NSIndexPath.FromItemSection(index, 0) : null;

            CollectionView.ReloadData();
        }

        private void dateChanged(DateTimeOffset dateTimeOffset)
        {
            this.date = dateTimeOffset.ToLocalTime().Date;
        }

        private void updateLayoutAttributesIfNeeded()
        {
            if (runningTimeEntryIndexPath == null)
                return;

            layoutAttributes = calculateLayoutAttributes();
            layout.InvalidateLayout();
        }

        private void registerCells()
        {
            CollectionView.RegisterNibForCell(CalendarItemView.Nib, itemReuseIdentifier);

            CollectionView.RegisterNibForSupplementaryView(HourSupplementaryView.Nib,
                                                           CalendarCollectionViewLayout.HourSupplementaryViewKind,
                                                           new NSString(hourReuseIdentifier));

            CollectionView.RegisterNibForSupplementaryView(EditingHourSupplementaryView.Nib,
                                                           CalendarCollectionViewLayout.EditingHourSupplementaryViewKind,
                                                           new NSString(editingHourReuseIdentifier));

            CollectionView.RegisterNibForSupplementaryView(CurrentTimeSupplementaryView.Nib,
                                                           CalendarCollectionViewLayout.CurrentTimeSupplementaryViewKind,
                                                           new NSString(currentTimeReuseIdentifier));
        }

        private IList<CalendarItemLayoutAttributes> calculateLayoutAttributes()
            => layoutCalculator.CalculateLayoutAttributes(calendarItems);

        private NSIndexPath insertCalendarItem(DateTimeOffset startTime, TimeSpan duration)
        {
            var calendarItem = new CalendarItem("", CalendarItemSource.TimeEntry, startTime, duration, FoundationResources.NewTimeEntry, CalendarIconKind.None);

            calendarItems.Add(calendarItem);
            var position = calendarItems.Count - 1;

            layoutAttributes = calculateLayoutAttributes();

            var indexPath = NSIndexPath.FromItemSection(position, 0);
            return indexPath;
        }

        private NSIndexPath updateCalendarItem(NSIndexPath indexPath, DateTimeOffset startTime, TimeSpan? duration)
        {
            var position = (int)indexPath.Item;

            calendarItems[position] = calendarItems[position]
                .WithStartTime(startTime)
                .WithDuration(duration);

            layoutAttributes = calculateLayoutAttributes();

            var updatedIndexPath = NSIndexPath.FromItemSection(position, 0);
            return updatedIndexPath;
        }

        private void removeCalendarItem(NSIndexPath indexPath)
        {
            calendarItems.RemoveAt((int)indexPath.Item);
            layoutAttributes = calculateLayoutAttributes();
        }

        private void updateEditingHours()
        {
            var startEditingHour = CollectionView
                .GetSupplementaryView(CalendarCollectionViewLayout.EditingHourSupplementaryViewKind, NSIndexPath.FromItemSection(0, 0)) as EditingHourSupplementaryView;
            var endEditingHour = CollectionView
                .GetSupplementaryView(CalendarCollectionViewLayout.EditingHourSupplementaryViewKind, NSIndexPath.FromItemSection(1, 0)) as EditingHourSupplementaryView;

            var attrs = layoutAttributes[(int)editingItemIndexPath.Item];
            if (startEditingHour != null)
            {
                var hour = attrs.StartTime.ToLocalTime();
                startEditingHour.SetLabel(hour.ToString(editingHourFormat(), CultureInfo.InvariantCulture));
            }
            if (endEditingHour != null)
            {
                var hour = attrs.EndTime.ToLocalTime();
                endEditingHour.SetLabel(hour.ToString(editingHourFormat(), CultureInfo.InvariantCulture));
            }
        }

        private string supplementaryHourFormat()
            => timeOfDayFormat.IsTwentyFourHoursFormat ? twentyFourHoursFormat : twelveHoursFormat;

        private string editingHourFormat()
            => timeOfDayFormat.IsTwentyFourHoursFormat ? editingTwentyFourHoursFormat : editingTwelveHoursFormat;
    }
}
