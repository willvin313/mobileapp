using Android.Graphics;
using Android.Views;
using MvvmCross.Plugin.Color.Platforms.Android;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Giskard.Views;
using TogglColors = Toggl.Foundation.MvvmCross.Helper.Color;

namespace Toggl.Giskard.ViewHolders
{
    public sealed class CalendarRecyclerViewHolder : BaseRecyclerViewHolder<CalendarDayViewModel>
    {
        private CalendarDayView calendarDayView;

        public CalendarRecyclerViewHolder(View itemView)
            : base(itemView)
        {
        }

        protected override void InitializeViews()
        {
            calendarDayView = ItemView.FindViewById<CalendarDayView>(Resource.Id.ReportsCalendarDayView);
        }

        protected override void UpdateView()
        {
            calendarDayView.IsSelected = Item.Selected;
            calendarDayView.IsToday = Item.IsToday;
            calendarDayView.Text = Item.Day.ToString();
            calendarDayView.SetTextColor(calculateTextColor(Item.IsInCurrentMonth));
            calendarDayView.RoundLeft = Item.IsStartOfSelectedPeriod;
            calendarDayView.RoundRight = Item.IsEndOfSelectedPeriod;
            calendarDayView.PostInvalidate();
        }

        private Color calculateTextColor(bool isInCurrentMonth)
            => isInCurrentMonth
                ? Color.White
                : TogglColors.Calendar.CellTextColorOutOfCurrentMonth.ToNativeColor();
    }
}