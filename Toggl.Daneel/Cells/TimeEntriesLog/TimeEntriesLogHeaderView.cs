using System;
using System.Reactive.Disposables;
using Foundation;
using Toggl.Daneel.Cells;
using Toggl.Daneel.Extensions;
using UIKit;
using Toggl.Foundation.MvvmCross.ViewModels.TimeEntriesLog;

namespace Toggl.Daneel.Views
{
    public partial class TimeEntriesLogHeaderView : BaseTableHeaderFooterView<DaySummaryViewModel>
    {
        public static readonly string Identifier = "timeEntryLogHeaderCell";

        public static readonly NSString Key = new NSString(nameof(TimeEntriesLogHeaderView));
        public static readonly UINib Nib;

        static TimeEntriesLogHeaderView()
        {
            Nib = UINib.FromName(nameof(TimeEntriesLogHeaderView), NSBundle.MainBundle);
        }

        protected TimeEntriesLogHeaderView(IntPtr handle)
            : base(handle)
        {
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            ContentView.BackgroundColor = UIColor.White;
            DurationLabel.Font = DurationLabel.Font.GetMonospacedDigitFont();
        }

        protected override void UpdateView()
        {
            DateLabel.Text = Item.Title;
            DurationLabel.Text = Item.TotalTrackedTime;
        }
    }
}
