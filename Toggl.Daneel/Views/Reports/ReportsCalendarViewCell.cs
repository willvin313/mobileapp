﻿using System;
using Foundation;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Foundation.UI.Helper;
using Toggl.Foundation.UI.Parameters;
using Toggl.Foundation.UI.ViewModels.ReportsCalendar;
using UIKit;

namespace Toggl.Daneel.Views
{
    public sealed partial class ReportsCalendarViewCell : ReactiveCollectionViewCell<ReportsCalendarDayViewModel>
    {
        private const int cornerRadius = 16;

        public static readonly NSString Key = new NSString(nameof(ReportsCalendarViewCell));
        public static readonly UINib Nib;

        private ReportsDateRangeParameter dateRange;

        static ReportsCalendarViewCell()
        {
            Nib = UINib.FromName(nameof(ReportsCalendarViewCell), NSBundle.MainBundle);
        }

        public ReportsCalendarViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();
            prepareViews();
        }

        private void prepareViews()
        {
            //Background view
            BackgroundView.CornerRadius = cornerRadius;

            //Today background indicator
            TodayBackgroundView.CornerRadius = cornerRadius;
            TodayBackgroundView.RoundLeft = true;
            TodayBackgroundView.RoundRight = true;
            TodayBackgroundView.BackgroundColor = Color.ReportsCalendar.Today.ToNativeColor();
        }

        private readonly UIColor otherMonthColor = Color.ReportsCalendar.CellTextColorOutOfCurrentMonth.ToNativeColor();
        private readonly UIColor thisMonthColor = Color.ReportsCalendar.CellTextColorInCurrentMonth.ToNativeColor();
        private readonly UIColor selectedColor = Color.ReportsCalendar.CellTextColorSelected.ToNativeColor();

        protected override void UpdateView()
        {
            Text.Text = Item.Day.ToString();

            if (Item.IsSelected(dateRange))
            {
                Text.TextColor = selectedColor;
            }
            else
            {
                Text.TextColor = Item.IsInCurrentMonth ? thisMonthColor : otherMonthColor;
            }

            BackgroundView.BackgroundColor = Item.IsSelected(dateRange)
                ? Color.ReportsCalendar.SelectedDayBackgoundColor.ToNativeColor()
                : Color.Common.Transparent.ToNativeColor();

            BackgroundView.RoundLeft = Item.IsStartOfSelectedPeriod(dateRange);
            BackgroundView.RoundRight = Item.IsEndOfSelectedPeriod(dateRange);
            TodayBackgroundView.Hidden = !Item.IsToday;
        }

        public void UpdateDateRange(ReportsDateRangeParameter dateRange)
        {
            this.dateRange = dateRange;
        }
    }
}
