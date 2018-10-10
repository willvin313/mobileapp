using System;
using Foundation;
using Toggl.Daneel.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels.Selectable;
using UIKit;

namespace Toggl.Daneel.Cells.Calendar
{
    public sealed partial class SelectableUserCalendarViewCell
        : BaseTableViewCell<SelectableUserCalendarViewModel>
    {
        public static readonly NSString Key = new NSString(nameof(SelectableUserCalendarViewCell));
        public static readonly UINib Nib;

        static SelectableUserCalendarViewCell()
        {
            Nib = UINib.FromName(nameof(SelectableUserCalendarViewCell), NSBundle.MainBundle);
        }

        protected SelectableUserCalendarViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            //This way the tap "goes through" the UISwitch
            //and we only have to handle the tap event on the whole cell.
            IsSelectedSwitch.UserInteractionEnabled = false;
            FadeView.FadeRight = true;
        }

        protected override void UpdateView()
        {
            UpdateView(false);
        }

        public void UpdateView(bool animate)
        {
            CalendarNameLabel.Text = Item.Name;
            IsSelectedSwitch.SetState(Item.Selected, animate);
        }
    }
}
