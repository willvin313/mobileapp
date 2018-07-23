using System;
using System.Reactive.Disposables;
using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Commands;
using MvvmCross.Platforms.Ios.Binding;
using MvvmCross.Platforms.Ios.Binding.Views;
using MvvmCross.Plugin.Color.Platforms.Ios;
using MvvmCross.Plugin.Visibility;
using Toggl.Daneel.Combiners;
using Toggl.Daneel.Extensions;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using UIKit;

namespace Toggl.Daneel.Cells.Reports
{
    public sealed partial class ReportsCalendarViewCell : BaseCollectionViewCell<CalendarDayViewModel>
    {
        private const int cornerRadius = 16;

        public static readonly NSString Key = new NSString(nameof(ReportsCalendarViewCell));
        public static readonly UINib Nib;

        static ReportsCalendarViewCell()
        {
            Nib = UINib.FromName(nameof(ReportsCalendarViewCell), NSBundle.MainBundle);
        }

        public Action<CalendarDayViewModel> CellTapped { get; set; }

        public ReportsCalendarViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            prepareViews();

            AddGestureRecognizer(new UITapGestureRecognizer(
                () => CellTapped?.Invoke(Item)
            ));
        }

        protected override void UpdateView()
        {
            //Text
            Text.Text = Item.Day.ToString();

            //Color
            Text.TextColor = calculateTextColor(Item.Selected, Item.IsInCurrentMonth);
            BackgroundView.BackgroundColor = calculateBackgroundColor(Item.Selected);

            //Rounding 
            BackgroundView.RoundRight = Item.IsEndOfSelectedPeriod;
            BackgroundView.RoundLeft = Item.IsStartOfSelectedPeriod;

            //Today
            TodayBackgroundView.Hidden = !Item.IsToday;
        }

        private void prepareViews()
        {
            //Background view
            BackgroundView.CornerRadius = cornerRadius;

            //Today background indicator
            TodayBackgroundView.CornerRadius = cornerRadius;
            TodayBackgroundView.RoundLeft = true;
            TodayBackgroundView.RoundRight = true;
            TodayBackgroundView.BackgroundColor = Color.Calendar.Today.ToNativeColor();
        }

        private UIColor calculateTextColor(bool selected, bool isInCurrentMonth)
        {
            if (selected)
                return Color.Calendar.CellTextColorSelected.ToNativeColor();

            return isInCurrentMonth
                ? Color.Calendar.CellTextColorInCurrentMonth.ToNativeColor()
                : Color.Calendar.CellTextColorOutOfCurrentMonth.ToNativeColor();
        }

        private UIColor calculateBackgroundColor(bool selected)
            => selected
            ? Color.Calendar.SelectedDayBackgoundColor.ToNativeColor()
            : Color.Common.Transparent.ToNativeColor();
    }
}